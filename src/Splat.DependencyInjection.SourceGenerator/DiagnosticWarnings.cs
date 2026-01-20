// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Splat.DependencyInjection.SourceGenerator;

/// <summary>
/// Diagnostic descriptors for Splat DI source generator and analyzer.
/// </summary>
public static class DiagnosticWarnings
{
    /// <summary>
    /// SPLATDI001: Class has multiple constructors without DependencyInjectionConstructorAttribute.
    /// </summary>
    public static readonly DiagnosticDescriptor MultipleConstructorNeedAttribute = new(
        "SPLATDI001",
        "Can't find valid constructor",
        "{0} has more than one constructor and one hasn't been marked with DependencyInjectionConstructorAttribute",
        "Compiler",
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    /// SPLATDI002: Property marked with DependencyInjectionPropertyAttribute must have public or internal setter.
    /// </summary>
    public static readonly DiagnosticDescriptor PropertyMustPublicBeSettable = new(
        "SPLATDI002",
        "Property must be public/internal settable",
        "{0} property marked with DependencyInjectionPropertyAttribute must have a public or internal setter",
        "Compiler",
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    /// SPLATDI003: Multiple constructors marked with DependencyInjectionConstructorAttribute.
    /// </summary>
    public static readonly DiagnosticDescriptor MultipleConstructorsMarked = new(
        "SPLATDI003",
        "Multiple constructors have DependencyInjectionConstructorAttribute",
        "{0} has multiple constructors marked with the DependencyInjectionConstructorAttribute attribute change so only one is marked",
        "Compiler",
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    /// SPLATDI004: Constructor marked with DependencyInjectionConstructorAttribute must be public or internal.
    /// </summary>
    public static readonly DiagnosticDescriptor ConstructorsMustBePublic = new(
        "SPLATDI004",
        "Constructors not public or internal",
        "{0} constructor declared with DependencyInjectionConstructorAttribute attribute must be public or internal",
        "Compiler",
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    /// SPLATDI005: Constructor parameters must not have circular dependencies.
    /// </summary>
    public static readonly DiagnosticDescriptor ConstructorsMustNotHaveCircularDependency = new(
        "SPLATDI005",
        "Constructors must not have a circular dependency",
        "Constructor parameters must not have a circular dependency to another registered resource",
        "Compiler",
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    /// SPLATDI006: Interface has been registered multiple times.
    /// </summary>
    public static readonly DiagnosticDescriptor InterfaceRegisteredMultipleTimes = new(
        "SPLATDI006",
        "Interface has been registered before",
        "{0} has been registered in multiple places",
        "Compiler",
        DiagnosticSeverity.Warning,
        true);

    /// <summary>
    /// SPLATDI007: Constructor has a lazy parameter that is not registered with RegisterLazySingleton.
    /// </summary>
    public static readonly DiagnosticDescriptor LazyParameterNotRegisteredLazy = new(
        "SPLATDI007",
        "Constructor has a lazy parameter",
        "{0} constructor has a lazy parameter {1} which is not registered with RegisterLazySingleton",
        "Compiler",
        DiagnosticSeverity.Error,
        true);
}
