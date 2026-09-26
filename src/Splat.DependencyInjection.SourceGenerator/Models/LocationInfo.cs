// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Splat.DependencyInjection.SourceGenerator.Models;

/// <summary>A source location held as values, so a pipeline model compares by value and roots no syntax tree.</summary>
/// <param name="FilePath">The path of the file.</param>
/// <param name="TextSpan">The span within the file.</param>
/// <param name="LineSpan">The line and column span within the file.</param>
/// <remarks>
/// A <see cref="Location"/> holds its syntax tree, so keeping one in a model would keep every tree of an old
/// compilation alive and make two models from different compilations unequal even when nothing moved.
/// </remarks>
internal readonly record struct LocationInfo(string FilePath, TextSpan TextSpan, LinePositionSpan LineSpan)
{
    /// <summary>Captures the location of a syntax node.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The location.</returns>
    internal static LocationInfo From(SyntaxNode node)
    {
        var tree = node.SyntaxTree;
        var span = node.Span;
        return new(tree.FilePath, span, tree.GetLineSpan(span).Span);
    }

    /// <summary>Creates the <see cref="Location"/> a diagnostic is reported at.</summary>
    /// <returns>The location.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Location ToLocation() => Location.Create(FilePath, TextSpan, LineSpan);
}
