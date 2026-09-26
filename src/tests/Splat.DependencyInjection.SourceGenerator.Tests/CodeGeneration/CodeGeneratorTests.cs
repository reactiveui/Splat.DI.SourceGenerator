// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

using Splat.DependencyInjection.SourceGenerator.CodeGeneration;
using Splat.DependencyInjection.SourceGenerator.Models;

namespace Splat.DependencyInjection.SourceGenerator.Tests.CodeGeneration;

/// <summary>Tests the code <see cref="CodeGenerator"/> writes for each shape of registration.</summary>
public sealed class CodeGeneratorTests
{
    /// <summary>The indentation of a statement in the registration method.</summary>
    private const string Body = "            ";

    /// <summary>The start of the factory for <see cref="TypeA"/>, as a continuation line.</summary>
    private const string FactoryOfA = "    () => new global::T.A(\n";

    /// <summary>The constructed type most tests register.</summary>
    private const string TypeA = "global::T.A";

    /// <summary>A second constructed type.</summary>
    private const string TypeB = "global::T.B";

    /// <summary>The dependency most tests resolve.</summary>
    private const string DependencyX = "global::T.IX";

    /// <summary>A registration with nothing to resolve is one line, whose lambda captures nothing.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task WritesParameterlessTransientOnOneLine()
    {
        var source = Generate(Transient(TypeA), Transient(TypeB, contract: "\"c\""));

        await Assert.That(source).Contains(
            $"{Body}resolver.Register<global::T.IA>(() => new global::T.A());\n\n{Body}resolver.Register<global::T.IB>(() => new global::T.B(), \"c\");\n");
        await Assert.That(source).DoesNotContain(CodeGenerator.ThrowNotRegistered);
    }

    /// <summary>Each dependency takes its own line, and a missing one throws through the helper.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task WritesDependenciesOnePerLine()
    {
        var registration = Transient(
            TypeA,
            parameters:
            [
                new(DependencyX, DependencyKind.Service, null),
                new("global::System.Lazy<global::T.IY>", DependencyKind.Lazy, "global::T.IY"),
                new("global::System.Collections.Generic.IEnumerable<global::T.IZ>", DependencyKind.Collection, "global::T.IZ"),
            ],
            properties: [new("P", "global::T.IP")]);

        var source = Generate(registration);

        await Assert.That(source).Contains(
            Body + "resolver.Register<global::T.IA>(\n"
            + Body + FactoryOfA
            + Body + "        resolver.GetService<global::T.IX>() ?? ThrowNotRegistered<global::T.IX>(\"global::T.IX\"),\n"
            + Body + "        resolver.GetService<global::System.Lazy<global::T.IY>>() ?? ThrowNotRegistered<global::System.Lazy<global::T.IY>>(\"global::System.Lazy<global::T.IY>\"),\n"
            + Body + "        resolver.GetServices<global::T.IZ>())\n"
            + Body + "    {\n"
            + Body + "        P = resolver.GetService<global::T.IP>() ?? ThrowNotRegistered<global::T.IP>(\"global::T.IP\"),\n"
            + Body + "    });\n");
        await Assert.That(source).Contains("private static T ThrowNotRegistered<T>(string typeName)\n");
    }

    /// <summary>
    /// A registration's contract names that registration only; its dependencies are resolved without it, and the file
    /// ends with the throw helper.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ResolvesDependenciesWithoutContract()
    {
        var registration = Transient(
            TypeA,
            contract: "global::T.Keys.Key",
            parameters:
            [
                new(DependencyX, DependencyKind.Service, null),
                new("global::System.Collections.Generic.IEnumerable<global::T.IZ>", DependencyKind.Collection, "global::T.IZ"),
            ]);

        var source = Generate(registration);

        await Assert.That(source).Contains(
            Body + "resolver.Register<global::T.IA>(\n"
            + Body + FactoryOfA
            + Body + "        resolver.GetService<global::T.IX>() ?? ThrowNotRegistered<global::T.IX>(\"global::T.IX\"),\n"
            + Body + "        resolver.GetServices<global::T.IZ>()),\n"
            + Body + "    global::T.Keys.Key);\n");
        await Assert.That(source).EndsWith(
            "        }\n\n"
            + "        /// <summary>Throws for a dependency the resolver has no registration for.</summary>\n"
            + "        /// <typeparam name=\"T\">The type of the dependency.</typeparam>\n"
            + "        /// <param name=\"typeName\">The name of the dependency's type.</param>\n"
            + "        /// <returns>Never returns.</returns>\n"
            + "        private static T ThrowNotRegistered<T>(string typeName)\n"
            + "        {\n"
            + "            throw new global::System.InvalidOperationException(\"Dependency '\" + typeName + \"' not registered with Splat resolver.\");\n"
            + "        }\n"
            + "    }\n"
            + "}\n");
    }

    /// <summary>A type with injected properties and no constructor parameters keeps its empty argument list.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task WritesPropertiesWithoutParameters()
    {
        var source = Generate(Transient(TypeA, properties: [new("P", "global::T.IP")]));

        await Assert.That(source).Contains(
            $"{Body}    () => new global::T.A()\n{Body}    {{\n");
    }

    /// <summary>A lazy singleton registers the lazy and its value, sharing one lazy numbered by its position.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task WritesLazySingleton()
    {
        var source = Generate(LazySingleton(TypeA), LazySingleton(TypeB, contract: "\"c\"", mode: "global::System.Threading.LazyThreadSafetyMode.PublicationOnly"));

        await Assert.That(source).Contains(
            Body + "var lazy0 = new global::System.Lazy<global::T.IA>(() => new global::T.A());\n"
            + Body + "resolver.Register<global::System.Lazy<global::T.IA>>(() => lazy0);\n"
            + Body + "resolver.Register<global::T.IA>(() => lazy0.Value);\n\n"
            + Body + "var lazy1 = new global::System.Lazy<global::T.IB>(() => new global::T.B(), global::System.Threading.LazyThreadSafetyMode.PublicationOnly);\n"
            + Body + "resolver.Register<global::System.Lazy<global::T.IB>>(() => lazy1, \"c\");\n"
            + Body + "resolver.Register<global::T.IB>(() => lazy1.Value, \"c\");\n");
    }

    /// <summary>A lazy singleton with dependencies writes its factory and mode as continuation arguments.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task WritesLazySingletonWithDependencies()
    {
        var registration = LazySingleton(
            TypeA,
            mode: "global::System.Threading.LazyThreadSafetyMode.None",
            parameters: [new(DependencyX, DependencyKind.Service, null)]);

        var source = Generate(registration);

        await Assert.That(source).Contains(
            Body + "var lazy0 = new global::System.Lazy<global::T.IA>(\n"
            + Body + FactoryOfA
            + Body + "        resolver.GetService<global::T.IX>() ?? ThrowNotRegistered<global::T.IX>(\"global::T.IX\")),\n"
            + Body + "    global::System.Threading.LazyThreadSafetyMode.None);\n");
    }

    /// <summary>Transients are written before lazy singletons, whatever order the calls came in.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task WritesTransientsFirst()
    {
        var source = Generate(LazySingleton("global::T.L"), Transient(TypeA));

        await Assert.That(source.IndexOf("new global::T.A()", StringComparison.Ordinal))
            .IsLessThan(source.IndexOf("new global::T.L()", StringComparison.Ordinal));
    }

    /// <summary>Writes a file for the registrations.</summary>
    /// <param name="registrations">The registrations.</param>
    /// <returns>The file.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string Generate(params RegistrationInfo[] registrations) =>
        CodeGenerator.Generate([.. registrations]);

    /// <summary>Describes a transient registration of <c>I</c> plus the type's short name.</summary>
    /// <param name="concrete">The fully qualified constructed type.</param>
    /// <param name="contract">The contract.</param>
    /// <param name="parameters">The constructor parameters.</param>
    /// <param name="properties">The injected properties.</param>
    /// <returns>The registration.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static RegistrationInfo Transient(string concrete, string? contract = null, ConstructorParameter[]? parameters = null, PropertyInjection[]? properties = null) =>
        Create(RegistrationKind.Transient, concrete, contract, null, parameters, properties);

    /// <summary>Describes a lazy singleton registration of <c>I</c> plus the type's short name.</summary>
    /// <param name="concrete">The fully qualified constructed type.</param>
    /// <param name="contract">The contract.</param>
    /// <param name="mode">The thread safety mode.</param>
    /// <param name="parameters">The constructor parameters.</param>
    /// <returns>The registration.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static RegistrationInfo LazySingleton(string concrete, string? contract = null, string? mode = null, ConstructorParameter[]? parameters = null) =>
        Create(RegistrationKind.LazySingleton, concrete, contract, mode, parameters, null);

    /// <summary>Describes a registration of <c>I</c> plus the type's short name.</summary>
    /// <param name="kind">The kind.</param>
    /// <param name="concrete">The fully qualified constructed type.</param>
    /// <param name="contract">The contract.</param>
    /// <param name="mode">The thread safety mode.</param>
    /// <param name="parameters">The constructor parameters.</param>
    /// <param name="properties">The injected properties.</param>
    /// <returns>The registration.</returns>
    private static RegistrationInfo Create(RegistrationKind kind, string concrete, string? contract, string? mode, ConstructorParameter[]? parameters, PropertyInjection[]? properties)
    {
        var dot = concrete.LastIndexOf('.');
        var service = string.Concat(concrete.AsSpan(0, dot + 1), "I", concrete.AsSpan(dot + 1));
        return new(
            kind,
            service,
            concrete,
            parameters is null ? EquatableArray<ConstructorParameter>.Empty : new(parameters),
            properties is null ? EquatableArray<PropertyInjection>.Empty : new(properties),
            contract,
            mode);
    }
}
