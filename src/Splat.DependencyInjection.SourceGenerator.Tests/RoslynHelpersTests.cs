// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace Splat.DependencyInjection.SourceGenerator.Tests;

/// <summary>
/// Tests for the RoslynHelpers class.
/// </summary>
public class RoslynHelpersTests
{
    /// <summary>
    /// Verifies that IsRegisterInvocation correctly identifies Register calls.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task IsRegisterInvocation_IdentifiesRegisterCalls()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
            using Splat;
            public class Test {
                public void Run() {
                    SplatRegistrations.Register<IService, Service>();
                    Other.Register();
                }
            }
            ");
        var root = await syntaxTree.GetRootAsync();
        var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();
        await Assert.That(invocations).Count().IsEqualTo(2);
        await Assert.That(RoslynHelpers.IsRegisterInvocation(invocations[0], CancellationToken.None)).IsTrue();
        await Assert.That(RoslynHelpers.IsRegisterInvocation(invocations[1], CancellationToken.None)).IsTrue(); // Matches name "Register"
    }

    /// <summary>
    /// Verifies IsRegisterInvocation returns false for non-invocation nodes.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task IsRegisterInvocation_NonInvocation_ReturnsFalse()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("class Test {}");
        var root = await syntaxTree.GetRootAsync();
        var node = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();

        await Assert.That(RoslynHelpers.IsRegisterInvocation(node, CancellationToken.None)).IsFalse();
    }

    /// <summary>
    /// Verifies that IsRegisterInvocation returns false for non-matching invocations.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task IsRegisterInvocation_NonMatching_ReturnsFalse()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
            public class Test {
                public void Run() {
                    Other(); // Simple name
                    this.Other(); // Member access but wrong name
                }
                public void Other() {}
            }
            ");
        var root = await syntaxTree.GetRootAsync();
        var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();

        await Assert.That(RoslynHelpers.IsRegisterInvocation(invocations[0], CancellationToken.None)).IsFalse();
        await Assert.That(RoslynHelpers.IsRegisterInvocation(invocations[1], CancellationToken.None)).IsFalse();
    }

    /// <summary>
    /// Verifies that IsSplatRegistrationsMethod correctly identifies methods from SplatRegistrations.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task IsSplatRegistrationsMethod_IdentifiesCorrectMethods()
    {
        // This test requires a compilation to get symbols
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
            namespace Splat {
                public static class SplatRegistrations {
                    public static void Register<T>() {}
                    public static void Other() {}
                }
            }
            namespace Other {
                public static class SplatRegistrations {
                    public static void Register<T>() {}
                }
            }
            ");

        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree]);
        var splatReg = compilation.GetTypeByMetadataName("Splat.SplatRegistrations");
        await Assert.That(splatReg).IsNotNull();
        var registerMethod = splatReg!.GetMembers("Register").OfType<IMethodSymbol>().First();
        var otherMethod = splatReg.GetMembers("Other").OfType<IMethodSymbol>().First();
        await Assert.That(RoslynHelpers.IsSplatRegistrationsMethod(registerMethod, "Register")).IsTrue();
        await Assert.That(RoslynHelpers.IsSplatRegistrationsMethod(otherMethod, "Register")).IsFalse();

        // Test with wrong namespace but same class name
        var otherNamespaceType = compilation.GetTypeByMetadataName("Other.SplatRegistrations");
        await Assert.That(otherNamespaceType).IsNotNull();
        var wrongMethod = otherNamespaceType!.GetMembers("Register").OfType<IMethodSymbol>().First();
        await Assert.That(RoslynHelpers.IsSplatRegistrationsMethod(wrongMethod, "Register")).IsFalse();
    }

    /// <summary>
    /// Verifies that IsSplatRegistrationsMethod handles null containing type.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task IsSplatRegistrationsMethod_NullContainingType_ReturnsFalse()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
            public delegate void MyDelegate();
            ");
        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree]);
        var delegateType = (INamedTypeSymbol)compilation.GetTypeByMetadataName("MyDelegate")!;
        var invokeMethod = delegateType.DelegateInvokeMethod!;

        await Assert.That(RoslynHelpers.IsSplatRegistrationsMethod(invokeMethod, "Register")).IsFalse();
    }

    /// <summary>
    /// Verifies that IsRegisterLazySingletonInvocation correctly identifies RegisterLazySingleton calls.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task IsRegisterLazySingletonInvocation_IdentifiesCorrectCalls()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
            using Splat;
            public class Test {
                public void Run() {
                    SplatRegistrations.RegisterLazySingleton<IService, Service>();
                    Other.RegisterLazySingleton();
                }
            }
            ");
        var root = await syntaxTree.GetRootAsync();
        var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();
        await Assert.That(invocations).Count().IsEqualTo(2);
        await Assert.That(RoslynHelpers.IsRegisterLazySingletonInvocation(invocations[0], CancellationToken.None)).IsTrue();
        await Assert.That(RoslynHelpers.IsRegisterLazySingletonInvocation(invocations[1], CancellationToken.None)).IsTrue();
    }

    /// <summary>
    /// Verifies IsRegisterLazySingletonInvocation returns false for non-invocation nodes.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task IsRegisterLazySingletonInvocation_NonInvocation_ReturnsFalse()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("class Test {}");
        var root = await syntaxTree.GetRootAsync();
        var node = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();

        await Assert.That(RoslynHelpers.IsRegisterLazySingletonInvocation(node, CancellationToken.None)).IsFalse();
    }

    /// <summary>
    /// Verifies that IsRegisterLazySingletonInvocation returns false for non-matching invocations.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task IsRegisterLazySingletonInvocation_NonMatching_ReturnsFalse()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
            public class Test {
                public void Run() {
                    Other(); // Simple name
                }
                public void Other() {}
            }
            ");
        var root = await syntaxTree.GetRootAsync();
        var invocation = root.DescendantNodes().OfType<InvocationExpressionSyntax>().First();

        await Assert.That(RoslynHelpers.IsRegisterLazySingletonInvocation(invocation, CancellationToken.None)).IsFalse();
    }

    /// <summary>
    /// Verifies that ExtractContractParameter correctly extracts the contract value.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task ExtractContractParameter_ExtractsValue()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            using Splat;
            namespace Splat {
                public static class SplatRegistrations {
                    public static void Register<T>(string contract = null) {}
                }
            }
            namespace TestNamespace {
                public class Constants {
                    public const string MyContract = "MyConstantContract";
                }
            }
            public class Test {
                public void Run() {
                    SplatRegistrations.Register<string>("LiteralContract");
                    SplatRegistrations.Register<string>(contract: "NamedLiteralContract");
                    SplatRegistrations.Register<string>(TestNamespace.Constants.MyContract);
                }
            }
            """);

        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var root = await syntaxTree.GetRootAsync();
        var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();

        var splatReg = compilation.GetTypeByMetadataName("Splat.SplatRegistrations");
        await Assert.That(splatReg).IsNotNull();
        var registerMethod = splatReg!.GetMembers("Register").OfType<IMethodSymbol>().First();

        // 1. Literal "LiteralContract"
        var literalContract = RoslynHelpers.ExtractContractParameter(registerMethod, invocations[0], semanticModel, CancellationToken.None);
        await Assert.That(literalContract).IsEqualTo("\"LiteralContract\"");

        // 2. Named Literal
        var namedLiteralContract = RoslynHelpers.ExtractContractParameter(registerMethod, invocations[1], semanticModel, CancellationToken.None);
        await Assert.That(namedLiteralContract).IsEqualTo("\"NamedLiteralContract\"");

        // 3. Constant
        var constantContract = RoslynHelpers.ExtractContractParameter(registerMethod, invocations[2], semanticModel, CancellationToken.None);
        await Assert.That(constantContract).IsEqualTo("TestNamespace.Constants.MyContract");
    }

    /// <summary>
    /// Verifies ExtractContractParameter returns null if parameter not found.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task ExtractContractParameter_NoContractParam_ReturnsNull()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            using Splat;
            namespace Splat {
                public static class SplatRegistrations {
                    public static void Register<T>(int other) {}
                }
            }
            public class Test {
                public void Run() {
                    SplatRegistrations.Register<string>(123);
                }
            }
            """);

        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var root = await syntaxTree.GetRootAsync();
        var invocation = root.DescendantNodes().OfType<InvocationExpressionSyntax>().First();

        var splatReg = compilation.GetTypeByMetadataName("Splat.SplatRegistrations");
        await Assert.That(splatReg).IsNotNull();
        var registerMethod = splatReg!.GetMembers("Register").OfType<IMethodSymbol>().First();

        var result = RoslynHelpers.ExtractContractParameter(registerMethod, invocation, semanticModel, CancellationToken.None);
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Verifies that ExtractLazyThreadSafetyMode correctly extracts the mode.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task ExtractLazyThreadSafetyMode_ExtractsValue()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
            using System.Threading;
            using Splat;
            namespace Splat {
                public static class SplatRegistrations {
                    public static void RegisterLazySingleton<T>(LazyThreadSafetyMode mode) {}
                }
            }
            public class Test {
                public void Run() {
                    SplatRegistrations.RegisterLazySingleton<string>(LazyThreadSafetyMode.ExecutionAndPublication);
                    SplatRegistrations.RegisterLazySingleton<string>(mode: LazyThreadSafetyMode.None);
                }
            }
            ");

        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(System.Threading.LazyThreadSafetyMode).Assembly.Location));

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var root = await syntaxTree.GetRootAsync();
        var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();

        var splatReg = compilation.GetTypeByMetadataName("Splat.SplatRegistrations");
        await Assert.That(splatReg).IsNotNull();
        var registerMethod = splatReg!.GetMembers("RegisterLazySingleton").OfType<IMethodSymbol>().First();

        var mode1 = RoslynHelpers.ExtractLazyThreadSafetyMode(registerMethod, invocations[0], semanticModel, CancellationToken.None);
        await Assert.That(mode1).Contains("ExecutionAndPublication");

        var mode2 = RoslynHelpers.ExtractLazyThreadSafetyMode(registerMethod, invocations[1], semanticModel, CancellationToken.None);
        await Assert.That(mode2).Contains("None");
    }

    /// <summary>
    /// Verifies that GetBaseTypesAndThis returns the inheritance chain correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task GetBaseTypesAndThis_ReturnsInheritanceChain()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
            public class Base {}
            public class Derived : Base {}
            public class Leaf : Derived {}
            ");
        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree]);
        var leafType = compilation.GetTypeByMetadataName("Leaf");
        await Assert.That(leafType).IsNotNull();
        var hierarchy = RoslynHelpers.GetBaseTypesAndThis(leafType!);

        // Order is Leaf -> Derived -> Base -> Object
        await Assert.That(hierarchy).Count().IsEqualTo(4);
        await Assert.That(hierarchy[0].Name).IsEqualTo("Leaf");
        await Assert.That(hierarchy[1].Name).IsEqualTo("Derived");
        await Assert.That(hierarchy[2].Name).IsEqualTo("Base");
        await Assert.That(hierarchy[3].Name).IsEqualTo("Object");
    }

    /// <summary>
    /// Verifies GetBaseTypesAndThis handles interfaces (BaseType is null).
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task GetBaseTypesAndThis_Interface_ReturnsSelf()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("interface ITest {}");
        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree]);
        var type = compilation.GetTypeByMetadataName("ITest");
        await Assert.That(type).IsNotNull();

        var result = RoslynHelpers.GetBaseTypesAndThis(type!);

        // Interface has no base class, so result should be just [ITest]
        await Assert.That(result).Count().IsEqualTo(1);
        await Assert.That(result[0].Name).IsEqualTo("ITest");
    }

    /// <summary>
    /// Verifies that IsRegisterInvocation identifies MemberBindingExpression correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task IsRegisterInvocation_MemberBinding_ReturnsTrue()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            using Splat;
            public class Test {
                public void Run(SplatRegistrations r) {
                    r?.Register<IService>();
                }
            }
            """);
        var root = await syntaxTree.GetRootAsync();
        var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();

        await Assert.That(invocations).Count().IsGreaterThan(0);
        await Assert.That(RoslynHelpers.IsRegisterInvocation(invocations[0], CancellationToken.None)).IsTrue();
    }

    /// <summary>
    /// Verifies that IsRegisterLazySingletonInvocation identifies MemberBindingExpression correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task IsRegisterLazySingletonInvocation_MemberBinding_ReturnsTrue()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            using Splat;
            public class Test {
                public void Run(SplatRegistrations r) {
                    r?.RegisterLazySingleton<IService>();
                }
            }
            """);
        var root = await syntaxTree.GetRootAsync();
        var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();

        await Assert.That(invocations).Count().IsGreaterThan(0);
        await Assert.That(RoslynHelpers.IsRegisterLazySingletonInvocation(invocations[0], CancellationToken.None)).IsTrue();
    }

    /// <summary>
    /// Verifies ExtractLazyThreadSafetyMode returns null when no mode parameter exists.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task ExtractLazyThreadSafetyMode_NoModeParam_ReturnsNull()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            using Splat;
            namespace Splat {
                public static class SplatRegistrations {
                    public static void RegisterLazySingleton<T>(int other) {}
                }
            }
            public class Test {
                public void Run() {
                    SplatRegistrations.RegisterLazySingleton<string>(123);
                }
            }
            """);

        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var root = await syntaxTree.GetRootAsync();
        var invocation = root.DescendantNodes().OfType<InvocationExpressionSyntax>().First();

        var splatReg = compilation.GetTypeByMetadataName("Splat.SplatRegistrations");
        await Assert.That(splatReg).IsNotNull();
        var registerMethod = splatReg!.GetMembers("RegisterLazySingleton").OfType<IMethodSymbol>().First();

        var result = RoslynHelpers.ExtractLazyThreadSafetyMode(registerMethod, invocation, semanticModel, CancellationToken.None);
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Verifies ExtractContractParameter handles local variable references.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task ExtractContractParameter_LocalVariable_ReturnsExpression()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            using Splat;
            namespace Splat {
                public static class SplatRegistrations {
                    public static void Register<T>(string contract = null) {}
                }
            }
            public class Test {
                public void Run() {
                    var unknownVar = "test";
                    SplatRegistrations.Register<string>(unknownVar);
                }
            }
            """);

        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var root = await syntaxTree.GetRootAsync();
        var invocation = root.DescendantNodes().OfType<InvocationExpressionSyntax>().First();

        var splatReg = compilation.GetTypeByMetadataName("Splat.SplatRegistrations");
        await Assert.That(splatReg).IsNotNull();
        var registerMethod = splatReg!.GetMembers("Register").OfType<IMethodSymbol>().First();

        var result = RoslynHelpers.ExtractContractParameter(registerMethod, invocation, semanticModel, CancellationToken.None);

        // Local variable reference should be preserved
        await Assert.That(result).IsEqualTo("unknownVar");
    }

    /// <summary>
    /// Verifies IsSplatRegistrationsMethod returns false for extension methods.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task IsSplatRegistrationsMethod_ExtensionMethod_ReturnsFalse()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
            namespace Splat {
                public static class SplatRegistrations {
                    public static void Register<T>(this object obj) {}
                }
            }
            ");
        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree]);
        var splatReg = compilation.GetTypeByMetadataName("Splat.SplatRegistrations");
        await Assert.That(splatReg).IsNotNull();
        var method = splatReg!.GetMembers("Register").OfType<IMethodSymbol>().First();

        await Assert.That(method.IsExtensionMethod).IsTrue();
        await Assert.That(RoslynHelpers.IsSplatRegistrationsMethod(method, "Register")).IsFalse();
    }

    /// <summary>
    /// Verifies GetBaseTypesAndThis returns empty array for null input.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task GetBaseTypesAndThis_Null_ReturnsEmpty()
    {
        var result = RoslynHelpers.GetBaseTypesAndThis(null!);
        await Assert.That(result).IsEmpty();
    }

    /// <summary>
    /// Verifies ExtractContractParameter returns null when symbol is unresolved.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task ExtractContractParameter_UnresolvedSymbol_ReturnsNull()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            using Splat;
            namespace Splat {
                public static class SplatRegistrations {
                    public static void Register<T>(string contract = null) {}
                }
            }
            public class Test {
                public void Run() {
                    SplatRegistrations.Register<string>(undeclaredVar);
                }
            }
            """);

        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var root = await syntaxTree.GetRootAsync();
        var invocation = root.DescendantNodes().OfType<InvocationExpressionSyntax>().First();

        var splatReg = compilation.GetTypeByMetadataName("Splat.SplatRegistrations");
        await Assert.That(splatReg).IsNotNull();
        var registerMethod = splatReg!.GetMembers("Register").OfType<IMethodSymbol>().First();

        var result = RoslynHelpers.ExtractContractParameter(registerMethod, invocation, semanticModel, CancellationToken.None);
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Verifies ExtractLazyThreadSafetyMode returns null when symbol is unresolved.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task ExtractLazyThreadSafetyMode_UnresolvedSymbol_ReturnsNull()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            using System.Threading;
            using Splat;
            namespace Splat {
                public static class SplatRegistrations {
                    public static void RegisterLazySingleton<T>(LazyThreadSafetyMode mode) {}
                }
            }
            public class Test {
                public void Run() {
                    SplatRegistrations.RegisterLazySingleton<string>(undeclaredVar);
                }
            }
            """);

        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(System.Threading.LazyThreadSafetyMode).Assembly.Location));

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var root = await syntaxTree.GetRootAsync();
        var invocation = root.DescendantNodes().OfType<InvocationExpressionSyntax>().First();

        var splatReg = compilation.GetTypeByMetadataName("Splat.SplatRegistrations");
        await Assert.That(splatReg).IsNotNull();
        var registerMethod = splatReg!.GetMembers("RegisterLazySingleton").OfType<IMethodSymbol>().First();

        var result = RoslynHelpers.ExtractLazyThreadSafetyMode(registerMethod, invocation, semanticModel, CancellationToken.None);
        await Assert.That(result).IsNull();
    }
}