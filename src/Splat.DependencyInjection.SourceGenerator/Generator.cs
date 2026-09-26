// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using Splat.DependencyInjection.SourceGenerator.CodeGeneration;
using Splat.DependencyInjection.SourceGenerator.Models;

namespace Splat.DependencyInjection.SourceGenerator;

/// <summary>Generates the Splat registrations the marker calls in a project describe.</summary>
/// <remarks>
/// <para>
/// One syntax provider finds the calls to both marker methods, so each syntax node is tested once. The transform turns
/// each call into a <see cref="RegistrationSite"/>, which holds values only and so compares by value.
/// </para>
/// <para>
/// The pipeline then splits. The generated code is built from the registrations alone, without their locations, so an
/// edit that only moves a registration leaves the file cached and nothing is regenerated. The graph diagnostics need
/// the locations, and are reported from their own output.
/// </para>
/// </remarks>
[Generator]
public sealed class Generator : IIncrementalGenerator
{
    /// <summary>The tracking name of the step that extracts each call's registration and location.</summary>
    internal const string SitesStep = "RegistrationSites";

    /// <summary>The tracking name of the step that drops the locations, leaving what the generated code needs.</summary>
    internal const string RegistrationsStep = "Registrations";

    /// <summary>The tracking name of the step that gathers the registrations for the file.</summary>
    internal const string CollectedRegistrationsStep = "CollectedRegistrations";

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static postInitializationContext =>
        {
            postInitializationContext.AddEmbeddedAttributeDefinition();
            postInitializationContext.AddSource(Constants.ExtensionMethodFileName, MarkerSource.Text);
        });

        var sites = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => RoslynHelpers.IsRegistrationInvocation(node),
                transform: MetadataExtractor.Extract)
            .WithTrackingName(SitesStep)
            .Where(static site => site is not null);

        var registrations = sites
            .Select(static (site, _) => site!.Registration)
            .WithTrackingName(RegistrationsStep)
            .Collect()
            .WithTrackingName(CollectedRegistrationsStep);

        context.RegisterSourceOutput(sites.Collect(), RegistrationValidator.ReportDiagnostics);
        context.RegisterSourceOutput(registrations, GenerateCode);
    }

    /// <summary>Writes the registrations file.</summary>
    /// <param name="context">The context the file is added to.</param>
    /// <param name="registrations">The registrations, in source order.</param>
    /// <remarks>
    /// With no registrations the file is left out: <c>SetupIOCInternal</c> is a partial method with no body, so the
    /// compiler removes the calls to it.
    /// </remarks>
    internal static void GenerateCode(SourceProductionContext context, ImmutableArray<RegistrationInfo> registrations)
    {
        if (registrations.IsEmpty)
        {
            return;
        }

        context.AddSource(Constants.RegistrationFileName, CodeGenerator.Generate(registrations));
    }
}
