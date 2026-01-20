// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Splat.DependencyInjection.SourceGenerator.Tests;

/// <summary>
/// Modern test helper using Basic.Reference.Assemblies for testing incremental generators.
/// Follows the pattern from the Roslyn Source Generators Cookbook.
/// </summary>
public static class TestHelper
{
    /// <summary>
    /// Initializes resources before tests run.
    /// </summary>
    /// <returns>A task representing the asynchronous initialization operation.</returns>
    public static Task InitializeAsync()
    {
        // No initialization needed with Basic.Reference.Assemblies
        return Task.CompletedTask;
    }

    /// <summary>
    /// Tests a source generator scenario that is expected to fail with compilation or generator errors.
    /// Verifies the errors against a snapshot.
    /// </summary>
    /// <param name="source">The source code to compile and generate.</param>
    /// <param name="contractParameter">The contract parameter value for registration.</param>
    /// <param name="callerType">The type of the calling test class for snapshot organization.</param>
    /// <param name="file">The source file path of the caller (automatically populated).</param>
    /// <param name="memberName">The member name of the caller (automatically populated).</param>
    /// <returns>A task representing the asynchronous verification operation.</returns>
    public static Task TestFail(string source, string contractParameter, Type callerType, [CallerFilePath] string file = "", [CallerMemberName] string memberName = "")
    {
        ArgumentNullException.ThrowIfNull(callerType);

        var driver = RunGenerator(source, out var compilation, out var generatorDiagnostics);

        // For fail tests, we expect compilation or generator diagnostics
        var allDiagnostics = compilation.GetDiagnostics()
            .Concat(generatorDiagnostics)
            .Where(d => d.Severity >= DiagnosticSeverity.Error)
            .ToImmutableArray();

        if (allDiagnostics.Length == 0)
        {
            Assert.Fail("Expected compilation or generator to produce errors");
        }

        // Log error diagnostics for debugging
        foreach (var diagnostic in allDiagnostics)
        {
            Console.WriteLine($"{diagnostic.Severity}: {diagnostic.GetMessage()}");
        }

        return RunVerify(file, memberName, callerType, driver, contractParameter);
    }

    /// <summary>
    /// Tests a source generator scenario that is expected to succeed without errors.
    /// Verifies the generated output against a snapshot.
    /// </summary>
    /// <param name="source">The source code to compile and generate.</param>
    /// <param name="contractParameter">The contract parameter value for registration.</param>
    /// <param name="callerType">The type of the calling test class for snapshot organization.</param>
    /// <param name="file">The source file path of the caller (automatically populated).</param>
    /// <param name="memberName">The member name of the caller (automatically populated).</param>
    /// <returns>A task representing the asynchronous verification operation.</returns>
    public static Task TestPass(string source, string contractParameter, Type callerType, [CallerFilePath] string file = "", [CallerMemberName] string memberName = "")
    {
        ArgumentNullException.ThrowIfNull(callerType);

        var driver = RunGenerator(source, out var compilation, out var generatorDiagnostics);

        // Log any diagnostics for debugging
        var allDiagnostics = compilation.GetDiagnostics()
            .Concat(generatorDiagnostics)
            .Where(d => d.Severity >= DiagnosticSeverity.Warning)
            .ToImmutableArray();

        foreach (var diagnostic in allDiagnostics)
        {
            Console.WriteLine($"{diagnostic.Severity}: {diagnostic.GetMessage()}");
        }

        return RunVerify(file, memberName, callerType, driver, contractParameter);
    }

    /// <summary>
    /// Tests a source generator scenario for lazy singleton registration that is expected to succeed without errors.
    /// Verifies the generated output against a snapshot including lazy thread safety mode.
    /// </summary>
    /// <param name="source">The source code to compile and generate.</param>
    /// <param name="contractParameter">The contract parameter value for registration.</param>
    /// <param name="mode">The lazy thread safety mode for the singleton.</param>
    /// <param name="callerType">The type of the calling test class for snapshot organization.</param>
    /// <param name="file">The source file path of the caller (automatically populated).</param>
    /// <param name="memberName">The member name of the caller (automatically populated).</param>
    /// <returns>A task representing the asynchronous verification operation.</returns>
    public static Task TestPass(string source, string contractParameter, System.Threading.LazyThreadSafetyMode mode, Type callerType, [CallerFilePath] string file = "", [CallerMemberName] string memberName = "")
    {
        ArgumentNullException.ThrowIfNull(callerType);

        var driver = RunGenerator(source, out var compilation, out var generatorDiagnostics);

        // Log any diagnostics for debugging
        var allDiagnostics = compilation.GetDiagnostics()
            .Concat(generatorDiagnostics)
            .Where(d => d.Severity >= DiagnosticSeverity.Warning)
            .ToImmutableArray();

        foreach (var diagnostic in allDiagnostics)
        {
            Console.WriteLine($"{diagnostic.Severity}: {diagnostic.GetMessage()}");
        }

        return RunVerify(file, memberName, callerType, driver, contractParameter, mode);
    }

    /// <summary>
    /// Runs snapshot verification on the generator driver output.
    /// Configures verify settings based on caller information and parameters.
    /// </summary>
    /// <param name="file">The source file path of the caller.</param>
    /// <param name="callerMember">The member name of the caller.</param>
    /// <param name="type">The type of the calling test class for snapshot organization.</param>
    /// <param name="driver">The generator driver containing the output to verify.</param>
    /// <param name="parameters">Additional parameters to include in the snapshot file name.</param>
    /// <returns>A task representing the asynchronous verification operation.</returns>
    private static Task RunVerify(string file, string callerMember, Type type, GeneratorDriver driver, params object[] parameters)
    {
        var parametersString = string.Join("_", parameters.Select(AbbreviateParameter));
        VerifySettings settings = new();
        settings.DisableRequireUniquePrefix();

        // Shorten type name
        var shortTypeName = AbbreviateTypeName(type.Name);

        // Shorten method name
        var shortMethodName = AbbreviateMethodName(callerMember);

        if (!string.IsNullOrWhiteSpace(parametersString))
        {
            settings.UseTextForParameters(parametersString);
        }

        settings.UseTypeName(shortTypeName);
        settings.UseMethodName(shortMethodName);
        return Verifier.Verify(driver, settings, file);
    }

    /// <summary>
    /// Abbreviates test class names to keep file names short.
    /// </summary>
    private static string AbbreviateTypeName(string typeName)
    {
        return typeName switch
        {
            "RegisterLazySingletonTests" => "LS",
            "RegisterTests" => "R",
            _ => typeName
        };
    }

    /// <summary>
    /// Abbreviates method names to keep file names short.
    /// </summary>
    private static string AbbreviateMethodName(string methodName)
    {
        return methodName
            .Replace("ConstructionAnd", "C")
            .Replace("Construction", "C")
            .Replace("Multiple", "M")
            .Replace("Property", "P")
            .Replace("Injection", "I")
            .Replace("Internal", "Int")
            .Replace("Setter", "Set")
            .Replace("WithLazyMode", "LM")
            .Replace("Parameter", "Pm")
            .Replace("Registered", "Reg")
            .Replace("Attribute", "Attr")
            .Replace("Without", "No")
            .Replace("NonPublic", "NP")
            .Replace("Fail", "F")
            .Replace("Pass", "P")
            .Replace("Lazy", "L")
            .Replace("Empty", "E")
            .Replace("Interface", "I")
            .Replace("Times", "x");
    }

    /// <summary>
    /// Abbreviates parameter values to keep file names short.
    /// </summary>
    private static string AbbreviateParameter(object parameter)
    {
        if (parameter is System.Threading.LazyThreadSafetyMode mode)
        {
            return mode switch
            {
                System.Threading.LazyThreadSafetyMode.None => "N",
                System.Threading.LazyThreadSafetyMode.PublicationOnly => "P",
                System.Threading.LazyThreadSafetyMode.ExecutionAndPublication => "EP",
                _ => mode.ToString()
            };
        }

        if (parameter is string str)
        {
            return string.IsNullOrWhiteSpace(str) ? "contractParameter=" : str;
        }

        return parameter?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Runs the source generator on the provided source code.
    /// Creates a compilation with appropriate references and executes the incremental generator.
    /// </summary>
    /// <param name="source">The source code to compile and generate.</param>
    /// <param name="outputCompilation">The compilation after generator execution.</param>
    /// <param name="diagnostics">Diagnostics produced during generation.</param>
    /// <returns>The generator driver containing the generated output.</returns>
    private static GeneratorDriver RunGenerator(string source, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics)
    {
        // Parse the source
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        // Create compilation using Basic.Reference.Assemblies (modern approach)
        // Use the appropriate reference assemblies based on target framework
        IEnumerable<MetadataReference> references;

#if NET10_0_OR_GREATER
        references = Basic.Reference.Assemblies.Net100.References.All;
#elif NET9_0_OR_GREATER
        references = Basic.Reference.Assemblies.Net90.References.All;
#else
        references = Basic.Reference.Assemblies.Net80.References.All;
#endif

        // Add Splat assembly reference
        var splatAssembly = typeof(Splat.IReadonlyDependencyResolver).Assembly;
        var allReferences = references.Concat(new[] { MetadataReference.CreateFromFile(splatAssembly.Location) });

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            allReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Create generator driver with incremental step tracking (Cookbook pattern)
        var generator = new Generator();
        var sourceGenerator = generator.AsSourceGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: new[] { sourceGenerator },
            driverOptions: new GeneratorDriverOptions(
                disabledOutputs: default,
                trackIncrementalGeneratorSteps: true)); // Enable tracking for testing

        // Run generators
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out outputCompilation, out diagnostics);

        return driver;
    }
}