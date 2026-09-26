// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Splat.DependencyInjection.SourceGenerator.Models;

/// <summary>A constructor parameter the generated factory resolves.</summary>
/// <param name="TypeFullName">The fully qualified type of the parameter.</param>
/// <param name="Kind">How the parameter is resolved.</param>
/// <param name="InnerTypeFullName">
/// The fully qualified type argument of a <see cref="DependencyKind.Lazy"/> or <see cref="DependencyKind.Collection"/>
/// parameter; <see langword="null"/> for a <see cref="DependencyKind.Service"/>.
/// </param>
/// <remarks>A value type, so a registration's parameters are one array rather than one object each.</remarks>
internal readonly record struct ConstructorParameter(string TypeFullName, DependencyKind Kind, string? InnerTypeFullName);
