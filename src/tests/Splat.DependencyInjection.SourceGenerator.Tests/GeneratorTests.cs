// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Splat.DependencyInjection.SourceGenerator.Tests;

/// <summary>Tests the pipeline: what it adds, and what an edit makes it redo.</summary>
public sealed class GeneratorTests
{
    /// <summary>The file that makes the registrations.</summary>
    private const string Registrations = """
        using Splat;

        namespace T
        {
            public interface IA { }
            public interface IB { }
            public sealed class A : IA { public A(IB b) { } }
            public sealed class B : IB { }

            public static class Bootstrapper
            {
                public static void Register()
                {
                    SplatRegistrations.Register<IA, A>();
                    SplatRegistrations.RegisterLazySingleton<IB, B>();
                }
            }
        }
        """;

    /// <summary>The path of the first file of a test compilation.</summary>
    private const string FirstFile = "File0.cs";

    /// <summary>A project with no registrations gets the marker source and no registrations file.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task LeavesOutRegistrationsFileWithoutRegistrations()
    {
        var run = TestHelper.Run("namespace T { public static class C { public static void M() => Splat.SplatRegistrations.SetupIOC(); } }");

        await Assert.That(run.RegistrationSource()).IsNull();
        await Assert.That(run.HintNames()).Contains(Constants.ExtensionMethodFileName);
        await Assert.That(run.Errors()).IsEmpty();
    }

    /// <summary>The generated registrations compile against Splat.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GeneratesCompilingRegistrations()
    {
        var run = TestHelper.Run(Registrations);

        await Assert.That(run.RegistrationSource()).IsNotNull();
        await Assert.That(run.Errors()).IsEmpty();
        await Assert.That(run.Result.Diagnostics).IsEmpty();
    }

    /// <summary>An edit to a file without registrations reuses every registration and the generated file.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EditElsewhereReusesEverything()
    {
        var compilation = TestHelper.CreateCompilation([Registrations, "namespace T { public static class U { public static int V => 1; } }"]);
        var first = TestHelper.Run(compilation);

        var edited = ReplaceFile(compilation, "File1.cs", "namespace T { public static class U { public static int V => 2; } }");
        var second = first.Driver.RunGenerators(edited).GetRunResult().Results[0];

        await Assert.That(OutputReasons(second, Generator.CollectedRegistrationsStep)).IsEquivalentTo([IncrementalStepRunReason.Cached]);
        await Assert.That(OutputReasons(second, Generator.RegistrationsStep)).DoesNotContain(IncrementalStepRunReason.Modified);
    }

    /// <summary>An edit that only moves the registrations re-extracts them, but leaves the generated file as it was.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EditThatMovesRegistrationsKeepsGeneratedFile()
    {
        var compilation = TestHelper.CreateCompilation([Registrations]);
        var first = TestHelper.Run(compilation);

        var edited = ReplaceFile(compilation, FirstFile, $"\n\n{Registrations}");
        var second = first.Driver.RunGenerators(edited).GetRunResult().Results[0];

        await Assert.That(OutputReasons(second, Generator.SitesStep)).Contains(IncrementalStepRunReason.Modified);
        await Assert.That(OutputReasons(second, Generator.RegistrationsStep)).DoesNotContain(IncrementalStepRunReason.Modified);
        await Assert.That(OutputReasons(second, Generator.CollectedRegistrationsStep)).DoesNotContain(IncrementalStepRunReason.Modified);
    }

    /// <summary>An edit that changes a registration regenerates the file.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EditThatChangesRegistrationRegenerates()
    {
        var compilation = TestHelper.CreateCompilation([Registrations]);
        var first = TestHelper.Run(compilation);

        var edited = ReplaceFile(compilation, FirstFile, Registrations.Replace("Register<IA, A>();", "Register<IA, A>(\"c\");", StringComparison.Ordinal));
        var second = first.Driver.RunGenerators(edited).GetRunResult().Results[0];

        await Assert.That(OutputReasons(second, Generator.CollectedRegistrationsStep)).IsEquivalentTo([IncrementalStepRunReason.Modified]);
    }

    /// <summary>Gets the reasons each output of a tracked step was produced for.</summary>
    /// <param name="result">The run.</param>
    /// <param name="stepName">The step's tracking name.</param>
    /// <returns>The reasons.</returns>
    private static List<IncrementalStepRunReason> OutputReasons(in GeneratorRunResult result, string stepName)
    {
        var reasons = new List<IncrementalStepRunReason>();
        foreach (var step in result.TrackedSteps[stepName])
        {
            foreach (var output in step.Outputs)
            {
                reasons.Add(output.Reason);
            }
        }

        return reasons;
    }

    /// <summary>Replaces one file of a compilation.</summary>
    /// <param name="compilation">The compilation.</param>
    /// <param name="path">The file's path.</param>
    /// <param name="source">The new text.</param>
    /// <returns>The edited compilation.</returns>
    /// <exception cref="InvalidOperationException">The compilation has no file at the path.</exception>
    private static Compilation ReplaceFile(Compilation compilation, string path, string source)
    {
        foreach (var tree in compilation.SyntaxTrees)
        {
            if (tree.FilePath == path)
            {
                return compilation.ReplaceSyntaxTree(tree, CSharpSyntaxTree.ParseText(source, TestHelper.ParseOptions, path));
            }
        }

        throw new InvalidOperationException($"The compilation has no file {path}.");
    }
}
