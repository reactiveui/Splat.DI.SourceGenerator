// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CodeFixes;

using TUnit.Assertions;

namespace Splat.DependencyInjection.Analyzer.Tests;

/// <summary>
/// Tests for the constructor code fix provider that adds the DependencyInjectionConstructor attribute.
/// Validates that the code fix correctly adds the attribute to the selected constructor.
/// </summary>
public class ConstructorCodeFixProviderTests
{
    /// <summary>The attribute text the code fix inserts.</summary>
    private const string ConstructorAttributeText = "[DependencyInjectionConstructor]";

    /// <summary>Source with a parameterless constructor and a single-parameter constructor.</summary>
    private const string TwoConstructorsSource = """
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

    /// <summary>Source with constructors taking zero, one and two parameters.</summary>
    private const string ThreeConstructorsSource = """
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

    /// <summary>Source with a static constructor alongside two instance constructors.</summary>
    private const string StaticConstructorSource = """
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

    /// <summary>Source with a struct that has two constructors.</summary>
    private const string StructSource = """
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

    /// <summary>Source whose first constructor already has an [Obsolete] attribute.</summary>
    private const string ObsoleteConstructorSource = """
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

    /// <summary>Source whose constructors have XML documentation.</summary>
    private const string XmlDocumentedConstructorsSource = """
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

    /// <summary>Source with two internal constructors.</summary>
    private const string InternalConstructorsSource = """
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

    /// <summary>Tests that the provider configuration is valid.</summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task Provider_Configuration_IsValid()
    {
        var provider = CodeFixTestHelper.GetExportedCodeFixProvider<CodeFixes.ConstructorCodeFixProvider>();

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

        // Select first constructor (0 parameters)
        await AssertFixAppliedAsync(TwoConstructorsSource, expectedFixed, codeActionIndex: 0);
    }

    /// <summary>
    /// Tests that the code fix adds the DependencyInjectionConstructor attribute to the second constructor.
    /// Verifies the code action at index 1 targets the constructor with one parameter.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task AddAttributeToSecondConstructor_AppliesFix()
    {
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

        // Select second constructor (1 parameter)
        await AssertFixAppliedAsync(TwoConstructorsSource, expectedFixed, codeActionIndex: 1);
    }

    /// <summary>Tests that the code fix works with a class that has multiple constructors with different parameter counts.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task MultipleConstructorsWithDifferentParameterCounts_AppliesFix()
    {
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

        // Select third constructor (2 parameters)
        await AssertFixAppliedAsync(ThreeConstructorsSource, expectedFixed, codeActionIndex: 2);
    }

    /// <summary>Tests that static constructors are ignored by the code fix provider.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task ClassWithStaticConstructor_IgnoresStaticConstructor()
    {
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

        // Should select first non-static constructor
        await AssertFixAppliedAsync(StaticConstructorSource, expectedFixed, codeActionIndex: 0);
    }

    /// <summary>Tests that the code fix works with structs that have multiple constructors.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task StructWithMultipleConstructors_AppliesFix()
    {
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

        // Select first constructor
        await AssertFixAppliedAsync(StructSource, expectedFixed, codeActionIndex: 0);
    }

    /// <summary>Tests that the code fix adds the attribute to a constructor that already has other attributes.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task ConstructorWithExistingAttribute_AddsAttribute()
    {
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

        // Select first constructor
        await AssertFixAppliedAsync(ObsoleteConstructorSource, expectedFixed, codeActionIndex: 0);
    }

    /// <summary>Tests that the code fix works with constructors that have XML documentation.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task ConstructorWithXmlDocumentation_AppliesFix()
    {
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

        // Select first constructor
        await AssertFixAppliedAsync(XmlDocumentedConstructorsSource, expectedFixed, codeActionIndex: 0);
    }

    /// <summary>Tests that the code fix works with internal constructors.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task InternalConstructors_AppliesFix()
    {
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

        // Select first constructor
        await AssertFixAppliedAsync(InternalConstructorsSource, expectedFixed, codeActionIndex: 0);
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

        await Assert.That(fixedCode).Contains(ConstructorAttributeText);
    }

    /// <summary>Tests that ConstructorCodeFixProvider works when constructor has existing attributes but no leading trivia.</summary>
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

        await Assert.That(fixedCode).Contains(ConstructorAttributeText);
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

        await Assert.That(fixedCode).Contains(ConstructorAttributeText);
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

        var (document, workspace) = CreateSimpleDocument(source);
        using var disposableWorkspace = workspace;
        var root = await document.GetSyntaxRootAsync();
        var constructor = TestUtilities.FirstDescendant<ConstructorDeclarationSyntax>(root!);

        var result = await CodeFixes.ConstructorCodeFixProvider.AddAttributeAsync(document, constructor, CancellationToken.None);
        var resultText = (await result.GetTextAsync()).ToString();

        await Assert.That(resultText).Contains(ConstructorAttributeText);
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

        var (document, workspace) = CreateSimpleDocument(source);
        using var disposableWorkspace = workspace;
        var root = await document.GetSyntaxRootAsync();
        var constructor = TestUtilities.FirstDescendant<ConstructorDeclarationSyntax>(root!);

        var result = await CodeFixes.ConstructorCodeFixProvider.AddAttributeAsync(document, constructor, CancellationToken.None);
        var resultText = (await result.GetTextAsync()).ToString();

        await Assert.That(resultText).Contains(ConstructorAttributeText);
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

        var (document, workspace) = CreateSimpleDocument(source);
        using var disposableWorkspace = workspace;
        var root = await document.GetSyntaxRootAsync();
        var constructor = TestUtilities.FirstDescendant<ConstructorDeclarationSyntax>(root!);

        var result = await CodeFixes.ConstructorCodeFixProvider.AddAttributeAsync(document, constructor, CancellationToken.None);
        var resultText = (await result.GetTextAsync()).ToString();

        await Assert.That(resultText).Contains(ConstructorAttributeText);
        await Assert.That(resultText).Contains("[Obsolete]");
    }

    /// <summary>Tests that no code fix is offered when the diagnostic points outside any type declaration.</summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task DiagnosticOutsideTypeDeclaration_RegistersNoFix()
    {
        var codeActions = await CodeFixTestHelper.RegisterCodeFixesAtAsync<CodeFixes.ConstructorCodeFixProvider>(
            TwoConstructorsSource,
            0,
            Splat.DependencyInjection.SourceGenerator.DiagnosticWarnings.MultipleConstructorNeedAttribute);

        await Assert.That(codeActions).IsEmpty();
    }

    /// <summary>Applies the constructor code fix and asserts the result matches the expected source.</summary>
    /// <param name="code">The source code containing the diagnostic.</param>
    /// <param name="expectedFixed">The expected source after the fix is applied.</param>
    /// <param name="codeActionIndex">The zero-based index of the code action to apply.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private static async Task AssertFixAppliedAsync(string code, string expectedFixed, int codeActionIndex)
    {
        var actualFixed = await CodeFixTestHelper.ApplyCodeFixAsync<
            Analyzers.ConstructorAnalyzer,
            CodeFixes.ConstructorCodeFixProvider>(
            code,
            codeActionIndex);

        await Assert.That(TestUtilities.AreEquivalent(expectedFixed, actualFixed)).IsTrue();
    }

    /// <summary>
    /// Creates a simple Document from source code for direct unit testing.
    /// The caller is responsible for disposing the returned workspace.
    /// </summary>
    /// <param name="source">The C# source code.</param>
    /// <returns>A tuple containing the Document and the AdhocWorkspace that must be disposed by the caller.</returns>
    private static (Document Document, AdhocWorkspace Workspace) CreateSimpleDocument(string source)
    {
        const string projectName = "TestProject";
        var projectId = ProjectId.CreateNewId(projectName);
        var workspace = new AdhocWorkspace();
        var solution = workspace.CurrentSolution
            .AddProject(projectId, projectName, projectName, LanguageNames.CSharp)
            .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var systemRuntime = TestUtilities.FindSystemRuntimeAssembly();
        if (systemRuntime is not null)
        {
            solution = solution.AddMetadataReference(projectId, MetadataReference.CreateFromFile(systemRuntime.Location));
        }

        var document = solution.GetProject(projectId)!
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddDocument("Test.cs", source);
        return (document, workspace);
    }
}
