// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Splat.DependencyInjection.SourceGenerator.CodeGeneration;

namespace Splat.DependencyInjection.SourceGenerator.Tests.CodeGeneration;

/// <summary>Tests the layout rules <see cref="SourceWriter"/> applies.</summary>
public sealed class SourceWriterTests
{
    /// <summary>The capacity each rent asks for.</summary>
    private const int Capacity = 16;

    /// <summary>A line is indented by the level it starts at, and text appended to it is not indented again.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task IndentsEachLineOnce()
    {
        var writer = SourceWriter.Rent(Capacity).Line("a").Indent().Append("b").Append('c').Append("d").EndLine().Outdent().Line("e");

        await Assert.That(writer.ToString()).IsEqualTo("a\n    bcd\ne\n");
        await Assert.That(writer.Level).IsEqualTo(0);
    }

    /// <summary>Blocks write their braces at the outer level and their content one level deeper.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task OpensAndClosesBlocks()
    {
        var writer = SourceWriter.Rent(Capacity).OpenNamespace("N").Line("x").OpenBlock().CloseBlockInline().Line(";").CloseBlock();

        await Assert.That(writer.ToStringAndReturn()).IsEqualTo("namespace N\n{\n    x\n    {\n    };\n}\n");
    }

    /// <summary>A continuation moves the rest of an expression one level deeper and back.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task WritesContinuations()
    {
        var writer = SourceWriter.Rent(Capacity).Append("f(").OpenContinuation().Append("a").ArgumentSeparator().Append("b").CloseContinuation(");").BlankLine().Line("g();");

        await Assert.That(writer.ToString()).IsEqualTo("f(\n    a,\n    b);\n\ng();\n");
        await Assert.That(writer.Level).IsEqualTo(0);
    }

    /// <summary>
    /// A block of lines is written at the writer's level, keeping its own indentation, taking either line ending,
    /// leaving empty lines empty, and finishing an unterminated last line.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task WritesBlocksOfLines()
    {
        var writer = SourceWriter.Rent(Capacity).Indent().Lines("a\r\n\n  b\nc");

        await Assert.That(writer.ToString()).IsEqualTo("    a\n\n      b\n    c\n");
    }

    /// <summary>A block of lines ending in a line break writes no extra blank line.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task WritesTerminatedBlockWithoutExtraLine()
    {
        var writer = SourceWriter.Rent(Capacity).Lines("a\n");

        await Assert.That(writer.ToString()).IsEqualTo("a\n");
    }
}
