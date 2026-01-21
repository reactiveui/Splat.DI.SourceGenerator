// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Splat.DependencyInjection.SourceGenerator.Models;

namespace Splat.DependencyInjection.SourceGenerator;

/// <summary>
/// Extracts metadata from Roslyn symbols for DI registration.
/// </summary>
internal static class MetadataExtractor
{
    private static readonly SymbolDisplayFormat _fullyQualifiedFormat = SymbolDisplayFormat.FullyQualifiedFormat;

    /// <summary>
    /// Extracts metadata for a Register call.
    /// </summary>
    /// <param name="context">The generator syntax context.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Transient registration info or null.</returns>
    internal static TransientRegistrationInfo? ExtractRegisterMetadata(
        GeneratorSyntaxContext context,
        CancellationToken ct)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        if (semanticModel.GetSymbolInfo(invocation, ct).Symbol is not IMethodSymbol methodSymbol)
        {
            return null;
        }

        if (!RoslynHelpers.IsSplatRegistrationsMethod(methodSymbol, "Register"))
        {
            return null;
        }

        var numberTypeParameters = methodSymbol.TypeArguments.Length;
        if (numberTypeParameters is 0 or > 2)
        {
            return null;
        }

        var interfaceType = methodSymbol.TypeArguments[0];
        var concreteType = numberTypeParameters == 2
            ? methodSymbol.TypeArguments[1]
            : interfaceType;

        var constructorParams = ExtractConstructorParameters(concreteType);
        if (constructorParams == null)
        {
            return null;
        }

        var propertyInjections = ExtractPropertyInjections(concreteType);
        if (propertyInjections == null)
        {
            return null;
        }

        var contractValue = RoslynHelpers.ExtractContractParameter(methodSymbol, invocation, semanticModel, ct);

        return new TransientRegistrationInfo(
            InterfaceTypeFullName: interfaceType.ToDisplayString(_fullyQualifiedFormat),
            ConcreteTypeFullName: concreteType.ToDisplayString(_fullyQualifiedFormat),
            ConstructorParameters: new EquatableArray<ConstructorParameter>(constructorParams),
            PropertyInjections: new EquatableArray<PropertyInjection>(propertyInjections),
            ContractValue: contractValue,
            InvocationLocation: invocation.GetLocation());
    }

    /// <summary>
    /// Extracts metadata for a RegisterLazySingleton call.
    /// </summary>
    /// <param name="context">The generator syntax context.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Lazy singleton registration info or null.</returns>
    internal static LazySingletonRegistrationInfo? ExtractLazySingletonMetadata(
        GeneratorSyntaxContext context,
        CancellationToken ct)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        if (semanticModel.GetSymbolInfo(invocation, ct).Symbol is not IMethodSymbol methodSymbol)
        {
            return null;
        }

        if (!RoslynHelpers.IsSplatRegistrationsMethod(methodSymbol, "RegisterLazySingleton"))
        {
            return null;
        }

        var numberTypeParameters = methodSymbol.TypeArguments.Length;
        if (numberTypeParameters is 0 or > 2)
        {
            return null;
        }

        var interfaceType = methodSymbol.TypeArguments[0];
        var concreteType = numberTypeParameters == 2
            ? methodSymbol.TypeArguments[1]
            : interfaceType;

        var constructorParams = ExtractConstructorParameters(concreteType);
        if (constructorParams == null)
        {
            return null;
        }

        var propertyInjections = ExtractPropertyInjections(concreteType);
        if (propertyInjections == null)
        {
            return null;
        }

        var contractValue = RoslynHelpers.ExtractContractParameter(methodSymbol, invocation, semanticModel, ct);
        var lazyMode = RoslynHelpers.ExtractLazyThreadSafetyMode(methodSymbol, invocation, semanticModel, ct);

        return new LazySingletonRegistrationInfo(
            InterfaceTypeFullName: interfaceType.ToDisplayString(_fullyQualifiedFormat),
            ConcreteTypeFullName: concreteType.ToDisplayString(_fullyQualifiedFormat),
            ConstructorParameters: new EquatableArray<ConstructorParameter>(constructorParams),
            PropertyInjections: new EquatableArray<PropertyInjection>(propertyInjections),
            ContractValue: contractValue,
            LazyThreadSafetyMode: lazyMode,
            InvocationLocation: invocation.GetLocation());
    }

    /// <summary>
    /// Extracts constructor parameters for a type.
    /// </summary>
    /// <param name="concreteType">The type to extract from.</param>
    /// <returns>Array of constructor parameters or null.</returns>
    internal static ConstructorParameter[]? ExtractConstructorParameters(ITypeSymbol concreteType)
    {
        var members = concreteType.GetMembers();
        var constructors = new List<IMethodSymbol>(capacity: 4);

        for (var i = 0; i < members.Length; i++)
        {
            if (members[i] is IMethodSymbol { MethodKind: MethodKind.Constructor, IsStatic: false } ctor)
            {
                constructors.Add(ctor);
            }
        }

        var constructorCount = constructors.Count;
        IMethodSymbol? selectedConstructor = null;

        if (constructorCount == 1)
        {
            selectedConstructor = constructors[0];
        }
        else if (constructorCount > 1)
        {
            for (var i = 0; i < constructorCount; i++)
            {
                var constructor = constructors[i];
                var attrs = constructor.GetAttributes();
                var hasAttribute = false;

                for (var j = 0; j < attrs.Length; j++)
                {
                    if (attrs[j].AttributeClass?.ToDisplayString(_fullyQualifiedFormat) == Constants.ConstructorAttribute)
                    {
                        hasAttribute = true;
                        break;
                    }
                }

                if (hasAttribute)
                {
                    if (selectedConstructor != null)
                    {
                        return null;
                    }

                    selectedConstructor = constructor;
                }
            }

            if (selectedConstructor == null)
            {
                return null;
            }
        }

        if (selectedConstructor == null)
        {
            return [];
        }

        if (selectedConstructor.DeclaredAccessibility < Accessibility.Internal)
        {
            return null;
        }

        var parameters = new List<ConstructorParameter>(selectedConstructor.Parameters.Length);
        foreach (var param in selectedConstructor.Parameters)
        {
            var paramType = param.Type;
            var paramTypeName = paramType.ToDisplayString(_fullyQualifiedFormat);

            bool isLazy = false;
            string? lazyInnerType = null;

            if (paramType is INamedTypeSymbol namedType &&
                namedType.OriginalDefinition.ToDisplayString(_fullyQualifiedFormat) == "global::System.Lazy<T>")
            {
                isLazy = true;
                if (namedType.TypeArguments.Length > 0)
                {
                    lazyInnerType = namedType.TypeArguments[0].ToDisplayString(_fullyQualifiedFormat);
                }
            }

            bool isCollection = false;
            string? collectionItemType = null;

            if (paramType is INamedTypeSymbol namedCollType &&
                namedCollType.OriginalDefinition.ToDisplayString(_fullyQualifiedFormat) == "global::System.Collections.Generic.IEnumerable<T>")
            {
                isCollection = true;
                if (namedCollType.TypeArguments.Length > 0)
                {
                    collectionItemType = namedCollType.TypeArguments[0].ToDisplayString(_fullyQualifiedFormat);
                }
            }

            parameters.Add(new ConstructorParameter(
                ParameterName: param.Name,
                TypeFullName: paramTypeName,
                IsLazy: isLazy,
                LazyInnerType: lazyInnerType,
                IsCollection: isCollection,
                CollectionItemType: collectionItemType));
        }

        return parameters.ToArray();
    }

    /// <summary>
    /// Extracts property injections for a type.
    /// </summary>
    /// <param name="concreteType">The type to extract from.</param>
    /// <returns>Array of property injections or null.</returns>
    internal static PropertyInjection[]? ExtractPropertyInjections(ITypeSymbol concreteType)
    {
        var properties = new List<PropertyInjection>(capacity: 4);
        var allTypes = RoslynHelpers.GetBaseTypesAndThis(concreteType);

        foreach (var type in allTypes)
        {
            foreach (var member in type.GetMembers())
            {
                if (member is not IPropertySymbol property)
                {
                    continue;
                }

                var attrs = property.GetAttributes();
                var hasAttribute = false;

                for (var i = 0; i < attrs.Length; i++)
                {
                    if (attrs[i].AttributeClass?.ToDisplayString(_fullyQualifiedFormat) == Constants.PropertyAttribute)
                    {
                        hasAttribute = true;
                        break;
                    }
                }

                if (!hasAttribute)
                {
                    continue;
                }

                if (property.SetMethod == null || property.SetMethod.DeclaredAccessibility < Accessibility.Internal)
                {
                    return null;
                }

                properties.Add(new PropertyInjection(
                    PropertyName: property.Name,
                    TypeFullName: property.Type.ToDisplayString(_fullyQualifiedFormat),
                    PropertyLocation: property.Locations.Length > 0 ? property.Locations[0] : Location.None));
            }
        }

        return properties.ToArray();
    }
}
