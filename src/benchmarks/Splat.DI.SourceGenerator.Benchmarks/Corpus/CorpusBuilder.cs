// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Splat.DependencyInjection.SourceGenerator;

namespace Splat.DI.SourceGenerator.Benchmarks.Corpus;

/// <summary>Builds a consumer compilation holding a chosen number of registrations, and the driver that runs over it.</summary>
/// <remarks>
/// <para>
/// Registrations cycle through the shapes the generator handles: a parameterless constructor, constructor and
/// property injection, a lazy singleton with a thread safety mode taking a collection, and a contract taking a lazy
/// dependency. Every third type carries two constructors, one marked for injection.
/// </para>
/// <para>
/// Every service file also calls Splat's own <c>Register</c> and other methods, so the syntax predicate sees the
/// invocations a real project has and the transform sees calls that share a name with the marker methods.
/// </para>
/// </remarks>
internal static class CorpusBuilder
{
    /// <summary>The name of the file that makes the registrations.</summary>
    internal const string BootstrapperFile = "Bootstrapper.cs";

    /// <summary>The name of the file that takes no part in registration.</summary>
    internal const string UnrelatedFile = "Unrelated.cs";

    /// <summary>How many services each service file declares.</summary>
    private const int ServicesPerFile = 8;

    /// <summary>The files beside the service files: the bootstrapper and the unrelated file.</summary>
    private const int FixedFiles = 2;

    /// <summary>The characters the bootstrapper takes besides its registrations.</summary>
    private const int BootstrapperCapacity = 256;

    /// <summary>The characters one registration call takes.</summary>
    private const int RegistrationCapacity = 96;

    /// <summary>The characters a file of services takes.</summary>
    private const int ServicesCapacity = 2048;

    /// <summary>How many blank lines an edit to the bootstrapper can insert, cycling by revision.</summary>
    private const int MaxInsertedLines = 8;

    /// <summary>One type in this many carries a second, unmarked constructor.</summary>
    private const uint SecondConstructorEvery = 3;

    /// <summary>The number of registration shapes the corpus cycles through.</summary>
    private const uint ShapeCount = 4;

    /// <summary>How far back a contract registration's service dependency reaches.</summary>
    private const int ServiceDependencyDistance = 2;

    /// <summary>The start of a registration of a service.</summary>
    private const string RegisterService = "        SplatRegistrations.Register<IService";

    /// <summary>The separator between a service and its implementation in a registration.</summary>
    private const string ServiceSeparator = ", Service";

    /// <summary>The parse options every tree is parsed with.</summary>
    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Latest);

    /// <summary>The shape of registration each service is given, by its number modulo <see cref="ShapeCount"/>.</summary>
    private enum Shape
    {
        /// <summary>A parameterless constructor.</summary>
        Parameterless = 0,

        /// <summary>A constructor dependency and an injected property.</summary>
        Injected = 1,

        /// <summary>A lazy singleton taking a collection.</summary>
        LazyCollection = 2,

        /// <summary>A contract, with a service and a lazy dependency.</summary>
        ContractLazy = 3,
    }

    /// <summary>Builds the consumer compilation.</summary>
    /// <param name="registrations">The number of registrations.</param>
    /// <returns>The compilation.</returns>
    internal static CSharpCompilation Build(int registrations)
    {
        var trees = new List<SyntaxTree>((registrations / ServicesPerFile) + FixedFiles) { Parse(BuildBootstrapper(registrations, 0), BootstrapperFile) };

        for (var start = 0; start < registrations; start += ServicesPerFile)
        {
            trees.Add(Parse(BuildServices(start, Math.Min(start + ServicesPerFile, registrations)), $"Services{start}.cs"));
        }

        trees.Add(Parse(BuildUnrelated(0), UnrelatedFile));

        List<MetadataReference> references = [.. Basic.Reference.Assemblies.Net100.References.All, MetadataReference.CreateFromFile(typeof(IReadonlyDependencyResolver).Assembly.Location)];

        return CSharpCompilation.Create("BenchmarkConsumer", trees, references, new(OutputKind.DynamicallyLinkedLibrary));
    }

    /// <summary>Returns the compilation with one file's text changed, as an edit between two builds.</summary>
    /// <param name="compilation">The compilation to edit.</param>
    /// <param name="fileName">The file to change: <see cref="BootstrapperFile"/> or <see cref="UnrelatedFile"/>.</param>
    /// <param name="registrations">The number of registrations the compilation was built with.</param>
    /// <param name="revision">Tells one edit from the next.</param>
    /// <returns>The edited compilation.</returns>
    /// <exception cref="ArgumentException">The corpus has no file of that name.</exception>
    internal static CSharpCompilation Edit(CSharpCompilation compilation, string fileName, int registrations, int revision)
    {
        ArgumentNullException.ThrowIfNull(compilation);

        foreach (var tree in compilation.SyntaxTrees)
        {
            if (tree.FilePath != fileName)
            {
                continue;
            }

            var text = fileName == BootstrapperFile
                ? BuildBootstrapper(registrations, revision)
                : BuildUnrelated(revision);
            return compilation.ReplaceSyntaxTree(tree, Parse(text, fileName));
        }

        throw new ArgumentException($"The corpus has no file named {fileName}.", nameof(fileName));
    }

    /// <summary>Creates a driver that runs the generator with step tracking off, as a build does.</summary>
    /// <returns>The driver.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static GeneratorDriver CreateDriver() =>
        CSharpGeneratorDriver.Create(
            [new Generator().AsSourceGenerator()],
            parseOptions: ParseOptions,
            driverOptions: new(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: false));

    /// <summary>Parses a file.</summary>
    /// <param name="text">The source.</param>
    /// <param name="path">The file path.</param>
    /// <returns>The tree.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static SyntaxTree Parse(string text, string path) => CSharpSyntaxTree.ParseText(text, ParseOptions, path);

    /// <summary>Gets the shape of registration a service is given; the first service always takes no parameters.</summary>
    /// <param name="service">The service number, which is never negative.</param>
    /// <returns>The shape.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Shape ShapeOf(int service) => service == 0 ? Shape.Parameterless : (Shape)((uint)service % ShapeCount);

    /// <summary>Builds the file that makes every registration.</summary>
    /// <param name="registrations">The number of registrations.</param>
    /// <param name="revision">Blank lines added at the top, so an edit moves every registration.</param>
    /// <returns>The source.</returns>
    private static string BuildBootstrapper(int registrations, int revision)
    {
        var builder = new StringBuilder(BootstrapperCapacity + (registrations * RegistrationCapacity));
        _ = builder.Append('\n', revision % MaxInsertedLines);
        _ = builder.AppendLine("using System.Threading;").AppendLine("using Splat;").AppendLine();
        _ = builder.AppendLine("namespace Consumer;").AppendLine();
        _ = builder.AppendLine("public static class Bootstrapper").AppendLine("{");
        _ = builder.AppendLine("    public static void Register()").AppendLine("    {");
        for (var i = 0; i < registrations; i++)
        {
            _ = ShapeOf(i) switch
            {
                Shape.LazyCollection => builder.Append("        SplatRegistrations.RegisterLazySingleton<IService").Append(i)
                    .Append(ServiceSeparator).Append(i).AppendLine(">(LazyThreadSafetyMode.PublicationOnly);"),
                Shape.ContractLazy => builder.Append(RegisterService).Append(i).Append(ServiceSeparator).Append(i).AppendLine(">(Keys.Contract);"),
                _ => builder.Append(RegisterService).Append(i).Append(ServiceSeparator).Append(i).AppendLine(">();"),
            };
        }

        _ = builder.AppendLine("        SplatRegistrations.SetupIOC();").AppendLine("    }").AppendLine("}").AppendLine();
        _ = builder.AppendLine("public static class Keys").AppendLine("{").AppendLine("    public const string Contract = \"contract\";").AppendLine("}");
        return builder.ToString();
    }

    /// <summary>Builds a file of services.</summary>
    /// <param name="start">The first service.</param>
    /// <param name="end">One past the last service.</param>
    /// <returns>The source.</returns>
    private static string BuildServices(int start, int end)
    {
        var builder = new StringBuilder(ServicesCapacity);
        _ = builder.AppendLine("using System;").AppendLine("using System.Collections.Generic;").AppendLine("using Splat;").AppendLine();
        _ = builder.AppendLine("namespace Consumer;").AppendLine();
        for (var i = start; i < end; i++)
        {
            _ = builder.Append("public interface IService").Append(i).AppendLine(" { }").AppendLine();
            _ = builder.Append("public sealed class Service").Append(i).Append(" : IService").AppendLine(i.ToString(CultureInfo.InvariantCulture)).AppendLine("{");
            AppendConstructors(builder, i);
            _ = builder.AppendLine("    public void Work(IMutableDependencyResolver resolver, List<int> values)").AppendLine("    {");
            _ = builder.AppendLine("        values.Add(1);").AppendLine("        Console.WriteLine(values.Count);");
            _ = builder.Append("        resolver.Register<IService").Append(i).Append(">(() => new Service").Append(i).Append('(');
            AppendNewArguments(builder, i);
            _ = builder.AppendLine("));");
            _ = builder.AppendLine("    }").AppendLine("}").AppendLine();
        }

        return builder.ToString();
    }

    /// <summary>Writes a service's constructors and injected property.</summary>
    /// <param name="builder">The builder.</param>
    /// <param name="i">The service number.</param>
    private static void AppendConstructors(StringBuilder builder, int i)
    {
        var shape = ShapeOf(i);
        var parameters = shape switch
        {
            Shape.Parameterless => string.Empty,
            Shape.Injected => $"IService{i - 1} dependency",
            Shape.LazyCollection => $"IEnumerable<IService{i - 1}> dependencies",
            _ => $"IService{i - ServiceDependencyDistance} dependency, Lazy<IService{i - 1}> lazy",
        };

        if (shape == Shape.Injected && i > 1)
        {
            _ = builder.Append("    [DependencyInjectionProperty] public IService").Append(i - ServiceDependencyDistance).AppendLine(" Injected { get; set; }");
        }

        if ((uint)i % SecondConstructorEvery == 0)
        {
            _ = builder.Append("    public Service").Append(i).AppendLine("(int ignored) { }");
            _ = builder.AppendLine("    [DependencyInjectionConstructor]");
        }

        _ = builder.Append("    public Service").Append(i).Append('(').Append(parameters).AppendLine(") { }");
    }

    /// <summary>Writes the arguments Splat's own registration passes to a service's constructor.</summary>
    /// <param name="builder">The builder.</param>
    /// <param name="i">The service number.</param>
    private static void AppendNewArguments(StringBuilder builder, int i) =>
        _ = ShapeOf(i) switch
        {
            Shape.Parameterless => builder,
            Shape.Injected => builder.Append("null!"),
            Shape.LazyCollection => builder.Append("Array.Empty<IService").Append(i - 1).Append(">()"),
            _ => builder.Append("null!, null!"),
        };

    /// <summary>Builds a file that takes no part in registration.</summary>
    /// <param name="revision">A value written into the file, so an edit changes it.</param>
    /// <returns>The source.</returns>
    private static string BuildUnrelated(int revision) =>
        $$"""
        namespace Consumer;

        public static class Unrelated
        {
            public static int Compute(int value) => (value * {{revision}}) + System.Math.Max(value, 1);
        }
        """;
}
