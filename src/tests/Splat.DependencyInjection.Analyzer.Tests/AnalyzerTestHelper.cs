// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Splat.DependencyInjection.Analyzer.Tests;

/// <summary>Helper for testing Roslyn analyzers without heavy testing framework dependencies.</summary>
internal static class AnalyzerTestHelper
{
    /// <summary>Runs an analyzer on the provided source code and returns diagnostics.</summary>
    /// <typeparam name="TAnalyzer">The type of analyzer to run.</typeparam>
    /// <param name="source">The source code to analyze.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the diagnostics.</returns>
    internal static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync<TAnalyzer>(string source)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var compilation = CreateCompilation(source);
        var analyzer = new TAnalyzer();

        var compilationWithAnalyzers = compilation.WithAnalyzers(
            [analyzer]);

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        // Filter to only analyzer diagnostics (exclude compiler errors)
        var builder = ImmutableArray.CreateBuilder<Diagnostic>(diagnostics.Length);
        foreach (var diagnostic in diagnostics)
        {
            if (IsSupported(analyzer, diagnostic))
            {
                builder.Add(diagnostic);
            }
        }

        return builder.ToImmutable();
    }

    /// <summary>Determines whether a diagnostic is one the analyzer declares as supported.</summary>
    /// <param name="analyzer">The analyzer.</param>
    /// <param name="diagnostic">The diagnostic to check.</param>
    /// <returns><see langword="true"/> if the analyzer supports the diagnostic's ID.</returns>
    internal static bool IsSupported(DiagnosticAnalyzer analyzer, Diagnostic diagnostic)
    {
        foreach (var descriptor in analyzer.SupportedDiagnostics)
        {
            if (descriptor.Id == diagnostic.Id)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Creates a CSharpCompilation from source code with necessary references.</summary>
    /// <param name="source">The source code to compile.</param>
    /// <returns>The compilation containing the source and the Splat stubs.</returns>
    private static CSharpCompilation CreateCompilation(string source)
    {
        // Add the attribute definitions and SplatRegistrations class so the analyzer can find them
        // Note: Attributes must be at namespace level to match Constants.ConstructorAttribute and Constants.PropertyAttribute
        const string attributeAndExtensionsSource = """
            namespace Splat
            {
                [System.AttributeUsage(System.AttributeTargets.Property)]
                internal sealed class DependencyInjectionPropertyAttribute : System.Attribute
                {
                }

                [System.AttributeUsage(System.AttributeTargets.Constructor)]
                internal sealed class DependencyInjectionConstructorAttribute : System.Attribute
                {
                }

                /// <summary>
                /// Extension methods for the Splat DI source generator.
                /// </summary>
                internal static partial class SplatRegistrations
                {
                    public static void Register<TInterface, TConcrete>() { }
                    public static void Register<TInterface, TConcrete>(string contract) { }
                    public static void RegisterLazySingleton<TInterface, TConcrete>() { }
                    public static void RegisterLazySingleton<TInterface, TConcrete>(System.Threading.LazyThreadSafetyMode mode) { }
                    public static void RegisterLazySingleton<TInterface, TConcrete>(string contract) { }
                    public static void RegisterLazySingleton<TInterface, TConcrete>(string contract, System.Threading.LazyThreadSafetyMode mode) { }
                    public static void Register<T>() { }
                    public static void Register<T>(string contract) { }
                    public static void RegisterLazySingleton<T>() { }
                    public static void RegisterLazySingleton<T>(string contract) { }
                    public static void RegisterConstant<T>(T instance) { }
                    public static void RegisterConstant<T>(T instance, string contract) { }
                }
            }
            """;

        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var attributeTree = CSharpSyntaxTree.ParseText(attributeAndExtensionsSource);

        // Get references for the current runtime
        // Add core framework references, plus Splat for testing DI attributes
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IReadonlyDependencyResolver).Assembly.Location),
        };

        // Add System.Runtime reference (needed for netstandard2.0 compatibility)
        var systemRuntime = TestUtilities.FindSystemRuntimeAssembly();
        if (systemRuntime is not null)
        {
            references.Add(MetadataReference.CreateFromFile(systemRuntime.Location));
        }

        return CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree, attributeTree],
            references,
            new(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
    }
}
