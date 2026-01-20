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
/// Analyzer that detects constructor issues for dependency injection.
/// Warns when a class has multiple constructors without [DependencyInjectionConstructor] attribute.
/// Errors when multiple constructors are marked with the attribute.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ConstructorAnalyzer : DiagnosticAnalyzer
{
    private static readonly SymbolDisplayFormat _fullyQualifiedFormat = SymbolDisplayFormat.FullyQualifiedFormat;

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            SourceGenerator.DiagnosticWarnings.MultipleConstructorNeedAttribute,
            SourceGenerator.DiagnosticWarnings.MultipleConstructorsMarked,
            SourceGenerator.DiagnosticWarnings.ConstructorsMustBePublic);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var namedType = (INamedTypeSymbol)context.Symbol;

        // Only analyze classes and structs
        if (namedType.TypeKind != TypeKind.Class && namedType.TypeKind != TypeKind.Struct)
        {
            return;
        }

        // Get all instance constructors
        var allConstructors = namedType.Constructors
            .Where(c => !c.IsStatic)
            .ToList();

        // Find constructors marked with [DependencyInjectionConstructor] (check ALL constructors including private)
        var markedConstructors = allConstructors
            .Where(c => c.GetAttributes().Any(a =>
                a.AttributeClass?.ToDisplayString(_fullyQualifiedFormat) == SourceGenerator.Constants.ConstructorAttribute))
            .ToList();

        // Only consider constructors with >= Internal accessibility for counting
        var accessibleConstructors = allConstructors
            .Where(c => c.DeclaredAccessibility >= Accessibility.Internal)
            .ToList();

        // If there's a marked constructor, always validate it
        if (markedConstructors.Count > 0)
        {
            if (markedConstructors.Count > 1)
            {
                // SPLATDI003: Multiple constructors marked
                foreach (var ctor in markedConstructors)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        SourceGenerator.DiagnosticWarnings.MultipleConstructorsMarked,
                        ctor.Locations.First(),
                        namedType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
                }
            }
            else
            {
                // Exactly one constructor is marked - check accessibility
                var markedConstructor = markedConstructors[0];
                if (markedConstructor.DeclaredAccessibility < Accessibility.Internal)
                {
                    // SPLATDI004: Constructor must be public or internal
                    context.ReportDiagnostic(Diagnostic.Create(
                        SourceGenerator.DiagnosticWarnings.ConstructorsMustBePublic,
                        markedConstructor.Locations.First(),
                        namedType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
                }
            }

            return; // Don't check for multiple constructors without attribute if one is marked
        }

        // No marked constructors - check if we need to warn about multiple accessible constructors
        if (accessibleConstructors.Count > 1)
        {
            // SPLATDI001: Multiple constructors without attribute
            context.ReportDiagnostic(Diagnostic.Create(
                SourceGenerator.DiagnosticWarnings.MultipleConstructorNeedAttribute,
                namedType.Locations.First(),
                namedType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
        }
    }
}
