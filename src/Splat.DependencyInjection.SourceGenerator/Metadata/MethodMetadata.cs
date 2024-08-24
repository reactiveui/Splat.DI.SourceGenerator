// Copyright (c) 2019-2021 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using ReactiveMarbles.RoslynHelpers;

namespace Splat.DependencyInjection.SourceGenerator.Metadata;

internal abstract record MethodMetadata
{
    protected MethodMetadata(IMethodSymbol method, ITypeSymbol interfaceType, ITypeSymbol concreteType, InvocationExpressionSyntax methodInvocation, bool isLazy, IReadOnlyList<ConstructorDependencyMetadata> constructorDependencies, IReadOnlyList<PropertyDependencyMetadata> properties, IReadOnlyList<ParameterMetadata> registerParameterValues)
    {
        Method = method;
        MethodInvocation = methodInvocation;
        IsLazy = isLazy;
        ConstructorDependencies = constructorDependencies;
        Properties = properties;
        ConcreteType = concreteType;
        InterfaceType = interfaceType;
        ConcreteTypeName = ConcreteType.ToDisplayString(RoslynCommonHelpers.TypeFormat);
        InterfaceTypeName = InterfaceType.ToDisplayString(RoslynCommonHelpers.TypeFormat);
        RegisterParameterValues = registerParameterValues;
    }

    public IMethodSymbol Method { get; }

    public InvocationExpressionSyntax MethodInvocation { get; }

    public bool IsLazy { get; }

    public IReadOnlyList<ConstructorDependencyMetadata> ConstructorDependencies { get; }

    public IReadOnlyList<PropertyDependencyMetadata> Properties { get; }

    public IReadOnlyList<ParameterMetadata> RegisterParameterValues { get; }

    public ITypeSymbol ConcreteType { get; }

    public ITypeSymbol InterfaceType { get; }

    public string ConcreteTypeName { get; }

    public string InterfaceTypeName { get; }
}
