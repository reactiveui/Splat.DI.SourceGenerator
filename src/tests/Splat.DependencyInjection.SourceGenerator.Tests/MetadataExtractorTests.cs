// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Splat.DependencyInjection.SourceGenerator.Tests;

/// <summary>
/// Tests for MetadataExtractor.
/// </summary>
public class MetadataExtractorTests
{
    /// <summary>
    /// Verifies ExtractConstructorParameters handles empty constructor list.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task ExtractConstructorParameters_NoConstructors_ReturnsEmpty()
    {
        // Static class has no instance constructors
        var syntaxTree = CSharpSyntaxTree.ParseText("public static class StaticClass {}");
        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var type = compilation.GetTypeByMetadataName("StaticClass");
        var symbols = MetadataExtractor.ResolveWellKnownSymbols(compilation);
        var result = MetadataExtractor.ExtractConstructorParameters(type!, symbols);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!).IsEmpty();
    }

    /// <summary>
    /// Verifies ExtractPropertyInjections handles read-only properties (no setter).
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task ExtractPropertyInjections_ReadOnly_ReturnsNull()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
            namespace Splat {
                public class DependencyInjectionPropertyAttribute : System.Attribute {}
            }
            public class TestClass {
                [Splat.DependencyInjectionProperty]
                public string Prop { get; }
            }
            ");
        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var type = compilation.GetTypeByMetadataName("TestClass");
        var symbols = MetadataExtractor.ResolveWellKnownSymbols(compilation);
        var result = MetadataExtractor.ExtractPropertyInjections(type!, symbols.PropertyAttribute);

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Verifies ExtractConstructorParameters handles multiple constructors correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task ExtractConstructorParameters_MultipleConstructors_Logic()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            namespace Splat {
                public class DependencyInjectionConstructorAttribute : System.Attribute {}
            }
            public class SingleCtor {
                public SingleCtor(int i) {}
            }
            public class PrivateCtor {
                private PrivateCtor() {}
            }
            public class MultipleCtorNoAttr {
                public MultipleCtorNoAttr() {}
                public MultipleCtorNoAttr(int i) {}
            }
            public class MultipleCtorOneAttr {
                public MultipleCtorOneAttr() {}
                [Splat.DependencyInjectionConstructor]
                public MultipleCtorOneAttr(int i) {}
            }
            public class MultipleCtorTwoAttr {
                [Splat.DependencyInjectionConstructor]
                public MultipleCtorTwoAttr() {}
                [Splat.DependencyInjectionConstructor]
                public MultipleCtorTwoAttr(int i) {}
            }
            """);

        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var symbols = MetadataExtractor.ResolveWellKnownSymbols(compilation);

        // 1. Single public ctor
        var type = compilation.GetTypeByMetadataName("SingleCtor");
        var result = MetadataExtractor.ExtractConstructorParameters(type!, symbols);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!).Count().IsEqualTo(1);

        // 2. Private ctor
        type = compilation.GetTypeByMetadataName("PrivateCtor");
        result = MetadataExtractor.ExtractConstructorParameters(type!, symbols);
        await Assert.That(result).IsNull();

        // 3. Multiple ctors, no attribute
        type = compilation.GetTypeByMetadataName("MultipleCtorNoAttr");
        result = MetadataExtractor.ExtractConstructorParameters(type!, symbols);
        await Assert.That(result).IsNull();

        // 4. Multiple ctors, one attribute
        type = compilation.GetTypeByMetadataName("MultipleCtorOneAttr");
        result = MetadataExtractor.ExtractConstructorParameters(type!, symbols);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!).Count().IsEqualTo(1);
        await Assert.That(result![0].ParameterName).IsEqualTo("i");

        // 5. Multiple ctors, two attributes
        type = compilation.GetTypeByMetadataName("MultipleCtorTwoAttr");
        result = MetadataExtractor.ExtractConstructorParameters(type!, symbols);
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Verifies ExtractConstructorParameters handles special types like Lazy and IEnumerable.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task ExtractConstructorParameters_SpecialTypes()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            using System;
            using System.Collections.Generic;
            public class SpecialTypes {
                public SpecialTypes(Lazy<string> lazy, IEnumerable<int> list) {}
            }
            """);

        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(System.Lazy<>).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(System.Collections.Generic.IEnumerable<>).Assembly.Location));

        var type = compilation.GetTypeByMetadataName("SpecialTypes");
        var symbols = MetadataExtractor.ResolveWellKnownSymbols(compilation);
        var result = MetadataExtractor.ExtractConstructorParameters(type!, symbols);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!).Count().IsEqualTo(2);

        await Assert.That(result![0].IsLazy).IsTrue();
        await Assert.That(result![0].LazyInnerType).Contains("string");

        await Assert.That(result![1].IsCollection).IsTrue();
        await Assert.That(result![1].CollectionItemType).Contains("int");
    }

    /// <summary>
    /// Verifies ExtractPropertyInjections handles various property scenarios.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task ExtractPropertyInjections_Logic()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            namespace Splat {
                public class DependencyInjectionPropertyAttribute : System.Attribute {}
            }
            public class Base {
                [Splat.DependencyInjectionProperty]
                public string BaseProp { get; set; }
            }
            public class Derived : Base {
                [Splat.DependencyInjectionProperty]
                public int DerivedProp { get; set; }

                public double NoAttr { get; set; }
            }
            public class PrivateSetter {
                [Splat.DependencyInjectionProperty]
                public string Prop { get; private set; }
            }
            """);

        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var symbols = MetadataExtractor.ResolveWellKnownSymbols(compilation);

        // 1. Derived class (should get base prop too)
        var type = compilation.GetTypeByMetadataName("Derived");
        var result = MetadataExtractor.ExtractPropertyInjections(type!, symbols.PropertyAttribute);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!).Count().IsEqualTo(2);
        await Assert.That(result!.Any(p => p.PropertyName == "BaseProp")).IsTrue();
        await Assert.That(result!.Any(p => p.PropertyName == "DerivedProp")).IsTrue();

        // 2. Private setter
        type = compilation.GetTypeByMetadataName("PrivateSetter");
        result = MetadataExtractor.ExtractPropertyInjections(type!, symbols.PropertyAttribute);
        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// Verifies HasAttribute returns true when symbol comparison matches.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task HasAttribute_SymbolComparison_ReturnsTrue()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            namespace Splat {
                public class DependencyInjectionConstructorAttribute : System.Attribute {}
            }
            public class MyClass {
                [Splat.DependencyInjectionConstructor]
                public MyClass() {}
            }
            """);

        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var type = compilation.GetTypeByMetadataName("MyClass")!;
        var ctor = type.Constructors[0];
        var attributeSymbol = compilation.GetTypeByMetadataName(Constants.ConstructorAttributeMetadataName);

        var result = MetadataExtractor.HasAttribute(ctor, attributeSymbol, Constants.ConstructorAttribute);

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    /// Verifies HasAttribute returns false when attribute is not present.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task HasAttribute_NoAttribute_ReturnsFalse()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            public class MyClass {
                public MyClass() {}
            }
            """);

        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var type = compilation.GetTypeByMetadataName("MyClass")!;
        var ctor = type.Constructors[0];

        var result = MetadataExtractor.HasAttribute(ctor, null, Constants.ConstructorAttribute);

        await Assert.That(result).IsFalse();
    }

    /// <summary>
    /// Verifies HasAttribute falls back to string comparison when symbol is null.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task HasAttribute_StringFallback_ReturnsTrue()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            namespace Splat {
                public class DependencyInjectionConstructorAttribute : System.Attribute {}
            }
            public class MyClass {
                [Splat.DependencyInjectionConstructor]
                public MyClass() {}
            }
            """);

        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var type = compilation.GetTypeByMetadataName("MyClass")!;
        var ctor = type.Constructors[0];

        // Pass null symbol to force string fallback path
        var result = MetadataExtractor.HasAttribute(ctor, null, Constants.ConstructorAttribute);

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    /// Verifies IsOriginalDefinition matches Lazy via symbol comparison.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task IsOriginalDefinition_LazyType_MatchesViaSymbol()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            using System;
            public class MyClass {
                public MyClass(Lazy<string> lazy) {}
            }
            """);

        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(System.Lazy<>).Assembly.Location));

        var type = compilation.GetTypeByMetadataName("MyClass")!;
        var param = type.Constructors[0].Parameters[0];
        var namedType = (INamedTypeSymbol)param.Type;
        var symbols = MetadataExtractor.ResolveWellKnownSymbols(compilation);

        var result = MetadataExtractor.IsOriginalDefinition(namedType, symbols.LazyType, Constants.LazyOpenGenericTypeName);

        await Assert.That(result).IsTrue();
    }

    /// <summary>
    /// Verifies IsOriginalDefinition does not match unrelated types.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task IsOriginalDefinition_NonLazyType_ReturnsFalse()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            public class MyClass {
                public MyClass(string s) {}
            }
            """);

        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var type = compilation.GetTypeByMetadataName("MyClass")!;
        var param = type.Constructors[0].Parameters[0];

        // string is a NamedTypeSymbol but not Lazy<T>
        if (param.Type is INamedTypeSymbol namedType)
        {
            var result = MetadataExtractor.IsOriginalDefinition(namedType, null, Constants.LazyOpenGenericTypeName);
            await Assert.That(result).IsFalse();
        }
    }

    /// <summary>
    /// Verifies ResolveWellKnownSymbols resolves all symbols from a compilation with appropriate references.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task ResolveWellKnownSymbols_WithReferences_ResolvesAll()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            namespace Splat {
                public class DependencyInjectionConstructorAttribute : System.Attribute {}
                public class DependencyInjectionPropertyAttribute : System.Attribute {}
            }
            """);

        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(System.Lazy<>).Assembly.Location));

        var symbols = MetadataExtractor.ResolveWellKnownSymbols(compilation);

        await Assert.That(symbols.ConstructorAttribute).IsNotNull();
        await Assert.That(symbols.PropertyAttribute).IsNotNull();
        await Assert.That(symbols.LazyType).IsNotNull();
        await Assert.That(symbols.EnumerableType).IsNotNull();
    }

    /// <summary>
    /// Verifies ResolveWellKnownSymbols returns nulls when types are not available.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task ResolveWellKnownSymbols_WithoutReferences_ReturnsNulls()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("public class Empty {}");

        var compilation = CSharpCompilation.Create("TestAssembly", [syntaxTree]);

        var symbols = MetadataExtractor.ResolveWellKnownSymbols(compilation);

        await Assert.That(symbols.ConstructorAttribute).IsNull();
        await Assert.That(symbols.PropertyAttribute).IsNull();
        await Assert.That(symbols.LazyType).IsNull();
        await Assert.That(symbols.EnumerableType).IsNull();
    }
}
