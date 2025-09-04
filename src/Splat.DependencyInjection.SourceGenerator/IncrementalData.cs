// Copyright (c) 2019-2021 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Splat.DependencyInjection.SourceGenerator;

/// <summary>
/// Represents a method invocation for DI registration.
/// </summary>
/// <param name="InvocationSyntax">The invocation expression syntax node.</param>
/// <param name="MethodName">The name of the method being called (Register, RegisterLazySingleton, RegisterConstant).</param>
internal record RegistrationCall(
    InvocationExpressionSyntax InvocationSyntax,
    string MethodName
);

/// <summary>
/// Represents dependency information for constructor or property injection.
/// </summary>
/// <param name="TypeName">The fully qualified type name of the dependency.</param>
/// <param name="IsLazy">Whether the dependency is wrapped in Lazy.</param>
/// <param name="ParameterName">The parameter name for constructor dependencies.</param>
/// <param name="PropertyName">The property name for property dependencies.</param>
internal record DependencyInfo(
    string TypeName,
    bool IsLazy,
    string? ParameterName = null,
    string? PropertyName = null
);

/// <summary>
/// Represents a validated registration target ready for code generation.
/// </summary>
/// <param name="MethodName">The registration method name (Register, RegisterLazySingleton, RegisterConstant).</param>
/// <param name="InterfaceType">The interface or service type being registered.</param>
/// <param name="ConcreteType">The concrete implementation type (null for RegisterConstant).</param>
/// <param name="Contract">Optional contract string.</param>
/// <param name="LazyMode">Optional lazy thread safety mode.</param>
/// <param name="ConstructorDependencies">Dependencies injected via constructor.</param>
/// <param name="PropertyDependencies">Dependencies injected via properties.</param>
/// <param name="HasAttribute">Whether the constructor has DependencyInjectionConstructor attribute.</param>
internal record RegistrationTarget(
    string MethodName,
    string InterfaceType,
    string? ConcreteType,
    string? Contract,
    string? LazyMode,
    ImmutableArray<DependencyInfo> ConstructorDependencies,
    ImmutableArray<DependencyInfo> PropertyDependencies,
    bool HasAttribute
);

/// <summary>
/// Represents all registrations collected for code generation.
/// </summary>
/// <param name="Registrations">All validated registration targets.</param>
internal record RegistrationGroup(
    ImmutableArray<RegistrationTarget> Registrations
);

/// <summary>
/// Represents the generated source code.
/// </summary>
/// <param name="FileName">The name of the generated file.</param>
/// <param name="SourceCode">The generated source code content.</param>
internal record GeneratedSource(
    string FileName,
    string SourceCode
);
