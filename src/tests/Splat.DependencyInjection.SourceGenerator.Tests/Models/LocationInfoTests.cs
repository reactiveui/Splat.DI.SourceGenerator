// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp;

using Splat.DependencyInjection.SourceGenerator.Models;

namespace Splat.DependencyInjection.SourceGenerator.Tests.Models;

/// <summary>Tests that <see cref="LocationInfo"/> keeps a node's position as values and rebuilds it.</summary>
public sealed class LocationInfoTests
{
    /// <summary>The file every tree is parsed as.</summary>
    private const string FilePath = "C.cs";

    /// <summary>The start of the call each test locates.</summary>
    private const string Call = "N()";

    /// <summary>The captured location reports the same file, span and lines as the node.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RoundTripsNodeLocation()
    {
        var tree = CSharpSyntaxTree.ParseText("class C\n{\n    void M() => N();\n}", path: FilePath);
        var invocation = await TestHelper.FindInvocationAsync(tree, Call);

        var location = LocationInfo.From(invocation).ToLocation();

        await Assert.That(location.GetLineSpan()).IsEqualTo(invocation.GetLocation().GetLineSpan());
        await Assert.That(location.SourceSpan).IsEqualTo(invocation.Span);
    }

    /// <summary>The same position in two parses of a file compares equal, although the trees differ.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EqualAcrossTrees()
    {
        const string Source = "class C { void M() => N(); }";
        var first = await TestHelper.FindInvocationAsync(CSharpSyntaxTree.ParseText(Source, path: FilePath), Call);
        var second = await TestHelper.FindInvocationAsync(CSharpSyntaxTree.ParseText(Source, path: FilePath), Call);

        await Assert.That(LocationInfo.From(first)).IsEqualTo(LocationInfo.From(second));
        await Assert.That(first.GetLocation()).IsNotEqualTo(second.GetLocation());
    }
}
