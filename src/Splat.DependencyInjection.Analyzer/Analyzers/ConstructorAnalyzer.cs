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

        // Cache attribute symbol for comparison (avoids repeated string allocations)
        var constructorAttributeSymbol = context.Compilation.GetTypeByMetadataName(
            "Splat.DependencyInjection.DependencyInjectionConstructorAttribute");

        // Single-pass counting (eliminates 3 List allocations and repeated GetAttributes() calls)
        var constructors = namedType.Constructors;
        var accessibleCount = 0;
        var markedCount = 0;
        IMethodSymbol? firstMarked = null;
        IMethodSymbol? secondMarked = null;

        for (var i = 0; i < constructors.Length; i++)
        {
            var ctor = constructors[i];

            if (ctor.IsStatic)
            {
                continue;
            }

            // Check accessibility
            if (ctor.DeclaredAccessibility >= Accessibility.Internal)
            {
                accessibleCount++;
            }

            // Check for attribute
            var attrs = ctor.GetAttributes();
            var isMarked = false;

            if (constructorAttributeSymbol != null)
            {
                // Fast path: symbol comparison
                for (var j = 0; j < attrs.Length; j++)
                {
                    if (SymbolEqualityComparer.Default.Equals(attrs[j].AttributeClass, constructorAttributeSymbol))
                    {
                        isMarked = true;
                        break;
                    }
                }
            }
            else
            {
                // Fallback: string comparison (for test scenarios where metadata lookup may fail)
                for (var j = 0; j < attrs.Length; j++)
                {
                    if (attrs[j].AttributeClass?.ToDisplayString(_fullyQualifiedFormat) == SourceGenerator.Constants.ConstructorAttribute)
                    {
                        isMarked = true;
                        break;
                    }
                }
            }

            if (isMarked)
            {
                markedCount++;
                if (firstMarked == null)
                {
                    firstMarked = ctor;
                }
                else if (secondMarked == null)
                {
                    secondMarked = ctor;
                }
            }
        }

        // If there's a marked constructor, always validate it
        if (markedCount > 0)
        {
            if (markedCount > 1)
            {
                // SPLATDI003: Multiple constructors marked - report on all marked constructors
                for (var i = 0; i < constructors.Length; i++)
                {
                    var ctor = constructors[i];
                    var attrs = ctor.GetAttributes();
                    var ctorIsMarked = false;

                    // Check if this constructor is marked
                    if (constructorAttributeSymbol != null)
                    {
                        // Fast path: symbol comparison
                        for (var j = 0; j < attrs.Length; j++)
                        {
                            if (SymbolEqualityComparer.Default.Equals(attrs[j].AttributeClass, constructorAttributeSymbol))
                            {
                                ctorIsMarked = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        // Fallback: string comparison
                        for (var j = 0; j < attrs.Length; j++)
                        {
                            if (attrs[j].AttributeClass?.ToDisplayString(_fullyQualifiedFormat) == SourceGenerator.Constants.ConstructorAttribute)
                            {
                                ctorIsMarked = true;
                                break;
                            }
                        }
                    }

                    if (ctorIsMarked)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            SourceGenerator.DiagnosticWarnings.MultipleConstructorsMarked,
                            ctor.Locations.Length > 0 ? ctor.Locations[0] : Location.None,
                            namedType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
                    }
                }
            }
            else if (firstMarked != null)
            {
                // Exactly one constructor is marked - check accessibility
                if (firstMarked.DeclaredAccessibility < Accessibility.Internal)
                {
                    // SPLATDI004: Constructor must be public or internal
                    context.ReportDiagnostic(Diagnostic.Create(
                        SourceGenerator.DiagnosticWarnings.ConstructorsMustBePublic,
                        firstMarked.Locations.Length > 0 ? firstMarked.Locations[0] : Location.None,
                        namedType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
                }
            }

            return; // Don't check for multiple constructors without attribute if one is marked
        }

        // No marked constructors - check if we need to warn about multiple accessible constructors
        if (accessibleCount > 1)
        {
            // SPLATDI001: Multiple constructors without attribute
            context.ReportDiagnostic(Diagnostic.Create(
                SourceGenerator.DiagnosticWarnings.MultipleConstructorNeedAttribute,
                namedType.Locations.Length > 0 ? namedType.Locations[0] : Location.None,
                namedType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
        }
    }
}
