// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Splat.DependencyInjection.Analyzer.Tests;

/// <summary>
/// Tests for the constructor analyzer that validates dependency injection constructor selection.
/// Ensures classes with multiple constructors properly indicate which one should be used for DI.
/// </summary>
public class ConstructorAnalyzerTests
{
    /// <summary>
    /// Tests that Initialize throws ArgumentNullException when passed a null context.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task Initialize_NullContext_ThrowsArgumentNullException()
    {
        var analyzer = new Analyzers.ConstructorAnalyzer();

        await Assert.That(() => analyzer.Initialize(null!)).ThrowsExactly<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that Register with zero type arguments doesn't trigger analysis.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task RegisterWithZeroTypeArguments_NoDiagnostic()
    {
        // This is a bit of a trick because the compiler won't let you call a generic method without args usually,
        // but we can have a non-generic method with the same name.
        const string code = """
            using Splat;
            namespace Splat {
                public static class SplatRegistrations {
                    public static void Register() {}
                }
            }
            public class Startup {
                public void Configure() {
                    Splat.SplatRegistrations.Register();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<Analyzers.ConstructorAnalyzer>(code);
        await Assert.That(diagnostics.Length).IsEqualTo(0);
    }

    /// <summary>
    /// Tests that Register with three type arguments doesn't trigger analysis.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task RegisterWithThreeTypeArguments_NoDiagnostic()
    {
        const string code = """
            using Splat;
            namespace Splat {
                public static class SplatRegistrations {
                    public static void Register<T1, T2, T3>() {}
                }
            }
            public class Startup {
                public void Configure() {
                    Splat.SplatRegistrations.Register<int, int, int>();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<Analyzers.ConstructorAnalyzer>(code);
        await Assert.That(diagnostics.Length).IsEqualTo(0);
    }

    /// <summary>
    /// Tests that multiple constructors without an attribute triggers diagnostic SPLATDI001.
    /// When a class has multiple constructors, one must be marked for dependency injection.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task MultipleConstructorsWithoutAttribute_ReportsDiagnostic()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public class TestClass
                {
                    public TestClass()
                    {
                    }

                    public TestClass(IService service)
                    {
                    }
                }

                public interface IService { }

                public class Startup
                {
                    public void ConfigureDI()
                    {
                        Register<TestClass>();
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<Analyzers.ConstructorAnalyzer>(code);

        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("SPLATDI001");
        await Assert.That(diagnostics[0].GetMessage()).Contains("TestClass");
    }

    /// <summary>
    /// Tests that multiple constructors with exactly one marked does not trigger diagnostics.
    /// One properly marked constructor satisfies dependency injection requirements.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task MultipleConstructorsWithOneAttribute_NoDiagnostic()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public class TestClass
                {
                    public TestClass()
                    {
                    }

                    [DependencyInjectionConstructor]
                    public TestClass(IService service)
                    {
                    }
                }

                public interface IService { }

                public class Startup
                {
                    public void ConfigureDI()
                    {
                        Register<TestClass>();
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<Analyzers.ConstructorAnalyzer>(code);

        await Assert.That(diagnostics.Length).IsEqualTo(0);
    }

    /// <summary>
    /// Tests that multiple constructors with multiple attributes triggers diagnostic SPLATDI003.
    /// Only one constructor should be marked for dependency injection.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task MultipleConstructorsWithMultipleAttributes_ReportsDiagnostic()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public class TestClass
                {
                    [DependencyInjectionConstructor]
                    public TestClass()
                    {
                    }

                    [DependencyInjectionConstructor]
                    public TestClass(IService service)
                    {
                    }
                }

                public interface IService { }

                public class Startup
                {
                    public void ConfigureDI()
                    {
                        Register<TestClass>();
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<Analyzers.ConstructorAnalyzer>(code);

        await Assert.That(diagnostics.Length).IsEqualTo(2);
        await Assert.That(diagnostics[0].Id).IsEqualTo("SPLATDI003");
        await Assert.That(diagnostics[1].Id).IsEqualTo("SPLATDI003");
    }

    /// <summary>
    /// Tests that a class with a single constructor does not trigger diagnostics.
    /// No ambiguity exists when there is only one constructor option.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task SingleConstructor_NoDiagnostic()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public class TestClass
                {
                    public TestClass(IService service)
                    {
                    }
                }

                public interface IService { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<Analyzers.ConstructorAnalyzer>(code);

        await Assert.That(diagnostics.Length).IsEqualTo(0);
    }

    /// <summary>
    /// Tests that a private constructor marked for DI triggers diagnostic SPLATDI004.
    /// Constructors marked for dependency injection must be publicly accessible.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task MarkedConstructorWithPrivateAccessibility_ReportsDiagnostic()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public class TestClass
                {
                    public TestClass()
                    {
                    }

                    [DependencyInjectionConstructor]
                    private TestClass(IService service)
                    {
                    }
                }

                public interface IService { }

                public class Startup
                {
                    public void ConfigureDI()
                    {
                        Register<TestClass>();
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<Analyzers.ConstructorAnalyzer>(code);

        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("SPLATDI004");
    }

    /// <summary>
    /// Tests that interfaces are not analyzed by the constructor analyzer.
    /// Interfaces cannot have constructors, so they should be skipped.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task Interface_NoDiagnostic()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public interface IService
                {
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<Analyzers.ConstructorAnalyzer>(code);

        await Assert.That(diagnostics.Length).IsEqualTo(0);
    }

    /// <summary>
    /// Tests that structs with multiple constructors triggers diagnostic SPLATDI001.
    /// Structs are also supported by dependency injection and should follow the same rules.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task StructWithMultipleConstructors_ReportsDiagnostic()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public struct TestStruct
                {
                    public TestStruct(int value)
                    {
                    }

                    public TestStruct(IService service)
                    {
                    }
                }

                public interface IService { }

                public class Startup
                {
                    public void ConfigureDI()
                    {
                        Register<TestStruct>();
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<Analyzers.ConstructorAnalyzer>(code);

        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("SPLATDI001");
    }

    /// <summary>
    /// Tests that non-Splat Register methods do not trigger diagnostics (Bug #1/3 fix).
    /// Only SplatRegistrations.Register should be analyzed, not other Register methods.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task NonSplatRegisterMethod_NoDiagnostic()
    {
        const string code = """
            using Splat;

            namespace Test
            {
                public class DialogManager
                {
                    public static void Register<TView, TContext>()
                    {
                    }
                }

                public class MyClass
                {
                    public MyClass()
                    {
                    }

                    public MyClass(IService service)
                    {
                    }
                }

                public interface IService { }

                public class TestSetup
                {
                    public void Setup()
                    {
                        // This should NOT trigger analyzer - it's not Splat.SplatRegistrations
                        DialogManager.Register<MyClass, string>();
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<Analyzers.ConstructorAnalyzer>(code);

        await Assert.That(diagnostics.Length).IsEqualTo(0);
    }

    /// <summary>
    /// Tests that classes with multiple constructors but not used in DI don't trigger diagnostics (Bug #1/3 fix).
    /// Only types actually registered with Splat should be analyzed.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task ClassNotUsedInDI_NoDiagnostic()
    {
        const string code = """
            using Splat;

            namespace Test
            {
                public class NotRegistered
                {
                    public NotRegistered()
                    {
                    }

                    public NotRegistered(IService service)
                    {
                    }
                }

                public interface IService { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<Analyzers.ConstructorAnalyzer>(code);

        await Assert.That(diagnostics.Length).IsEqualTo(0);
    }

    /// <summary>
    /// Tests that Splat registrations with multiple constructors still report diagnostics.
    /// Verifies the fix doesn't break detection of actual Splat DI issues.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task SplatRegistrationWithMultipleConstructors_ReportsDiagnostic()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public class MyService
                {
                    public MyService()
                    {
                    }

                    public MyService(ILogger logger)
                    {
                    }
                }

                public interface ILogger { }

                public class Startup
                {
                    public void ConfigureDI()
                    {
                        Register<MyService>();
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<Analyzers.ConstructorAnalyzer>(code);

        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("SPLATDI001");
        await Assert.That(diagnostics[0].GetMessage()).Contains("MyService");
    }

    /// <summary>
    /// Tests that RegisterLazySingleton with multiple constructors reports diagnostics.
    /// Verifies lazy singleton registrations are also analyzed correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task RegisterLazySingletonWithMultipleConstructors_ReportsDiagnostic()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public class MyService
                {
                    public MyService()
                    {
                    }

                    public MyService(ILogger logger)
                    {
                    }
                }

                public interface ILogger { }

                public class Startup
                {
                    public void ConfigureDI()
                    {
                        RegisterLazySingleton<MyService>();
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<Analyzers.ConstructorAnalyzer>(code);

        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("SPLATDI001");
        await Assert.That(diagnostics[0].GetMessage()).Contains("MyService");
    }

    /// <summary>
    /// Tests that RegisterConstant with multiple constructors does NOT report diagnostics.
    /// RegisterConstant takes a pre-instantiated object, so constructor analysis is not needed.
    /// This is a regression test for GitHub issue #292.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task RegisterConstantWithMultipleConstructors_NoDiagnostic()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public class MyService
                {
                    public MyService()
                    {
                    }

                    public MyService(ILogger logger)
                    {
                    }
                }

                public interface ILogger { }

                public class Startup
                {
                    public void ConfigureDI()
                    {
                        var instance = new MyService();
                        RegisterConstant<MyService>(instance);
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<Analyzers.ConstructorAnalyzer>(code);

        // RegisterConstant should NOT trigger constructor analysis - the instance is already created
        await Assert.That(diagnostics.Length).IsEqualTo(0);
    }

    /// <summary>
    /// Tests that RegisterConstant with an external library type having multiple constructors does NOT report diagnostics.
    /// This is a regression test for GitHub issue #292 (Jot.Tracker scenario).
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task RegisterConstantWithExternalTypeMultipleConstructors_NoDiagnostic()
    {
        // Simulates the Jot.Tracker scenario from issue #292
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace ExternalLibrary
            {
                // Simulates Jot.Tracker which has multiple constructors
                public class Tracker
                {
                    public Tracker()
                    {
                    }

                    public Tracker(IStore store)
                    {
                    }
                }

                public interface IStore { }
            }

            namespace Test
            {
                using ExternalLibrary;

                public class Startup
                {
                    public void ConfigureDI()
                    {
                        // User creates instance themselves - no DI constructor needed
                        var tracker = new Tracker();
                        RegisterConstant(tracker);
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<Analyzers.ConstructorAnalyzer>(code);

        // RegisterConstant should NOT trigger constructor analysis - the instance is already created
        await Assert.That(diagnostics.Length).IsEqualTo(0);
    }

    /// <summary>
    /// Tests that enum types are not analyzed by the constructor analyzer.
    /// Enums cannot have user-defined constructors, so they should be skipped.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task EnumType_NoDiagnostic()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public enum MyEnum
                {
                    Value1,
                    Value2
                }

                public class Startup
                {
                    public void ConfigureDI()
                    {
                        RegisterConstant<MyEnum>(MyEnum.Value1);
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<Analyzers.ConstructorAnalyzer>(code);

        await Assert.That(diagnostics.Length).IsEqualTo(0);
    }

    /// <summary>
    /// Tests that delegate types are not analyzed by the constructor analyzer.
    /// Delegates have compiler-generated constructors that should not be analyzed.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task DelegateType_NoDiagnostic()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public delegate void MyDelegate(string message);

                public class Startup
                {
                    public void ConfigureDI()
                    {
                        MyDelegate del = (msg) => { };
                        RegisterConstant<MyDelegate>(del);
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<Analyzers.ConstructorAnalyzer>(code);

        await Assert.That(diagnostics.Length).IsEqualTo(0);
    }

    /// <summary>
    /// Tests that internal constructor marked for DI does not trigger diagnostics.
    /// Internal constructors are accessible for dependency injection.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task MarkedConstructorWithInternalAccessibility_NoDiagnostic()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public class TestClass
                {
                    public TestClass()
                    {
                    }

                    [DependencyInjectionConstructor]
                    internal TestClass(IService service)
                    {
                    }
                }

                public interface IService { }

                public class Startup
                {
                    public void ConfigureDI()
                    {
                        Register<TestClass>();
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<Analyzers.ConstructorAnalyzer>(code);

        await Assert.That(diagnostics.Length).IsEqualTo(0);
    }

    /// <summary>
    /// Tests that protected constructor marked for DI triggers diagnostic SPLATDI004.
    /// Protected constructors are not accessible for dependency injection.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task MarkedConstructorWithProtectedAccessibility_ReportsDiagnostic()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public class TestClass
                {
                    public TestClass()
                    {
                    }

                    [DependencyInjectionConstructor]
                    protected TestClass(IService service)
                    {
                    }
                }

                public interface IService { }

                public class Startup
                {
                    public void ConfigureDI()
                    {
                        Register<TestClass>();
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<Analyzers.ConstructorAnalyzer>(code);

        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("SPLATDI004");
    }

    /// <summary>
    /// Tests that two-type-argument Register call with multiple constructors reports diagnostics.
    /// Verifies Register&lt;TInterface, TImplementation&gt; is analyzed correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task RegisterTwoTypeArgumentsWithMultipleConstructors_ReportsDiagnostic()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public interface IMyService
                {
                }

                public class MyService : IMyService
                {
                    public MyService()
                    {
                    }

                    public MyService(ILogger logger)
                    {
                    }
                }

                public interface ILogger { }

                public class Startup
                {
                    public void ConfigureDI()
                    {
                        Register<IMyService, MyService>();
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<Analyzers.ConstructorAnalyzer>(code);

        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("SPLATDI001");
        await Assert.That(diagnostics[0].GetMessage()).Contains("MyService");
    }

    /// <summary>
    /// Tests that two-type-argument RegisterLazySingleton call with multiple constructors reports diagnostics.
    /// Verifies RegisterLazySingleton&lt;TInterface, TImplementation&gt; is analyzed correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task RegisterLazySingletonTwoTypeArgumentsWithMultipleConstructors_ReportsDiagnostic()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public interface IMyService
                {
                }

                public class MyService : IMyService
                {
                    public MyService()
                    {
                    }

                    public MyService(ILogger logger)
                    {
                    }
                }

                public interface ILogger { }

                public class Startup
                {
                    public void ConfigureDI()
                    {
                        RegisterLazySingleton<IMyService, MyService>();
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<Analyzers.ConstructorAnalyzer>(code);

        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("SPLATDI001");
        await Assert.That(diagnostics[0].GetMessage()).Contains("MyService");
    }
}
