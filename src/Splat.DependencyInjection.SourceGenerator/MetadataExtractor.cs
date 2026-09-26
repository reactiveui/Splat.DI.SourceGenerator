// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Splat.DependencyInjection.SourceGenerator.Models;

namespace Splat.DependencyInjection.SourceGenerator;

/// <summary>Turns a bound marker call into the value-only model the rest of the pipeline works from.</summary>
/// <remarks>
/// Binding a call is the costliest thing a transform does, so each check that can turn a call away without binding it
/// runs first, and each name is read from a symbol at most once.
/// </remarks>
internal static class MetadataExtractor
{
    /// <summary>The fully qualified name of a lazy before its type argument.</summary>
    private const string LazyPrefix = "global::System.Lazy<";

    /// <summary>The fully qualified name of a collection before its type argument.</summary>
    private const string EnumerablePrefix = "global::System.Collections.Generic.IEnumerable<";

    /// <summary>The fully qualified <see cref="System.Threading.LazyThreadSafetyMode"/> members, indexed by value.</summary>
    private static readonly string[] LazyThreadSafetyModeNames =
    [
        "global::System.Threading.LazyThreadSafetyMode.None",
        "global::System.Threading.LazyThreadSafetyMode.PublicationOnly",
        "global::System.Threading.LazyThreadSafetyMode.ExecutionAndPublication",
    ];

    /// <summary>Extracts the registration a marker call makes; the pipeline's transform.</summary>
    /// <param name="context">The call and its semantic model.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The registration and where it is made, or <see langword="null"/> when the call is not a valid registration.</returns>
    internal static RegistrationSite? Extract(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess
            && !IsRegistrationsReceiver(memberAccess.Expression, semanticModel, cancellationToken))
        {
            return null;
        }

        if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol method
            || !RoslynHelpers.IsSplatRegistrationsMethod(method))
        {
            return null;
        }

        var registration = ExtractRegistration(method, invocation, semanticModel, WellKnownSymbols.For(semanticModel.Compilation), cancellationToken);
        return registration is null ? null : new(registration, LocationInfo.From(invocation));
    }

    /// <summary>Tests whether the receiver of a call names the <c>SplatRegistrations</c> class.</summary>
    /// <param name="receiver">The expression before the call's name.</param>
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when the receiver could be the class.</returns>
    /// <remarks>
    /// A receiver spelled <c>SplatRegistrations</c> is accepted from syntax. Anything else - a using alias, or a
    /// resolver variable in a call to Splat's own <c>Register</c> - binds on its own, which is far cheaper than binding
    /// the whole call with its overloads and type inference.
    /// </remarks>
    internal static bool IsRegistrationsReceiver(ExpressionSyntax receiver, SemanticModel semanticModel, CancellationToken cancellationToken) =>
        receiver is IdentifierNameSyntax { Identifier.ValueText: Constants.ClassName }
            or MemberAccessExpressionSyntax { Name.Identifier.ValueText: Constants.ClassName }
        || semanticModel.GetSymbolInfo(receiver, cancellationToken).Symbol is INamedTypeSymbol { Name: Constants.ClassName };

    /// <summary>Extracts the registration a bound marker call makes.</summary>
    /// <param name="method">The bound marker method.</param>
    /// <param name="invocation">The call.</param>
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="symbols">The compilation's well-known symbols.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The registration, or <see langword="null"/> when the type cannot be constructed or injected.</returns>
    internal static RegistrationInfo? ExtractRegistration(
        IMethodSymbol method,
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        WellKnownSymbols symbols,
        CancellationToken cancellationToken)
    {
        var typeArguments = method.TypeArguments;
        var concreteType = typeArguments[typeArguments.Length - 1];

        if (!TryExtractConstructorParameters(concreteType, symbols, out var constructorParameters)
            || !TryExtractPropertyInjections(concreteType, symbols, out var propertyInjections))
        {
            return null;
        }

        ExtractArguments(method, invocation.ArgumentList.Arguments, semanticModel, symbols, cancellationToken, out var contract, out var mode);

        var serviceTypeName = DisplayName(typeArguments[0]);
        return new(
            method.Name == Constants.MethodNameRegister ? RegistrationKind.Transient : RegistrationKind.LazySingleton,
            serviceTypeName,
            typeArguments.Length == 1 ? serviceTypeName : DisplayName(concreteType),
            constructorParameters,
            propertyInjections,
            contract,
            mode);
    }

    /// <summary>Extracts the parameters of the constructor a registration calls.</summary>
    /// <param name="concreteType">The type constructed.</param>
    /// <param name="symbols">The compilation's well-known symbols.</param>
    /// <param name="parameters">The parameters, when a constructor can be called.</param>
    /// <returns><see langword="false"/> when no single accessible constructor can be chosen.</returns>
    /// <remarks>
    /// Constructors are looked up by name, which a type answers from its member table without building a list of
    /// every member. A type with one constructor uses it; a type with several uses the one marked
    /// <c>[DependencyInjectionConstructor]</c>. The analyzers report the types this turns away.
    /// </remarks>
    internal static bool TryExtractConstructorParameters(ITypeSymbol concreteType, WellKnownSymbols symbols, out EquatableArray<ConstructorParameter> parameters)
    {
        parameters = EquatableArray<ConstructorParameter>.Empty;
        var constructors = concreteType.GetMembers(WellKnownMemberNames.InstanceConstructorName);

        if (constructors.IsEmpty)
        {
            return true;
        }

        var constructor = constructors.Length == 1
            ? (IMethodSymbol)constructors[0]
            : FindMarkedConstructor(concreteType, constructors, symbols);

        if (constructor is null || constructor.DeclaredAccessibility < Accessibility.Internal)
        {
            return false;
        }

        var constructorParameters = constructor.Parameters;
        if (constructorParameters.IsEmpty)
        {
            return true;
        }

        var extracted = new ConstructorParameter[constructorParameters.Length];
        for (var i = 0; i < extracted.Length; i++)
        {
            extracted[i] = CreateParameter(constructorParameters[i].Type, symbols);
        }

        parameters = new(extracted);
        return true;
    }

    /// <summary>Finds the one constructor marked <c>[DependencyInjectionConstructor]</c>.</summary>
    /// <param name="concreteType">The type the constructors belong to.</param>
    /// <param name="constructors">The type's instance constructors.</param>
    /// <param name="symbols">The compilation's well-known symbols.</param>
    /// <returns>The marked constructor, or <see langword="null"/> when none or several are marked.</returns>
    /// <remarks>
    /// The attribute is internal and embedded, so only a type compiled here can carry it. A type from a reference is
    /// turned away without decoding the attributes of its constructors.
    /// </remarks>
    internal static IMethodSymbol? FindMarkedConstructor(ITypeSymbol concreteType, ImmutableArray<ISymbol> constructors, WellKnownSymbols symbols)
    {
        var attribute = symbols.ConstructorAttribute;
        if (attribute is null || !SymbolEqualityComparer.Default.Equals(concreteType.ContainingAssembly, symbols.SourceAssembly))
        {
            return null;
        }

        IMethodSymbol? marked = null;
        foreach (var constructor in constructors)
        {
            if (!HasAttribute(constructor, attribute))
            {
                continue;
            }

            if (marked is not null)
            {
                return null;
            }

            marked = (IMethodSymbol)constructor;
        }

        return marked;
    }

    /// <summary>Extracts the properties marked <c>[DependencyInjectionProperty]</c> on a type and its base types.</summary>
    /// <param name="concreteType">The type constructed.</param>
    /// <param name="symbols">The compilation's well-known symbols.</param>
    /// <param name="properties">The properties, when every marked one can be set.</param>
    /// <returns><see langword="false"/> when a marked property has no setter the generated code can call.</returns>
    /// <remarks>
    /// The walk stops at the first base type from a reference: the attribute is internal and embedded, so no type
    /// compiled elsewhere carries this compilation's copy, and its members need never be loaded. The list is only
    /// allocated once a property is found; most types have none.
    /// </remarks>
    internal static bool TryExtractPropertyInjections(ITypeSymbol concreteType, WellKnownSymbols symbols, out EquatableArray<PropertyInjection> properties)
    {
        properties = EquatableArray<PropertyInjection>.Empty;
        var attribute = symbols.PropertyAttribute;
        if (attribute is null)
        {
            return true;
        }

        List<PropertyInjection>? found = null;
        for (var type = concreteType; type is not null && SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, symbols.SourceAssembly); type = type.BaseType)
        {
            foreach (var member in type.GetMembers())
            {
                if (member is not IPropertySymbol property || !HasAttribute(property, attribute))
                {
                    continue;
                }

                if (property.SetMethod is not { DeclaredAccessibility: >= Accessibility.Internal })
                {
                    return false;
                }

                (found ??= []).Add(new(property.Name, DisplayName(property.Type)));
            }
        }

        if (found is not null)
        {
            properties = new(found.ToArray());
        }

        return true;
    }

    /// <summary>Reads the contract and thread safety mode a marker call passes.</summary>
    /// <param name="method">The bound marker method.</param>
    /// <param name="arguments">The call's arguments.</param>
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="symbols">The compilation's well-known symbols.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="contract">The contract, as C# source; <see langword="null"/> for none.</param>
    /// <param name="mode">The thread safety mode, as C# source; <see langword="null"/> for none.</param>
    internal static void ExtractArguments(
        IMethodSymbol method,
        SeparatedSyntaxList<ArgumentSyntax> arguments,
        SemanticModel semanticModel,
        WellKnownSymbols symbols,
        CancellationToken cancellationToken,
        out string? contract,
        out string? mode)
    {
        contract = null;
        mode = null;
        for (var i = 0; i < arguments.Count; i++)
        {
            var argument = arguments[i];
            var parameterName = RoslynHelpers.GetParameterName(argument, method, i);
            if (parameterName == Constants.ParameterNameContract)
            {
                contract = ExtractContract(argument.Expression, semanticModel, cancellationToken);
            }
            else if (parameterName == Constants.ParameterNameMode)
            {
                mode = ExtractMode(argument.Expression, semanticModel, symbols, cancellationToken);
            }
        }
    }

    /// <summary>Reads a contract argument as C# source that compiles in the generated file.</summary>
    /// <param name="expression">The argument.</param>
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The contract, or <see langword="null"/> when it does not bind.</returns>
    /// <remarks>
    /// A literal is copied from its token, whose text the syntax tree already holds, so it costs no allocation and no
    /// binding. Members are qualified with their type, so they compile from the generated file's namespace.
    /// </remarks>
    internal static string? ExtractContract(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken) =>
        expression is LiteralExpressionSyntax literal
            ? literal.Token.Text
            : ExtractValue(expression, semanticModel.GetSymbolInfo(expression, cancellationToken).Symbol);

    /// <summary>Reads a thread safety mode argument as C# source that compiles in the generated file.</summary>
    /// <param name="expression">The argument.</param>
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="symbols">The compilation's well-known symbols.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The mode, or <see langword="null"/> when it does not bind.</returns>
    /// <remarks>A member of the enum, the usual argument, is looked up in a table rather than formatted.</remarks>
    internal static string? ExtractMode(ExpressionSyntax expression, SemanticModel semanticModel, WellKnownSymbols symbols, CancellationToken cancellationToken)
    {
        var symbol = semanticModel.GetSymbolInfo(expression, cancellationToken).Symbol;
        return symbol is IFieldSymbol { ConstantValue: int value } field
            && SymbolEqualityComparer.Default.Equals(field.ContainingType, symbols.LazyThreadSafetyModeType)
            ? LazyThreadSafetyModeNames[value]
            : ExtractValue(expression, symbol);
    }

    /// <summary>Renders a bound argument as C# source that compiles in the generated file.</summary>
    /// <param name="expression">The argument.</param>
    /// <param name="symbol">The symbol the argument binds to.</param>
    /// <returns>The source, or <see langword="null"/> when the argument does not bind.</returns>
    internal static string? ExtractValue(ExpressionSyntax expression, ISymbol? symbol) =>
        symbol switch
        {
            null => null,
            IFieldSymbol or IPropertySymbol => RoslynHelpers.GetFullyQualifiedMemberReference(symbol),
            IMethodSymbol invokedMethod when expression is InvocationExpressionSyntax invocation =>
                RoslynHelpers.GetFullyQualifiedMethodInvocation(invokedMethod, invocation),
            _ => expression.ToString(),
        };

    /// <summary>Describes how a constructor parameter is resolved.</summary>
    /// <param name="type">The parameter's type.</param>
    /// <param name="symbols">The compilation's well-known symbols.</param>
    /// <returns>The parameter.</returns>
    /// <remarks>
    /// The type argument of a lazy or a collection is cut from the name already formatted for the whole type, which
    /// is the type argument's own name between the generic type's prefix and the closing bracket.
    /// </remarks>
    internal static ConstructorParameter CreateParameter(ITypeSymbol type, WellKnownSymbols symbols)
    {
        var typeName = DisplayName(type);
        if (type is INamedTypeSymbol { TypeArguments.Length: 1 } namedType)
        {
            var definition = namedType.OriginalDefinition;
            if (SymbolEqualityComparer.Default.Equals(definition, symbols.LazyType))
            {
                return new(typeName, DependencyKind.Lazy, TypeArgumentName(typeName, LazyPrefix.Length));
            }

            if (SymbolEqualityComparer.Default.Equals(definition, symbols.EnumerableType))
            {
                return new(typeName, DependencyKind.Collection, TypeArgumentName(typeName, EnumerablePrefix.Length));
            }
        }

        return new(typeName, DependencyKind.Service, null);
    }

    /// <summary>Tests whether a symbol carries an attribute.</summary>
    /// <param name="symbol">The symbol.</param>
    /// <param name="attribute">The attribute type.</param>
    /// <returns><see langword="true"/> when the symbol carries the attribute.</returns>
    internal static bool HasAttribute(ISymbol symbol, INamedTypeSymbol attribute)
    {
        foreach (var data in symbol.GetAttributes())
        {
            if (attribute.Equals(data.AttributeClass, SymbolEqualityComparer.Default))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Formats a type's fully qualified name, as the generated code writes it.</summary>
    /// <param name="type">The type.</param>
    /// <returns>The name, starting <c>global::</c> for a named type.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string DisplayName(ITypeSymbol type) => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    /// <summary>Cuts the single type argument out of a generic type's formatted name.</summary>
    /// <param name="typeName">The generic type's name.</param>
    /// <param name="prefixLength">The length of the name before the type argument.</param>
    /// <returns>The type argument's name.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string TypeArgumentName(string typeName, int prefixLength) =>
        typeName.Substring(prefixLength, typeName.Length - prefixLength - 1);
}
