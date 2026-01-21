// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Splat.DependencyInjection.Analyzer.Tests;

/// <summary>
/// Tests for the property analyzer that validates DependencyInjectionProperty attributes.
/// Ensures properties marked for injection have accessible setters.
/// </summary>
public class PropertyAnalyzerTests
{
    /// <summary>
    /// Tests that a property with a private setter triggers diagnostic SPLATDI002.
    /// Properties marked for dependency injection must have accessible setters.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task PropertyWithPrivateSetter_ReportsDiagnostic()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public class TestClass
                {
                    [DependencyInjectionProperty]
                    public IService Service { get; private set; }
                }

                public interface IService { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<Analyzers.PropertyAnalyzer>(code);

        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("SPLATDI002");
        await Assert.That(diagnostics[0].GetMessage()).Contains("TestClass.Service");
    }

    /// <summary>
    /// Tests that a read-only property triggers diagnostic SPLATDI002.
    /// Properties marked for dependency injection must have setters.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task PropertyWithNoSetter_ReportsDiagnostic()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public class TestClass
                {
                    [DependencyInjectionProperty]
                    public IService Service { get; }
                }

                public interface IService { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<Analyzers.PropertyAnalyzer>(code);

        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("SPLATDI002");
    }

    /// <summary>
    /// Tests that a property with a public setter does not trigger diagnostics.
    /// Public setters are accessible for dependency injection.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task PropertyWithPublicSetter_NoDiagnostic()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public class TestClass
                {
                    [DependencyInjectionProperty]
                    public IService Service { get; set; }
                }

                public interface IService { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<Analyzers.PropertyAnalyzer>(code);

        await Assert.That(diagnostics.Length).IsEqualTo(0);
    }

    /// <summary>
    /// Tests that a property with an internal setter does not trigger diagnostics.
    /// Internal setters are accessible for dependency injection within the assembly.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task PropertyWithInternalSetter_NoDiagnostic()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public class TestClass
                {
                    [DependencyInjectionProperty]
                    public IService Service { get; internal set; }
                }

                public interface IService { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<Analyzers.PropertyAnalyzer>(code);

        await Assert.That(diagnostics.Length).IsEqualTo(0);
    }

    /// <summary>
    /// Tests that a property without the DependencyInjectionProperty attribute does not trigger diagnostics.
    /// Only properties explicitly marked for injection are validated.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task PropertyWithoutAttribute_NoDiagnostic()
    {
        const string code = """
            namespace Test
            {
                public class TestClass
                {
                    public IService Service { get; private set; }
                }

                public interface IService { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<Analyzers.PropertyAnalyzer>(code);

        await Assert.That(diagnostics.Length).IsEqualTo(0);
    }

    /// <summary>
    /// Tests that a property with a protected setter triggers diagnostic SPLATDI002.
    /// Protected setters are not accessible enough for dependency injection.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task PropertyWithProtectedSetter_ReportsDiagnostic()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public class TestClass
                {
                    [DependencyInjectionProperty]
                    public IService Service { get; protected set; }
                }

                public interface IService { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<Analyzers.PropertyAnalyzer>(code);

        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("SPLATDI002");
    }

    /// <summary>
    /// Tests that a property on a struct with public setter does not trigger diagnostics.
    /// Structs can also use dependency injection.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task PropertyOnStruct_WithPublicSetter_NoDiagnostic()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public struct TestStruct
                {
                    [DependencyInjectionProperty]
                    public IService Service { get; set; }
                }

                public interface IService { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<Analyzers.PropertyAnalyzer>(code);

        await Assert.That(diagnostics.Length).IsEqualTo(0);
    }

    /// <summary>
    /// Tests that a static property with DependencyInjectionProperty attribute is analyzed.
    /// Even though static properties might be unusual for DI, the analyzer still validates them.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task StaticPropertyWithPrivateSetter_ReportsDiagnostic()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public class TestClass
                {
                    [DependencyInjectionProperty]
                    public static IService Service { get; private set; }
                }

                public interface IService { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<Analyzers.PropertyAnalyzer>(code);

        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("SPLATDI002");
    }

    /// <summary>
    /// Tests that multiple properties with issues all report diagnostics.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task MultiplePropertiesWithIssues_ReportMultipleDiagnostics()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public class TestClass
                {
                    [DependencyInjectionProperty]
                    public IService Service1 { get; private set; }

                    [DependencyInjectionProperty]
                    public IService Service2 { get; }

                    [DependencyInjectionProperty]
                    public IService Service3 { get; set; }
                }

                public interface IService { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<Analyzers.PropertyAnalyzer>(code);

        await Assert.That(diagnostics.Length).IsEqualTo(2);
        await Assert.That(diagnostics.All(d => d.Id == "SPLATDI002")).IsTrue();
    }

    /// <summary>
    /// Tests that if the attribute is missing from compilation, no analysis happens.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task AttributeMissingFromCompilation_NoDiagnostic()
    {
        const string code = """
            namespace Test
            {
                public class TestClass
                {
                    // Attribute is used but not defined in metadata
                    [DependencyInjectionProperty]
                    public IService Service { get; private set; }
                }
                public interface IService { }
            }
            """;

        // Create compilation WITHOUT adding Splat attributes
        var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("TestAssembly", [syntaxTree])
            .AddReferences(Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var compilationWithAnalyzers = compilation.WithAnalyzers(
            System.Collections.Immutable.ImmutableArray.Create<Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer>(
                new Analyzers.PropertyAnalyzer()));

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        // Should be empty because Initialize checks for attribute existence
        await Assert.That(diagnostics).IsEmpty();
    }

    /// <summary>
    /// Tests that Initialize throws ArgumentNullException when context is null.
    /// </summary>
    [Test]
    public void Initialize_NullContext_ThrowsArgumentNullException()
    {
        var analyzer = new Analyzers.PropertyAnalyzer();
        Assert.Throws<ArgumentNullException>(() => analyzer.Initialize(null!));
    }

    /// <summary>
    /// Tests that a property with a different attribute does not trigger diagnostics.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task PropertyWithOtherAttribute_NoDiagnostic()
    {
        const string code = """
            using System;
            using Splat;

            namespace Test
            {
                public class TestClass
                {
                    [Obsolete]
                    public IService Service { get; private set; }
                }

                public interface IService { }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync<Analyzers.PropertyAnalyzer>(code);

        await Assert.That(diagnostics).IsEmpty();
    }
}
