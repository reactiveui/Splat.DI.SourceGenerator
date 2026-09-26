// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;

using Microsoft.CodeAnalysis;

namespace Splat.DependencyInjection.Analyzer.Tests;

/// <summary>
/// A runtime proxy for <see cref="IMethodSymbol"/> that returns a fixed set of attributes,
/// used to feed attribute shapes the compiler only produces for malformed metadata.
/// Every member other than <see cref="ISymbol.GetAttributes"/> throws.
/// </summary>
[DebuggerDisplay("AttributeOverridingMethodProxy: {Attributes.Length} attribute(s)")]
public class AttributeOverridingMethodProxy : DispatchProxy
{
    /// <summary>Gets or sets the attributes returned from <see cref="ISymbol.GetAttributes"/>.</summary>
    internal ImmutableArray<AttributeData> Attributes { get; set; } = [];

    /// <summary>Creates a method symbol whose <see cref="ISymbol.GetAttributes"/> returns the given attributes.</summary>
    /// <param name="attributes">The attributes to return.</param>
    /// <returns>The proxied method symbol.</returns>
    internal static IMethodSymbol Create(ImmutableArray<AttributeData> attributes)
    {
        var method = Create<IMethodSymbol, AttributeOverridingMethodProxy>();
        ((AttributeOverridingMethodProxy)(object)method).Attributes = attributes;
        return method;
    }

    /// <inheritdoc/>
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
        targetMethod?.Name == nameof(ISymbol.GetAttributes)
            ? Attributes
            : throw new NotSupportedException($"{targetMethod?.Name} is not supported by this test proxy.");
}
