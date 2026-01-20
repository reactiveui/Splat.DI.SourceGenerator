// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Splat.DependencyInjection.Analyzer.Tests;

/// <summary>
/// Helper for testing Roslyn analyzers without heavy testing framework dependencies.
/// </summary>
public static class AnalyzerTestHelper
{
    /// <summary>
    /// Runs an analyzer on the provided source code and returns diagnostics.
    /// </summary>
    /// <typeparam name="TAnalyzer">The type of analyzer to run.</typeparam>
    /// <param name="source">The source code to analyze.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the diagnostics.</returns>
    public static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync<TAnalyzer>(string source)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var compilation = CreateCompilation(source);
        var analyzer = new TAnalyzer();

        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        // Filter to only analyzer diagnostics (exclude compiler errors)
        return diagnostics
            .Where(d => analyzer.SupportedDiagnostics.Any(sd => sd.Id == d.Id))
            .ToImmutableArray();
    }

    /// <summary>
    /// Creates a CSharpCompilation from source code with necessary references.
    /// </summary>
    private static CSharpCompilation CreateCompilation(string source)
    {
        // Add the attribute definitions so the analyzer can find them
        const string attributeSource = """
            namespace Splat
            {
                internal static partial class SplatRegistrations
                {
                    [System.AttributeUsage(System.AttributeTargets.Property)]
                    internal sealed class DependencyInjectionPropertyAttribute : System.Attribute
                    {
                    }

                    [System.AttributeUsage(System.AttributeTargets.Constructor)]
                    internal sealed class DependencyInjectionConstructorAttribute : System.Attribute
                    {
                    }
                }
            }
            """;

        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var attributeTree = CSharpSyntaxTree.ParseText(attributeSource);

        // Get references for the current runtime
        var references = new List<MetadataReference>();

        // Add core framework references
        references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(System.Attribute).Assembly.Location));

        // Add Splat reference for testing DI attributes
        var splatAssembly = typeof(Splat.IReadonlyDependencyResolver).Assembly;
        references.Add(MetadataReference.CreateFromFile(splatAssembly.Location));

        // Add System.Runtime reference (needed for netstandard2.0 compatibility)
        var systemRuntime = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "System.Runtime");
        if (systemRuntime != null)
        {
            references.Add(MetadataReference.CreateFromFile(systemRuntime.Location));
        }

        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree, attributeTree },
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
    }
}
