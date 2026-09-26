// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Splat.DependencyInjection.SourceGenerator.Tests;

/// <summary>Tests the files every project gets, and that the stored snapshots can be checked out on Windows.</summary>
public sealed class SnapshotTests
{
    /// <summary>
    /// The deepest checkout root a Windows CI runner uses: <c>D:\a\{repository}\{repository}\</c>. A path under it must
    /// stay within the Windows path limit, or the checkout and the received files fail on that runner only.
    /// </summary>
    private const string WindowsCheckoutRoot = @"D:\a\Splat.DI.SourceGenerator\Splat.DI.SourceGenerator\";

    /// <summary>The longest path Windows accepts without long-path support.</summary>
    private const int WindowsPathLimit = 256;

    /// <summary>The marker source and the embedded attribute are stored once, as every project gets the same files.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task WritesFixedSources()
    {
        var run = TestHelper.Run("namespace T { public static class C { public static void M() => Splat.SplatRegistrations.SetupIOC(); } }");

        return GeneratorSnapshot.VerifyAsync(run.Driver, $"{nameof(SnapshotTests)}.{nameof(WritesFixedSources)}", includeFixedSources: true);
    }

    /// <summary>Every snapshot, and the received file written beside it, fits the Windows path limit on a CI runner.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SnapshotPathsFitWindowsLimit()
    {
        var directory = GeneratorSnapshot.SnapshotDirectory;
        var repositoryRelative = directory[(directory.LastIndexOf($"{Path.DirectorySeparatorChar}src{Path.DirectorySeparatorChar}", StringComparison.Ordinal) + 1)..];
        var tooLong = new List<string>();
        foreach (var snapshot in Directory.EnumerateFiles(directory, "*.verified.*"))
        {
            var windowsPath = WindowsCheckoutRoot + Path.Combine(repositoryRelative, Path.GetFileName(snapshot));
            if (windowsPath.Length >= WindowsPathLimit)
            {
                tooLong.Add($"{windowsPath.Length}: {Path.GetFileName(snapshot)}");
            }
        }

        await Assert.That(tooLong).IsEmpty();
    }
}
