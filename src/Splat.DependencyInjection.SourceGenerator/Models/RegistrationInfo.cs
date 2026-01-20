// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Splat.DependencyInjection.SourceGenerator.Models;

/// <summary>
/// Base record for all registration types.
/// This is a cache-friendly POCO - contains only primitive data, no ISymbol/SyntaxNode references.
/// </summary>
internal abstract record RegistrationInfo(
    string InterfaceTypeFullName,
    string ConcreteTypeFullName,
    EquatableArray<ConstructorParameter> ConstructorParameters,
    EquatableArray<PropertyInjection> PropertyInjections,
    string? ContractValue,
    Location InvocationLocation);
