// Copyright (c) 2019-2021 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using ReactiveMarbles.RoslynHelpers;

using Splat.DependencyInjection.SourceGenerator.Metadata;

namespace Splat.DependencyInjection.SourceGenerator
{
    internal static class MetadataExtractor
    {
        public static IEnumerable<MethodMetadata> GetValidMethods(GeneratorExecutionContext context, SyntaxReceiver syntaxReceiver, Compilation compilation)
        {
            foreach (var invocationExpression in syntaxReceiver.Register)
            {
                var methodMetadata = GetValidMethod(context, invocationExpression, compilation, (method, interfaceType, concreteType, invocation, constructors, properties, registerProperties) =>
                    new RegisterMetadata(method, interfaceType, concreteType, invocation, constructors, properties, registerProperties));

                if (methodMetadata != null)
                {
                    yield return methodMetadata;
                }
            }

            foreach (var invocationExpression in syntaxReceiver.RegisterLazySingleton)
            {
                var methodMetadata = GetValidMethod(context, invocationExpression, compilation, (method, interfaceType, concreteType, invocation, constructors, properties, registerProperties) =>
                    new RegisterLazySingletonMetadata(method, interfaceType, concreteType, invocation, constructors, properties, registerProperties));

                if (methodMetadata != null)
                {
                    yield return methodMetadata;
                }
            }

            foreach (var invocationExpression in syntaxReceiver.RegisterConstant)
            {
                var methodMetadata = GetValidRegisterConstant(context, invocationExpression, compilation, (method, interfaceType, concreteType, invocation) =>
                    new(method, interfaceType, concreteType, invocation));

                if (methodMetadata != null)
                {
                    yield return methodMetadata;
                }
            }
        }

        private static RegisterConstantMetadata? GetValidRegisterConstant(
            GeneratorExecutionContext context,
            InvocationExpressionSyntax invocationExpression,
            Compilation compilation,
            Func<IMethodSymbol, ITypeSymbol, ITypeSymbol, InvocationExpressionSyntax, RegisterConstantMetadata> createFunc)
        {
            try
            {
                var semanticModel = compilation.GetSemanticModel(invocationExpression.SyntaxTree);
                if (ModelExtensions.GetSymbolInfo(semanticModel, invocationExpression).Symbol is not IMethodSymbol methodSymbol)
                {
                    // Produce a diagnostic error.
                    return null;
                }

                if (methodSymbol.Parameters.Length is 0 or > 2)
                {
                    return null;
                }

                var concreteTarget = methodSymbol.Parameters[0].Type;

                return createFunc(methodSymbol, concreteTarget, concreteTarget, invocationExpression);
            }
            catch (ContextDiagnosticException ex)
            {
                context.ReportDiagnostic(ex.Diagnostic);
            }

            return null;
        }

        private static T? GetValidMethod<T>(
            GeneratorExecutionContext context,
            InvocationExpressionSyntax invocationExpression,
            Compilation compilation,
            Func<IMethodSymbol, ITypeSymbol, ITypeSymbol, InvocationExpressionSyntax, IReadOnlyList<ConstructorDependencyMetadata>, IReadOnlyList<PropertyDependencyMetadata>, IReadOnlyList<ParameterMetadata>, T> createFunc)
            where T : MethodMetadata
        {
            try
            {
                var semanticModel = compilation.GetSemanticModel(invocationExpression.SyntaxTree);
                if (ModelExtensions.GetSymbolInfo(semanticModel, invocationExpression).Symbol is not IMethodSymbol methodSymbol)
                {
                    // Produce a diagnostic error.
                    return null;
                }

                var numberTypeParameters = methodSymbol.TypeArguments.Length;

                if (numberTypeParameters is 0 or > 2)
                {
                    return null;
                }

                if (methodSymbol.IsExtensionMethod)
                {
                    return null;
                }

                if (methodSymbol.Parameters.Length > 2)
                {
                    return null;
                }

                ITypeSymbol interfaceTarget;
                ITypeSymbol concreteTarget;

                if (numberTypeParameters == 1)
                {
                    interfaceTarget = methodSymbol.TypeArguments[0];
                    concreteTarget = interfaceTarget;
                }
                else
                {
                    interfaceTarget = methodSymbol.TypeArguments[0];
                    concreteTarget = methodSymbol.TypeArguments[1];
                }

                var constructorDependencies = GetConstructorDependencies(concreteTarget, invocationExpression).ToList();

                var properties = GetPropertyDependencies(concreteTarget).ToList();

                var registerParameters = GetRegisterParameters(methodSymbol, semanticModel, invocationExpression).ToList();

                return createFunc(methodSymbol, interfaceTarget, concreteTarget, invocationExpression, constructorDependencies, properties, registerParameters);
            }
            catch (ContextDiagnosticException ex)
            {
                context.ReportDiagnostic(ex.Diagnostic);
            }

            return null;
        }

        private static IEnumerable<ParameterMetadata> GetRegisterParameters(IMethodSymbol methodSymbol, SemanticModel semanticModel, InvocationExpressionSyntax invocationExpression)
        {
            for (var i = 0; i < invocationExpression.ArgumentList.Arguments.Count; ++i)
            {
                var argument = invocationExpression.ArgumentList.Arguments[i];
                var argumentName = methodSymbol.Parameters[i].Name;
                var expression = argument.Expression;

                if (expression is LiteralExpressionSyntax literal)
                {
                    yield return new(argumentName, literal.ToString());
                }
                else
                {
                    var mode = ModelExtensions.GetSymbolInfo(semanticModel, expression);

                    if (mode.Symbol is not null)
                    {
                        yield return new(argumentName, mode.Symbol.ToDisplayString());
                    }
                }
            }
        }

        private static IEnumerable<ConstructorDependencyMetadata> GetConstructorDependencies(INamespaceOrTypeSymbol concreteTarget, CSharpSyntaxNode invocationExpression)
        {
            var constructors = concreteTarget
                .GetMembers()
                .Where(x => x.Kind == SymbolKind.Method)
                .Cast<IMethodSymbol>()
                .Where(x => x.MethodKind == MethodKind.Constructor)
                .ToList();

            IMethodSymbol? returnConstructor = null;

            if (constructors.Count == 1)
            {
                returnConstructor = constructors[0];
            }
            else
            {
                foreach (var constructor in constructors)
                {
                    if (constructor.GetAttributes().Any(x => x.AttributeClass?.ToDisplayString(RoslynCommonHelpers.TypeFormat) == Constants.ConstructorAttribute))
                    {
                        if (returnConstructor != null)
                        {
                            throw new ContextDiagnosticException(Diagnostic.Create(DiagnosticWarnings.MultipleConstructorsMarked, constructor.Locations.FirstOrDefault(x => x is not null), concreteTarget.ToDisplayString(RoslynCommonHelpers.TypeFormat)));
                        }

                        returnConstructor = constructor;
                    }
                }
            }

            if (returnConstructor is null)
            {
                throw new ContextDiagnosticException(Diagnostic.Create(DiagnosticWarnings.MultipleConstructorNeedAttribute, invocationExpression.GetLocation(), concreteTarget.ToDisplayString(RoslynCommonHelpers.TypeFormat)));
            }

            if (returnConstructor.DeclaredAccessibility < Accessibility.Internal)
            {
                throw new ContextDiagnosticException(Diagnostic.Create(DiagnosticWarnings.ConstructorsMustBePublic, returnConstructor.Locations.FirstOrDefault(x => x is not null), concreteTarget.ToDisplayString(RoslynCommonHelpers.TypeFormat)));
            }

            return returnConstructor.Parameters.Select(x => new ConstructorDependencyMetadata(x, x.Type));
        }

        private static IEnumerable<PropertyDependencyMetadata> GetPropertyDependencies(ITypeSymbol concreteTarget)
        {
            var propertySymbols = concreteTarget
                .GetBaseTypesAndThis()
                .SelectMany(x => x.GetMembers())
                .Where(x => x.Kind == SymbolKind.Property)
                .Cast<IPropertySymbol>()
                .Where(x => x.GetAttributes().Any(attr => attr.AttributeClass?.ToDisplayString(RoslynCommonHelpers.TypeFormat) == Constants.PropertyAttribute));

            foreach (var property in propertySymbols)
            {
                if (property.SetMethod?.DeclaredAccessibility < Accessibility.Internal)
                {
                    throw new ContextDiagnosticException(Diagnostic.Create(DiagnosticWarnings.PropertyMustPublicBeSettable, property.SetMethod?.Locations.FirstOrDefault(x => x is not null), property.ToDisplayString(RoslynCommonHelpers.TypeFormat)));
                }

                yield return new(property);
            }
        }
    }
}
