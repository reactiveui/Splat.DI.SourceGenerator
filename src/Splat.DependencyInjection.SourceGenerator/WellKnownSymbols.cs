// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis;

namespace Splat.DependencyInjection.SourceGenerator;

/// <summary>The symbols extraction compares against, resolved once per compilation.</summary>
/// <remarks>
/// Every registration call in a compilation needs the same symbols. They are kept in a
/// <see cref="ConditionalWeakTable{TKey, TValue}"/> keyed by the compilation, so each transform finds them with one
/// lookup rather than five metadata-name searches, and they go when the compilation goes.
/// </remarks>
internal sealed class WellKnownSymbols
{
    /// <summary>The symbols of each live compilation.</summary>
    private static readonly ConditionalWeakTable<Compilation, WellKnownSymbols> Cache = new();

    /// <summary>The factory the cache calls on a miss, allocated once.</summary>
    private static readonly ConditionalWeakTable<Compilation, WellKnownSymbols>.CreateValueCallback Create =
        static compilation => new(compilation);

    /// <summary>Initializes a new instance of the <see cref="WellKnownSymbols"/> class.</summary>
    /// <param name="compilation">The compilation to resolve the symbols from.</param>
    internal WellKnownSymbols(Compilation compilation)
    {
        SourceAssembly = compilation.Assembly;
        ConstructorAttribute = compilation.GetTypeByMetadataName(Constants.ConstructorAttributeMetadataName);
        PropertyAttribute = compilation.GetTypeByMetadataName(Constants.PropertyAttributeMetadataName);
        LazyType = compilation.GetTypeByMetadataName(Constants.LazyMetadataName);
        EnumerableType = compilation.GetTypeByMetadataName(Constants.EnumerableMetadataName);
        LazyThreadSafetyModeType = compilation.GetTypeByMetadataName(Constants.LazyThreadSafetyModeMetadataName);
    }

    /// <summary>Gets the assembly being compiled: the only one whose types can carry this compilation's attributes.</summary>
    internal IAssemblySymbol SourceAssembly { get; }

    /// <summary>Gets the <c>DependencyInjectionConstructorAttribute</c> type, or <see langword="null"/> when it is ambiguous.</summary>
    internal INamedTypeSymbol? ConstructorAttribute { get; }

    /// <summary>Gets the <c>DependencyInjectionPropertyAttribute</c> type, or <see langword="null"/> when it is ambiguous.</summary>
    internal INamedTypeSymbol? PropertyAttribute { get; }

    /// <summary>Gets the open <see cref="System.Lazy{T}"/> type.</summary>
    internal INamedTypeSymbol? LazyType { get; }

    /// <summary>Gets the open <see cref="System.Collections.Generic.IEnumerable{T}"/> type.</summary>
    internal INamedTypeSymbol? EnumerableType { get; }

    /// <summary>Gets the <see cref="System.Threading.LazyThreadSafetyMode"/> type.</summary>
    internal INamedTypeSymbol? LazyThreadSafetyModeType { get; }

    /// <summary>Gets the symbols of a compilation, resolving them on its first request.</summary>
    /// <param name="compilation">The compilation.</param>
    /// <returns>The symbols.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static WellKnownSymbols For(Compilation compilation) => Cache.GetValue(compilation, Create);
}
