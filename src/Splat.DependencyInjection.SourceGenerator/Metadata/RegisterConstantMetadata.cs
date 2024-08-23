// Copyright (c) 2019-2021 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Splat.DependencyInjection.SourceGenerator.Metadata;

internal record RegisterConstantMetadata(IMethodSymbol Method, ITypeSymbol InterfaceType, ITypeSymbol ConcreteType, InvocationExpressionSyntax MethodInvocation)
    : MethodMetadata(Method, InterfaceType, ConcreteType, MethodInvocation, false, [], [], []);
