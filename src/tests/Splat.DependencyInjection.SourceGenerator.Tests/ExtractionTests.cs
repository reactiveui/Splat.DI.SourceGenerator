// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Splat.DependencyInjection.SourceGenerator.Models;

namespace Splat.DependencyInjection.SourceGenerator.Tests;

/// <summary>Tests which calls become registrations, and what is read from each.</summary>
public sealed class ExtractionTests
{
    /// <summary>The registration of <c>A</c> as <c>IA</c> with nothing to resolve.</summary>
    private const string RegistrationOfA = "resolver.Register<global::T.IA>(() => new global::T.A());";

    /// <summary>A type from a reference as the base of <c>C</c>, with injected, plain and otherwise attributed properties.</summary>
    private const string ReferencedBase =
        "public sealed class C : System.Collections.ObjectModel.Collection<int>, IA { [DependencyInjectionProperty] public IB P { get; internal set; } "
        + "public IB Q { get; set; } [System.ComponentModel.Description(\"r\")] public IB R { get; set; } }";

    /// <summary>A call through a using alias, a qualified name or a static import is a registration.</summary>
    /// <param name="call">The call.</param>
    /// <param name="usings">The directives the call needs.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("SR.Register<IA, A>();", "using SR = Splat.SplatRegistrations;")]
    [Arguments("Splat.SplatRegistrations.Register<IA, A>();", "")]
    [Arguments("global::Splat.SplatRegistrations.Register<IA, A>();", "")]
    [Arguments("Register<IA, A>();", "using static Splat.SplatRegistrations;")]
    public async Task RecognisesEverySpelling(string call, string usings)
    {
        var run = TestHelper.Run(Source(call, usings: usings));

        await Assert.That(run.RegistrationSource()).Contains(RegistrationOfA);
        await Assert.That(run.Errors()).IsEmpty();
    }

    /// <summary>
    /// Calls that share a marker's name but are something else - Splat's own registrations, another class's method,
    /// a class of the same name elsewhere, a call that does not bind - are not registrations.
    /// </summary>
    /// <param name="call">The call.</param>
    /// <param name="types">The declarations the call needs.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("AppLocator.CurrentMutable.Register<IA>(() => new A());", "")]
    [Arguments("AppLocator.CurrentMutable.Register<IA, A>();", "")]
    [Arguments("AppLocator.CurrentMutable.Register<IA>(delegate { return new A(); });", "")]
    [Arguments("Other.Register<IA, A>();", "")]
    [Arguments("My.SplatRegistrations.Register<IA, A>();", "namespace My { public static class SplatRegistrations { public static void Register<TI, TC>() { } } }")]
    [Arguments("SplatRegistrations.Register<IA>(\"x\", 1);", "")]
    [Arguments("SplatRegistrations.Register<IA>(1, 2, 3);", "")]
    [Arguments("SplatRegistrations.Register<IA, A, B>();", "")]
    [Arguments("SplatRegistrations.Unregister<IA, A>();", "")]
    [Arguments("SplatRegistrations.RegisterConstant<IA>(new A());", "")]
    [Arguments("SplatRegistrations.SetupIOC();", "")]
    [Arguments("new Action[] { () => { } }[0]();", "")]
    public async Task IgnoresOtherCalls(string call, string types)
    {
        var run = TestHelper.Run(Source(call, types: types));

        await Assert.That(run.RegistrationSource()).IsNull();
    }

    /// <summary>A marker's name on an extension method declared on the marker class is not a registration.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task IgnoresExtensionMethodOnMarkerClass()
    {
        const string Extension = """
            namespace Splat
            {
                internal static partial class SplatRegistrations
                {
                    public static void Register<T>(this string value) { }
                }
            }
            """;

        var run = TestHelper.Run(Source("SplatRegistrations.Register<IA>(\"x\");"), Extension);

        await Assert.That(run.RegistrationSource()).IsNull();
    }

    /// <summary>
    /// A call that binds to an extension method on the marker class, to a method of another class, or to a local
    /// function of the same name is not a registration.
    /// </summary>
    /// <param name="call">The call.</param>
    /// <param name="types">The declarations the call needs.</param>
    /// <param name="usings">The directives the call needs.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("SplatRegistrations.Register<IA>(5);", "", "")]
    [Arguments("Register<IA, A>();", "", "using static T.Other;")]
    [Arguments("Register<IA, A>(); static void Register<TI, TC>() { }", "", "")]
    public async Task IgnoresMethodsOutsideMarkerClass(string call, string types, string usings)
    {
        const string Extension = """
            namespace Splat
            {
                internal static partial class SplatRegistrations
                {
                    public static void Register<T>(this int value) { }
                }
            }
            """;

        var run = TestHelper.Run(Source(call, types, usings), Extension);

        await Assert.That(run.RegistrationSource()).IsNull();
    }

    /// <summary>A contract is written so it compiles from the generated file, whatever form the argument takes.</summary>
    /// <param name="argument">The contract argument.</param>
    /// <param name="expected">The contract the generated code passes.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("\"c\"", "\"c\"")]
    [Arguments("@\"c\"", "@\"c\"")]
    [Arguments("Keys.Field", "global::T.Keys.Field")]
    [Arguments("Keys.Property", "global::T.Keys.Property")]
    [Arguments("Keys.Get()", "global::T.Keys.Get()")]
    [Arguments("Keys.Generic<int>(1)", "global::T.Keys.Generic<int>(1)")]
    [Arguments("Local()", "global::T.Bootstrapper.Local()")]
    [Arguments("\"a\" + \"b\"", "\"a\" + \"b\"")]
    [Arguments("contract: \"c\"", "\"c\"")]
    public async Task ReadsContract(string argument, string expected)
    {
        var run = TestHelper.Run(Source($"SplatRegistrations.Register<IA, A>({argument});"));

        await Assert.That(run.RegistrationSource()).Contains($"resolver.Register<global::T.IA>(() => new global::T.A(), {expected});");
        await Assert.That(run.Errors()).IsEmpty();
    }

    /// <summary>A contract that binds to no symbol is left out, as it always has been.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DropsContractWithoutSymbol()
    {
        var run = TestHelper.Run(Source("SplatRegistrations.Register<IA, A>($\"c{1}\");"));

        await Assert.That(run.RegistrationSource()).Contains(RegistrationOfA);
    }

    /// <summary>A thread safety mode is written so it compiles from the generated file, whatever form it takes.</summary>
    /// <param name="arguments">The arguments.</param>
    /// <param name="expected">The arguments the generated lazy is created with, after its factory.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("LazyThreadSafetyMode.None", ", global::System.Threading.LazyThreadSafetyMode.None")]
    [Arguments("LazyThreadSafetyMode.PublicationOnly", ", global::System.Threading.LazyThreadSafetyMode.PublicationOnly")]
    [Arguments("LazyThreadSafetyMode.ExecutionAndPublication", ", global::System.Threading.LazyThreadSafetyMode.ExecutionAndPublication")]
    [Arguments("Keys.Mode", ", global::T.Keys.Mode")]
    [Arguments("Keys.ConstantMode", ", global::T.Keys.ConstantMode")]
    [Arguments("(LazyThreadSafetyMode)1", "")]
    [Arguments("mode: LazyThreadSafetyMode.None, contract: \"c\"", ", global::System.Threading.LazyThreadSafetyMode.None")]
    public async Task ReadsMode(string arguments, string expected)
    {
        var run = TestHelper.Run(Source($"SplatRegistrations.RegisterLazySingleton<IA, A>({arguments});"));

        await Assert.That(run.RegistrationSource()).Contains($"var lazy0 = new global::System.Lazy<global::T.IA>(() => new global::T.A(){expected});");
        await Assert.That(run.Errors()).IsEmpty();
    }

    /// <summary>A mode held in a local is copied as written.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CopiesLocalMode()
    {
        var run = TestHelper.Run(Source("var mode = LazyThreadSafetyMode.None; SplatRegistrations.RegisterLazySingleton<IA, A>(mode);"));

        await Assert.That(run.RegistrationSource()).Contains("var lazy0 = new global::System.Lazy<global::T.IA>(() => new global::T.A(), mode);");
    }

    /// <summary>An argument to a parameter that is neither the contract nor the mode is ignored.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task IgnoresOtherParameters()
    {
        const string Overload = """
            namespace Splat
            {
                internal static partial class SplatRegistrations
                {
                    public static void Register<TInterface, TConcrete>(int other) { }
                }
            }
            """;

        var run = TestHelper.Run(Source("SplatRegistrations.Register<IA, A>(1);"), Overload);

        await Assert.That(run.RegistrationSource()).Contains(RegistrationOfA);
    }

    /// <summary>A lazy, a collection, and any other type are each resolved their own way.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ResolvesEachParameterKind()
    {
        const string Types = """
            public sealed class Many : IA
            {
                public Many(Lazy<IB> lazy, IEnumerable<IB> all, List<IB> list, Dictionary<int, IB> map, IB[] array) { }
            }
            """;

        var run = TestHelper.Run(Source("SplatRegistrations.Register<IA, Many>(); SplatRegistrations.RegisterLazySingleton<IB, B>();", types: Types));
        var source = run.RegistrationSource();

        await Assert.That(source).Contains("resolver.GetService<global::System.Lazy<global::T.IB>>() ?? ");
        await Assert.That(source).Contains("resolver.GetServices<global::T.IB>(),");
        await Assert.That(source).Contains("resolver.GetService<global::System.Collections.Generic.List<global::T.IB>>() ?? ");
        await Assert.That(source).Contains("resolver.GetService<global::System.Collections.Generic.Dictionary<int, global::T.IB>>() ?? ");
        await Assert.That(source).Contains("resolver.GetService<global::T.IB[]>() ?? ");
        await Assert.That(run.Errors()).IsEmpty();
    }

    /// <summary>
    /// The constructor chosen and the properties injected for <c>C</c>, and the registrations turned away for having
    /// no usable constructor or a property that cannot be set.
    /// </summary>
    /// <param name="type">The declarations of <c>C</c> and its base.</param>
    /// <param name="expected">Text the registration contains, or <see langword="null"/> when it is turned away.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("public sealed class C : IA { public C(IB b) { } }", "new global::T.C(\n")]
    [Arguments("public struct C : IA { }", "new global::T.C()")]
    [Arguments("public sealed class C : IA { private C() { } }", null)]
    [Arguments("public sealed class C : IA { public C() { } public C(IB b) { } }", null)]
    [Arguments("public sealed class C : IA { [DependencyInjectionConstructor] public C() { } [DependencyInjectionConstructor] public C(IB b) { } }", null)]
    [Arguments("public sealed class C : IA { public C() { } [DependencyInjectionConstructor] public C(IB b) { } }", "new global::T.C(\n")]
    [Arguments("public class Base { [DependencyInjectionProperty] public IB Inherited { get; set; } } public sealed class C : Base, IA { }", "Inherited = ")]
    [Arguments(ReferencedBase, "P = ")]
    [Arguments("public sealed class C : IA { [DependencyInjectionProperty] public IB P { get; private set; } }", null)]
    [Arguments("public sealed class C : IA { [DependencyInjectionProperty] public IB P { get; } }", null)]
    public async Task ExtractsConstructorAndProperties(string type, string? expected)
    {
        var source = TestHelper.Run(Source("SplatRegistrations.Register<IA, C>();", types: type)).RegistrationSource();

        if (expected is null)
        {
            await Assert.That(source).IsNull();
        }
        else
        {
            await Assert.That(source).Contains(expected);
        }
    }

    /// <summary>A type from a reference is constructed through its only constructor, without walking its members.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ConstructsReferencedType()
    {
        var run = TestHelper.Run(Source("SplatRegistrations.Register<object>();"));

        await Assert.That(run.RegistrationSource()).Contains("resolver.Register<object>(() => new object());");
        await Assert.That(run.Errors()).IsEmpty();
    }

    /// <summary>A type from a reference with several constructors cannot mark one, so it is turned away.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task TurnsAwayReferencedTypeWithSeveralConstructors()
    {
        var run = TestHelper.Run(Source("SplatRegistrations.Register<object, System.Text.StringBuilder>();"));

        await Assert.That(run.RegistrationSource()).IsNull();
    }

    /// <summary>An interface or other type without constructors takes no constructor parameters.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReadsNoParametersFromTypeWithoutConstructors()
    {
        var compilation = TestHelper.Run(Source(string.Empty)).Output;
        var symbols = WellKnownSymbols.For(compilation);

        var extracted = MetadataExtractor.TryExtractConstructorParameters(compilation.GetTypeByMetadataName("T.IA")!, symbols, out var parameters);

        await Assert.That(extracted).IsTrue();
        await Assert.That(parameters.Length).IsEqualTo(0);
    }

    /// <summary>
    /// Without the injection attributes in the compilation, no property is injected and no constructor is marked, so a
    /// type with several constructors is turned away.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task InjectsNothingWithoutAttributes()
    {
        const string Types = """
            public sealed class C : IA { public IB P { get; set; } }
            public sealed class D : IB { public D() { } public D(IA a) { } }
            """;
        var compilation = TestHelper.CreateCompilation(Source(string.Empty, types: Types));
        var symbols = WellKnownSymbols.For(compilation);
        var typeD = compilation.GetTypeByMetadataName("T.D")!;

        var injected = MetadataExtractor.TryExtractPropertyInjections(compilation.GetTypeByMetadataName("T.C")!, symbols, out var properties);

        await Assert.That(symbols.PropertyAttribute).IsNull();
        await Assert.That(injected).IsTrue();
        await Assert.That(properties.Length).IsEqualTo(0);
        await Assert.That(MetadataExtractor.FindMarkedConstructor(typeD, typeD.GetMembers(".ctor"), symbols)).IsNull();
    }

    /// <summary>A registration naming one type uses one formatted name for both the service and the construction.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReusesNameForSingleTypeRegistration()
    {
        var compilation = TestHelper.CreateCompilation(Source("SplatRegistrations.Register<A>();"));
        var output = (CSharpCompilation)TestHelper.Run(compilation).Output;
        var invocation = await TestHelper.FindInvocationAsync(output.SyntaxTrees[0], "SplatRegistrations.");
        var model = output.GetSemanticModel(invocation.SyntaxTree);
        var method = (IMethodSymbol)model.GetSymbolInfo(invocation).Symbol!;

        var registration = MetadataExtractor.ExtractRegistration(method, invocation, model, WellKnownSymbols.For(output), CancellationToken.None);

        await Assert.That(registration!.Kind).IsEqualTo(RegistrationKind.Transient);
        await Assert.That(registration.ConcreteTypeFullName).IsSameReferenceAs(registration.InterfaceTypeFullName);
    }

    /// <summary>Builds a file that makes registrations.</summary>
    /// <param name="registrations">The statements of the registering method.</param>
    /// <param name="types">Declarations added to the namespace.</param>
    /// <param name="usings">Directives added to the file.</param>
    /// <returns>The file.</returns>
    private static string Source(string registrations, string types = "", string usings = "") => $$"""
        using System;
        using System.Collections.Generic;
        using System.Threading;
        using Splat;
        {{usings}}

        namespace T
        {
            public interface IA { }
            public interface IB { }
            public sealed class A : IA { }
            public sealed class B : IB { }

            public static class Keys
            {
                public const string Field = "field";
                public const LazyThreadSafetyMode ConstantMode = LazyThreadSafetyMode.None;
                public static readonly LazyThreadSafetyMode Mode = LazyThreadSafetyMode.None;
                public static string Property => "property";
                public static string Get() => "get";
                public static string Generic<TValue>(TValue value) => value.ToString();
            }

            public static class Other
            {
                public static void Register<TI, TC>() { }
            }

            {{types}}

            public static class Bootstrapper
            {
                public static void Register()
                {
                    {{registrations}}
                }

                internal static string Local() => "local";
            }
        }
        """;
}
