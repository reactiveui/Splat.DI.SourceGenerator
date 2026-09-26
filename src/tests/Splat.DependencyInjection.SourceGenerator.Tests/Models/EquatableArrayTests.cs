// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Splat.DependencyInjection.SourceGenerator.Models;

namespace Splat.DependencyInjection.SourceGenerator.Tests.Models;

/// <summary>Tests the value equality <see cref="EquatableArray{T}"/> gives the pipeline models.</summary>
public sealed class EquatableArrayTests
{
    /// <summary>A first value.</summary>
    private const int First = 1;

    /// <summary>A second value.</summary>
    private const int Second = 2;

    /// <summary>A third value, and the length of a three-element array.</summary>
    private const int Third = 3;

    /// <summary>Arrays with the same elements are equal and hash alike, whichever array holds them.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EqualByContent()
    {
        var left = new EquatableArray<string>(["a", "b"]);
        var right = new EquatableArray<string>(["a", "b"]);

        await Assert.That(left.Equals(right)).IsTrue();
        await Assert.That(left == right).IsTrue();
        await Assert.That(left != right).IsFalse();
        await Assert.That(left.Equals((object)right)).IsTrue();
        await Assert.That(left.GetHashCode()).IsEqualTo(right.GetHashCode());
    }

    /// <summary>Arrays differing in length, or in an element, are unequal.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UnequalByContent()
    {
        var array = new EquatableArray<string>(["a", "b"]);

        await Assert.That(array.Equals(new(["a"]))).IsFalse();
        await Assert.That(array.Equals(new(["a", "c"]))).IsFalse();
        await Assert.That(array == new EquatableArray<string>(["b", "a"])).IsFalse();
        await Assert.That(array.Equals("a")).IsFalse();
    }

    /// <summary>Two arrays that hash alike but differ are told apart by their elements.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UnequalWithSameHash()
    {
        var left = new EquatableArray<CollidingValue>([new(First), new(Second)]);
        var right = new EquatableArray<CollidingValue>([new(First), new(Third)]);

        await Assert.That(left.GetHashCode()).IsEqualTo(right.GetHashCode());
        await Assert.That(left.Equals(right)).IsFalse();
    }

    /// <summary>A default array is empty, and equal to an empty array.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DefaultIsEmpty()
    {
        var empty = default(EquatableArray<string>);

        await Assert.That(empty.Length).IsEqualTo(0);
        await Assert.That(empty.Equals(EquatableArray<string>.Empty)).IsTrue();
        await Assert.That(empty.Equals(default)).IsTrue();
    }

    /// <summary>The indexer reads the elements in order.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task IndexesInOrder()
    {
        var array = new EquatableArray<string>(["a", "b", "c"]);

        await Assert.That(array[0]).IsEqualTo("a");
        await Assert.That(array[1]).IsEqualTo("b");
        await Assert.That(array.Length).IsEqualTo(Third);
    }

    /// <summary>A value whose hash ignores its content, to force a hash collision.</summary>
    /// <param name="Value">The content.</param>
    private sealed record CollidingValue(int Value)
    {
        /// <inheritdoc/>
        public override int GetHashCode() => 0;
    }
}
