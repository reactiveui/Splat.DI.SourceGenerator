// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Splat.DependencyInjection.Analyzer.Analyzers;

/// <summary>
/// Analyzer that validates properties marked with [DependencyInjectionProperty] attribute.
/// Ensures that properties have public or internal setters.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PropertyAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The fully qualified symbol display format used for diagnostic messages.
    /// </summary>
    private static readonly SymbolDisplayFormat _fullyQualifiedFormat = SymbolDisplayFormat.FullyQualifiedFormat;

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(SourceGenerator.DiagnosticWarnings.PropertyMustPublicBeSettable);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Use RegisterCompilationStartAction for access to the compilation-wide attribute symbol
        context.RegisterCompilationStartAction(compilationContext =>
        {
            // Try both possible attribute locations (namespace-level from generator, or user-defined)
            var propertyAttributeSymbol = compilationContext.Compilation.GetTypeByMetadataName(SourceGenerator.Constants.PropertyAttributeMetadataName);

            // Only register the symbol action if the attribute exists in this compilation
            if (propertyAttributeSymbol != null)
            {
                compilationContext.RegisterSymbolAction(
                    symbolContext => AnalyzeProperty(symbolContext, propertyAttributeSymbol),
                    SymbolKind.Property);
            }
        });
    }

    /// <summary>
    /// Analyzes a property symbol to check if it has the DependencyInjectionProperty attribute
    /// and validates that its setter is accessible (public or internal).
    /// </summary>
    /// <param name="context">The symbol analysis context.</param>
    /// <param name="propertyAttributeSymbol">The resolved DependencyInjectionPropertyAttribute symbol.</param>
    private static void AnalyzeProperty(SymbolAnalysisContext context, INamedTypeSymbol propertyAttributeSymbol)
    {
        var property = (IPropertySymbol)context.Symbol;

        // Check if property has [DependencyInjectionProperty] attribute (manual loop to avoid LINQ allocation)
        var attrs = property.GetAttributes();
        var hasAttribute = false;

        // Fast path: symbol comparison (no string allocations)
        for (var i = 0; i < attrs.Length; i++)
        {
            if (SymbolEqualityComparer.Default.Equals(attrs[i].AttributeClass, propertyAttributeSymbol))
            {
                hasAttribute = true;
                break;
            }
        }

        if (!hasAttribute)
        {
            return;
        }

        // Check if setter exists and is accessible (public or internal)
        if (property.SetMethod == null || property.SetMethod.DeclaredAccessibility < Accessibility.Internal)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                SourceGenerator.DiagnosticWarnings.PropertyMustPublicBeSettable,
                property.Locations.Length > 0 ? property.Locations[0] : Location.None,
                property.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
        }
    }
}
