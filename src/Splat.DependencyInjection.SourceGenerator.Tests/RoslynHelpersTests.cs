// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Assertions;

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
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            using Splat;
            public class Test {
                public void Run() {
                    SplatRegistrations.Register<IService, Service>();
                    Other.Register();
                }
            }
            """);
        var root = await syntaxTree.GetRootAsync();
        var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();
        await Assert.That(invocations).Count().IsEqualTo(2);
        await Assert.That(RoslynHelpers.IsRegisterInvocation(invocations[0], CancellationToken.None)).IsTrue();
        await Assert.That(RoslynHelpers.IsRegisterInvocation(invocations[1], CancellationToken.None)).IsTrue(); // Matches name "Register"
    }

    /// <summary>
    /// Verifies that IsSplatRegistrationsMethod correctly identifies methods from SplatRegistrations.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task IsSplatRegistrationsMethod_IdentifiesCorrectMethods()
    {
        // This test requires a compilation to get symbols
        var syntaxTree = CSharpSyntaxTree.ParseText("""
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
            """);

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
    /// Verifies that GetBaseTypesAndThis returns the inheritance chain correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task GetBaseTypesAndThis_ReturnsInheritanceChain()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            public class Base {}
            public class Derived : Base {}
            public class Leaf : Derived {}
            """);
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
}
