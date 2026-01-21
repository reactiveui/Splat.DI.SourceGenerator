// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using TUnit.Assertions;

namespace Splat.DependencyInjection.Analyzer.Tests;

/// <summary>
/// Tests for the constructor code fix provider that adds the DependencyInjectionConstructor attribute.
/// Validates that the code fix correctly adds the attribute to the selected constructor.
/// </summary>
public class ConstructorCodeFixProviderTests
{
    /// <summary>
    /// Tests that the provider configuration is valid.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task Provider_Configuration_IsValid()
    {
        var provider = new CodeFixes.ConstructorCodeFixProvider();

        await Assert.That(provider.FixableDiagnosticIds).Contains(Splat.DependencyInjection.SourceGenerator.DiagnosticWarnings.MultipleConstructorNeedAttribute.Id);
        await Assert.That(provider.GetFixAllProvider()).IsEqualTo(WellKnownFixAllProviders.BatchFixer);
    }

    /// <summary>
    /// Tests that the code fix adds the DependencyInjectionConstructor attribute to the first (parameterless) constructor.
    /// Verifies the code action at index 0 targets the constructor with zero parameters.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task AddAttributeToFirstConstructor_AppliesFix()
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

        const string expectedFixed = """
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

        var actualFixed = await CodeFixTestHelper.ApplyCodeFixAsync<
            Analyzers.ConstructorAnalyzer,
            CodeFixes.ConstructorCodeFixProvider>(
            code,
            codeActionIndex: 0); // Select first constructor (0 parameters)

        await Assert.That(TestUtilities.AreEquivalent(expectedFixed, actualFixed)).IsTrue();
    }

    /// <summary>
    /// Tests that the code fix adds the DependencyInjectionConstructor attribute to the second constructor.
    /// Verifies the code action at index 1 targets the constructor with one parameter.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task AddAttributeToSecondConstructor_AppliesFix()
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

        const string expectedFixed = """
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

        var actualFixed = await CodeFixTestHelper.ApplyCodeFixAsync<
            Analyzers.ConstructorAnalyzer,
            CodeFixes.ConstructorCodeFixProvider>(
            code,
            codeActionIndex: 1); // Select second constructor (1 parameter)

        await Assert.That(TestUtilities.AreEquivalent(expectedFixed, actualFixed)).IsTrue();
    }

    /// <summary>
    /// Tests that the code fix works with a class that has multiple constructors with different parameter counts.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task MultipleConstructorsWithDifferentParameterCounts_AppliesFix()
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

                    public TestClass(IService1 service1)
                    {
                    }

                    public TestClass(IService1 service1, IService2 service2)
                    {
                    }
                }

                public interface IService1 { }
                public interface IService2 { }

                public class Startup
                {
                    public void ConfigureDI()
                    {
                        Register<TestClass>();
                    }
                }
            }
            """;

        const string expectedFixed = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public class TestClass
                {
                    public TestClass()
                    {
                    }

                    public TestClass(IService1 service1)
                    {
                    }

                    [DependencyInjectionConstructor]
                    public TestClass(IService1 service1, IService2 service2)
                    {
                    }
                }

                public interface IService1 { }
                public interface IService2 { }

                public class Startup
                {
                    public void ConfigureDI()
                    {
                        Register<TestClass>();
                    }
                }
            }
            """;

        var actualFixed = await CodeFixTestHelper.ApplyCodeFixAsync<
            Analyzers.ConstructorAnalyzer,
            CodeFixes.ConstructorCodeFixProvider>(
            code,
            codeActionIndex: 2); // Select third constructor (2 parameters)

        await Assert.That(TestUtilities.AreEquivalent(expectedFixed, actualFixed)).IsTrue();
    }

    /// <summary>
    /// Tests that static constructors are ignored by the code fix provider.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task ClassWithStaticConstructor_IgnoresStaticConstructor()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public class TestClass
                {
                    static TestClass()
                    {
                    }

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

        const string expectedFixed = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public class TestClass
                {
                    static TestClass()
                    {
                    }

                    [DependencyInjectionConstructor]
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

        var actualFixed = await CodeFixTestHelper.ApplyCodeFixAsync<
            Analyzers.ConstructorAnalyzer,
            CodeFixes.ConstructorCodeFixProvider>(
            code,
            codeActionIndex: 0); // Should select first non-static constructor

        await Assert.That(TestUtilities.AreEquivalent(expectedFixed, actualFixed)).IsTrue();
    }

    /// <summary>
    /// Tests that the code fix works with structs that have multiple constructors.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task StructWithMultipleConstructors_AppliesFix()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public struct TestStruct
                {
                    public TestStruct(IService service)
                    {
                    }

                    public TestStruct(IService service, int value)
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

        const string expectedFixed = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public struct TestStruct
                {
                    [DependencyInjectionConstructor]
                    public TestStruct(IService service)
                    {
                    }

                    public TestStruct(IService service, int value)
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

        var actualFixed = await CodeFixTestHelper.ApplyCodeFixAsync<
            Analyzers.ConstructorAnalyzer,
            CodeFixes.ConstructorCodeFixProvider>(
            code,
            codeActionIndex: 0); // Select first constructor

        await Assert.That(TestUtilities.AreEquivalent(expectedFixed, actualFixed)).IsTrue();
    }

    /// <summary>
    /// Tests that the code fix adds the attribute to a constructor that already has other attributes.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task ConstructorWithExistingAttribute_AddsAttribute()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;
            using System;

            namespace Test
            {
                public class TestClass
                {
                    [Obsolete]
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

        const string expectedFixed = """
            using Splat;
            using static Splat.SplatRegistrations;
            using System;

            namespace Test
            {
                public class TestClass
                {
                    [DependencyInjectionConstructor]
                    [Obsolete]
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

        var actualFixed = await CodeFixTestHelper.ApplyCodeFixAsync<
            Analyzers.ConstructorAnalyzer,
            CodeFixes.ConstructorCodeFixProvider>(
            code,
            codeActionIndex: 0); // Select first constructor

        await Assert.That(TestUtilities.AreEquivalent(expectedFixed, actualFixed)).IsTrue();
    }

    /// <summary>
    /// Tests that the code fix works with constructors that have XML documentation.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task ConstructorWithXmlDocumentation_AppliesFix()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public class TestClass
                {
                    /// <summary>
                    /// Default constructor.
                    /// </summary>
                    public TestClass()
                    {
                    }

                    /// <summary>
                    /// Constructor with service.
                    /// </summary>
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

        const string expectedFixed = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public class TestClass
                {
                    /// <summary>
                    /// Default constructor.
                    /// </summary>
                    [DependencyInjectionConstructor]
                    public TestClass()
                    {
                    }

                    /// <summary>
                    /// Constructor with service.
                    /// </summary>
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

        var actualFixed = await CodeFixTestHelper.ApplyCodeFixAsync<
            Analyzers.ConstructorAnalyzer,
            CodeFixes.ConstructorCodeFixProvider>(
            code,
            codeActionIndex: 0); // Select first constructor

        await Assert.That(TestUtilities.AreEquivalent(expectedFixed, actualFixed)).IsTrue();
    }

    /// <summary>
    /// Tests that the code fix works with internal constructors.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task InternalConstructors_AppliesFix()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public class TestClass
                {
                    internal TestClass()
                    {
                    }

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

        const string expectedFixed = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public class TestClass
                {
                    [DependencyInjectionConstructor]
                    internal TestClass()
                    {
                    }

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

        var actualFixed = await CodeFixTestHelper.ApplyCodeFixAsync<
            Analyzers.ConstructorAnalyzer,
            CodeFixes.ConstructorCodeFixProvider>(
            code,
            codeActionIndex: 0); // Select first constructor

        await Assert.That(TestUtilities.AreEquivalent(expectedFixed, actualFixed)).IsTrue();
    }
}
