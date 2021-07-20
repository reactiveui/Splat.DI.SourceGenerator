// Copyright (c) 2019-2021 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Splat.DependencyInjection.SourceGenerator
{
    internal static class DiagnosticWarnings
    {
        internal static readonly DiagnosticDescriptor MultipleConstructorNeedAttribute = new(
            "SPLATDI001",
            "Can't find valid constructor",
            "{0} has more than one constructor and one hasn't been marked with DependencyInjectionConstructorAttribute",
            "Compiler",
            DiagnosticSeverity.Error,
            true);

        internal static readonly DiagnosticDescriptor PropertyMustPublicBeSettable = new(
            "SPLATDI002",
            "Property must be public/internal settable",
            "{0} property marked with DependencyInjectionPropertyAttribute must have a public or internal setter",
            "Compiler",
            DiagnosticSeverity.Error,
            true);

        internal static readonly DiagnosticDescriptor MultipleConstructorsMarked = new(
            "SPLATDI003",
            "Multiple constructors have DependencyInjectionConstructorAttribute",
            "{0} has multiple constructors marked with the DependencyInjectionConstructorAttribute attribute change so only one is marked",
            "Compiler",
            DiagnosticSeverity.Error,
            true);

        internal static readonly DiagnosticDescriptor ConstructorsMustBePublic = new(
            "SPLATDI004",
            "Constructors not public or internal",
            "{0} constructor declared with DependencyInjectionConstructorAttribute attribute must be public or internal",
            "Compiler",
            DiagnosticSeverity.Error,
            true);

        internal static readonly DiagnosticDescriptor ConstructorsMustNotHaveCircularDependency = new(
            "SPLATDI005",
            "Constructors must not have a circular dependency",
            "Constructor parameters must not have a circular dependency to another registered resource",
            "Compiler",
            DiagnosticSeverity.Error,
            true);

        internal static readonly DiagnosticDescriptor InterfaceRegisteredMultipleTimes = new(
            "SPLATDI006",
            "Interface has been registered before",
            "{0} has been registered in multiple places",
            "Compiler",
            DiagnosticSeverity.Warning,
            true);

        internal static readonly DiagnosticDescriptor LazyParameterNotRegisteredLazy = new(
            "SPLATDI007",
            "Constructor has a lazy parameter",
            "{0} constructor has a lazy parameter {1} which is not registered with RegisterLazySingleton",
            "Compiler",
            DiagnosticSeverity.Error,
            true);
    }
}
