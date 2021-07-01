using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using ReactiveMarbles.RoslynHelpers;
using System.Linq;

using static ReactiveMarbles.RoslynHelpers.SyntaxFactoryHelpers;
using System.Linq.Expressions;

namespace Splat.DependencyInjection.SourceGenerator
{
    internal static class SourceGeneratorHelpers
    {
        private const string RegisterMethodName = "Register";
        private const string LocatorName = "Splat.Locator.CurrentMutable";
        private const string ConstructorAttribute = "global::Splat.DependencyInjectionConstructorAttribute";
        private const string PropertyAttribute = "global::Splat.DependencyInjectionPropertyAttribute";

        public static string Generate(GeneratorExecutionContext context, Compilation compilation, SyntaxReceiver syntaxReceiver)
        {
            var invocations = new List<StatementSyntax>();
            
            invocations.AddRange(syntaxReceiver.Register.SelectMany(x => Generate(context, compilation, x, false)));
            invocations.AddRange(syntaxReceiver.RegisterLazySingleton.SelectMany(x => Generate(context, compilation, x, true)));

            var staticConstructor = ConstructorDeclaration(default, new[] { SyntaxKind.StaticKeyword }, Array.Empty<ParameterSyntax>(), Constants.ClassName, Block(invocations, 2), 1);

            var registrationClass = ClassDeclaration(Constants.ClassName, new[] { SyntaxKind.InternalKeyword, SyntaxKind.StaticKeyword, SyntaxKind.PartialKeyword }, new[] { staticConstructor }, 1);

            var namespaceDeclaration = NamespaceDeclaration(Constants.NamespaceName, new[] { registrationClass }, false);

            var compilationUnit = CompilationUnit(default, new[] { namespaceDeclaration }, Array.Empty<UsingDirectiveSyntax>());

            return compilationUnit.ToFullString();
        }

        public static IEnumerable<StatementSyntax> Generate(GeneratorExecutionContext context, Compilation compilation, InvocationExpressionSyntax invocationExpression, bool isLazy)
        {
            var semanticModel = compilation.GetSemanticModel(invocationExpression.SyntaxTree);

            if (semanticModel.GetSymbolInfo(invocationExpression).Symbol is not IMethodSymbol methodSymbol)
            {
                // Produce a diagnostic error.
                yield break;
            }

            if (methodSymbol.TypeParameters.Length != 2)
            {
                yield break;
            }

            if (methodSymbol.IsExtensionMethod)
            {
                yield break;
            }

            if (methodSymbol.Parameters.Length > 2)
            {
                yield break;
            }

            var interfaceTarget = methodSymbol.TypeArguments[0];
            var concreteTarget = methodSymbol.TypeArguments[1];

            if (interfaceTarget is null || concreteTarget is null)
            {
                yield break;
            }

            var concreteTargetTypeName = concreteTarget.ToDisplayString(RoslynCommonHelpers.TypeFormat);
            var interfaceTargetTypeName = interfaceTarget.ToDisplayString(RoslynCommonHelpers.TypeFormat);

            var constructor = GetConstructor(concreteTarget, context, invocationExpression);

            if (constructor is null)
            {
                yield break;
            }

            var typeConstructorArguments = new List<ArgumentSyntax>();

            foreach (var parameter in constructor.Parameters)
            {
                var parameterType = parameter.Type;
                var parameterTypeName = parameterType.ToDisplayString(RoslynCommonHelpers.TypeFormat);

                typeConstructorArguments.Add(Argument(GetSplatService(parameterTypeName)));
            }

            InitializerExpressionSyntax? initializer = GetPropertyInitializer(concreteTarget, context);

            ExpressionSyntax call = initializer is null ?
                    ObjectCreationExpression(concreteTargetTypeName, typeConstructorArguments) :
                    ObjectCreationExpression(concreteTargetTypeName, typeConstructorArguments, initializer);

            if (isLazy)
            {
                var lazyType = $"System.Lazy<{concreteTargetTypeName}>";
                const string lazyVariableName = "lazy";

                var block = Block(new StatementSyntax[]
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
                                            new[]
                                            {
                                                Argument(ParenthesizedLambdaExpression(call))
                                            })))
                                })),
                    ExpressionStatement(InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, LocatorName, RegisterMethodName),
                        new[]
                        {
                            Argument(ParenthesizedLambdaExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, lazyVariableName, "Value"))),
                            Argument($"typeof({interfaceTargetTypeName})")
                        })),
                },
                3);

                yield return block;
            }
            else
            {
                yield return ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, LocatorName, RegisterMethodName),
                        new[]
                        {
                            Argument(ParenthesizedLambdaExpression(call)),
                            Argument($"typeof({interfaceTargetTypeName})") 
                        }));
            }
        }

        private static IMethodSymbol? GetConstructor(ITypeSymbol concreteTarget, GeneratorExecutionContext context, InvocationExpressionSyntax invocationExpression)
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
                    if (constructor.GetAttributes().Any(x => x.AttributeClass?.ToDisplayString(RoslynCommonHelpers.TypeFormat) == ConstructorAttribute))
                    {
                        if (returnConstructor != null)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(DiagnosticWarnings.MultipleConstructorsMarked, constructor.Locations.FirstOrDefault()));
                            return null;
                        }

                        returnConstructor = constructor;
                    }
                }
            }

            if (returnConstructor is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticWarnings.ExpressionMustBeInline, location: invocationExpression.GetLocation()));
                return null;
            }


            if (returnConstructor.DeclaredAccessibility < Accessibility.Internal)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticWarnings.ConstructorsMustBePublic, returnConstructor.Locations.FirstOrDefault()));
                return null;
            }

            return returnConstructor;
        }

        private static InitializerExpressionSyntax? GetPropertyInitializer(ITypeSymbol concreteTarget, GeneratorExecutionContext context)
        {
            var propertySet = new List<AssignmentExpressionSyntax>();
            foreach (var property in GetInitializeProperties(concreteTarget))
            {
                if (property.SetMethod?.DeclaredAccessibility < Accessibility.Internal)
                {
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticWarnings.PropertyMustPublicBeSettable, property.SetMethod?.Locations.FirstOrDefault()));
                }

                propertySet.Add(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, property.Name, GetSplatService(property.Type.ToDisplayString(RoslynCommonHelpers.TypeFormat))));
            }

            return propertySet.Count > 0 ? InitializerExpression(SyntaxKind.ObjectInitializerExpression, propertySet) : null;
        }

        private static IEnumerable<IPropertySymbol> GetInitializeProperties(ITypeSymbol concreteTarget) =>
            concreteTarget
                .GetBaseTypesAndThis()
                .SelectMany(x => x.GetMembers())
                .Where(x => x.Kind == SymbolKind.Property)
                .Cast<IPropertySymbol>()
                .Where(x => x.GetAttributes().Any(attr => attr.AttributeClass?.ToDisplayString(RoslynCommonHelpers.TypeFormat) == PropertyAttribute));

        private static CastExpressionSyntax GetSplatService(string parameterTypeName) =>
            CastExpression(
                parameterTypeName,
                InvocationExpression(
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, "Splat.Locator.Current", "GetService"),
                    new[]
                    {
                        Argument($"typeof({parameterTypeName})"),
                    }));
    }
}
