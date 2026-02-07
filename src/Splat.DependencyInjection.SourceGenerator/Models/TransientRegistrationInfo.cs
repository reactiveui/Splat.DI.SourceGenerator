// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Splat.DependencyInjection.SourceGenerator.Models;

/// <summary>
/// Represents a transient registration (created per resolve).
/// This is a cache-friendly POCO - contains only primitive data, no ISymbol/SyntaxNode references.
/// </summary>
/// <param name="InterfaceTypeFullName">The fully qualified type name of the interface being registered.</param>
/// <param name="ConcreteTypeFullName">The fully qualified type name of the concrete implementation.</param>
/// <param name="ConstructorParameters">The constructor parameters for the concrete type.</param>
/// <param name="PropertyInjections">The property injections for the concrete type.</param>
/// <param name="ContractValue">The optional contract key value, or null for unkeyed registrations.</param>
/// <param name="InvocationLocation">The source location of the registration invocation.</param>
internal sealed record TransientRegistrationInfo(
    string InterfaceTypeFullName,
    string ConcreteTypeFullName,
    EquatableArray<ConstructorParameter> ConstructorParameters,
    EquatableArray<PropertyInjection> PropertyInjections,
    string? ContractValue,
    Location InvocationLocation)
    : RegistrationInfo(
        InterfaceTypeFullName,
        ConcreteTypeFullName,
        ConstructorParameters,
        PropertyInjections,
        ContractValue,
        InvocationLocation);
