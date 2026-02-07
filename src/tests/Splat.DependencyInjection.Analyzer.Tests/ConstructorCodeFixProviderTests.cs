// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

    /// <summary>
    /// Tests that ConstructorCodeFixProvider works when constructor has no leading trivia.
    /// This hits the specific branch in AddAttributeAsync.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task ConstructorCodeFix_NoLeadingTrivia()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;
            namespace Test {
                public class TestClass {
                    public TestClass() {}
                    public TestClass(int i) {}
                }
                public class Startup {
                    public void Configure() {
                        Register<TestClass>();
                    }
                }
            }
            """;

        var fixedCode = await CodeFixTestHelper.ApplyCodeFixAsync<
            Analyzers.ConstructorAnalyzer,
            CodeFixes.ConstructorCodeFixProvider>(code);

        await Assert.That(fixedCode).Contains("[DependencyInjectionConstructor]");
    }

    /// <summary>
    /// Tests that ConstructorCodeFixProvider works when constructor has existing attributes but no leading trivia.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task ConstructorCodeFix_ExistingAttribute_NoLeadingTrivia()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;
            using System;
            namespace Test {
                public class TestClass {
                    [Obsolete]public TestClass() {}
                    public TestClass(int i) {}
                }
                public class Startup {
                    public void Configure() {
                        Register<TestClass>();
                    }
                }
            }
            """;

        var fixedCode = await CodeFixTestHelper.ApplyCodeFixAsync<
            Analyzers.ConstructorAnalyzer,
            CodeFixes.ConstructorCodeFixProvider>(code);

        await Assert.That(fixedCode).Contains("[DependencyInjectionConstructor]");
        await Assert.That(fixedCode).Contains("[Obsolete]public TestClass()");
    }

    /// <summary>
    /// Tests that the code fix works correctly for a nested class.
    /// This validates the ancestor walk logic in RegisterCodeFixesAsync.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task NestedClass_AppliesFix()
    {
        const string code = """
            using Splat;
            using static Splat.SplatRegistrations;

            namespace Test
            {
                public class Outer
                {
                    public class Inner
                    {
                        public Inner() {}
                        public Inner(IService service) {}
                    }
                }

                public interface IService { }

                public class Startup
                {
                    public void ConfigureDI()
                    {
                        Register<Outer.Inner>();
                    }
                }
            }
            """;

        var fixedCode = await CodeFixTestHelper.ApplyCodeFixAsync<
            Analyzers.ConstructorAnalyzer,
            CodeFixes.ConstructorCodeFixProvider>(code);

        await Assert.That(fixedCode).Contains("[DependencyInjectionConstructor]");
    }

    /// <summary>
    /// Tests AddAttributeAsync directly on a constructor with leading trivia (XML docs).
    /// Validates the trivia is preserved and moved to the attribute list.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task AddAttributeAsync_ConstructorWithLeadingTrivia_PreservesTrivia()
    {
        const string source = """
            public class Foo
            {
                /// <summary>My ctor.</summary>
                public Foo() { }
            }
            """;

        var document = CreateSimpleDocument(source);
        var root = await document.GetSyntaxRootAsync();
        var constructor = root!.DescendantNodes().OfType<ConstructorDeclarationSyntax>().First();

        var result = await CodeFixes.ConstructorCodeFixProvider.AddAttributeAsync(document, constructor, CancellationToken.None);
        var resultText = (await result.GetTextAsync()).ToString();

        await Assert.That(resultText).Contains("[DependencyInjectionConstructor]");
        await Assert.That(resultText).Contains("/// <summary>My ctor.</summary>");
    }

    /// <summary>
    /// Tests AddAttributeAsync directly on a constructor with no leading trivia.
    /// Validates the attribute is added without trivia issues.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task AddAttributeAsync_ConstructorWithNoTrivia_AddsAttribute()
    {
        const string source = "public class Foo { public Foo() { } }";

        var document = CreateSimpleDocument(source);
        var root = await document.GetSyntaxRootAsync();
        var constructor = root!.DescendantNodes().OfType<ConstructorDeclarationSyntax>().First();

        var result = await CodeFixes.ConstructorCodeFixProvider.AddAttributeAsync(document, constructor, CancellationToken.None);
        var resultText = (await result.GetTextAsync()).ToString();

        await Assert.That(resultText).Contains("[DependencyInjectionConstructor]");
    }

    /// <summary>
    /// Tests AddAttributeAsync on a constructor that already has an existing attribute.
    /// Validates the new attribute is prepended to the existing attribute list.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task AddAttributeAsync_ConstructorWithExistingAttribute_PrependsAttribute()
    {
        const string source = """
            using System;

            public class Foo
            {
                [Obsolete]
                public Foo() { }
            }
            """;

        var document = CreateSimpleDocument(source);
        var root = await document.GetSyntaxRootAsync();
        var constructor = root!.DescendantNodes().OfType<ConstructorDeclarationSyntax>().First();

        var result = await CodeFixes.ConstructorCodeFixProvider.AddAttributeAsync(document, constructor, CancellationToken.None);
        var resultText = (await result.GetTextAsync()).ToString();

        await Assert.That(resultText).Contains("[DependencyInjectionConstructor]");
        await Assert.That(resultText).Contains("[Obsolete]");
    }

    /// <summary>
    /// Creates a simple Document from source code for direct unit testing.
    /// </summary>
    /// <param name="source">The C# source code.</param>
    /// <returns>A Document containing the source.</returns>
    private static Document CreateSimpleDocument(string source)
    {
        var projectId = ProjectId.CreateNewId("TestProject");
        var solution = new AdhocWorkspace()
            .CurrentSolution
            .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
            .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var systemRuntime = System.AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "System.Runtime");
        if (systemRuntime != null)
        {
            solution = solution.AddMetadataReference(projectId, MetadataReference.CreateFromFile(systemRuntime.Location));
        }

        return solution.GetProject(projectId)!
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddDocument("Test.cs", source);
    }
}