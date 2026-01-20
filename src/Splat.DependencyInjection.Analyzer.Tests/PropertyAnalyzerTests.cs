// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

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
}
