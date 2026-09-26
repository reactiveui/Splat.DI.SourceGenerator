// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

using Microsoft.CodeAnalysis;

namespace Splat.DependencyInjection.SourceGenerator.Tests;

/// <summary>Compares a generator run's output with the snapshots stored in the test project.</summary>
/// <remarks>
/// <para>
/// A generated file's snapshot is named <c>{type}.{method}#{hint name}.verified.cs</c>, and the generator's diagnostics
/// are stored as <c>{type}.{method}#Diagnostics.verified.txt</c> when there are any. An output that differs from its
/// snapshot, or has none, is written beside it as <c>.received</c> and fails the test, as does a snapshot the run no
/// longer produces.
/// </para>
/// <para>
/// To accept new or changed snapshots, run the tests with the <c>ACCEPT_SNAPSHOTS=1</c> environment variable, then
/// run them again without it.
/// </para>
/// </remarks>
internal static class GeneratorSnapshot
{
    /// <summary>The suffix of a stored source snapshot.</summary>
    internal const string VerifiedSourceSuffix = ".verified.cs";

    /// <summary>The suffix of a stored diagnostics snapshot.</summary>
    internal const string VerifiedDiagnosticsSuffix = ".verified.txt";

    /// <summary>The environment variable that makes a run write its output over the snapshots.</summary>
    private const string AcceptVariable = "ACCEPT_SNAPSHOTS";

    /// <summary>The assembly metadata key the test project records its snapshot directory under.</summary>
    private const string DirectoryMetadataKey = "GeneratorSnapshotDirectory";

    /// <summary>The name that stands in for a hint name on the diagnostics snapshot.</summary>
    private const string DiagnosticsName = "Diagnostics";

    /// <summary>The suffix of the source output written beside a snapshot it does not match.</summary>
    private const string ReceivedSourceSuffix = ".received.cs";

    /// <summary>The suffix of the diagnostics written beside a snapshot they do not match.</summary>
    private const string ReceivedDiagnosticsSuffix = ".received.txt";

    /// <summary>Gets the directory holding the snapshots.</summary>
    internal static string SnapshotDirectory { get; } = ReadSnapshotDirectory();

    /// <summary>Asserts that the generated files and diagnostics match the stored snapshots.</summary>
    /// <param name="driver">The driver after the generator has run.</param>
    /// <param name="name">The snapshot name before the hint name, <c>{type}.{method}</c>.</param>
    /// <param name="includeFixedSources">
    /// Whether to include the marker source and the embedded attribute, which are the same for every project and so
    /// are stored once rather than by every test.
    /// </param>
    /// <returns>A task that completes once every file has been compared.</returns>
    internal static async Task VerifyAsync(GeneratorDriver driver, string name, bool includeFixedSources)
    {
        ArgumentNullException.ThrowIfNull(driver);

        var prefix = $"{name}#";
        var accept = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(AcceptVariable));
        var produced = new HashSet<string>(StringComparer.Ordinal);
        var failures = new List<string>();

        foreach (var result in driver.GetRunResult().Results)
        {
            foreach (var source in result.GeneratedSources)
            {
                if (!includeFixedSources && IsFixedSource(source.HintName))
                {
                    continue;
                }

                var fileName = prefix + Path.GetFileNameWithoutExtension(source.HintName) + VerifiedSourceSuffix;
                _ = produced.Add(fileName);
                var output = $"//HintName: {source.HintName}\n{Normalize(source.SourceText.ToString())}";
                await CompareAsync(fileName, ReceivedSourceSuffix, output, accept, failures).ConfigureAwait(false);
            }

            if (!result.Diagnostics.IsEmpty)
            {
                var fileName = prefix + DiagnosticsName + VerifiedDiagnosticsSuffix;
                _ = produced.Add(fileName);
                await CompareAsync(fileName, ReceivedDiagnosticsSuffix, FormatDiagnostics(result.Diagnostics), accept, failures).ConfigureAwait(false);
            }
        }

        RemoveStale(prefix, produced, accept, failures);
        await Assert.That(failures).IsEmpty();
    }

    /// <summary>Renders diagnostics one per line, as the diagnostics snapshot stores them.</summary>
    /// <param name="diagnostics">The diagnostics, in the order reported.</param>
    /// <returns>The rendered diagnostics.</returns>
    internal static string FormatDiagnostics(ImmutableArray<Diagnostic> diagnostics)
    {
        var text = new StringBuilder();
        foreach (var diagnostic in diagnostics)
        {
            var span = diagnostic.Location.GetLineSpan();
            _ = text.Append(diagnostic.Id).Append(' ').Append(diagnostic.Severity).Append(' ')
                .Append(span.Path).Append(span.Span.ToString()).Append(": ")
                .Append(diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture)).Append('\n');
        }

        return text.ToString();
    }

    /// <summary>Tests whether a generated file is one of the fixed files every project gets.</summary>
    /// <param name="hintName">The file's hint name.</param>
    /// <returns><see langword="true"/> for the marker source and the embedded attribute.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsFixedSource(string hintName) => hintName != Constants.RegistrationFileName;

    /// <summary>Settles one output against its snapshot, writing the received or accepted output as needed.</summary>
    /// <param name="fileName">The snapshot's file name.</param>
    /// <param name="receivedSuffix">The suffix the received output is written under.</param>
    /// <param name="output">The output, in snapshot form.</param>
    /// <param name="accept">Whether the output replaces the snapshot.</param>
    /// <param name="failures">The failures so far, added to when the output differs.</param>
    /// <returns>A task that completes once the output is settled.</returns>
    private static async Task CompareAsync(string fileName, string receivedSuffix, string output, bool accept, List<string> failures)
    {
        var verifiedPath = Path.Combine(SnapshotDirectory, fileName);
        var receivedPath = string.Concat(verifiedPath.AsSpan(0, verifiedPath.LastIndexOf(".verified.", StringComparison.Ordinal)), receivedSuffix);

        // Compared before accepting, so a matching snapshot keeps its bytes rather than being rewritten.
        if (File.Exists(verifiedPath) && Normalize(await File.ReadAllTextAsync(verifiedPath).ConfigureAwait(false)) == output)
        {
            File.Delete(receivedPath);
            return;
        }

        if (accept)
        {
            await File.WriteAllTextAsync(verifiedPath, output).ConfigureAwait(false);
            File.Delete(receivedPath);
            return;
        }

        await File.WriteAllTextAsync(receivedPath, output).ConfigureAwait(false);
        failures.Add($"{fileName} does not match the output; see {Path.GetFileName(receivedPath)}");
    }

    /// <summary>Deletes or reports the snapshots under a name that the run no longer produced.</summary>
    /// <param name="prefix">The snapshot name before the hint name.</param>
    /// <param name="produced">The snapshot file names the run produced.</param>
    /// <param name="accept">Whether stale snapshots are deleted rather than reported.</param>
    /// <param name="failures">The failures so far, added to for each stale snapshot.</param>
    private static void RemoveStale(string prefix, HashSet<string> produced, bool accept, List<string> failures)
    {
        foreach (var snapshot in Directory.EnumerateFiles(SnapshotDirectory, $"{prefix}*.verified.*"))
        {
            var fileName = Path.GetFileName(snapshot);
            if (produced.Contains(fileName))
            {
                continue;
            }

            if (accept)
            {
                File.Delete(snapshot);
            }
            else
            {
                failures.Add($"{fileName} is no longer produced");
            }
        }
    }

    /// <summary>Reads the snapshot directory the test project recorded when it was built.</summary>
    /// <returns>The absolute path of the snapshot directory.</returns>
    /// <exception cref="InvalidOperationException">The test assembly records no snapshot directory.</exception>
    private static string ReadSnapshotDirectory()
    {
        // Recorded at build time: a CI build maps caller file paths to /_/, which does not exist on disk.
        foreach (var attribute in typeof(GeneratorSnapshot).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
        {
            if (attribute.Key == DirectoryMetadataKey && !string.IsNullOrEmpty(attribute.Value))
            {
                return attribute.Value;
            }
        }

        throw new InvalidOperationException($"The test assembly records no '{DirectoryMetadataKey}' assembly metadata.");
    }

    /// <summary>Normalises line endings so a snapshot compares the same on every checkout.</summary>
    /// <param name="text">The text to normalise.</param>
    /// <returns>The text with LF line endings.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string Normalize(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);
}
