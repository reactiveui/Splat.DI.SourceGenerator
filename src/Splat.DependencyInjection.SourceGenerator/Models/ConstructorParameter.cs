// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;

namespace Splat.DependencyInjection.SourceGenerator.Models;

/// <summary>
/// Represents a constructor parameter for dependency injection.
/// This is a cache-friendly POCO - contains only primitive data, no ISymbol/SyntaxNode references.
/// </summary>
internal sealed record ConstructorParameter(
    string ParameterName,
    string TypeFullName,
    bool IsLazy,
    string? LazyInnerType) : IEquatable<ConstructorParameter>;
