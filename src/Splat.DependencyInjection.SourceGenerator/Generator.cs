// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

using Splat.DependencyInjection.SourceGenerator.CodeGeneration;
using Splat.DependencyInjection.SourceGenerator.Models;

namespace Splat.DependencyInjection.SourceGenerator;

/// <summary>
/// Incremental generator for Splat dependency injection registrations.
/// </summary>
[Generator]
public class Generator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Always emit attributes and marker methods (per Cookbook best practices)
        context.RegisterPostInitializationOutput(ctx =>
        {
            // Emit EmbeddedAttribute first to avoid conflicts with InternalsVisibleTo
            ctx.AddSource(
                "Microsoft.CodeAnalysis.EmbeddedAttribute.g.cs",
                SourceText.From(Constants.EmbeddedAttributeText, Encoding.UTF8));

            // Emit marker attributes and extension methods
            ctx.AddSource(
                "Splat.DI.g.cs",
                SourceText.From(Constants.ExtensionMethodText, Encoding.UTF8));
        });

        // Pipeline 1: Register<TInterface, TConcrete>() calls
        var registerCalls = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: RoslynHelpers.IsRegisterInvocation,
                transform: MetadataExtractor.ExtractRegisterMetadata)
            .Where(x => x is not null)
            .Select((x, _) => x!);

        // Pipeline 2: RegisterLazySingleton<TInterface, TConcrete>() calls
        var lazySingletonCalls = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: RoslynHelpers.IsRegisterLazySingletonInvocation,
                transform: MetadataExtractor.ExtractLazySingletonMetadata)
            .Where(x => x is not null)
            .Select((x, _) => x!);

        // Combine all registrations
        var allRegistrations = registerCalls
            .Collect()
            .Combine(lazySingletonCalls.Collect());

        // Generate output with validation
        context.RegisterSourceOutput(allRegistrations, GenerateCode);
    }

    private static void GenerateCode(
        SourceProductionContext context,
        (ImmutableArray<TransientRegistrationInfo> Transients, ImmutableArray<LazySingletonRegistrationInfo> LazySingletons) data)
    {
        var (transients, lazySingletons) = data;

        // Generate code only for valid registrations (invalid ones were filtered out in transform)
        var code = CodeGenerator.GenerateSetupIOCMethod(transients, lazySingletons);
        context.AddSource("Splat.DI.Reg.g.cs", SourceText.From(code, Encoding.UTF8));
    }
}