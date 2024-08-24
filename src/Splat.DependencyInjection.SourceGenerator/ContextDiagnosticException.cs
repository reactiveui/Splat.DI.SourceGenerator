// Copyright (c) 2019-2021 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;

using Microsoft.CodeAnalysis;

namespace Splat.DependencyInjection.SourceGenerator;

/// <summary>
/// When there is an context diagnostic issue.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ContextDiagnosticException"/> class.
/// </remarks>
/// <param name="diagnostic">The diagnostic.</param>
[SuppressMessage("Roslynator", "RCS1194: Implement exception constructor", Justification = "Deliberate usage.")]
public class ContextDiagnosticException(Diagnostic diagnostic) : Exception
{
    /// <summary>
    /// Gets the diagnostic information about the generation context issue.
    /// </summary>
    public Diagnostic Diagnostic { get; } = diagnostic;
}
