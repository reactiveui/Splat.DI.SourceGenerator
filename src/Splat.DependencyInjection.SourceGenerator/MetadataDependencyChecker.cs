// Copyright (c) 2019-2021 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Splat.DependencyInjection.SourceGenerator.Metadata;

namespace Splat.DependencyInjection.SourceGenerator;

internal static class MetadataDependencyChecker
{
    // Standard display format for types
    private static readonly SymbolDisplayFormat TypeFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes
    );
    public static List<MethodMetadata> CheckMetadata(GeneratorExecutionContext context, IList<MethodMetadata> metadataMethods)
    {
        return CheckMetadata(new GeneratorExecutionContextAdapter(context), metadataMethods);
    }

    public static List<MethodMetadata> CheckMetadata(IGeneratorContext context, IList<MethodMetadata> metadataMethods)
    {
        var metadataDependencies = new Dictionary<string, MethodMetadata>();
        foreach (var metadataMethod in metadataMethods)
        {
            if (metadataDependencies.ContainsKey(metadataMethod.InterfaceTypeName))
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticWarnings.InterfaceRegisteredMultipleTimes, metadataMethod.MethodInvocation.GetLocation(), metadataMethod.InterfaceTypeName));
            }
            else
            {
                metadataDependencies[metadataMethod.InterfaceTypeName] = metadataMethod;
            }
        }

        var methods = new List<MethodMetadata>();

        foreach (var metadataMethod in metadataMethods)
        {
            var isError = false;
            foreach (var constructorDependency in metadataMethod.ConstructorDependencies)
            {
                if (metadataDependencies.TryGetValue(constructorDependency.TypeName, out var dependencyMethod))
                {
                    foreach (var childConstructor in dependencyMethod.ConstructorDependencies)
                    {
                        if (childConstructor.TypeName == metadataMethod.InterfaceTypeName)
                        {
                            var location = childConstructor.Parameter.GetLocation(metadataMethod.MethodInvocation);

                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    DiagnosticWarnings.ConstructorsMustNotHaveCircularDependency,
                                    location));
                            isError = true;
                        }
                    }
                }

                if (constructorDependency.Type.Name == "Lazy" && constructorDependency.Type is INamedTypeSymbol namedTypeSymbol)
                {
                    var typeArguments = namedTypeSymbol.TypeArguments;

                    if (typeArguments.Length != 1)
                    {
                        continue;
                    }

                    var lazyType = namedTypeSymbol.TypeArguments[0];

                    if (metadataDependencies.TryGetValue(lazyType.ToDisplayString(TypeFormat), out dependencyMethod) && !dependencyMethod.IsLazy)
                    {
                        var location = constructorDependency.Parameter.GetLocation(metadataMethod.MethodInvocation);

                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                DiagnosticWarnings.LazyParameterNotRegisteredLazy,
                                location,
                                metadataMethod.ConcreteTypeName,
                                constructorDependency.Parameter.Name));
                        isError = true;
                    }
                }
            }

            if (!isError)
            {
                methods.Add(metadataMethod);
            }
        }

        return methods;
    }

    private static Location GetLocation(this ISymbol symbol, InvocationExpressionSyntax backupInvocation)
    {
        var location = symbol.Locations.FirstOrDefault();

        if (location?.Kind != LocationKind.SourceFile)
        {
            location = backupInvocation.GetLocation();
        }

        return location;
    }
}
