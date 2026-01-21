// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Splat.DependencyInjection.Analyzer.Analyzers;

namespace Splat.DependencyInjection.Analyzer.Tests;

/// <summary>
/// Tests for the AnalyzerHelpers class.
/// </summary>
public class AnalyzerHelpersTests
{
    /// <summary>
    /// Tests IsSplatRegistrationsMethod with various scenarios.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task IsSplatRegistrationsMethod_ValidatesCorrectly()
    {
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
            public static class Extensions {
                public static void Register(this string s) {}
            }
            """);

        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree]);
        var splatReg = compilation.GetTypeByMetadataName("Splat.SplatRegistrations");
        await Assert.That(splatReg).IsNotNull();

        var registerMethod = splatReg!.GetMembers("Register").OfType<IMethodSymbol>().First();
        var otherMethod = splatReg.GetMembers("Other").OfType<IMethodSymbol>().First();

        await Assert.That(AnalyzerHelpers.IsSplatRegistrationsMethod(registerMethod, "Register")).IsTrue();
        await Assert.That(AnalyzerHelpers.IsSplatRegistrationsMethod(otherMethod, "Register")).IsFalse();

        var otherType = compilation.GetTypeByMetadataName("Other.SplatRegistrations");
        await Assert.That(otherType).IsNotNull();
        var wrongNamespaceMethod = otherType!.GetMembers("Register").OfType<IMethodSymbol>().First();
        await Assert.That(AnalyzerHelpers.IsSplatRegistrationsMethod(wrongNamespaceMethod, "Register")).IsFalse();

        var extType = compilation.GetTypeByMetadataName("Extensions");
        await Assert.That(extType).IsNotNull();
        var extMethod = extType!.GetMembers("Register").OfType<IMethodSymbol>().First();
        await Assert.That(AnalyzerHelpers.IsSplatRegistrationsMethod(extMethod, "Register")).IsFalse();
    }

    /// <summary>
    /// Tests GetConstructorAnalysis with various constructor types.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task GetConstructorAnalysis_CountsCorrectly()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            namespace Splat {
                public class DependencyInjectionConstructorAttribute : System.Attribute {}
            }

            public class TestClass {
                static TestClass() {} // Static - ignored
                
                public TestClass() {} // Public - accessible
                
                internal TestClass(int i) {} // Internal - accessible
                
                private TestClass(string s) {} // Private - not accessible
                
                [Splat.DependencyInjectionConstructor]
                public TestClass(double d) {} // Marked
                
                [Splat.DependencyInjectionConstructor]
                internal TestClass(float f) {} // Marked
            }
            """);

        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var testClass = compilation.GetTypeByMetadataName("TestClass");
        await Assert.That(testClass).IsNotNull();

        var attrSymbol = compilation.GetTypeByMetadataName("Splat.DependencyInjectionConstructorAttribute");
        await Assert.That(attrSymbol).IsNotNull();

        var analysis = AnalyzerHelpers.GetConstructorAnalysis(testClass!, attrSymbol);

        await Assert.That(analysis.AccessibleCount).IsEqualTo(4);
        await Assert.That(analysis.MarkedCount).IsEqualTo(2);
        await Assert.That(analysis.FirstMarked).IsNotNull();
        await Assert.That(analysis.SecondMarked).IsNotNull();
    }

    /// <summary>
    /// Tests IsConstructorMarked fallback path.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task IsConstructorMarked_FallbackPath_IdentifiesAttribute()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            namespace Splat {
                // Must match the string expected by fallback: "global::Splat.DependencyInjectionConstructorAttribute"
                public class DependencyInjectionConstructorAttribute : System.Attribute {}
            }

            public class TestClass {
                [Splat.DependencyInjectionConstructor]
                public TestClass() {}
                
                public TestClass(int i) {}
            }
            """);

        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(System.Attribute).Assembly.Location));

        var testClass = compilation.GetTypeByMetadataName("TestClass");
        await Assert.That(testClass).IsNotNull();

        var markedCtor = testClass!.Constructors.First(c => c.Parameters.Length == 0);
        var unmarkedCtor = testClass.Constructors.First(c => c.Parameters.Length == 1);

        // Pass null for attribute symbol to force fallback
        await Assert.That(AnalyzerHelpers.IsConstructorMarked(markedCtor, null)).IsTrue();
        await Assert.That(AnalyzerHelpers.IsConstructorMarked(unmarkedCtor, null)).IsFalse();
    }

    /// <summary>
    /// Tests IsConstructorMarked fast path.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task IsConstructorMarked_FastPath_IdentifiesAttribute()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            namespace Splat {
                public class DependencyInjectionConstructorAttribute : System.Attribute {}
            }

            public class TestClass {
                [Splat.DependencyInjectionConstructor]
                public TestClass() {}
                
                public TestClass(int i) {}
            }
            """);

        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(System.Attribute).Assembly.Location));

        var testClass = compilation.GetTypeByMetadataName("TestClass");
        await Assert.That(testClass).IsNotNull();

        var attrSymbol = compilation.GetTypeByMetadataName("Splat.DependencyInjectionConstructorAttribute");
        await Assert.That(attrSymbol).IsNotNull();

        var markedCtor = testClass!.Constructors.First(c => c.Parameters.Length == 0);
        var unmarkedCtor = testClass.Constructors.First(c => c.Parameters.Length == 1);

        await Assert.That(AnalyzerHelpers.IsConstructorMarked(markedCtor, attrSymbol)).IsTrue();
        await Assert.That(AnalyzerHelpers.IsConstructorMarked(unmarkedCtor, attrSymbol)).IsFalse();
    }
}
