// Copyright (c) 2019-2021 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Splat.DependencyInjection.SourceGenerator.Metadata;

internal record RegisterMetadata(IMethodSymbol Method, ITypeSymbol InterfaceType, ITypeSymbol ConcreteType, InvocationExpressionSyntax MethodInvocation, IReadOnlyList<ConstructorDependencyMetadata> ConstructorDependencies, IReadOnlyList<PropertyDependencyMetadata> Properties, IReadOnlyList<ParameterMetadata> RegisterParameterValues)
    : MethodMetadata(Method, InterfaceType, ConcreteType, MethodInvocation, false, ConstructorDependencies, Properties, RegisterParameterValues);
