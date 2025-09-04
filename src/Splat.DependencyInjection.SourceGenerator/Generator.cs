// Copyright (c) 2019-2021 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Splat.DependencyInjection.SourceGenerator;

/// <summary>
/// Modern incremental generator for Splat DI registrations.
/// </summary>
[Generator]
public class Generator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Always add the extension method text first
        context.RegisterPostInitializationOutput(ctx =>
            ctx.AddSource("Splat.DI.g.cs", SourceText.From(Constants.ExtensionMethodText, Encoding.UTF8)));

        // Stage 1: Transform invocation syntax into RegistrationCall records
        var registrationCalls = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsRegistrationInvocation(node),
                transform: static (ctx, ct) => CreateRegistrationCall(ctx))
            .Where(static call => call is not null)!;

        // Stage 2: Transform RegistrationCall into RegistrationTarget with semantic analysis
        var registrationTargets = registrationCalls
            .Combine(context.CompilationProvider)
            .Select(static (data, ct) => CreateRegistrationTarget(data.Left, data.Right))
            .Where(static target => target is not null)!;

        // Stage 3: Collect all RegistrationTarget into RegistrationGroup
        var registrationGroup = registrationTargets
            .Collect()
            .Select(static (targets, ct) => new RegistrationGroup(targets));

        // Stage 4: Transform RegistrationGroup into GeneratedSource
        context.RegisterSourceOutput(registrationGroup, static (ctx, group) =>
        {
            if (group.Registrations.IsEmpty)
                return;

            var generatedSource = CreateGeneratedSource(group);
            if (generatedSource is not null)
            {
                ctx.AddSource(generatedSource.FileName, SourceText.From(generatedSource.SourceCode, Encoding.UTF8));
            }
        });
    }

    /// <summary>
    /// Stage 1: Detect registration method calls and create RegistrationCall records.
    /// </summary>
    private static bool IsRegistrationInvocation(SyntaxNode node)
    {
        if (node is not InvocationExpressionSyntax invocation)
            return false;

        var methodName = invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            MemberBindingExpressionSyntax bindingAccess => bindingAccess.Name.Identifier.Text,
            _ => null
        };

        return methodName is "Register" or "RegisterLazySingleton" or "RegisterConstant";
    }

    /// <summary>
    /// Stage 1: Transform syntax node into RegistrationCall record.
    /// </summary>
    private static RegistrationCall? CreateRegistrationCall(GeneratorSyntaxContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocation)
            return null;

        var methodName = invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            MemberBindingExpressionSyntax bindingAccess => bindingAccess.Name.Identifier.Text,
            _ => null
        };

        if (methodName is not ("Register" or "RegisterLazySingleton" or "RegisterConstant"))
            return null;

        return new RegistrationCall(invocation, methodName);
    }

    /// <summary>
    /// Stage 2: Transform RegistrationCall into RegistrationTarget with semantic analysis.
    /// </summary>
    private static RegistrationTarget? CreateRegistrationTarget(RegistrationCall call, Compilation compilation)
    {
        try
        {
            var semanticModel = compilation.GetSemanticModel(call.InvocationSyntax.SyntaxTree);
            if (semanticModel.GetSymbolInfo(call.InvocationSyntax).Symbol is not IMethodSymbol methodSymbol)
                return null;

            return call.MethodName switch
            {
                "Register" => ProcessRegister(call, methodSymbol, compilation),
                "RegisterLazySingleton" => ProcessRegisterLazySingleton(call, methodSymbol, compilation),
                "RegisterConstant" => ProcessRegisterConstant(call, methodSymbol, compilation),
                _ => null
            };
        }
        catch
        {
            // If semantic analysis fails, skip this registration
            return null;
        }
    }

    /// <summary>
    /// Process Register method call.
    /// </summary>
    private static RegistrationTarget? ProcessRegister(RegistrationCall call, IMethodSymbol methodSymbol, Compilation compilation)
    {
        if (!methodSymbol.IsGenericMethod || methodSymbol.TypeArguments.Length != 2)
            return null;

        var interfaceType = methodSymbol.TypeArguments[0];
        var concreteType = methodSymbol.TypeArguments[1];

        var contract = ExtractContractParameter(call.InvocationSyntax);
        var (constructorDeps, propertyDeps, hasAttribute) = AnalyzeDependencies(concreteType, compilation);

        return new RegistrationTarget(
            "Register",
            interfaceType.ToDisplayString(),
            concreteType.ToDisplayString(),
            contract,
            null,
            constructorDeps,
            propertyDeps,
            hasAttribute);
    }

    /// <summary>
    /// Process RegisterLazySingleton method call.
    /// </summary>
    private static RegistrationTarget? ProcessRegisterLazySingleton(RegistrationCall call, IMethodSymbol methodSymbol, Compilation compilation)
    {
        if (!methodSymbol.IsGenericMethod || methodSymbol.TypeArguments.Length != 2)
            return null;

        var interfaceType = methodSymbol.TypeArguments[0];
        var concreteType = methodSymbol.TypeArguments[1];

        var contract = ExtractContractParameter(call.InvocationSyntax);
        var lazyMode = ExtractLazyModeParameter(call.InvocationSyntax);
        var (constructorDeps, propertyDeps, hasAttribute) = AnalyzeDependencies(concreteType, compilation);

        return new RegistrationTarget(
            "RegisterLazySingleton",
            interfaceType.ToDisplayString(),
            concreteType.ToDisplayString(),
            contract,
            lazyMode,
            constructorDeps,
            propertyDeps,
            hasAttribute);
    }

    /// <summary>
    /// Process RegisterConstant method call.
    /// </summary>
    private static RegistrationTarget? ProcessRegisterConstant(RegistrationCall call, IMethodSymbol methodSymbol, Compilation compilation)
    {
        if (methodSymbol.Parameters.Length is 0 or > 2)
            return null;

        var concreteType = methodSymbol.Parameters[0].Type;
        var contract = ExtractContractParameter(call.InvocationSyntax);

        return new RegistrationTarget(
            "RegisterConstant",
            concreteType.ToDisplayString(),
            null, // No concrete type for constants
            contract,
            null,
            ImmutableArray<DependencyInfo>.Empty,
            ImmutableArray<DependencyInfo>.Empty,
            false);
    }

    /// <summary>
    /// Extract contract parameter from method arguments.
    /// </summary>
    private static string? ExtractContractParameter(InvocationExpressionSyntax invocation)
    {
        var arguments = invocation.ArgumentList.Arguments;
        foreach (var arg in arguments)
        {
            if (arg.Expression is LiteralExpressionSyntax literal &&
                literal.Token.ValueText is string contract)
            {
                return contract;
            }
        }
        return null;
    }

    /// <summary>
    /// Extract lazy mode parameter from method arguments.
    /// </summary>
    private static string? ExtractLazyModeParameter(InvocationExpressionSyntax invocation)
    {
        // Look for LazyThreadSafetyMode enum values in arguments
        var arguments = invocation.ArgumentList.Arguments;
        foreach (var arg in arguments)
        {
            var argText = arg.Expression.ToString();
            if (argText.Contains("LazyThreadSafetyMode"))
            {
                return argText;
            }
        }
        return null;
    }

    /// <summary>
    /// Analyze type dependencies for constructor and property injection.
    /// </summary>
    private static (ImmutableArray<DependencyInfo> ConstructorDeps, ImmutableArray<DependencyInfo> PropertyDeps, bool HasAttribute)
        AnalyzeDependencies(ITypeSymbol concreteType, Compilation compilation)
    {
        var constructorDeps = ImmutableArray.CreateBuilder<DependencyInfo>();
        var propertyDeps = ImmutableArray.CreateBuilder<DependencyInfo>();
        bool hasAttribute = false;

        // Find constructor to use for injection
        var constructor = FindConstructorForInjection(concreteType, out hasAttribute);
        if (constructor is not null)
        {
            foreach (var parameter in constructor.Parameters)
            {
                var (typeName, isLazy) = ExtractTypeInfo(parameter.Type);
                constructorDeps.Add(new DependencyInfo(typeName, isLazy, parameter.Name));
            }
        }

        // Find properties with DependencyInjectionProperty attribute
        foreach (var member in concreteType.GetMembers())
        {
            if (member is IPropertySymbol property && HasDependencyInjectionAttribute(property))
            {
                var (typeName, isLazy) = ExtractTypeInfo(property.Type);
                propertyDeps.Add(new DependencyInfo(typeName, isLazy, PropertyName: property.Name));
            }
        }

        return (constructorDeps.ToImmutable(), propertyDeps.ToImmutable(), hasAttribute);
    }

    /// <summary>
    /// Find the appropriate constructor for dependency injection.
    /// </summary>
    private static IMethodSymbol? FindConstructorForInjection(ITypeSymbol type, out bool hasAttribute)
    {
        hasAttribute = false;
        var constructors = type.GetMembers().OfType<IMethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.Constructor && !m.IsStatic)
            .ToList();

        if (constructors.Count == 1)
            return constructors.First();

        // Look for constructor with DependencyInjectionConstructor attribute
        var attributedConstructors = constructors
            .Where(HasDependencyInjectionConstructorAttribute)
            .ToList();

        if (attributedConstructors.Count == 1)
        {
            hasAttribute = true;
            return attributedConstructors.First();
        }

        return null; // Multiple constructors without clear choice
    }

    /// <summary>
    /// Check if constructor has DependencyInjectionConstructor attribute.
    /// </summary>
    private static bool HasDependencyInjectionConstructorAttribute(IMethodSymbol constructor)
    {
        return constructor.GetAttributes()
            .Any(attr => attr.AttributeClass?.Name == "DependencyInjectionConstructorAttribute");
    }

    /// <summary>
    /// Check if property has DependencyInjectionProperty attribute.
    /// </summary>
    private static bool HasDependencyInjectionAttribute(IPropertySymbol property)
    {
        return property.GetAttributes()
            .Any(attr => attr.AttributeClass?.Name == "DependencyInjectionPropertyAttribute");
    }

    /// <summary>
    /// Extract type information, handling Lazy&lt;T&gt; wrapper.
    /// </summary>
    private static (string TypeName, bool IsLazy) ExtractTypeInfo(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType &&
            namedType.Name == "Lazy" &&
            namedType.TypeArguments.Length == 1)
        {
            return (namedType.TypeArguments[0].ToDisplayString(), true);
        }

        return (type.ToDisplayString(), false);
    }

    /// <summary>
    /// Stage 4: Generate final source code from RegistrationGroup.
    /// </summary>
    private static GeneratedSource? CreateGeneratedSource(RegistrationGroup group)
    {
        if (group.Registrations.IsEmpty)
            return null;

        var statements = group.Registrations
            .Select(GenerateRegistrationStatement)
            .Where(stmt => !string.IsNullOrEmpty(stmt))
            .Select(stmt => $"            {stmt}")
            .ToList();

        if (!statements.Any())
            return null;

        var sourceCode = $$"""
            // <auto-generated />
            namespace {{Constants.NamespaceName}}
            {
                internal static partial class {{Constants.ClassName}}
                {
                    static partial void {{Constants.IocMethod}}({{Constants.ResolverType}} {{Constants.ResolverParameterName}})
                    {
            {{string.Join("\n", statements)}}
                    }
                }
            }
            """;

        return new GeneratedSource("Splat.DI.Reg.g.cs", sourceCode);
    }

    /// <summary>
    /// Generate registration statement for a single RegistrationTarget.
    /// </summary>
    private static string GenerateRegistrationStatement(RegistrationTarget target)
    {
        return target.MethodName switch
        {
            "Register" => GenerateRegisterStatement(target),
            "RegisterLazySingleton" => GenerateRegisterLazySingletonStatement(target),
            "RegisterConstant" => string.Empty, // Constants are handled differently
            _ => string.Empty
        };
    }

    /// <summary>
    /// Generate Register statement.
    /// </summary>
    private static string GenerateRegisterStatement(RegistrationTarget target)
    {
        if (target.ConcreteType is null)
            return string.Empty;

        var objectCreation = GenerateObjectCreation(target);

        return target.Contract is null
            ? $"{Constants.ResolverParameterName}.Register<{target.InterfaceType}>(() => {objectCreation});"
            : $"{Constants.ResolverParameterName}.Register<{target.InterfaceType}>(() => {objectCreation}, \"{target.Contract}\");";
    }

    /// <summary>
    /// Generate RegisterLazySingleton statement.
    /// </summary>
    private static string GenerateRegisterLazySingletonStatement(RegistrationTarget target)
    {
        if (target.ConcreteType is null)
            return string.Empty;

        var objectCreation = GenerateObjectCreation(target);
        var lazyMode = target.LazyMode ?? "System.Threading.LazyThreadSafetyMode.ExecutionAndPublication";
        var lazyCreation = $"new System.Lazy<{target.InterfaceType}>(() => {objectCreation}, {lazyMode})";

        return target.Contract is null
            ? $"{Constants.ResolverParameterName}.RegisterLazySingleton<{target.InterfaceType}>(() => {lazyCreation}.Value);"
            : $"{Constants.ResolverParameterName}.RegisterLazySingleton<{target.InterfaceType}>(() => {lazyCreation}.Value, \"{target.Contract}\");";
    }

    /// <summary>
    /// Generate object creation expression with constructor and property injection.
    /// </summary>
    private static string GenerateObjectCreation(RegistrationTarget target)
    {
        if (target.ConcreteType is null)
            return string.Empty;

        var constructorArgs = target.ConstructorDependencies
            .Select(dep => $"{Constants.ResolverParameterName}.{Constants.LocatorGetService}<{dep.TypeName}>()")
            .ToList();

        if (target.PropertyDependencies.IsEmpty)
        {
            return $"new {target.ConcreteType}({string.Join(", ", constructorArgs)})";
        }

        var propertyInits = target.PropertyDependencies
            .Select(prop => $"{prop.PropertyName} = {Constants.ResolverParameterName}.{Constants.LocatorGetService}<{prop.TypeName}>()")
            .ToList();

        return constructorArgs.Any()
            ? $"new {target.ConcreteType}({string.Join(", ", constructorArgs)}) {{ {string.Join(", ", propertyInits)} }}"
            : $"new {target.ConcreteType}() {{ {string.Join(", ", propertyInits)} }}";
    }
}
