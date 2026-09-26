// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Splat.DI.SourceGenerator.Benchmarks.Corpus;

namespace Splat.DI.SourceGenerator.Benchmarks;

/// <summary>Measures what a consumer's build pays while the generator runs.</summary>
/// <remarks>
/// <para>
/// <see cref="Cold"/> builds a fresh driver per operation, so each measurement is a whole pass: syntax scan,
/// extraction, validation and emission. A reused driver would serve the pass from its caches.
/// </para>
/// <para>
/// The <c>Edit</c> cases keep one driver and alternate between two compilations that differ in one file, which is
/// the pass an IDE runs after each keystroke. <see cref="EditUnrelated"/> changes a file with no registrations;
/// <see cref="EditBootstrapper"/> moves every registration down a line without changing any of them.
/// </para>
/// <para>
/// The compilations are built once per parameter set: loading a framework's worth of metadata references costs
/// far more than the pass under measurement, and is work the host build does once.
/// </para>
/// </remarks>
[DebuggerDisplay("GenerationBenchmarks: {Registrations}")]
public class GenerationBenchmarks
{
    /// <summary>The number of edited compilations each edit case alternates between.</summary>
    private const int EditCount = 2;

    /// <summary>The generated files a clean run adds: the marker source and the registrations.</summary>
    private const int ExpectedGeneratedFiles = 2;

    /// <summary>The corpus compilation.</summary>
    private CSharpCompilation _compilation = null!;

    /// <summary>The corpus with the unrelated file edited.</summary>
    private CSharpCompilation[] _unrelatedEdits = null!;

    /// <summary>The corpus with the bootstrapper edited.</summary>
    private CSharpCompilation[] _bootstrapperEdits = null!;

    /// <summary>The driver the edit cases carry from one operation to the next.</summary>
    private GeneratorDriver _driver = null!;

    /// <summary>The number of operations run on <see cref="_driver"/>.</summary>
    private int _revision;

    /// <summary>Gets or sets the number of registrations in the corpus.</summary>
    [Params(1, 16, 128)]
    public int Registrations { get; set; }

    /// <summary>Builds the corpus and its edited forms, and primes the edit driver.</summary>
    /// <exception cref="InvalidOperationException">The corpus does not generate, or its generated code does not compile.</exception>
    [GlobalSetup]
    public void Setup()
    {
        _compilation = CorpusBuilder.Build(Registrations);
        _unrelatedEdits = CreateEdits(CorpusBuilder.UnrelatedFile);
        _bootstrapperEdits = CreateEdits(CorpusBuilder.BootstrapperFile);

        var result = CorpusBuilder.CreateDriver().RunGeneratorsAndUpdateCompilation(_compilation, out var output, out var diagnostics).GetRunResult();
        if (result.GeneratedTrees.Length < ExpectedGeneratedFiles || !result.Diagnostics.IsEmpty || !diagnostics.IsEmpty)
        {
            throw new InvalidOperationException($"The corpus did not generate cleanly: {string.Join(", ", result.Diagnostics)}");
        }

        foreach (var diagnostic in output.GetDiagnostics())
        {
            if (diagnostic.Severity == DiagnosticSeverity.Error)
            {
                throw new InvalidOperationException($"The generated corpus does not compile: {diagnostic}");
            }
        }

        // Each benchmark case runs in its own process, so the edit cases start from a primed driver.
        _driver = CorpusBuilder.CreateDriver().RunGenerators(_compilation);
    }

    /// <summary>Runs a whole cold generation pass.</summary>
    /// <returns>The number of generated characters, so the work cannot be optimized away.</returns>
    [Benchmark(Baseline = true)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Cold() => CountCharacters(CorpusBuilder.CreateDriver().RunGenerators(_compilation));

    /// <summary>Reruns the generator after an edit to a file with no registrations.</summary>
    /// <returns>The number of generated characters.</returns>
    [Benchmark]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int EditUnrelated() => RunEdit(_unrelatedEdits);

    /// <summary>Reruns the generator after an edit that moves every registration without changing one.</summary>
    /// <returns>The number of generated characters.</returns>
    [Benchmark]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int EditBootstrapper() => RunEdit(_bootstrapperEdits);

    /// <summary>Counts the characters a run generated.</summary>
    /// <param name="driver">The driver after the run.</param>
    /// <returns>The number of characters.</returns>
    private static int CountCharacters(GeneratorDriver driver)
    {
        var characters = 0;
        foreach (var generated in driver.GetRunResult().Results[0].GeneratedSources)
        {
            characters += generated.SourceText.Length;
        }

        return characters;
    }

    /// <summary>Builds the compilations an edit case alternates between.</summary>
    /// <param name="fileName">The file each edit changes.</param>
    /// <returns>The edited compilations.</returns>
    private CSharpCompilation[] CreateEdits(string fileName)
    {
        var edits = new CSharpCompilation[EditCount];
        for (var i = 0; i < edits.Length; i++)
        {
            edits[i] = CorpusBuilder.Edit(_compilation, fileName, Registrations, i + 1);
        }

        return edits;
    }

    /// <summary>Runs the carried driver over the next of the alternating edits.</summary>
    /// <param name="edits">The edited compilations.</param>
    /// <returns>The number of generated characters.</returns>
    private int RunEdit(CSharpCompilation[] edits)
    {
        _revision++;
        _driver = _driver.RunGenerators(edits[_revision % EditCount]);
        return CountCharacters(_driver);
    }
}
