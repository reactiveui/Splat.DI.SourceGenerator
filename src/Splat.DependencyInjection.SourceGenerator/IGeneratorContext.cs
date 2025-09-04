// Copyright (c) 2019-2021 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Splat.DependencyInjection.SourceGenerator;

/// <summary>
/// Minimal interface for generator context compatibility.
/// </summary>
internal interface IGeneratorContext
{
    Compilation Compilation { get; }
    void ReportDiagnostic(Diagnostic diagnostic);
}

/// <summary>
/// Adapter for GeneratorExecutionContext to implement IGeneratorContext.
/// </summary>
internal readonly struct GeneratorExecutionContextAdapter : IGeneratorContext
{
    private readonly GeneratorExecutionContext _context;

    public GeneratorExecutionContextAdapter(GeneratorExecutionContext context)
    {
        _context = context;
    }

    public Compilation Compilation => _context.Compilation;

    public void ReportDiagnostic(Diagnostic diagnostic) => _context.ReportDiagnostic(diagnostic);
}

/// <summary>
/// Minimal implementation for incremental generators.
/// </summary>
internal class MinimalGeneratorContext : IGeneratorContext
{
    public MinimalGeneratorContext(Compilation compilation)
    {
        Compilation = compilation;
    }

    public Compilation Compilation { get; }

    public void ReportDiagnostic(Diagnostic diagnostic)
    {
        // For incremental generators, diagnostics are handled differently
        // In a full implementation, these would be collected and reported through the proper channels
    }
}
