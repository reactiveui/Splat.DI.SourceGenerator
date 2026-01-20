// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Splat.DependencyInjection.Analyzer.Tests;

/// <summary>
/// Tests for the constructor analyzer that validates dependency injection constructor selection.
/// Ensures classes with multiple constructors properly indicate which one should be used for DI.
/// </summary>
public class ConstructorAnalyzerTests
{
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
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<Analyzers.ConstructorAnalyzer>(code);

        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("SPLATDI004");
    }
}
