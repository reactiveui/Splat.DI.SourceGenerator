// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

extern alias analyzer;

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Splat.DependencyInjection.SourceGenerator.Tests;

/// <summary>Runs the generator over test source and verifies what it produces.</summary>
/// <remarks>
/// Snapshots are stored beside this file. Every snapshot test lives in the same folder, and names its snapshots by
/// its own type and method, so the folder is the same whichever test calls.
/// </remarks>
public static class TestHelper
{
    /// <summary>The parse options every test tree is parsed with.</summary>
    public static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Latest);

    /// <summary>The framework and Splat references every test compilation gets.</summary>
    private static readonly MetadataReference[] DefaultReferences = CreateDefaultReferences();

    /// <summary>Creates a compilation from source with the framework and Splat referenced.</summary>
    /// <param name="source">The source code to compile.</param>
    /// <returns>A compilation ready for testing.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CSharpCompilation CreateCompilation(string source) => CreateCompilation([source]);

    /// <summary>Creates a compilation from several files with the framework and Splat referenced.</summary>
    /// <param name="sources">The files, which become <c>File0.cs</c>, <c>File1.cs</c> and so on.</param>
    /// <returns>A compilation ready for testing.</returns>
    public static CSharpCompilation CreateCompilation(IReadOnlyList<string> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        var trees = new SyntaxTree[sources.Count];
        for (var i = 0; i < trees.Length; i++)
        {
            trees[i] = CSharpSyntaxTree.ParseText(sources[i], ParseOptions, $"File{i}.cs");
        }

        return CSharpCompilation.Create("TestAssembly", trees, DefaultReferences, new(OutputKind.DynamicallyLinkedLibrary));
    }

    /// <summary>Creates a driver for the generator that tracks its pipeline steps.</summary>
    /// <returns>The driver.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GeneratorDriver CreateDriver() =>
        CSharpGeneratorDriver.Create(
            [new Generator().AsSourceGenerator()],
            parseOptions: ParseOptions,
            driverOptions: new(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

    /// <summary>Runs the generator over a compilation.</summary>
    /// <param name="compilation">The compilation.</param>
    /// <returns>The run: its driver, result and output compilation.</returns>
    public static GeneratorRun Run(Compilation compilation)
    {
        var driver = CreateDriver().RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
        return new(driver, driver.GetRunResult().Results[0], output);
    }

    /// <summary>Runs the generator over source.</summary>
    /// <param name="sources">The files.</param>
    /// <returns>The run: its driver, result and output compilation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GeneratorRun Run(params string[] sources) => Run(CreateCompilation(sources));

    /// <summary>Finds the first invocation in a tree whose text starts with a prefix.</summary>
    /// <param name="tree">The tree.</param>
    /// <param name="prefix">The start of the invocation's text.</param>
    /// <returns>The invocation.</returns>
    /// <exception cref="InvalidOperationException">No invocation starts with the prefix.</exception>
    public static async Task<InvocationExpressionSyntax> FindInvocationAsync(SyntaxTree tree, string prefix)
    {
        ArgumentNullException.ThrowIfNull(tree);

        var root = await tree.GetRootAsync().ConfigureAwait(false);
        foreach (var node in root.DescendantNodes())
        {
            if (node is InvocationExpressionSyntax invocation && invocation.ToString().StartsWith(prefix, StringComparison.Ordinal))
            {
                return invocation;
            }
        }

        throw new InvalidOperationException($"No invocation starts with {prefix}.");
    }

    /// <summary>
    /// Tests a scenario that is expected to fail: the compiler, the generator or an analyzer reports a problem.
    /// Verifies the generated output against a snapshot.
    /// </summary>
    /// <param name="source">The source code to compile and generate.</param>
    /// <param name="contractParameter">The contract parameter value for registration.</param>
    /// <param name="callerType">The type of the calling test class for snapshot organization.</param>
    /// <param name="memberName">The member name of the caller (automatically populated).</param>
    /// <returns>A task representing the asynchronous verification operation.</returns>
    public static async Task TestFail(string source, string contractParameter, Type callerType, [CallerMemberName] string memberName = "")
    {
        ArgumentNullException.ThrowIfNull(callerType);

        var run = Run(source);

        // The SPLATDI diagnostics for invalid types are reported by the analyzers rather than the generator, so run
        // them too, against the compilation with the generated members in it.
        var analyzerDiagnostics = await GetAnalyzerDiagnosticsAsync(run.Output).ConfigureAwait(false);

        if (!HasWarningOrError(run.Output.GetDiagnostics()) && !HasWarningOrError(run.Result.Diagnostics) && !HasWarningOrError(analyzerDiagnostics))
        {
            Assert.Fail("Expected the compiler, generator, or analyzer to produce diagnostics");
        }

        await RunVerify(memberName, callerType, run.Driver, contractParameter).ConfigureAwait(false);
    }

    /// <summary>Tests a scenario that is expected to succeed: the generated code compiles. Verifies it against a snapshot.</summary>
    /// <param name="source">The source code to compile and generate.</param>
    /// <param name="contractParameter">The contract parameter value for registration.</param>
    /// <param name="callerType">The type of the calling test class for snapshot organization.</param>
    /// <param name="memberName">The member name of the caller (automatically populated).</param>
    /// <returns>A task representing the asynchronous verification operation.</returns>
    public static Task TestPass(string source, string contractParameter, Type callerType, [CallerMemberName] string memberName = "")
    {
        ArgumentNullException.ThrowIfNull(callerType);
        return RunVerify(memberName, callerType, RunCompiling(source).Driver, contractParameter);
    }

    /// <summary>
    /// Tests a lazy singleton scenario that is expected to succeed: the generated code compiles. Verifies it against a
    /// snapshot.
    /// </summary>
    /// <param name="source">The source code to compile and generate.</param>
    /// <param name="contractParameter">The contract parameter value for registration.</param>
    /// <param name="mode">The lazy thread safety mode for the singleton.</param>
    /// <param name="callerType">The type of the calling test class for snapshot organization.</param>
    /// <param name="memberName">The member name of the caller (automatically populated).</param>
    /// <returns>A task representing the asynchronous verification operation.</returns>
    public static Task TestPass(string source, string contractParameter, LazyThreadSafetyMode mode, Type callerType, [CallerMemberName] string memberName = "")
    {
        ArgumentNullException.ThrowIfNull(callerType);
        return RunVerify(memberName, callerType, RunCompiling(source).Driver, contractParameter, mode);
    }

    /// <summary>Tests whether any diagnostic is a warning or an error.</summary>
    /// <param name="diagnostics">The diagnostics.</param>
    /// <returns><see langword="true"/> when one is.</returns>
    private static bool HasWarningOrError(ImmutableArray<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            if (diagnostic.Severity >= DiagnosticSeverity.Warning)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Runs the generator and fails the test when the output does not compile.</summary>
    /// <param name="source">The source code to compile and generate.</param>
    /// <returns>The run.</returns>
    private static GeneratorRun RunCompiling(string source)
    {
        var run = Run(source);
        var errors = run.Errors();
        if (!errors.IsEmpty)
        {
            Assert.Fail($"The generated code does not compile: {string.Join(Environment.NewLine, errors)}");
        }

        return run;
    }

    /// <summary>Runs snapshot verification on the generator driver output.</summary>
    /// <param name="callerMember">The member name of the caller.</param>
    /// <param name="type">The type of the calling test class for snapshot organization.</param>
    /// <param name="driver">The generator driver containing the output to verify.</param>
    /// <param name="parameters">Additional parameters to include in the snapshot file name.</param>
    /// <returns>A task representing the asynchronous verification operation.</returns>
    private static Task RunVerify(string callerMember, Type type, GeneratorDriver driver, params object[] parameters)
    {
        var parametersText = new StringBuilder();
        foreach (var parameter in parameters)
        {
            _ = (parametersText.Length == 0 ? parametersText : parametersText.Append('_')).Append(AbbreviateParameter(parameter));
        }

        return GeneratorSnapshot.VerifyAsync(
            driver,
            $"{AbbreviateTypeName(type.Name)}.{AbbreviateMethodName(callerMember)}_{parametersText}",
            includeFixedSources: false);
    }

    /// <summary>Abbreviates test class names to keep file names short.</summary>
    /// <param name="typeName">The test class name.</param>
    /// <returns>The abbreviation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string AbbreviateTypeName(string typeName) =>
        typeName switch
        {
            "RegisterLazySingletonTests" => "LS",
            "RegisterTests" => "R",
            _ => typeName,
        };

    /// <summary>Abbreviates method names to keep file names short.</summary>
    /// <param name="methodName">The test method name.</param>
    /// <returns>The abbreviation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string AbbreviateMethodName(string methodName) =>
        methodName
            .Replace("ConstructionAnd", "C", StringComparison.Ordinal)
            .Replace("Construction", "C", StringComparison.Ordinal)
            .Replace("Multiple", "M", StringComparison.Ordinal)
            .Replace("Property", "P", StringComparison.Ordinal)
            .Replace("Injection", "I", StringComparison.Ordinal)
            .Replace("Internal", "Int", StringComparison.Ordinal)
            .Replace("Setter", "Set", StringComparison.Ordinal)
            .Replace("WithLazyMode", "LM", StringComparison.Ordinal)
            .Replace("Parameter", "Pm", StringComparison.Ordinal)
            .Replace("Registered", "Reg", StringComparison.Ordinal)
            .Replace("Attribute", "Attr", StringComparison.Ordinal)
            .Replace("Without", "No", StringComparison.Ordinal)
            .Replace("NonPublic", "NP", StringComparison.Ordinal)
            .Replace("Fail", "F", StringComparison.Ordinal)
            .Replace("Pass", "P", StringComparison.Ordinal)
            .Replace("Lazy", "L", StringComparison.Ordinal)
            .Replace("Empty", "E", StringComparison.Ordinal)
            .Replace("Interface", "I", StringComparison.Ordinal)
            .Replace("Times", "x", StringComparison.Ordinal);

    /// <summary>Abbreviates parameter values to keep file names short.</summary>
    /// <param name="parameter">The parameter value.</param>
    /// <returns>The abbreviation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string AbbreviateParameter(object parameter) =>
        parameter switch
        {
            LazyThreadSafetyMode.None => "N",
            LazyThreadSafetyMode.PublicationOnly => "P",
            LazyThreadSafetyMode.ExecutionAndPublication => "EP",
            string text when string.IsNullOrWhiteSpace(text) => "contractParameter=",
            _ => parameter.ToString() ?? string.Empty,
        };

    /// <summary>Runs the Splat dependency injection analyzers against a compilation and returns their diagnostics.</summary>
    /// <param name="compilation">The compilation to analyze.</param>
    /// <returns>A task producing the analyzer diagnostics.</returns>
    private static Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync(Compilation compilation)
    {
        ImmutableArray<DiagnosticAnalyzer> analyzers =
        [
            new analyzer::Splat.DependencyInjection.Analyzer.Analyzers.ConstructorAnalyzer(),
            new analyzer::Splat.DependencyInjection.Analyzer.Analyzers.PropertyAnalyzer(),
        ];
        return compilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync();
    }

    /// <summary>Builds the framework and Splat references for the test's target framework.</summary>
    /// <returns>The references.</returns>
    private static MetadataReference[] CreateDefaultReferences()
    {
#if NET10_0_OR_GREATER
        var framework = Basic.Reference.Assemblies.Net100.References.All;
#elif NET9_0_OR_GREATER
        var framework = Basic.Reference.Assemblies.Net90.References.All;
#else
        var framework = Basic.Reference.Assemblies.Net80.References.All;
#endif
        List<MetadataReference> references = [.. framework, MetadataReference.CreateFromFile(typeof(IReadonlyDependencyResolver).Assembly.Location)];
        return [.. references];
    }
}
