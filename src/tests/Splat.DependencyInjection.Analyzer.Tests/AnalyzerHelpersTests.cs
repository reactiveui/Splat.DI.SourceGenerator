// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Splat.DependencyInjection.Analyzer.Analyzers;

namespace Splat.DependencyInjection.Analyzer.Tests;

/// <summary>Tests for the AnalyzerHelpers class.</summary>
public class AnalyzerHelpersTests
{
    /// <summary>The name of the assembly compiled for each test.</summary>
    private const string AssemblyName = "TestAssembly";

    /// <summary>The name of the Splat registration method under test.</summary>
    private const string RegisterMethodName = "Register";

    /// <summary>The metadata name of the test class declared in the sources.</summary>
    private const string TestClassName = "TestClass";

    /// <summary>The metadata name of the Splat registrations class.</summary>
    private const string SplatRegistrationsMetadataName = "Splat.SplatRegistrations";

    /// <summary>Tests IsSplatRegistrationsMethod with various scenarios.</summary>
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

        var compilation = CSharpCompilation.Create(AssemblyName, [syntaxTree]);
        var splatReg = compilation.GetTypeByMetadataName(SplatRegistrationsMetadataName);
        await Assert.That(splatReg).IsNotNull();

        var registerMethod = TestUtilities.FirstMethod(splatReg!, RegisterMethodName);
        var otherMethod = TestUtilities.FirstMethod(splatReg!, "Other");

        await Assert.That(AnalyzerHelpers.IsSplatRegistrationsMethod(registerMethod, RegisterMethodName)).IsTrue();
        await Assert.That(AnalyzerHelpers.IsSplatRegistrationsMethod(otherMethod, RegisterMethodName)).IsFalse();

        var otherType = compilation.GetTypeByMetadataName("Other.SplatRegistrations");
        await Assert.That(otherType).IsNotNull();
        var wrongNamespaceMethod = TestUtilities.FirstMethod(otherType!, RegisterMethodName);
        await Assert.That(AnalyzerHelpers.IsSplatRegistrationsMethod(wrongNamespaceMethod, RegisterMethodName)).IsFalse();

        var extType = compilation.GetTypeByMetadataName("Extensions");
        await Assert.That(extType).IsNotNull();
        var extMethod = TestUtilities.FirstMethod(extType!, RegisterMethodName);
        await Assert.That(AnalyzerHelpers.IsSplatRegistrationsMethod(extMethod, RegisterMethodName)).IsFalse();
    }

    /// <summary>Tests GetConstructorAnalysis with various constructor types.</summary>
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

        var compilation = CSharpCompilation.Create(AssemblyName, [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var testClass = compilation.GetTypeByMetadataName(TestClassName);
        await Assert.That(testClass).IsNotNull();

        var attrSymbol = compilation.GetTypeByMetadataName("Splat.DependencyInjectionConstructorAttribute");
        await Assert.That(attrSymbol).IsNotNull();

        var analysis = AnalyzerHelpers.GetConstructorAnalysis(testClass!, attrSymbol);

        const int expectedAccessibleCount = 4;
        const int expectedMarkedCount = 2;
        await Assert.That(analysis.AccessibleCount).IsEqualTo(expectedAccessibleCount);
        await Assert.That(analysis.MarkedCount).IsEqualTo(expectedMarkedCount);
        await Assert.That(analysis.FirstMarked).IsNotNull();
        await Assert.That(analysis.SecondMarked).IsNotNull();
    }

    /// <summary>Tests IsConstructorMarked fallback path.</summary>
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

        var compilation = CSharpCompilation.Create(AssemblyName, [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location));

        var testClass = compilation.GetTypeByMetadataName(TestClassName);
        await Assert.That(testClass).IsNotNull();

        var markedCtor = TestUtilities.ConstructorWithParameterCount(testClass!, 0);
        var unmarkedCtor = TestUtilities.ConstructorWithParameterCount(testClass!, 1);

        // Pass null for attribute symbol to force fallback
        await Assert.That(AnalyzerHelpers.IsConstructorMarked(markedCtor, null)).IsTrue();
        await Assert.That(AnalyzerHelpers.IsConstructorMarked(unmarkedCtor, null)).IsFalse();
    }

    /// <summary>Tests IsConstructorMarked fallback path returns false when attribute does not match.</summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task IsConstructorMarked_FallbackPath_NonMatchingAttribute_ReturnsFalse()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            public class TestClass {
                [System.Obsolete]
                public TestClass() {}
            }
            """);

        var compilation = CSharpCompilation.Create(AssemblyName, [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var testClass = compilation.GetTypeByMetadataName(TestClassName);
        await Assert.That(testClass).IsNotNull();

        var ctor = TestUtilities.FirstExplicitConstructor(testClass!);

        // ctor has [Obsolete], null symbol forces string fallback - should not match
        await Assert.That(AnalyzerHelpers.IsConstructorMarked(ctor, null)).IsFalse();
    }

    /// <summary>Tests IsConstructorMarked fast path.</summary>
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

        var compilation = CSharpCompilation.Create(AssemblyName, [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location));

        var testClass = compilation.GetTypeByMetadataName(TestClassName);
        await Assert.That(testClass).IsNotNull();

        var attrSymbol = compilation.GetTypeByMetadataName("Splat.DependencyInjectionConstructorAttribute");
        await Assert.That(attrSymbol).IsNotNull();

        var markedCtor = TestUtilities.ConstructorWithParameterCount(testClass!, 0);
        var unmarkedCtor = TestUtilities.ConstructorWithParameterCount(testClass!, 1);

        await Assert.That(AnalyzerHelpers.IsConstructorMarked(markedCtor, attrSymbol)).IsTrue();
        await Assert.That(AnalyzerHelpers.IsConstructorMarked(unmarkedCtor, attrSymbol)).IsFalse();
    }

    /// <summary>Tests AnalyzeConstructorsForType with interface (should skip).</summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task AnalyzeConstructorsForType_Interface_Skips()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("public interface ITest {}");
        var compilation = CSharpCompilation.Create(AssemblyName, [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var type = compilation.GetTypeByMetadataName("ITest");
        await Assert.That(type).IsNotNull();

        var diagnostics = new List<Diagnostic>();
        AnalyzerHelpers.AnalyzeConstructorsForType(compilation, type!, diagnostics.Add);

        // Should not report any diagnostics for interface
        await Assert.That(diagnostics).IsEmpty();
    }

    /// <summary>Tests AnalyzeConstructorsForType with enum (should skip).</summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task AnalyzeConstructorsForType_Enum_Skips()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("public enum TestEnum { A, B }");
        var compilation = CSharpCompilation.Create(AssemblyName, [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var type = compilation.GetTypeByMetadataName("TestEnum");
        await Assert.That(type).IsNotNull();

        var diagnostics = new List<Diagnostic>();
        AnalyzerHelpers.AnalyzeConstructorsForType(compilation, type!, diagnostics.Add);

        // Should not report any diagnostics for enum
        await Assert.That(diagnostics).IsEmpty();
    }

    /// <summary>Tests IsSplatRegistrationsMethod with extension method.</summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task IsSplatRegistrationsMethod_ExtensionMethod_ReturnsFalse()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            namespace Splat {
                public static class SplatRegistrations {
                }
                public static class Extensions {
                    public static void Register<T>(this SplatRegistrations r) {}
                }
            }
            """);

        var compilation = CSharpCompilation.Create(AssemblyName, [syntaxTree]);
        var extensionsType = compilation.GetTypeByMetadataName("Splat.Extensions");
        await Assert.That(extensionsType).IsNotNull();

        var registerMethod = TestUtilities.FirstMethod(extensionsType!, RegisterMethodName);
        await Assert.That(registerMethod.IsExtensionMethod).IsTrue();
        await Assert.That(AnalyzerHelpers.IsSplatRegistrationsMethod(registerMethod, RegisterMethodName)).IsFalse();
    }

    /// <summary>Tests AnalyzeConstructorsForType where one constructor is marked but is not accessible. Should report SPLATDI004.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AnalyzeConstructorsForType_MarkedPrivateConstructor_ReportsError()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            namespace Splat {
                public class DependencyInjectionConstructorAttribute : System.Attribute {}
            }

            public class TestClass {
                [Splat.DependencyInjectionConstructor]
                private TestClass() {} 
            }
            """);

        var compilation = CSharpCompilation.Create(AssemblyName, [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location));

        var type = compilation.GetTypeByMetadataName(TestClassName);
        var diagnostics = new List<Diagnostic>();

        AnalyzerHelpers.AnalyzeConstructorsForType(compilation, type!, diagnostics.Add);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("SPLATDI004");
    }

    /// <summary>
    /// Tests AnalyzeConstructorsForType where multiple constructors are marked.
    /// Should report SPLATDI003 for each marked constructor.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AnalyzeConstructorsForType_MultipleMarked_ReportsErrorOnAll()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            namespace Splat {
                public class DependencyInjectionConstructorAttribute : System.Attribute {}
            }

            public class TestClass {
                [Splat.DependencyInjectionConstructor]
                public TestClass() {} 

                [Splat.DependencyInjectionConstructor]
                public TestClass(int i) {}
            }
            """);

        var compilation = CSharpCompilation.Create(AssemblyName, [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location));

        var type = compilation.GetTypeByMetadataName(TestClassName);
        var diagnostics = new List<Diagnostic>();

        AnalyzerHelpers.AnalyzeConstructorsForType(compilation, type!, diagnostics.Add);

        const int expectedDiagnosticCount = 2;
        await Assert.That(diagnostics).Count().IsEqualTo(expectedDiagnosticCount);
        await Assert.That(diagnostics[0].Id).IsEqualTo("SPLATDI003");
        await Assert.That(diagnostics[1].Id).IsEqualTo("SPLATDI003");
    }

    /// <summary>Tests IsContainedInSplatRegistrations returns false for null.</summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task IsContainedInSplatRegistrations_Null_ReturnsFalse() =>
        await Assert.That(AnalyzerHelpers.IsContainedInSplatRegistrations(null)).IsFalse();

    /// <summary>Tests IsContainedInSplatRegistrations returns false for a type with no containing namespace.</summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task IsContainedInSplatRegistrations_NoContainingNamespace_ReturnsFalse()
    {
        var compilation = CSharpCompilation.Create(AssemblyName);
        var errorType = compilation.CreateErrorTypeSymbol(null, "SplatRegistrations", 0);

        await Assert.That(errorType.ContainingNamespace).IsNull();
        await Assert.That(AnalyzerHelpers.IsContainedInSplatRegistrations(errorType)).IsFalse();
    }

    /// <summary>Tests IsSplatRegistrationsMethod returns false for a method with no containing type.</summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task IsSplatRegistrationsMethod_NoContainingType_ReturnsFalse()
    {
        var compilation = CSharpCompilation.Create(AssemblyName)
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        var functionPointer = compilation.CreateFunctionPointerTypeSymbol(
            compilation.GetSpecialType(SpecialType.System_Void),
            RefKind.None,
            [],
            []);

        await Assert.That(functionPointer.Signature.ContainingType).IsNull();
        await Assert.That(AnalyzerHelpers.IsSplatRegistrationsMethod(functionPointer.Signature, RegisterMethodName)).IsFalse();
    }

    /// <summary>Tests IsSplatRegistrationsMethod returns false for an extension method declared on SplatRegistrations.</summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task IsSplatRegistrationsMethod_ExtensionMethodOnSplatRegistrations_ReturnsFalse()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            namespace Splat {
                public static class SplatRegistrations {
                    public static void Register(this string s) {}
                }
            }
            """);

        var compilation = CSharpCompilation.Create(AssemblyName, [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        var splatReg = compilation.GetTypeByMetadataName(SplatRegistrationsMetadataName)!;
        var extensionMethod = TestUtilities.FirstMethod(splatReg, RegisterMethodName);

        await Assert.That(extensionMethod.IsExtensionMethod).IsTrue();
        await Assert.That(AnalyzerHelpers.IsSplatRegistrationsMethod(extensionMethod, RegisterMethodName)).IsFalse();
    }

    /// <summary>
    /// Tests IsConstructorMarked fallback path skips an attribute whose class could not be decoded,
    /// which Roslyn reports as a null attribute class for malformed metadata attributes.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task IsConstructorMarked_FallbackPath_NullAttributeClass_ReturnsFalse()
    {
        var ctor = AttributeOverridingMethodProxy.Create([new NullClassAttributeData()]);

        await Assert.That(AnalyzerHelpers.IsConstructorMarked(ctor, null)).IsFalse();
    }

    /// <summary>Tests IsContainedInSplatRegistrations returns true for Splat.SplatRegistrations.</summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task IsContainedInSplatRegistrations_SplatRegistrations_ReturnsTrue()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            namespace Splat {
                public static class SplatRegistrations {}
            }
            """);

        var compilation = CSharpCompilation.Create(AssemblyName, [syntaxTree]);
        var type = compilation.GetTypeByMetadataName(SplatRegistrationsMetadataName);

        await Assert.That(AnalyzerHelpers.IsContainedInSplatRegistrations(type)).IsTrue();
    }

    /// <summary>Tests IsContainedInSplatRegistrations returns false for a type in the wrong namespace.</summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task IsContainedInSplatRegistrations_WrongNamespace_ReturnsFalse()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            namespace Other {
                public static class SplatRegistrations {}
            }
            """);

        var compilation = CSharpCompilation.Create(AssemblyName, [syntaxTree]);
        var type = compilation.GetTypeByMetadataName("Other.SplatRegistrations");

        await Assert.That(AnalyzerHelpers.IsContainedInSplatRegistrations(type)).IsFalse();
    }

    /// <summary>Tests GetFirstLocation returns Location.None for an empty locations array.</summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task GetFirstLocation_EmptyArray_ReturnsLocationNone()
    {
        var result = AnalyzerHelpers.GetFirstLocation(ImmutableArray<Location>.Empty);

        await Assert.That(result).IsEqualTo(Location.None);
    }

    /// <summary>Tests GetFirstLocation returns the first location when locations are present.</summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task GetFirstLocation_WithLocations_ReturnsFirst()
    {
        var tree = CSharpSyntaxTree.ParseText("class C {}");
        var location = (await tree.GetRootAsync()).GetLocation();
        var locations = ImmutableArray.Create(location);

        var result = AnalyzerHelpers.GetFirstLocation(locations);

        await Assert.That(result).IsEqualTo(location);
    }

    /// <summary>Tests ReportDiagnostics reports SPLATDI001 when multiple accessible constructors exist without marked constructor.</summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task ReportDiagnostics_MultipleAccessibleNoMarked_ReportsSPLATDI001()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            public class TestClass {
                public TestClass() {}
                public TestClass(int i) {}
            }
            """);

        var compilation = CSharpCompilation.Create(AssemblyName, [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var namedType = compilation.GetTypeByMetadataName(TestClassName)!;
        const int accessibleCount = 2;
        var analysis = new AnalyzerHelpers.ConstructorAnalysisResult(accessibleCount, 0, null, null);
        var diagnostics = new List<Diagnostic>();

        AnalyzerHelpers.ReportDiagnostics(analysis, namedType, null, diagnostics.Add);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("SPLATDI001");
    }

    /// <summary>Tests ReportDiagnostics reports nothing when only one accessible constructor exists.</summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task ReportDiagnostics_SingleAccessible_NoDiagnostic()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("public class TestClass { public TestClass() {} }");

        var compilation = CSharpCompilation.Create(AssemblyName, [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var namedType = compilation.GetTypeByMetadataName(TestClassName)!;
        var analysis = new AnalyzerHelpers.ConstructorAnalysisResult(1, 0, null, null);
        var diagnostics = new List<Diagnostic>();

        AnalyzerHelpers.ReportDiagnostics(analysis, namedType, null, diagnostics.Add);

        await Assert.That(diagnostics).IsEmpty();
    }

    /// <summary>Tests ReportDiagnostics reports SPLATDI004 when a single marked constructor is not accessible.</summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task ReportDiagnostics_MarkedPrivateConstructor_ReportsSPLATDI004()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            namespace Splat {
                public class DependencyInjectionConstructorAttribute : System.Attribute {}
            }
            public class TestClass {
                [Splat.DependencyInjectionConstructor]
                private TestClass() {}
            }
            """);

        var compilation = CSharpCompilation.Create(AssemblyName, [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var namedType = compilation.GetTypeByMetadataName(TestClassName)!;
        var privateCtor = TestUtilities.FirstExplicitConstructor(namedType);
        var analysis = new AnalyzerHelpers.ConstructorAnalysisResult(0, 1, privateCtor, null);
        var diagnostics = new List<Diagnostic>();

        AnalyzerHelpers.ReportDiagnostics(analysis, namedType, null, diagnostics.Add);

        await Assert.That(diagnostics).Count().IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("SPLATDI004");
    }
}
