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
        var result = MetadataExtractor.ExtractConstructorParameters(type!);

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
        var result = MetadataExtractor.ExtractPropertyInjections(type!);

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

        // 1. Single public ctor
        var type = compilation.GetTypeByMetadataName("SingleCtor");
        var result = MetadataExtractor.ExtractConstructorParameters(type!);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!).Count().IsEqualTo(1);

        // 2. Private ctor
        type = compilation.GetTypeByMetadataName("PrivateCtor");
        result = MetadataExtractor.ExtractConstructorParameters(type!);
        await Assert.That(result).IsNull();

        // 3. Multiple ctors, no attribute
        type = compilation.GetTypeByMetadataName("MultipleCtorNoAttr");
        result = MetadataExtractor.ExtractConstructorParameters(type!);
        await Assert.That(result).IsNull();

        // 4. Multiple ctors, one attribute
        type = compilation.GetTypeByMetadataName("MultipleCtorOneAttr");
        result = MetadataExtractor.ExtractConstructorParameters(type!);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!).Count().IsEqualTo(1);
        await Assert.That(result![0].ParameterName).IsEqualTo("i");

        // 5. Multiple ctors, two attributes
        type = compilation.GetTypeByMetadataName("MultipleCtorTwoAttr");
        result = MetadataExtractor.ExtractConstructorParameters(type!);
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
        var result = MetadataExtractor.ExtractConstructorParameters(type!);

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

        // 1. Derived class (should get base prop too)
        var type = compilation.GetTypeByMetadataName("Derived");
        var result = MetadataExtractor.ExtractPropertyInjections(type!);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!).Count().IsEqualTo(2);
        await Assert.That(result!.Any(p => p.PropertyName == "BaseProp")).IsTrue();
        await Assert.That(result!.Any(p => p.PropertyName == "DerivedProp")).IsTrue();

        // 2. Private setter
        type = compilation.GetTypeByMetadataName("PrivateSetter");
        result = MetadataExtractor.ExtractPropertyInjections(type!);
        await Assert.That(result).IsNull();
    }
}
