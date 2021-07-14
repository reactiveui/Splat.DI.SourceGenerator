// Copyright (c) 2019-2021 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Splat.DependencyInjection.SourceGenerator
{
    internal static class DiagnosticWarnings
    {
        internal static readonly DiagnosticDescriptor MultipleConstructorNeedAttribute = new(
            id: "SPLATDI001",
            title: "Can't find valid constructor",
            messageFormat: "{0} has more than one constructor and one hasn't been marked with DependencyInjectionConstructorAttribute",
            category: "Compiler",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor PropertyMustPublicBeSettable = new(
            id: "SPLATDI002",
            title: "Property must be public/internal settable",
            messageFormat: "{0} property marked with DependencyInjectionPropertyAttribute must have a public or internal setter",
            category: "Compiler",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor MultipleConstructorsMarked = new(
            id: "SPLATDI003",
            title: "Multiple constructors have DependencyInjectionConstructorAttribute",
            messageFormat: "{0} has multiple constructors marked with the DependencyInjectionConstructorAttribute attribute change so only one is marked",
            category: "Compiler",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor ConstructorsMustBePublic = new(
            id: "SPLATDI004",
            title: "Constructors not public or internal",
            messageFormat: "{0} constructor declared with DependencyInjectionConstructorAttribute attribute must be public or internal",
            category: "Compiler",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor ConstructorsMustNotHaveCircularDependency = new(
            id: "SPLATDI005",
            title: "Constructors must not have a circular dependency",
            messageFormat: "Constructor parameters must not have a circular dependency to another registered resource",
            category: "Compiler",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}
