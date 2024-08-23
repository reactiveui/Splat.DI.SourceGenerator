// Copyright (c) 2019-2021 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

using ReactiveMarbles.RoslynHelpers;

namespace Splat.DependencyInjection.SourceGenerator.Metadata;

internal abstract record DependencyMetadata
{
    protected DependencyMetadata(ITypeSymbol type)
    {
        Type = type;
        TypeName = type.ToDisplayString(RoslynCommonHelpers.TypeFormat);
    }

    public ITypeSymbol Type { get; }

    public string TypeName { get; }
}
