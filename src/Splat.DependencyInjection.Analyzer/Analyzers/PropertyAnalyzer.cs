// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Linq;

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

        context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
    }

    private static void AnalyzeProperty(SymbolAnalysisContext context)
    {
        var property = (IPropertySymbol)context.Symbol;

        // Cache attribute symbol for comparison (avoids repeated string allocations)
        var propertyAttributeSymbol = context.Compilation.GetTypeByMetadataName(
            "Splat.DependencyInjection.DependencyInjectionPropertyAttribute");

        // Check if property has [DependencyInjectionProperty] attribute (manual loop to avoid LINQ allocation)
        var attrs = property.GetAttributes();
        var hasAttribute = false;

        if (propertyAttributeSymbol != null)
        {
            // Fast path: symbol comparison
            for (var i = 0; i < attrs.Length; i++)
            {
                if (SymbolEqualityComparer.Default.Equals(attrs[i].AttributeClass, propertyAttributeSymbol))
                {
                    hasAttribute = true;
                    break;
                }
            }
        }
        else
        {
            // Fallback: string comparison (for test scenarios where metadata lookup may fail)
            for (var i = 0; i < attrs.Length; i++)
            {
                if (attrs[i].AttributeClass?.ToDisplayString(_fullyQualifiedFormat) == SourceGenerator.Constants.PropertyAttribute)
                {
                    hasAttribute = true;
                    break;
                }
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
