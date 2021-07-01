using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Splat.DependencyInjection.SourceGenerator
{
    internal class SyntaxReceiver : ISyntaxReceiver
    {
        public List<InvocationExpressionSyntax> Register { get; } = new List<InvocationExpressionSyntax>();

        public List<InvocationExpressionSyntax> RegisterLazySingleton { get; } = new List<InvocationExpressionSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is not InvocationExpressionSyntax invocationExpression)
            {
                return;
            }

            if (invocationExpression.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                HandleSimpleName(memberAccess.Name, invocationExpression);

            }
            
            if (invocationExpression.Expression is MemberBindingExpressionSyntax bindingAccess)
            {
                HandleSimpleName(bindingAccess.Name, invocationExpression);
            }
        }

        private void HandleSimpleName(SimpleNameSyntax simpleName, InvocationExpressionSyntax invocationExpression)
        {
            if (simpleName is null)
            {
                return;
            }

            var methodName = simpleName.Identifier.Text;

            if (string.Equals(methodName, nameof(Register)))
            {
                Register.Add(invocationExpression);
            }

            if (string.Equals(methodName, nameof(RegisterLazySingleton)))
            {
                RegisterLazySingleton.Add(invocationExpression);
            }
        }
    }
}
