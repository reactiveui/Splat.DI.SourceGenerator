// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Splat.DependencyInjection.SourceGenerator;

/// <summary>Syntax and symbol tests the pipeline runs before and while it binds a call.</summary>
internal static class RoslynHelpers
{
    /// <summary>The largest number of arguments, and of type arguments, a marker method takes.</summary>
    private const int MaxMarkerArguments = 2;

    /// <summary>Tests whether a syntax node could be a call to a marker method, from its syntax alone.</summary>
    /// <param name="node">The syntax node.</param>
    /// <returns><see langword="true"/> for a call worth binding.</returns>
    /// <remarks>
    /// <para>
    /// This runs on every node of every file on every edit, so it reads only syntax and allocates nothing. Every
    /// marker is generic and none can infer its type arguments, so a call names them; a call with no type argument
    /// list is not one. No marker takes a lambda, so a call passing one - Splat's own
    /// <c>resolver.Register&lt;T&gt;(() =&gt; ...)</c> - is not one either.
    /// </para>
    /// <para>
    /// A call through <c>?.</c> is not considered: the markers are static, and a type cannot be conditionally accessed.
    /// </para>
    /// </remarks>
    internal static bool IsRegistrationInvocation(SyntaxNode node)
    {
        if (node is not InvocationExpressionSyntax invocation)
        {
            return false;
        }

        var name = invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
            SimpleNameSyntax simpleName => simpleName,
            _ => null,
        };

        if (name is not GenericNameSyntax { TypeArgumentList.Arguments.Count: > 0 and <= MaxMarkerArguments } genericName
            || genericName.Identifier.ValueText is not (Constants.MethodNameRegister or Constants.MethodNameRegisterLazySingleton))
        {
            return false;
        }

        var arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count > MaxMarkerArguments)
        {
            return false;
        }

        foreach (var argument in arguments)
        {
            if (argument.Expression is AnonymousFunctionExpressionSyntax)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Tests whether a bound method is one of the marker methods on <c>Splat.SplatRegistrations</c>.</summary>
    /// <param name="methodSymbol">The method symbol.</param>
    /// <returns><see langword="true"/> for a marker method.</returns>
    /// <remarks>
    /// The syntax predicate has already matched the method's name. A method bound from a call always has a containing
    /// type - a local function's is the type it is declared in - and a type always has a containing namespace.
    /// </remarks>
    internal static bool IsSplatRegistrationsMethod(IMethodSymbol methodSymbol)
    {
        var containingType = methodSymbol.ContainingType!;
        return !methodSymbol.IsExtensionMethod
            && containingType.Name == Constants.ClassName
            && containingType.ContainingNamespace!.Name == Constants.NamespaceName;
    }

    /// <summary>Builds a reference to a field or property that compiles from any namespace.</summary>
    /// <param name="symbol">The field or property.</param>
    /// <returns>The fully qualified containing type, a dot, and the member's name.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string GetFullyQualifiedMemberReference(ISymbol symbol) =>
        $"{symbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{symbol.Name}";

    /// <summary>Builds a call to a method that compiles from any namespace.</summary>
    /// <param name="invokedMethod">The called method.</param>
    /// <param name="invocation">The call as written.</param>
    /// <returns>
    /// The fully qualified containing type, then the method's name and type arguments and the argument list as written.
    /// </returns>
    internal static string GetFullyQualifiedMethodInvocation(IMethodSymbol invokedMethod, InvocationExpressionSyntax invocation)
    {
        var name = invocation.Expression is MemberAccessExpressionSyntax memberAccess ? memberAccess.Name : invocation.Expression;
        return $"{invokedMethod.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{name}{invocation.ArgumentList}";
    }

    /// <summary>Finds the name of the parameter an argument is passed to.</summary>
    /// <param name="argument">The argument.</param>
    /// <param name="methodSymbol">The bound method.</param>
    /// <param name="position">The argument's position in the list.</param>
    /// <returns>The parameter's name.</returns>
    /// <remarks>
    /// The call bound, so a named argument names a parameter that exists and a positional one sits at a position that
    /// exists.
    /// </remarks>
    internal static string GetParameterName(ArgumentSyntax argument, IMethodSymbol methodSymbol, int position) =>
        argument.NameColon?.Name.Identifier.ValueText ?? methodSymbol.Parameters[position].Name;
}
