// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text;

namespace Splat.DependencyInjection.SourceGenerator.CodeGeneration;

/// <summary>Writes generated C# source, tracking the indentation level so emitters never spell it out.</summary>
/// <remarks>
/// <para>
/// The writer owns the layout rules: a line is indented when its first character is written, a blank line carries no
/// trailing whitespace, and every line ends with <c>\n</c>. Emitters say what they write - a line, a block, a nested
/// argument list - and the level follows from the structure, so a fragment written by one emitter lands at the right
/// depth whichever emitter called it.
/// </para>
/// <para>
/// The text accumulates in a <see cref="StringBuilder"/>, which grows in chunks rather than by copying. Indentation
/// comes from a table of precomputed strings, so indenting a line is one append. Names are appended straight from the
/// model's strings, so no intermediate string is built for a line.
/// </para>
/// <para>
/// A writer is used by one source-output callback at a time and never outlives it. <see cref="Rent"/> takes its
/// builder from <see cref="PooledBuilder"/>'s per-thread slot. Nothing a writer holds reaches a pipeline model, so it
/// has no bearing on incremental caching.
/// </para>
/// </remarks>
internal sealed class SourceWriter
{
    /// <summary>The number of spaces one indentation level adds.</summary>
    internal const int IndentWidth = 4;

    /// <summary>The number of indentation levels served from the precomputed table.</summary>
    internal const int CachedIndentLevels = 16;

    /// <summary>The indentation for each level, indexed by level.</summary>
    private static readonly string[] Indents = BuildIndents();

    /// <summary>The builder the text accumulates in.</summary>
    private readonly StringBuilder _builder;

    /// <summary>The length of the text when the current line began; equal to the length while nothing is on it.</summary>
    private int _lineStart;

    /// <summary>Initializes a new instance of the <see cref="SourceWriter"/> class over an empty builder.</summary>
    /// <param name="builder">The builder to write into.</param>
    private SourceWriter(StringBuilder builder) => _builder = builder;

    /// <summary>Gets the current indentation level.</summary>
    internal int Level { get; private set; }

    /// <summary>Materializes the text written so far, leaving the writer usable.</summary>
    /// <returns>The accumulated text.</returns>
    public override string ToString() => _builder.ToString();

    /// <summary>Rents an empty writer at level zero, over a builder from the thread's pool.</summary>
    /// <param name="capacity">The capacity the caller expects to need.</param>
    /// <returns>An empty writer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static SourceWriter Rent(int capacity) => new(PooledBuilder.Rent(capacity));

    /// <summary>Materializes the text and hands the builder back for reuse.</summary>
    /// <returns>The accumulated text.</returns>
    /// <remarks>The caller must not touch the writer afterwards.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal string ToStringAndReturn() => PooledBuilder.ToStringAndReturn(_builder);

    /// <summary>Moves one level deeper.</summary>
    /// <returns>This writer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal SourceWriter Indent()
    {
        Level++;
        return this;
    }

    /// <summary>Moves one level shallower.</summary>
    /// <returns>This writer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal SourceWriter Outdent()
    {
        Level--;
        return this;
    }

    /// <summary>Writes text, indenting it when it starts a line.</summary>
    /// <param name="text">The text, which must not contain a line break.</param>
    /// <returns>This writer.</returns>
    internal SourceWriter Append(string text)
    {
        StartLine();
        _ = _builder.Append(text);
        return this;
    }

    /// <summary>Writes one character, indenting it when it starts a line.</summary>
    /// <param name="value">The character, which must not be a line break.</param>
    /// <returns>This writer.</returns>
    internal SourceWriter Append(char value)
    {
        StartLine();
        _ = _builder.Append(value);
        return this;
    }

    /// <summary>Writes the invariant decimal rendering of an integer.</summary>
    /// <param name="value">The value, which is never negative.</param>
    /// <returns>This writer.</returns>
    internal SourceWriter Append(int value)
    {
        StartLine();
        _ = _builder.Append(value);
        return this;
    }

    /// <summary>Ends the current line; at the start of a line, writes a blank one.</summary>
    /// <returns>This writer.</returns>
    internal SourceWriter EndLine()
    {
        _ = _builder.Append('\n');
        _lineStart = _builder.Length;
        return this;
    }

    /// <summary>Writes text and ends the line.</summary>
    /// <param name="text">The text, which must not contain a line break.</param>
    /// <returns>This writer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal SourceWriter Line(string text) => Append(text).EndLine();

    /// <summary>Writes a block of lines, each at the current level plus the indentation it carries itself.</summary>
    /// <param name="block">The lines, separated by <c>\n</c> or <c>\r\n</c>; the last line need not be terminated.</param>
    /// <returns>This writer.</returns>
    /// <remarks>
    /// For fixed text written as a raw string literal. The literal's own indentation is relative to the level it is
    /// written at, so the same block reads correctly wherever an emitter places it. An empty line stays empty.
    /// </remarks>
    internal SourceWriter Lines(string block)
    {
        var start = 0;
        while (start < block.Length)
        {
            var end = block.IndexOf('\n', start);
            var next = end < 0 ? block.Length : end + 1;
            var contentEnd = end < 0 ? block.Length : end;
            if (contentEnd > start && block[contentEnd - 1] == '\r')
            {
                contentEnd--;
            }

            if (contentEnd > start)
            {
                StartLine();
                _ = _builder.Append(block, start, contentEnd - start);
            }

            _ = EndLine();
            start = next;
        }

        return this;
    }

    /// <summary>Opens a brace block on its own line and moves one level deeper.</summary>
    /// <returns>This writer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal SourceWriter OpenBlock() => Line("{").Indent();

    /// <summary>Moves one level shallower and closes the brace block on its own line.</summary>
    /// <returns>This writer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal SourceWriter CloseBlock() => Outdent().Line("}");

    /// <summary>Moves one level shallower and writes the closing brace, leaving the line open.</summary>
    /// <returns>This writer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal SourceWriter CloseBlockInline() => Outdent().Append('}');

    /// <summary>Builds the indentation table.</summary>
    /// <returns>The indentation for each cached level.</returns>
    private static string[] BuildIndents()
    {
        var indents = new string[CachedIndentLevels];
        for (var i = 0; i < indents.Length; i++)
        {
            indents[i] = new(' ', i * IndentWidth);
        }

        return indents;
    }

    /// <summary>Writes the indentation when nothing has been written on the current line yet.</summary>
    /// <remarks>
    /// A line that has started is longer than its start, so the test needs no flag of its own. At level zero the
    /// indentation is empty, so the table's first entry writes nothing. The registration file nests six levels at
    /// most, so every level is served from the table.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void StartLine()
    {
        if (_builder.Length == _lineStart)
        {
            _ = _builder.Append(Indents[Level]);
        }
    }
}
