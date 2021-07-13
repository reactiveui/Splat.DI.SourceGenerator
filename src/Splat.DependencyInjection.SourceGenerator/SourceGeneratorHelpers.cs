﻿// Copyright (c) 2019-2021 ReactiveUI Association Incorporated. All rights reserved.
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

using static ReactiveMarbles.RoslynHelpers.SyntaxFactoryHelpers;

namespace Splat.DependencyInjection.SourceGenerator
{
    internal static class SourceGeneratorHelpers
    {
        private const string RegisterMethodName = "Register";
        private const string LocatorName = "Splat.Locator.CurrentMutable";

        public static string Generate(GeneratorExecutionContext context, Compilation compilation, SyntaxReceiver syntaxReceiver)
        {
            var methods = MetadataExtractor.GetValidMethods(context, syntaxReceiver, compilation).ToList();

            methods = MetadataDependencyChecker.CheckMetadata(context, methods);

            var invocations = Generate(compilation, methods);

            var staticConstructor = ConstructorDeclaration(default, new[] { SyntaxKind.StaticKeyword }, Array.Empty<ParameterSyntax>(), Constants.ClassName, Block(invocations.ToList(), 2), 1);

            var registrationClass = ClassDeclaration(Constants.ClassName, new[] { SyntaxKind.InternalKeyword, SyntaxKind.StaticKeyword, SyntaxKind.PartialKeyword }, new[] { staticConstructor }, 1);

            var namespaceDeclaration = NamespaceDeclaration(Constants.NamespaceName, new[] { registrationClass }, false);

            var compilationUnit = CompilationUnit(default, new[] { namespaceDeclaration }, Array.Empty<UsingDirectiveSyntax>());

            return compilationUnit.ToFullString();
        }

        private static IEnumerable<StatementSyntax> Generate(Compilation compilation, IEnumerable<MethodMetadata> methodMetadatas)
        {
            foreach (var methodMetadata in methodMetadatas)
            {
                var semanticModel = compilation.GetSemanticModel(methodMetadata.MethodInvocation.SyntaxTree);

                var typeConstructorArguments = new List<ArgumentSyntax>();

                foreach (var parameter in methodMetadata.ConstructorDependencies)
                {
                    var parameterType = parameter.Type;
                    var parameterTypeName = parameterType.ToDisplayString(RoslynCommonHelpers.TypeFormat);

                    typeConstructorArguments.Add(Argument(GetSplatService(parameterTypeName)));
                }

                var contractParameter = methodMetadata.RegisterParameterValues.FirstOrDefault(x => x.ParameterName == "contract");

                string? contract = null;
                if (contractParameter is not null)
                {
                    contract = contractParameter.ParameterValue;
                }

                var initializer = GetPropertyInitializer(methodMetadata.Properties);

                ExpressionSyntax call = initializer is null ?
                        ObjectCreationExpression(methodMetadata.ConcreteTypeName, typeConstructorArguments) :
                        ObjectCreationExpression(methodMetadata.ConcreteTypeName, typeConstructorArguments, initializer);

                if (methodMetadata.IsLazy)
                {
                    yield return GetLazyBlock(methodMetadata, call, contract);
                }
                else
                {
                    yield return GenerateLocatorSetService(Argument(ParenthesizedLambdaExpression(call)), methodMetadata.InterfaceTypeName, contract);
                }
            }
        }

        private static InitializerExpressionSyntax? GetPropertyInitializer(IEnumerable<PropertyDependencyMetadata> properties)
        {
            var propertySet = new List<AssignmentExpressionSyntax>();
            foreach (var property in properties)
            {
                propertySet.Add(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, property.Name, GetSplatService(property.TypeName)));
            }

            return propertySet.Count > 0 ? InitializerExpression(SyntaxKind.ObjectInitializerExpression, propertySet) : null;
        }

        private static CastExpressionSyntax GetSplatService(string parameterTypeName) =>
            CastExpression(
                parameterTypeName,
                InvocationExpression(
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, Constants.LocatorCurrent, Constants.LocatorGetService),
                    new[]
                    {
                        Argument($"typeof({parameterTypeName})"),
                    }));

        private static BlockSyntax GetLazyBlock(MethodMetadata methodMetadata, ExpressionSyntax call, string? contract)
        {
            var lazyType = $"global::System.Lazy<{methodMetadata.InterfaceType}>";

            const string lazyTypeValueProperty = "Value";
            const string lazyVariableName = "lazy";

            var lambdaArguments = new ArgumentSyntax[]
            {
                Argument(ParenthesizedLambdaExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, lazyVariableName, lazyTypeValueProperty))),
                Argument($"typeof({methodMetadata.InterfaceTypeName})")
            };

            var lazyArguments = new List<ArgumentSyntax>()
            {
                Argument(ParenthesizedLambdaExpression(call))
            };

            var lazyModeParameter = methodMetadata.RegisterParameterValues.FirstOrDefault(x => x.ParameterName == "mode");

            if (lazyModeParameter is not null)
            {
                var modeName = lazyModeParameter.ParameterValue;

                lazyArguments.Add(Argument(modeName));
            }

            return Block(
                new StatementSyntax[]
                {
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            lazyType,
                            new[]
                            {
                                VariableDeclarator(
                                    lazyVariableName,
                                    EqualsValueClause(
                                        ObjectCreationExpression(
                                            lazyType,
                                            lazyArguments)))
                            })),
                    GenerateLocatorSetService(
                        Argument(ParenthesizedLambdaExpression(IdentifierName(lazyVariableName))),
                        lazyType,
                        contract),
                    GenerateLocatorSetService(
                        Argument(ParenthesizedLambdaExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, lazyVariableName, lazyTypeValueProperty))),
                        methodMetadata.InterfaceTypeName,
                        contract)
                },
                3);
        }

        private static ExpressionStatementSyntax GenerateLocatorSetService(ArgumentSyntax argument, string interfaceType, string? contract)
        {
            var lambdaArguments = new List<ArgumentSyntax>
            {
                argument,
                Argument($"typeof({interfaceType})")
            };

            if (contract is not null)
            {
                lambdaArguments.Add(Argument(contract));
            }

            return ExpressionStatement(InvocationExpression(
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, LocatorName, RegisterMethodName),
                lambdaArguments));
        }
    }
}
