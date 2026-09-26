// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Composition.Hosting;
using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Splat.DependencyInjection.Analyzer.Tests;

/// <summary>
/// Helper for testing code fix providers by creating compilations and applying fixes.
/// Provides lightweight testing without external test framework dependencies.
/// </summary>
internal static class CodeFixTestHelper
{
    /// <summary>Applies the first code fix offered for the first diagnostic and returns the result.</summary>
    /// <typeparam name="TAnalyzer">The diagnostic analyzer that detects issues.</typeparam>
    /// <typeparam name="TCodeFixProvider">The code fix provider that fixes issues.</typeparam>
    /// <param name="source">The source code containing the issue to be fixed.</param>
    /// <returns>A task representing the asynchronous operation containing the fixed source code.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Task<string> ApplyCodeFixAsync<TAnalyzer, TCodeFixProvider>(string source)
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFixProvider : CodeFixProvider =>
        ApplyCodeFixAsync<TAnalyzer, TCodeFixProvider>(source, 0);

    /// <summary>
    /// Applies a code fix to source code and returns the result.
    /// Creates a compilation, runs the analyzer to get diagnostics, then applies the specified code fix.
    /// </summary>
    /// <typeparam name="TAnalyzer">The diagnostic analyzer that detects issues.</typeparam>
    /// <typeparam name="TCodeFixProvider">The code fix provider that fixes issues.</typeparam>
    /// <param name="source">The source code containing the issue to be fixed.</param>
    /// <param name="codeActionIndex">The zero-based index of the code action to apply when multiple fixes are offered.</param>
    /// <returns>A task representing the asynchronous operation containing the fixed source code.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no code actions are registered by the provider.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the code action index is invalid.</exception>
    internal static async Task<string> ApplyCodeFixAsync<TAnalyzer, TCodeFixProvider>(
        string source,
        int codeActionIndex)
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFixProvider : CodeFixProvider
    {
        ArgumentOutOfRangeException.ThrowIfNegative(codeActionIndex);

        var (document, workspace) = CreateDocument(source);
        using var disposableWorkspace = workspace;
        var compilation = await document.Project.GetCompilationAsync();
        var analyzer = new TAnalyzer();
        var codeFixProvider = GetExportedCodeFixProvider<TCodeFixProvider>();

        // Run analyzer to get diagnostics
        var compilationWithAnalyzers = compilation!.WithAnalyzers(
            [analyzer]);

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        var diagnostic = FirstSupportedDiagnostic(analyzer, diagnostics);

        // Get code fixes
        var codeActions = new List<CodeAction>();
        var context = new CodeFixContext(
            document,
            diagnostic,
            (action, _) => codeActions.Add(action),
            CancellationToken.None);

        await codeFixProvider.RegisterCodeFixesAsync(context);

        // Apply the selected code fix
        var operations = await SelectCodeAction(codeActions, codeActionIndex).GetOperationsAsync(CancellationToken.None);
        var changedDocument = SingleApplyChangesOperation(operations).ChangedSolution.GetDocument(document.Id);

        // Get the fixed source code
        var sourceText = await changedDocument!.GetTextAsync();
        return sourceText.ToString();
    }

    /// <summary>
    /// Asks a code fix provider for fixes for a diagnostic reported at a chosen position in the source,
    /// without running an analyzer, and returns the code actions it registered.
    /// </summary>
    /// <typeparam name="TCodeFixProvider">The code fix provider under test.</typeparam>
    /// <param name="source">The test source code.</param>
    /// <param name="position">The position in <paramref name="source"/> the diagnostic points at.</param>
    /// <param name="descriptor">The descriptor of the diagnostic to report.</param>
    /// <returns>A task containing the code actions registered by the provider.</returns>
    internal static async Task<IReadOnlyList<CodeAction>> RegisterCodeFixesAtAsync<TCodeFixProvider>(
        string source,
        int position,
        DiagnosticDescriptor descriptor)
        where TCodeFixProvider : CodeFixProvider
    {
        var (document, workspace) = CreateDocument(source);
        using var disposableWorkspace = workspace;
        var syntaxTree = await document.GetSyntaxTreeAsync();
        var diagnostic = Diagnostic.Create(descriptor, Location.Create(syntaxTree!, new(position, 0)), "Test");

        var codeActions = new List<CodeAction>();
        var context = new CodeFixContext(
            document,
            diagnostic,
            (action, _) => codeActions.Add(action),
            CancellationToken.None);

        await GetExportedCodeFixProvider<TCodeFixProvider>().RegisterCodeFixesAsync(context);
        return codeActions;
    }

    /// <summary>
    /// Obtains a code fix provider from a MEF composition container, the way the IDE does,
    /// so that its shared export is honored instead of being constructed directly.
    /// </summary>
    /// <typeparam name="TCodeFixProvider">The exported code fix provider type.</typeparam>
    /// <returns>The code fix provider instance created by the container.</returns>
    internal static TCodeFixProvider GetExportedCodeFixProvider<TCodeFixProvider>()
        where TCodeFixProvider : CodeFixProvider
    {
        using var host = new ContainerConfiguration()
            .WithPart<TCodeFixProvider>()
            .CreateContainer();
        return (TCodeFixProvider)host.GetExport<CodeFixProvider>();
    }

    /// <summary>Selects the code action at the requested index.</summary>
    /// <param name="codeActions">The registered code actions.</param>
    /// <param name="codeActionIndex">The zero-based index of the code action.</param>
    /// <returns>The selected code action.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no code actions were registered.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is out of range.</exception>
    private static CodeAction SelectCodeAction(List<CodeAction> codeActions, int codeActionIndex)
    {
        if (codeActions.Count == 0)
        {
            throw new InvalidOperationException("No code actions were registered by the code fix provider.");
        }

        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(codeActionIndex, codeActions.Count);
        return codeActions[codeActionIndex];
    }

    /// <summary>Gets the first diagnostic that the analyzer declares as supported.</summary>
    /// <param name="analyzer">The analyzer that produced the diagnostics.</param>
    /// <param name="diagnostics">The diagnostics to search.</param>
    /// <returns>The first supported diagnostic.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the analyzer reported no supported diagnostic.</exception>
    private static Diagnostic FirstSupportedDiagnostic(DiagnosticAnalyzer analyzer, ImmutableArray<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            if (AnalyzerTestHelper.IsSupported(analyzer, diagnostic))
            {
                return diagnostic;
            }
        }

        throw new InvalidOperationException("The analyzer reported no supported diagnostic.");
    }

    /// <summary>Gets the single <see cref="ApplyChangesOperation"/> from a code action's operations.</summary>
    /// <param name="operations">The code action operations.</param>
    /// <returns>The single apply-changes operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when there is not exactly one apply-changes operation.</exception>
    private static ApplyChangesOperation SingleApplyChangesOperation(ImmutableArray<CodeActionOperation> operations)
    {
        for (var i = 0; i < operations.Length; i++)
        {
            if (operations[i] is not ApplyChangesOperation applyChanges)
            {
                continue;
            }

            for (var j = i + 1; j < operations.Length; j++)
            {
                if (operations[j] is ApplyChangesOperation)
                {
                    throw new InvalidOperationException("More than one ApplyChangesOperation was produced.");
                }
            }

            return applyChanges;
        }

        throw new InvalidOperationException("No ApplyChangesOperation was produced.");
    }

    /// <summary>
    /// Creates a Document from source code with necessary references.
    /// The caller is responsible for disposing the returned workspace.
    /// </summary>
    /// <param name="source">The test source code.</param>
    /// <returns>The test document and the workspace that owns it.</returns>
    private static (Document Document, AdhocWorkspace Workspace) CreateDocument(string source)
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

        const string projectName = "TestProject";
        var projectId = ProjectId.CreateNewId(projectName);

        var workspace = new AdhocWorkspace();
        var solution = workspace.CurrentSolution
            .AddProject(projectId, projectName, projectName, LanguageNames.CSharp)
            .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location))
            .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location))
            .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(IReadonlyDependencyResolver).Assembly.Location));

        // Add System.Runtime reference
        var systemRuntime = TestUtilities.FindSystemRuntimeAssembly();
        if (systemRuntime is not null)
        {
            solution = solution.AddMetadataReference(projectId, MetadataReference.CreateFromFile(systemRuntime.Location));
        }

        var project = solution.GetProject(projectId)!
            .WithCompilationOptions(new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        // Add attribute definitions first
        project = project.AddDocument("Attributes.cs", attributeAndExtensionsSource).Project;

        // Then add the test source
        return (project.AddDocument("Test.cs", source), workspace);
    }
}
