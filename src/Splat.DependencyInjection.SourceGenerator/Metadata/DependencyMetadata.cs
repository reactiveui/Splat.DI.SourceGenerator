// Copyright (c) 2019-2021 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Splat.DependencyInjection.SourceGenerator.Metadata;

internal abstract record DependencyMetadata
{
    // Standard display format for types
    private static readonly SymbolDisplayFormat TypeFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes
    );

    protected DependencyMetadata(ITypeSymbol type)
    {
        Type = type;
        TypeName = type.ToDisplayString(TypeFormat);
    }

    public ITypeSymbol Type { get; }

    public string TypeName { get; }
}
