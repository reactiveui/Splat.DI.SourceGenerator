// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Splat.DependencyInjection.SourceGenerator.Models;

/// <summary>Everything the generated code needs for one registration.</summary>
/// <param name="Kind">How the registration creates its instances.</param>
/// <param name="InterfaceTypeFullName">The fully qualified type being registered.</param>
/// <param name="ConcreteTypeFullName">The fully qualified type that is constructed.</param>
/// <param name="ConstructorParameters">The parameters of the constructor that is called.</param>
/// <param name="PropertyInjections">The properties set after construction.</param>
/// <param name="ContractValue">The contract expression, as C# source; <see langword="null"/> for none.</param>
/// <param name="LazyThreadSafetyMode">The thread safety mode expression, as C# source; <see langword="null"/> for the default.</param>
/// <remarks>
/// Holds strings and values only - no symbol, syntax node or location - so it compares by value and an edit that
/// leaves the registration unchanged, such as one that moves it down a line, leaves the generated file cached.
/// </remarks>
internal sealed record RegistrationInfo(
    RegistrationKind Kind,
    string InterfaceTypeFullName,
    string ConcreteTypeFullName,
    EquatableArray<ConstructorParameter> ConstructorParameters,
    EquatableArray<PropertyInjection> PropertyInjections,
    string? ContractValue,
    string? LazyThreadSafetyMode);
