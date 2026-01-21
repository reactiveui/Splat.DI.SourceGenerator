// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Splat.DependencyInjection.Analyzer.Analyzers;

/// <summary>
/// Analyzer that detects constructor issues for dependency injection.
/// Warns when a class has multiple constructors without [DependencyInjectionConstructor] attribute.
/// Errors when multiple constructors are marked with the attribute.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ConstructorAnalyzer : DiagnosticAnalyzer
{
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

        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;

        // Check if it's SplatRegistrations.Register or RegisterLazySingleton
        if (!AnalyzerHelpers.IsSplatRegistrationsMethod(method, "Register") &&
            !AnalyzerHelpers.IsSplatRegistrationsMethod(method, "RegisterLazySingleton") &&
            !AnalyzerHelpers.IsSplatRegistrationsMethod(method, "RegisterConstant"))
        {
            return;
        }

        // Extract concrete type from type arguments
        if (method.TypeArguments.Length == 0 || method.TypeArguments.Length > 2)
        {
            return;
        }

        var concreteType = method.TypeArguments.Length == 2
            ? method.TypeArguments[1]
            : method.TypeArguments[0];

        // Analyze this specific type's constructors
        AnalyzerHelpers.AnalyzeConstructorsForType(context.Compilation, concreteType, context.ReportDiagnostic);
    }
}
