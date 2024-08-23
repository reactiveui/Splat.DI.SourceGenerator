// Copyright (c) 2019-2021 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Splat.DependencyInjection.SourceGenerator;

internal class SyntaxReceiver : ISyntaxReceiver
{
    public List<InvocationExpressionSyntax> Register { get; } = [];

    public List<InvocationExpressionSyntax> RegisterLazySingleton { get; } = [];

    public List<InvocationExpressionSyntax> RegisterConstant { get; } = [];

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is not InvocationExpressionSyntax invocationExpression)
        {
            return;
        }

        switch (invocationExpression.Expression)
        {
            case MemberAccessExpressionSyntax memberAccess:
                HandleSimpleName(memberAccess.Name, invocationExpression);
                break;
            case MemberBindingExpressionSyntax bindingAccess:
                HandleSimpleName(bindingAccess.Name, invocationExpression);
                break;
        }
    }

    private void HandleSimpleName(SimpleNameSyntax simpleName, InvocationExpressionSyntax invocationExpression)
    {
        var methodName = simpleName.Identifier.Text;

        if (string.Equals(methodName, nameof(Register)))
        {
            Register.Add(invocationExpression);
        }

        if (string.Equals(methodName, nameof(RegisterLazySingleton)))
        {
            RegisterLazySingleton.Add(invocationExpression);
        }

        if (string.Equals(methodName, nameof(RegisterConstant)))
        {
            RegisterConstant.Add(invocationExpression);
        }
    }
}
