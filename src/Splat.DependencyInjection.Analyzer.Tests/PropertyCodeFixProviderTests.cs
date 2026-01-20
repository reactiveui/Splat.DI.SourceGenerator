// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Splat.DependencyInjection.Analyzer.Tests;

/// <summary>
/// Tests for the property code fix provider that fixes property setter accessibility.
/// Validates that properties marked with DependencyInjectionProperty get public or internal setters.
/// </summary>
public class PropertyCodeFixProviderTests
{
    /// <summary>
    /// Tests that the code fix changes a private setter to public.
    /// Verifies the first code action (index 0) makes the setter public.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task AddPublicSetter_AppliesFix()
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

        const string expectedFixed = """
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

        var actualFixed = await CodeFixTestHelper.ApplyCodeFixAsync<
            Analyzers.PropertyAnalyzer,
            CodeFixes.PropertyCodeFixProvider>(
            code,
            codeActionIndex: 0); // "Make setter public"

        await Assert.That(TestUtilities.AreEquivalent(expectedFixed, actualFixed)).IsTrue();
    }

    /// <summary>
    /// Tests that the code fix changes a private setter to internal.
    /// Verifies the second code action (index 1) makes the setter internal.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task AddInternalSetter_AppliesFix()
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

        const string expectedFixed = """
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

        var actualFixed = await CodeFixTestHelper.ApplyCodeFixAsync<
            Analyzers.PropertyAnalyzer,
            CodeFixes.PropertyCodeFixProvider>(
            code,
            codeActionIndex: 1); // "Make setter internal"

        await Assert.That(TestUtilities.AreEquivalent(expectedFixed, actualFixed)).IsTrue();
    }

    /// <summary>
    /// Tests that the code fix adds a public setter to a property that has no setter.
    /// Verifies the code fix can handle read-only properties by adding a setter.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task PropertyWithNoSetter_AddPublicSetter()
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

        const string expectedFixed = """
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

        var actualFixed = await CodeFixTestHelper.ApplyCodeFixAsync<
            Analyzers.PropertyAnalyzer,
            CodeFixes.PropertyCodeFixProvider>(
            code,
            codeActionIndex: 0); // "Make setter public" (or "Add public setter")

        await Assert.That(TestUtilities.AreEquivalent(expectedFixed, actualFixed)).IsTrue();
    }

    /// <summary>
    /// Tests that the code fix adds a setter to an expression-bodied property.
    /// Verifies the code fix can handle expression-bodied properties by converting them to standard properties with getter and setter.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task ExpressionBodiedProperty_AddPublicSetter()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public class TestClass
                {
                    private IService _service;

                    [DependencyInjectionProperty]
                    public IService Service => _service;
                }

                public interface IService { }
            }
            """;

        const string expectedFixed = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public class TestClass
                {
                    private IService _service;

                    [DependencyInjectionProperty]
                    public IService Service { get => _service; set; }
                }

                public interface IService { }
            }
            """;

        var actualFixed = await CodeFixTestHelper.ApplyCodeFixAsync<
            Analyzers.PropertyAnalyzer,
            CodeFixes.PropertyCodeFixProvider>(
            code,
            codeActionIndex: 0); // "Add public setter"

        await Assert.That(TestUtilities.AreEquivalent(expectedFixed, actualFixed)).IsTrue();
    }

    /// <summary>
    /// Tests that the code fix adds an internal setter to an expression-bodied property.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task ExpressionBodiedProperty_AddInternalSetter()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public class TestClass
                {
                    private IService _service;

                    [DependencyInjectionProperty]
                    public IService Service => _service;
                }

                public interface IService { }
            }
            """;

        const string expectedFixed = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public class TestClass
                {
                    private IService _service;

                    [DependencyInjectionProperty]
                    public IService Service { get => _service; internal set; }
                }

                public interface IService { }
            }
            """;

        var actualFixed = await CodeFixTestHelper.ApplyCodeFixAsync<
            Analyzers.PropertyAnalyzer,
            CodeFixes.PropertyCodeFixProvider>(
            code,
            codeActionIndex: 1); // "Add internal setter"

        await Assert.That(TestUtilities.AreEquivalent(expectedFixed, actualFixed)).IsTrue();
    }

    /// <summary>
    /// Tests that the code fix adds a setter to a property with internal modifier where setter doesn't need explicit modifier.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task InternalPropertyWithNoSetter_AddSetterWithoutModifier()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public class TestClass
                {
                    [DependencyInjectionProperty]
                    internal IService Service { get; }
                }

                public interface IService { }
            }
            """;

        const string expectedFixed = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public class TestClass
                {
                    [DependencyInjectionProperty]
                    internal IService Service { get; set; }
                }

                public interface IService { }
            }
            """;

        var actualFixed = await CodeFixTestHelper.ApplyCodeFixAsync<
            Analyzers.PropertyAnalyzer,
            CodeFixes.PropertyCodeFixProvider>(
            code,
            codeActionIndex: 1); // "Add internal setter"

        await Assert.That(TestUtilities.AreEquivalent(expectedFixed, actualFixed)).IsTrue();
    }

    /// <summary>
    /// Tests that the code fix adds an internal setter to a read-only property.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task PropertyWithNoSetter_AddInternalSetter()
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

        const string expectedFixed = """
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

        var actualFixed = await CodeFixTestHelper.ApplyCodeFixAsync<
            Analyzers.PropertyAnalyzer,
            CodeFixes.PropertyCodeFixProvider>(
            code,
            codeActionIndex: 1); // "Add internal setter"

        await Assert.That(TestUtilities.AreEquivalent(expectedFixed, actualFixed)).IsTrue();
    }
}
