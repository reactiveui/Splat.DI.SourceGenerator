// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections;

using Splat.DependencyInjection.SourceGenerator.Models;

namespace Splat.DependencyInjection.SourceGenerator.Tests;

/// <summary>
/// Tests for EquatableArray.
/// </summary>
public class EquatableArrayTests
{
    /// <summary>
    /// Verifies equality logic.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task Equality_Works()
    {
        var arr1 = new EquatableArray<int>([1, 2, 3]);
        var arr2 = new EquatableArray<int>([1, 2, 3]);
        var arr3 = new EquatableArray<int>([1, 2, 4]);
        var empty1 = new EquatableArray<int>([]);
        var null1 = new EquatableArray<int>(null!);
        var null2 = new EquatableArray<int>(null!);

        await Assert.That(arr1 == arr2).IsTrue();
        await Assert.That(arr1.Equals(arr2)).IsTrue();
        await Assert.That(arr1.Equals((object)arr2)).IsTrue();
        await Assert.That(arr1 != arr3).IsTrue();
        await Assert.That(arr1.Equals(arr3)).IsFalse();
        await Assert.That(null1 == null2).IsTrue();
        await Assert.That(null1 == arr1).IsFalse();
        await Assert.That(arr1 == null1).IsFalse();

        await Assert.That(arr1.Equals(null)).IsFalse();
        await Assert.That(arr1.Equals("not an array")).IsFalse();
    }

    /// <summary>
    /// Verifies GetHashCode logic.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task GetHashCode_Works()
    {
        var arr1 = new EquatableArray<int>([1, 2, 3]);
        var arr2 = new EquatableArray<int>([1, 2, 3]);
        var nullArr = new EquatableArray<int>(null!);

        await Assert.That(arr1.GetHashCode()).IsEqualTo(arr2.GetHashCode());
        await Assert.That(nullArr.GetHashCode()).IsEqualTo(0);
    }

    /// <summary>
    /// Verifies enumeration logic.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task Enumeration_Works()
    {
        var data = new[] { 1, 2, 3 };
        var arr = new EquatableArray<int>(data);
        var nullArr = new EquatableArray<int>(null!);

        await Assert.That(arr.ToList()).Count().IsEqualTo(3);
        await Assert.That(nullArr.ToList()).IsEmpty();

        // Test non-generic enumerator
        IEnumerable enumerable = arr;
        var count = 0;
        foreach (var item in enumerable)
        {
            count++;
        }

        await Assert.That(count).IsEqualTo(3);
    }

    /// <summary>
    /// Verifies indexer and length.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task Indexer_And_Length_Work()
    {
        var arr = new EquatableArray<int>([10, 20]);
        await Assert.That(arr.Length).IsEqualTo(2);
        await Assert.That(arr[0]).IsEqualTo(10);
        await Assert.That(arr[1]).IsEqualTo(20);
    }
}