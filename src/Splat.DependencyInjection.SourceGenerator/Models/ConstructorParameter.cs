// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;

namespace Splat.DependencyInjection.SourceGenerator.Models;

/// <summary>
/// Represents a constructor parameter for dependency injection.
/// This is a cache-friendly POCO - contains only primitive data, no ISymbol/SyntaxNode references.
/// </summary>
/// <param name="ParameterName">The name of the constructor parameter.</param>
/// <param name="TypeFullName">The fully qualified type name of the parameter.</param>
/// <param name="IsLazy">Whether the parameter is a <see cref="System.Lazy{T}"/> type.</param>
/// <param name="LazyInnerType">The inner type of the Lazy parameter, or null if not lazy.</param>
/// <param name="IsCollection">Whether the parameter is an IEnumerable collection type.</param>
/// <param name="CollectionItemType">The item type of the collection, or null if not a collection.</param>
internal sealed record ConstructorParameter(
    string ParameterName,
    string TypeFullName,
    bool IsLazy,
    string? LazyInnerType,
    bool IsCollection,
    string? CollectionItemType) : IEquatable<ConstructorParameter>;
