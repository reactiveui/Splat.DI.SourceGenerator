// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

namespace Splat.DependencyInjection.Analyzer.Analyzers;

/// <summary>
/// Helper methods for Splat analyzers.
/// </summary>
internal static class AnalyzerHelpers
{
    /// <summary>
    /// The fully qualified symbol display format used for attribute string comparison fallback.
    /// </summary>
    private static readonly SymbolDisplayFormat _fullyQualifiedFormat = SymbolDisplayFormat.FullyQualifiedFormat;

    /// <summary>
    /// Checks if a method symbol is a specific method from SplatRegistrations class.
    /// </summary>
    /// <param name="methodSymbol">The method symbol to check.</param>
    /// <param name="methodName">The expected method name.</param>
    /// <returns>True if the method is from SplatRegistrations with the specified name.</returns>
    public static bool IsSplatRegistrationsMethod(IMethodSymbol methodSymbol, string methodName)
    {
        return IsContainedInSplatRegistrations(methodSymbol.ContainingType?.OriginalDefinition) &&
               methodSymbol.Name == methodName &&
               !methodSymbol.IsExtensionMethod;
    }

    /// <summary>
    /// Analyzes constructors for a specific type and reports diagnostics.
    /// </summary>
    /// <param name="compilation">The compilation.</param>
    /// <param name="typeSymbol">The type to analyze.</param>
    /// <param name="reportDiagnostic">Action to report diagnostics.</param>
    public static void AnalyzeConstructorsForType(
        Compilation compilation,
        ITypeSymbol typeSymbol,
        Action<Diagnostic> reportDiagnostic)
    {
        // Only analyze classes and structs
        if (typeSymbol.TypeKind != TypeKind.Class && typeSymbol.TypeKind != TypeKind.Struct)
        {
            return;
        }

        var namedType = (INamedTypeSymbol)typeSymbol;

        // Cache attribute symbol
        var constructorAttributeSymbol = compilation.GetTypeByMetadataName(
            SourceGenerator.Constants.ConstructorAttributeMetadataName);

        var analysis = GetConstructorAnalysis(namedType, constructorAttributeSymbol);

        ReportDiagnostics(analysis, namedType, constructorAttributeSymbol, reportDiagnostic);
    }

    /// <summary>
    /// Checks if a type symbol represents the SplatRegistrations class in the Splat namespace.
    /// </summary>
    /// <param name="containingType">The containing type (may be null if the method has no containing type).</param>
    /// <returns>True if the type is Splat.SplatRegistrations.</returns>
    internal static bool IsContainedInSplatRegistrations(INamedTypeSymbol? containingType)
    {
        if (containingType == null)
        {
            return false;
        }

        return containingType.ContainingNamespace?.Name == SourceGenerator.Constants.NamespaceName &&
               containingType.Name == SourceGenerator.Constants.ClassName;
    }

    /// <summary>
    /// Analyzes the constructors of a type to count accessible and marked constructors.
    /// </summary>
    /// <param name="namedType">The type to analyze.</param>
    /// <param name="constructorAttributeSymbol">The attribute symbol to check for.</param>
    /// <returns>Analysis result.</returns>
    internal static ConstructorAnalysisResult GetConstructorAnalysis(INamedTypeSymbol namedType, INamedTypeSymbol? constructorAttributeSymbol)
    {
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

            if (IsConstructorMarked(ctor, constructorAttributeSymbol))
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

        return new ConstructorAnalysisResult(accessibleCount, markedCount, firstMarked, secondMarked);
    }

    /// <summary>
    /// Checks if a constructor is marked with the DependencyInjectionConstructor attribute.
    /// </summary>
    /// <param name="ctor">The constructor to check.</param>
    /// <param name="constructorAttributeSymbol">The attribute symbol to check against (if available).</param>
    /// <returns>True if marked.</returns>
    internal static bool IsConstructorMarked(IMethodSymbol ctor, INamedTypeSymbol? constructorAttributeSymbol)
    {
        var attrs = ctor.GetAttributes();
        if (constructorAttributeSymbol != null)
        {
            // Fast path: symbol comparison
            for (var j = 0; j < attrs.Length; j++)
            {
                if (SymbolEqualityComparer.Default.Equals(attrs[j].AttributeClass, constructorAttributeSymbol))
                {
                    return true;
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
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Reports diagnostics based on constructor analysis results.
    /// Reports SPLATDI003 when multiple constructors are marked, SPLATDI004 when a marked
    /// constructor is not accessible, and SPLATDI001 when multiple accessible constructors
    /// exist without a marked constructor.
    /// </summary>
    /// <param name="analysis">The constructor analysis result.</param>
    /// <param name="namedType">The type being analyzed.</param>
    /// <param name="constructorAttributeSymbol">The attribute symbol to check for (may be null).</param>
    /// <param name="reportDiagnostic">Action to report diagnostics.</param>
    internal static void ReportDiagnostics(
        ConstructorAnalysisResult analysis,
        INamedTypeSymbol namedType,
        INamedTypeSymbol? constructorAttributeSymbol,
        Action<Diagnostic> reportDiagnostic)
    {
        // If there's a marked constructor, always validate it
        if (analysis.MarkedCount > 0)
        {
            if (analysis.MarkedCount > 1)
            {
                // SPLATDI003: Multiple constructors marked - report on all marked constructors
                var constructors = namedType.Constructors;
                for (var i = 0; i < constructors.Length; i++)
                {
                    var ctor = constructors[i];
                    if (IsConstructorMarked(ctor, constructorAttributeSymbol))
                    {
                        reportDiagnostic(Diagnostic.Create(
                            SourceGenerator.DiagnosticWarnings.MultipleConstructorsMarked,
                            GetFirstLocation(ctor.Locations),
                            namedType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
                    }
                }
            }
            else if (analysis.FirstMarked != null)
            {
                // Exactly one constructor is marked - check accessibility
                if (analysis.FirstMarked.DeclaredAccessibility < Accessibility.Internal)
                {
                    // SPLATDI004: Constructor must be public or internal
                    reportDiagnostic(Diagnostic.Create(
                        SourceGenerator.DiagnosticWarnings.ConstructorsMustBePublic,
                        GetFirstLocation(analysis.FirstMarked.Locations),
                        namedType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
                }
            }

            return; // Don't check for multiple constructors without attribute if one is marked
        }

        // No marked constructors - check if we need to warn about multiple accessible constructors
        if (analysis.AccessibleCount > 1)
        {
            // SPLATDI001: Multiple constructors without attribute
            reportDiagnostic(Diagnostic.Create(
                SourceGenerator.DiagnosticWarnings.MultipleConstructorNeedAttribute,
                GetFirstLocation(namedType.Locations),
                namedType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
        }
    }

    /// <summary>
    /// Gets the first location from a locations array, or <see cref="Location.None"/> if empty.
    /// </summary>
    /// <param name="locations">The locations array.</param>
    /// <returns>The first location or <see cref="Location.None"/>.</returns>
    internal static Location GetFirstLocation(ImmutableArray<Location> locations)
        => locations.Length > 0 ? locations[0] : Location.None;

    /// <summary>
    /// Result of constructor analysis.
    /// </summary>
    /// <param name="AccessibleCount"> Gets the number of accessible constructors. </param>
    /// <param name="MarkedCount"> Gets the number of constructors marked with the attribute. </param>
    /// <param name="FirstMarked"> Gets the first constructor found that was marked with the attribute. </param>
    /// <param name="SecondMarked"> Gets the second constructor found that was marked with the attribute. </param>
    internal readonly record struct ConstructorAnalysisResult(int AccessibleCount, int MarkedCount, IMethodSymbol? FirstMarked, IMethodSymbol? SecondMarked);
}
