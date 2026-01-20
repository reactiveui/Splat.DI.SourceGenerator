// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Splat.DependencyInjection.Analyzer.Tests;

/// <summary>
/// Helper for testing code fix providers by creating compilations and applying fixes.
/// Provides lightweight testing without external test framework dependencies.
/// </summary>
public static class CodeFixTestHelper
{
    /// <summary>
    /// Applies a code fix to source code and returns the result.
    /// Creates a compilation, runs the analyzer to get diagnostics, then applies the specified code fix.
    /// </summary>
    /// <typeparam name="TAnalyzer">The diagnostic analyzer that detects issues.</typeparam>
    /// <typeparam name="TCodeFixProvider">The code fix provider that fixes issues.</typeparam>
    /// <param name="source">The source code containing the issue to be fixed.</param>
    /// <param name="codeActionIndex">The zero-based index of the code action to apply when multiple fixes are offered.</param>
    /// <returns>A task representing the asynchronous operation containing the fixed source code.</returns>
    /// <exception cref="System.InvalidOperationException">Thrown when no code actions are registered by the provider.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the code action index is invalid.</exception>
    public static async Task<string> ApplyCodeFixAsync<TAnalyzer, TCodeFixProvider>(
        string source,
        int codeActionIndex = 0)
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFixProvider : CodeFixProvider, new()
    {
        var document = CreateDocument(source);
        var compilation = await document.Project.GetCompilationAsync();
        var analyzer = new TAnalyzer();
        var codeFixProvider = new TCodeFixProvider();

        // Run analyzer to get diagnostics
        var compilationWithAnalyzers = compilation!.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        var diagnostic = diagnostics.First(d =>
            analyzer.SupportedDiagnostics.Any(sd => sd.Id == d.Id));

        // Get code fixes
        var codeActions = new List<CodeAction>();
        var context = new CodeFixContext(
            document,
            diagnostic,
            (action, _) => codeActions.Add(action),
            CancellationToken.None);

        await codeFixProvider.RegisterCodeFixesAsync(context);

        if (codeActions.Count == 0)
        {
            throw new System.InvalidOperationException("No code actions were registered by the code fix provider.");
        }

        if (codeActionIndex >= codeActions.Count)
        {
            throw new System.ArgumentOutOfRangeException(
                nameof(codeActionIndex),
                $"Code action index {codeActionIndex} is out of range. Only {codeActions.Count} code actions available.");
        }

        // Apply the selected code fix
        var operations = await codeActions[codeActionIndex].GetOperationsAsync(CancellationToken.None);
        var operation = operations.OfType<ApplyChangesOperation>().Single();
        var changedSolution = operation.ChangedSolution;
        var changedDocument = changedSolution.GetDocument(document.Id);

        // Get the fixed source code
        var sourceText = await changedDocument!.GetTextAsync();
        return sourceText.ToString();
    }

    /// <summary>
    /// Creates a Document from source code with necessary references.
    /// </summary>
    private static Document CreateDocument(string source)
    {
        // Add the attribute definitions so the analyzer can find them
        // Note: Attributes must be at namespace level to match Constants.ConstructorAttribute and Constants.PropertyAttribute
        const string attributeSource = """
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
            }
            """;

        const string projectName = "TestProject";
        var projectId = ProjectId.CreateNewId(projectName);

        var solution = new AdhocWorkspace()
            .CurrentSolution
            .AddProject(projectId, projectName, projectName, LanguageNames.CSharp)
            .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location))
            .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(System.Attribute).Assembly.Location))
            .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(Splat.IReadonlyDependencyResolver).Assembly.Location));

        // Add System.Runtime reference
        var systemRuntime = System.AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "System.Runtime");
        if (systemRuntime != null)
        {
            solution = solution.AddMetadataReference(projectId, MetadataReference.CreateFromFile(systemRuntime.Location));
        }

        var project = solution.GetProject(projectId)!
            .WithCompilationOptions(new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        // Add attribute definitions first
        project = project.AddDocument("Attributes.cs", attributeSource).Project;

        // Then add the test source
        return project.AddDocument("Test.cs", source);
    }
}
