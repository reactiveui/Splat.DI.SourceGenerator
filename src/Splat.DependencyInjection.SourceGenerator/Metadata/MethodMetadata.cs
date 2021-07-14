// Copyright (c) 2019-2021 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using ReactiveMarbles.RoslynHelpers;

namespace Splat.DependencyInjection.SourceGenerator.Metadata
{
    internal abstract record MethodMetadata
    {
        public MethodMetadata(IMethodSymbol method, ITypeSymbol interfaceType, ITypeSymbol concreteType, InvocationExpressionSyntax methodInvocation, bool isLazy, IReadOnlyList<ConstructorDependencyMetadata> constructorDependencies, IReadOnlyList<PropertyDependencyMetadata> properties, IReadOnlyList<ParameterMetadata> registerParameterValues)
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

        public IMethodSymbol Method { get; init; }

        public InvocationExpressionSyntax MethodInvocation { get; init; }

        public bool IsLazy { get; init; }

        public IReadOnlyList<ConstructorDependencyMetadata> ConstructorDependencies { get; init; }

        public IReadOnlyList<PropertyDependencyMetadata> Properties { get; init; }

        public IReadOnlyList<ParameterMetadata> RegisterParameterValues { get; init; }

        public ITypeSymbol ConcreteType { get; }

        public ITypeSymbol InterfaceType { get; }

        public string ConcreteTypeName { get; }

        public string InterfaceTypeName { get; }
    }
}
