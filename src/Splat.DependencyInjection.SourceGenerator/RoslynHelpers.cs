// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Splat.DependencyInjection.SourceGenerator;

/// <summary>
/// Helper methods for working with Roslyn symbols and syntax nodes.
/// </summary>
internal static class RoslynHelpers
{
    /// <summary>
    /// The fully qualified symbol display format used for type name resolution.
    /// </summary>
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
            MemberAccessExpressionSyntax { Name.Identifier.Text: Constants.MethodNameRegister } => true,
            MemberBindingExpressionSyntax { Name.Identifier.Text: Constants.MethodNameRegister } => true,
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
            MemberAccessExpressionSyntax { Name.Identifier.Text: Constants.MethodNameRegisterLazySingleton } => true,
            MemberBindingExpressionSyntax { Name.Identifier.Text: Constants.MethodNameRegisterLazySingleton } => true,
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
    /// <returns>An array of the type and all its base types.</returns>
    internal static ITypeSymbol[] GetBaseTypesAndThis(ITypeSymbol type)
    {
        // Count inheritance depth first (typical: 1-5 levels)
        var depth = 0;
        var current = type;
        while (current != null)
        {
            depth++;
            current = current.BaseType;
        }

        // Early exit for empty case (shouldn't happen, but defensive)
        if (depth == 0)
        {
            return [];
        }

        // Allocate exact-size array and populate
        var result = new ITypeSymbol[depth];
        current = type;
        for (var i = 0; i < depth; i++)
        {
            if (current == null)
            {
                // Defensive: shouldn't happen if depth calculation was correct
                // Return partial results collected so far
                var partial = new ITypeSymbol[i];
                Array.Copy(result, partial, i);
                return partial;
            }

            result[i] = current;
            current = current.BaseType;
        }

        return result;
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
            var parameter = ResolveParameterForArgument(argument, methodSymbol, i);

            if (parameter == null || parameter.Name != Constants.ParameterNameContract)
            {
                continue;
            }

            var expression = argument.Expression;

            // Handle string literals
            if (expression is LiteralExpressionSyntax literal)
            {
                // Returns raw literal token (includes quotes), safe for embedding in generated code
                return literal.ToString();
            }

            // Handle non-literal expressions (constant fields, properties, etc.)
            // We need to fully qualify the reference to avoid CS0103 errors in generated code
            // when the symbol is from a different namespace (GitHub issue: Keys from different namespace)
            var symbolInfo = semanticModel.GetSymbolInfo(expression, ct);
            if (symbolInfo.Symbol is IFieldSymbol or IPropertySymbol)
            {
                return GetFullyQualifiedMemberReference(symbolInfo.Symbol);
            }

            // Handle method invocation expressions by fully qualifying the containing type
            // while preserving the original argument list from the syntax tree.
            if (symbolInfo.Symbol is IMethodSymbol invokedMethod
                && expression is InvocationExpressionSyntax contractInvocation
                && invokedMethod.ContainingType != null)
            {
                return GetFullyQualifiedMethodInvocation(invokedMethod, contractInvocation);
            }

            // For other resolved symbols (locals, etc.)
            // preserve the expression as written since ToDisplayString may produce
            // a signature-like string that is not a valid expression in generated code
            if (symbolInfo.Symbol != null)
            {
                return expression.ToString();
            }
        }

        return null;
    }

    /// <summary>
    /// Gets a fully qualified reference string for a symbol (field, property, or other member).
    /// For fields/properties, returns the fully qualified containing type plus the member name.
    /// For other symbols, returns the fully qualified name directly.
    /// </summary>
    /// <param name="symbol">The symbol to get the fully qualified reference for.</param>
    /// <returns>A fully qualified reference string safe for use in generated code.</returns>
    internal static string GetFullyQualifiedMemberReference(ISymbol symbol)
    {
        // For fields and properties, we need to build: global::Namespace.Type.MemberName
        if (symbol is IFieldSymbol or IPropertySymbol)
        {
            var containingType = symbol.ContainingType;
            if (containingType != null)
            {
                var fullyQualifiedTypeName = containingType.ToDisplayString(_fullyQualifiedFormat);
                return $"{fullyQualifiedTypeName}.{symbol.Name}";
            }
        }

        // For other symbols (e.g., local variables, parameters), return the display string
        return symbol.ToDisplayString(_fullyQualifiedFormat);
    }

    /// <summary>
    /// Gets a fully qualified method invocation string for use in generated code.
    /// Fully qualifies the containing type while preserving the method name (including type arguments)
    /// and the original argument list from the syntax tree.
    /// </summary>
    /// <param name="invokedMethod">The method symbol being invoked.</param>
    /// <param name="invocation">The invocation expression syntax.</param>
    /// <returns>A fully qualified method invocation string safe for use in generated code.</returns>
    internal static string GetFullyQualifiedMethodInvocation(IMethodSymbol invokedMethod, InvocationExpressionSyntax invocation)
    {
        var fullyQualifiedTypeName = invokedMethod.ContainingType!.ToDisplayString(_fullyQualifiedFormat);

        // Extract method name from syntax to preserve type arguments (e.g., GetKey<string>)
        var methodName = invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.ToString(),
            SimpleNameSyntax simpleName => simpleName.ToString(),
            _ => invokedMethod.Name
        };

        return $"{fullyQualifiedTypeName}.{methodName}{invocation.ArgumentList}";
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
            var parameter = ResolveParameterForArgument(argument, methodSymbol, i);

            if (parameter == null || parameter.Name != Constants.ParameterNameMode)
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

    /// <summary>
    /// Resolves the method parameter that corresponds to a given invocation argument,
    /// handling both positional and named arguments correctly.
    /// </summary>
    /// <param name="argument">The argument syntax from the invocation.</param>
    /// <param name="methodSymbol">The method symbol being invoked.</param>
    /// <param name="positionalIndex">The zero-based positional index of the argument in the argument list.</param>
    /// <returns>The corresponding parameter symbol, or <see langword="null"/> if the parameter cannot be resolved.</returns>
    internal static IParameterSymbol? ResolveParameterForArgument(
        ArgumentSyntax argument,
        IMethodSymbol methodSymbol,
        int positionalIndex)
    {
        if (argument.NameColon != null)
        {
            var argumentName = argument.NameColon.Name.Identifier.Text;
            for (int j = 0; j < methodSymbol.Parameters.Length; j++)
            {
                if (methodSymbol.Parameters[j].Name == argumentName)
                {
                    return methodSymbol.Parameters[j];
                }
            }

            return null;
        }

        if (positionalIndex < 0 || positionalIndex >= methodSymbol.Parameters.Length)
        {
            return null;
        }

        return methodSymbol.Parameters[positionalIndex];
    }
}
