// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

namespace Splat.DependencyInjection.Analyzer.Tests;

/// <summary>
/// Attribute data with no attribute class or constructor, matching what the compiler produces
/// for a metadata attribute whose constructor cannot be decoded.
/// </summary>
internal sealed class NullClassAttributeData : AttributeData
{
    /// <inheritdoc/>
    protected override INamedTypeSymbol? CommonAttributeClass => null;

    /// <inheritdoc/>
    protected override IMethodSymbol? CommonAttributeConstructor => null;

    /// <inheritdoc/>
    protected override SyntaxReference? CommonApplicationSyntaxReference => null;

    /// <inheritdoc/>
    protected override ImmutableArray<TypedConstant> CommonConstructorArguments => [];

    /// <inheritdoc/>
    protected override ImmutableArray<KeyValuePair<string, TypedConstant>> CommonNamedArguments => [];
}
