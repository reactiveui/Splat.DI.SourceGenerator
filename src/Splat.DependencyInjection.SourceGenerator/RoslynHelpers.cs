// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Splat.DependencyInjection.SourceGenerator;

/// <summary>
/// Helper methods for working with Roslyn symbols and syntax nodes.
/// </summary>
internal static class RoslynHelpers
{
    private static readonly SymbolDisplayFormat _fullyQualifiedFormat = SymbolDisplayFormat.FullyQualifiedFormat;

    /// <summary>
    /// Checks if a syntax node is a Register method invocation.
    /// </summary>
    /// <param name="node">The syntax node to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the node is a Register invocation, false otherwise.</returns>
    internal static bool IsRegisterInvocation(SyntaxNode node, CancellationToken ct)
    {
        if (node is not InvocationExpressionSyntax invocation)
        {
            return false;
        }

        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax { Name.Identifier.Text: "Register" } => true,
            MemberBindingExpressionSyntax { Name.Identifier.Text: "Register" } => true,
            _ => false
        };
    }

    /// <summary>
    /// Checks if a syntax node is a RegisterLazySingleton method invocation.
    /// </summary>
    /// <param name="node">The syntax node to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the node is a RegisterLazySingleton invocation, false otherwise.</returns>
    internal static bool IsRegisterLazySingletonInvocation(SyntaxNode node, CancellationToken ct)
    {
        if (node is not InvocationExpressionSyntax invocation)
        {
            return false;
        }

        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax { Name.Identifier.Text: "RegisterLazySingleton" } => true,
            MemberBindingExpressionSyntax { Name.Identifier.Text: "RegisterLazySingleton" } => true,
            _ => false
        };
    }

    /// <summary>
    /// Checks if a method symbol is a specific method from SplatRegistrations class.
    /// </summary>
    /// <param name="methodSymbol">The method symbol to check.</param>
    /// <param name="methodName">The expected method name.</param>
    /// <returns>True if the method is from SplatRegistrations with the specified name.</returns>
    internal static bool IsSplatRegistrationsMethod(IMethodSymbol methodSymbol, string methodName)
    {
        var containingType = methodSymbol.ContainingType?.OriginalDefinition;
        if (containingType == null)
        {
            return false;
        }

        return containingType.ContainingNamespace?.Name == Constants.NamespaceName &&
               containingType.Name == Constants.ClassName &&
               methodSymbol.Name == methodName &&
               !methodSymbol.IsExtensionMethod;
    }

    /// <summary>
    /// Gets all base types and the type itself in inheritance order.
    /// </summary>
    /// <param name="type">The type to get base types for.</param>
    /// <returns>An enumerable of the type and all its base types.</returns>
    internal static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(ITypeSymbol type)
    {
        var current = type;
        while (current != null)
        {
            yield return current;
            current = current.BaseType;
        }
    }

    /// <summary>
    /// Extracts the contract parameter value from a method invocation.
    /// </summary>
    /// <param name="methodSymbol">The method symbol being invoked.</param>
    /// <param name="invocation">The invocation expression.</param>
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The contract value as a string, or null if not found.</returns>
    internal static string? ExtractContractParameter(
        IMethodSymbol methodSymbol,
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        CancellationToken ct)
    {
        for (int i = 0; i < invocation.ArgumentList.Arguments.Count; i++)
        {
            var argument = invocation.ArgumentList.Arguments[i];
            var parameter = methodSymbol.Parameters[i];

            if (parameter.Name != "contract")
            {
                continue;
            }

            var expression = argument.Expression;

            // Handle string literals
            if (expression is LiteralExpressionSyntax literal)
            {
                return literal.ToString();
            }

            // Handle constant expressions
            var symbolInfo = semanticModel.GetSymbolInfo(expression, ct);
            if (symbolInfo.Symbol != null)
            {
                return symbolInfo.Symbol.ToDisplayString(_fullyQualifiedFormat);
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts the LazyThreadSafetyMode parameter value from a RegisterLazySingleton invocation.
    /// </summary>
    /// <param name="methodSymbol">The method symbol being invoked.</param>
    /// <param name="invocation">The invocation expression.</param>
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The thread safety mode as a string, or null if not found.</returns>
    internal static string? ExtractLazyThreadSafetyMode(
        IMethodSymbol methodSymbol,
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        CancellationToken ct)
    {
        for (int i = 0; i < invocation.ArgumentList.Arguments.Count; i++)
        {
            var argument = invocation.ArgumentList.Arguments[i];
            var parameter = methodSymbol.Parameters[i];

            if (parameter.Name != "mode")
            {
                continue;
            }

            var expression = argument.Expression;
            var symbolInfo = semanticModel.GetSymbolInfo(expression, ct);

            if (symbolInfo.Symbol != null)
            {
                return symbolInfo.Symbol.ToDisplayString(_fullyQualifiedFormat);
            }
        }

        return null;
    }
}
