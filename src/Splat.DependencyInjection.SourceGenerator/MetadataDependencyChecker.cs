// Copyright (c) 2019-2021 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

using ReactiveMarbles.RoslynHelpers;

using Splat.DependencyInjection.SourceGenerator.Metadata;

namespace Splat.DependencyInjection.SourceGenerator
{
    internal static class MetadataDependencyChecker
    {
        public static List<MethodMetadata> CheckMetadata(GeneratorExecutionContext context, IList<MethodMetadata> metadataMethods)
        {
            var metadataDependencies = new Dictionary<string, MethodMetadata>();
            foreach (var metadataMethod in metadataMethods)
            {
                try
                {
                    if (metadataDependencies.ContainsKey(metadataMethod.InterfaceTypeName))
                    {
                        throw new ContextDiagnosticException(Diagnostic.Create(DiagnosticWarnings.InterfaceRegisteredMultipleTimes, metadataMethod.MethodInvocation.GetLocation(), metadataMethod.InterfaceTypeName));
                    }

                    metadataDependencies[metadataMethod.InterfaceTypeName] = metadataMethod;
                }
                catch (ContextDiagnosticException ex)
                {
                    context.ReportDiagnostic(ex.Diagnostic);
                }
            }

            var methods = new List<MethodMetadata>();

            foreach (var metadataMethod in metadataMethods)
            {
                try
                {
                    foreach (var constructorDependency in metadataMethod.ConstructorDependencies)
                    {
                        if (metadataDependencies.TryGetValue(constructorDependency.TypeName, out var dependencyMethod))
                        {
                            foreach (var childConstructor in dependencyMethod.ConstructorDependencies)
                            {
                                if (childConstructor.TypeName == metadataMethod.InterfaceTypeName)
                                {
                                    throw new ContextDiagnosticException(Diagnostic.Create(DiagnosticWarnings.ConstructorsMustNotHaveCircularDependency, childConstructor.Parameter.Locations.FirstOrDefault() ?? metadataMethod.MethodInvocation.GetLocation()));
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

                            if (metadataDependencies.TryGetValue(lazyType.ToDisplayString(RoslynCommonHelpers.TypeFormat), out dependencyMethod))
                            {
                                throw new ContextDiagnosticException(Diagnostic.Create(DiagnosticWarnings.LazyParameterNotRegisteredLazy, constructorDependency.Parameter.Locations.FirstOrDefault() ?? metadataMethod.MethodInvocation.GetLocation(), metadataMethod.ConcreteTypeName, constructorDependency.Parameter.Name));
                            }
                        }
                    }

                    methods.Add(metadataMethod);
                }
                catch (ContextDiagnosticException ex)
                {
                    context.ReportDiagnostic(ex.Diagnostic);
                }
            }

            return methods;
        }
    }
}
