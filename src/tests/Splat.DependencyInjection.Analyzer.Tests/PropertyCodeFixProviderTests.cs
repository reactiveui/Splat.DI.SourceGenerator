// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Splat.DependencyInjection.Analyzer.Tests;

/// <summary>
/// Tests for the property code fix provider that fixes property setter accessibility.
/// Validates that properties marked with DependencyInjectionProperty get public or internal setters.
/// </summary>
public class PropertyCodeFixProviderTests
{
    /// <summary>
    /// Tests that the provider configuration is valid.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task Provider_Configuration_IsValid()
    {
        var provider = new CodeFixes.PropertyCodeFixProvider();

        await Assert.That(provider.FixableDiagnosticIds).Contains(Splat.DependencyInjection.SourceGenerator.DiagnosticWarnings.PropertyMustPublicBeSettable.Id);
        await Assert.That(provider.GetFixAllProvider()).IsEqualTo(WellKnownFixAllProviders.BatchFixer);
    }

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

    /// <summary>
    /// Tests that the code fix changes a protected setter to public.
    /// Validates the code fix can handle protected setters.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task ProtectedSetter_ChangeToPublic()
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
            codeActionIndex: 0); // "Add public setter"

        await Assert.That(TestUtilities.AreEquivalent(expectedFixed, actualFixed)).IsTrue();
    }

    /// <summary>
    /// Tests that the code fix changes a protected setter to internal.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task ProtectedSetter_ChangeToInternal()
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

    /// <summary>
    /// Tests that the code fix adds a public setter to a public property with no setter.
    /// Validates the needsModifier=false path where property and setter have same accessibility.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task PublicPropertyWithNoSetter_AddPublicSetterWithoutModifier()
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
            codeActionIndex: 0); // "Add public setter"

        await Assert.That(TestUtilities.AreEquivalent(expectedFixed, actualFixed)).IsTrue();
    }

    /// <summary>
    /// Verifies BuildSetterModifiers returns empty token list when property already has the same modifier.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task BuildSetterModifiers_PropertyHasSameModifier_ReturnsEmpty()
    {
        var tree = CSharpSyntaxTree.ParseText("public class C { public int Foo { get; } }");
        var root = await tree.GetRootAsync();
        var property = root.DescendantNodes().OfType<PropertyDeclarationSyntax>().First();

        var result = CodeFixes.PropertyCodeFixProvider.BuildSetterModifiers(property, SyntaxKind.PublicKeyword);

        await Assert.That(result.Count).IsEqualTo(0);
    }

    /// <summary>
    /// Verifies BuildSetterModifiers returns modifier when property has different accessibility.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task BuildSetterModifiers_PropertyHasDifferentModifier_ReturnsModifier()
    {
        var tree = CSharpSyntaxTree.ParseText("public class C { public int Foo { get; } }");
        var root = await tree.GetRootAsync();
        var property = root.DescendantNodes().OfType<PropertyDeclarationSyntax>().First();

        var result = CodeFixes.PropertyCodeFixProvider.BuildSetterModifiers(property, SyntaxKind.InternalKeyword);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].Text).IsEqualTo("internal");
    }

    /// <summary>
    /// Verifies ConvertExpressionBodiedProperty transforms to accessor list with getter and setter.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task ConvertExpressionBodiedProperty_CreatesGetterAndSetter()
    {
        var tree = CSharpSyntaxTree.ParseText("public class C { private int _f; public int Foo => _f; }");
        var root = await tree.GetRootAsync();
        var property = root.DescendantNodes().OfType<PropertyDeclarationSyntax>().First();
        var setterModifiers = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.InternalKeyword));

        var result = CodeFixes.PropertyCodeFixProvider.ConvertExpressionBodiedProperty(property, setterModifiers);

        await Assert.That(result.AccessorList).IsNotNull();
        await Assert.That(result.ExpressionBody).IsNull();
        await Assert.That(result.AccessorList!.Accessors.Count).IsEqualTo(2);

        var getter = result.AccessorList.Accessors[0];
        var setter = result.AccessorList.Accessors[1];
        await Assert.That(getter.Kind()).IsEqualTo(SyntaxKind.GetAccessorDeclaration);
        await Assert.That(setter.Kind()).IsEqualTo(SyntaxKind.SetAccessorDeclaration);
        await Assert.That(setter.Modifiers.Count).IsEqualTo(1);
        await Assert.That(setter.Modifiers[0].Text).IsEqualTo("internal");
    }

    /// <summary>
    /// Verifies ConvertExpressionBodiedProperty with empty modifiers produces setter without modifier.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task ConvertExpressionBodiedProperty_EmptyModifiers_NoSetterModifier()
    {
        var tree = CSharpSyntaxTree.ParseText("public class C { private int _f; public int Foo => _f; }");
        var root = await tree.GetRootAsync();
        var property = root.DescendantNodes().OfType<PropertyDeclarationSyntax>().First();
        var setterModifiers = SyntaxFactory.TokenList();

        var result = CodeFixes.PropertyCodeFixProvider.ConvertExpressionBodiedProperty(property, setterModifiers);

        var setter = result.AccessorList!.Accessors[1];
        await Assert.That(setter.Kind()).IsEqualTo(SyntaxKind.SetAccessorDeclaration);
        await Assert.That(setter.Modifiers.Count).IsEqualTo(0);
    }

    /// <summary>
    /// Verifies AddSetterToGetterOnlyProperty adds a setter to a getter-only property.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task AddSetterToGetterOnlyProperty_AddsSetter()
    {
        var tree = CSharpSyntaxTree.ParseText("public class C { public int Foo { get; } }");
        var root = await tree.GetRootAsync();
        var property = root.DescendantNodes().OfType<PropertyDeclarationSyntax>().First();
        var setterModifiers = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.InternalKeyword));

        var result = CodeFixes.PropertyCodeFixProvider.AddSetterToGetterOnlyProperty(property, setterModifiers);

        await Assert.That(result.AccessorList!.Accessors.Count).IsEqualTo(2);

        var setter = result.AccessorList.Accessors[1];
        await Assert.That(setter.Kind()).IsEqualTo(SyntaxKind.SetAccessorDeclaration);
        await Assert.That(setter.Modifiers.Count).IsEqualTo(1);
        await Assert.That(setter.Modifiers[0].Text).IsEqualTo("internal");
    }

    /// <summary>
    /// Verifies AddSetterToGetterOnlyProperty with empty modifiers adds setter without modifier.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task AddSetterToGetterOnlyProperty_EmptyModifiers_NoSetterModifier()
    {
        var tree = CSharpSyntaxTree.ParseText("public class C { public int Foo { get; } }");
        var root = await tree.GetRootAsync();
        var property = root.DescendantNodes().OfType<PropertyDeclarationSyntax>().First();
        var setterModifiers = SyntaxFactory.TokenList();

        var result = CodeFixes.PropertyCodeFixProvider.AddSetterToGetterOnlyProperty(property, setterModifiers);

        var setter = result.AccessorList!.Accessors[1];
        await Assert.That(setter.Kind()).IsEqualTo(SyntaxKind.SetAccessorDeclaration);
        await Assert.That(setter.Modifiers.Count).IsEqualTo(0);
    }

    /// <summary>
    /// Verifies UpdateExistingSetterModifiers changes a private setter to internal.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task UpdateExistingSetterModifiers_PrivateToInternal()
    {
        var tree = CSharpSyntaxTree.ParseText("public class C { public int Foo { get; private set; } }");
        var root = await tree.GetRootAsync();
        var property = root.DescendantNodes().OfType<PropertyDeclarationSyntax>().First();
        var existingSetter = property.AccessorList!.Accessors
            .First(a => a.Kind() == SyntaxKind.SetAccessorDeclaration);
        var setterModifiers = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.InternalKeyword));

        var result = CodeFixes.PropertyCodeFixProvider.UpdateExistingSetterModifiers(property, existingSetter, setterModifiers);

        var setter = result.AccessorList!.Accessors
            .First(a => a.Kind() == SyntaxKind.SetAccessorDeclaration);
        await Assert.That(setter.Modifiers.Count).IsEqualTo(1);
        await Assert.That(setter.Modifiers[0].Text).IsEqualTo("internal");
    }

    /// <summary>
    /// Verifies UpdateExistingSetterModifiers with empty modifiers removes the existing modifier.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task UpdateExistingSetterModifiers_RemovesModifier()
    {
        var tree = CSharpSyntaxTree.ParseText("public class C { public int Foo { get; private set; } }");
        var root = await tree.GetRootAsync();
        var property = root.DescendantNodes().OfType<PropertyDeclarationSyntax>().First();
        var existingSetter = property.AccessorList!.Accessors
            .First(a => a.Kind() == SyntaxKind.SetAccessorDeclaration);
        var setterModifiers = SyntaxFactory.TokenList();

        var result = CodeFixes.PropertyCodeFixProvider.UpdateExistingSetterModifiers(property, existingSetter, setterModifiers);

        var setter = result.AccessorList!.Accessors
            .First(a => a.Kind() == SyntaxKind.SetAccessorDeclaration);
        await Assert.That(setter.Modifiers.Count).IsEqualTo(0);
    }
}