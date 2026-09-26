// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Splat.DependencyInjection.SourceGenerator.CodeGeneration;

/// <summary>The C# constructs generated code is built from, each written through a <see cref="SourceWriter"/>.</summary>
/// <remarks>
/// Each member names one construct and writes its fixed text and its level changes, so an emitter says what it builds
/// and the layout follows from the writer's level.
/// </remarks>
internal static class SourceWriterExtensions
{
    /// <summary>Writes C# constructs through a writer.</summary>
    /// <param name="writer">The writer the construct is written to.</param>
    extension(SourceWriter writer)
    {
        /// <summary>Opens a block-scoped namespace, the form every language version parses.</summary>
        /// <param name="namespaceName">The namespace.</param>
        /// <returns>The writer, one level deeper.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal SourceWriter OpenNamespace(string namespaceName) =>
            writer.Append("namespace ").Line(namespaceName).OpenBlock();

        /// <summary>Writes a blank line.</summary>
        /// <returns>The writer.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal SourceWriter BlankLine() => writer.EndLine();

        /// <summary>Ends the line and moves one level deeper, for the continuation lines of an expression.</summary>
        /// <returns>The writer, one level deeper.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal SourceWriter OpenContinuation() => writer.EndLine().Indent();

        /// <summary>Finishes the last continuation line and moves back to the level the expression began at.</summary>
        /// <param name="trailer">What ends the line, such as <c>);</c>.</param>
        /// <returns>The writer, one level shallower.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal SourceWriter CloseContinuation(string trailer) => writer.Line(trailer).Outdent();

        /// <summary>Writes a separating comma and ends the line, for an argument that is followed by another.</summary>
        /// <returns>The writer.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal SourceWriter ArgumentSeparator() => writer.Line(",");
    }
}
