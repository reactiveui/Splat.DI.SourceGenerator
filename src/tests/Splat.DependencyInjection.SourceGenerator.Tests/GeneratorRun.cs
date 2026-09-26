// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Diagnostics;

using Microsoft.CodeAnalysis;

namespace Splat.DependencyInjection.SourceGenerator.Tests;

/// <summary>One run of the generator.</summary>
/// <param name="Driver">The driver after the run, which carries its caches into the next.</param>
/// <param name="Result">The generator's result.</param>
/// <param name="Output">The compilation with the generated files added.</param>
[DebuggerDisplay("GeneratorRun: {Result.GeneratedSources.Length} files")]
public sealed record GeneratorRun(GeneratorDriver Driver, GeneratorRunResult Result, Compilation Output)
{
    /// <summary>Gets the registrations file the run generated.</summary>
    /// <returns>The file's text, or <see langword="null"/> when none was generated.</returns>
    public string? RegistrationSource()
    {
        foreach (var generated in Result.GeneratedSources)
        {
            if (generated.HintName == Constants.RegistrationFileName)
            {
                return generated.SourceText.ToString();
            }
        }

        return null;
    }

    /// <summary>Gets the errors in the output compilation, which includes the generated code.</summary>
    /// <returns>The errors.</returns>
    public ImmutableArray<Diagnostic> Errors()
    {
        var errors = ImmutableArray.CreateBuilder<Diagnostic>();
        foreach (var diagnostic in Output.GetDiagnostics())
        {
            if (diagnostic.Severity == DiagnosticSeverity.Error)
            {
                errors.Add(diagnostic);
            }
        }

        return errors.ToImmutable();
    }

    /// <summary>Gets the hint names of the generated files.</summary>
    /// <returns>The hint names.</returns>
    public ImmutableArray<string> HintNames()
    {
        var names = ImmutableArray.CreateBuilder<string>(Result.GeneratedSources.Length);
        foreach (var generated in Result.GeneratedSources)
        {
            names.Add(generated.HintName);
        }

        return names.MoveToImmutable();
    }

    /// <summary>Gets the diagnostics the generator reported, as <c>ID@line</c> with lines counted from one.</summary>
    /// <param name="firstLine">The zero-based line counted as line one.</param>
    /// <returns>The diagnostics, space separated, in the order reported.</returns>
    public string DiagnosticLines(int firstLine)
    {
        var text = new System.Text.StringBuilder();
        foreach (var diagnostic in Result.Diagnostics)
        {
            _ = (text.Length == 0 ? text : text.Append(' '))
                .Append(diagnostic.Id)
                .Append('@')
                .Append(diagnostic.Location.GetLineSpan().StartLinePosition.Line - firstLine + 1);
        }

        return text.ToString();
    }
}
