// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Splat.DependencyInjection.SourceGenerator.CodeGeneration;

namespace Splat.DependencyInjection.SourceGenerator.Tests.CodeGeneration;

/// <summary>Tests that <see cref="PooledBuilder"/> reuses builders on a thread and lets outsized ones go.</summary>
public sealed class PooledBuilderTests
{
    /// <summary>The capacity each rent asks for.</summary>
    private const int Capacity = 16;

    /// <summary>A returned builder is handed out, empty, by the next rent on the same thread.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReusesReturnedBuilder()
    {
        var first = PooledBuilder.Rent(Capacity);
        _ = first.Append("text");
        var text = PooledBuilder.ToStringAndReturn(first);

        var second = PooledBuilder.Rent(Capacity);
        var third = PooledBuilder.Rent(Capacity);

        await Assert.That(text).IsEqualTo("text");
        await Assert.That(second).IsSameReferenceAs(first);
        await Assert.That(second.Length).IsEqualTo(0);
        await Assert.That(third).IsNotSameReferenceAs(first);
    }

    /// <summary>A builder grown past the retention limit is not kept.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DropsOutsizedBuilder()
    {
        var outsized = PooledBuilder.Rent(PooledBuilder.MaxRetainedCapacity + 1);
        _ = PooledBuilder.ToStringAndReturn(outsized);

        var next = PooledBuilder.Rent(Capacity);

        await Assert.That(next).IsNotSameReferenceAs(outsized);
    }
}
