// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections;
using Splat.DependencyInjection.SourceGenerator.Models;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Splat.DependencyInjection.SourceGenerator.Tests.Models;

/// <summary>
/// Tests for the EquatableArray struct.
/// Ensures proper value-equality semantics for arrays in incremental generator pipelines.
/// </summary>
public class EquatableArrayTests
{
    /// <summary>
    /// Tests that two EquatableArray instances with the same values are equal.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task EquatableArray_WithSameValues_AreEqual()
    {
        var array1 = new EquatableArray<string>(new[] { "a", "b", "c" });
        var array2 = new EquatableArray<string>(new[] { "a", "b", "c" });

        await Assert.That(array1.Equals(array2)).IsTrue();
        await Assert.That(array1 == array2).IsTrue();
        await Assert.That(array1 != array2).IsFalse();
        await Assert.That(array1.GetHashCode()).IsEqualTo(array2.GetHashCode());
    }

    /// <summary>
    /// Tests that two EquatableArray instances with different values are not equal.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task EquatableArray_WithDifferentValues_AreNotEqual()
    {
        var array1 = new EquatableArray<string>(new[] { "a", "b", "c" });
        var array2 = new EquatableArray<string>(new[] { "a", "b", "d" });

        await Assert.That(array1.Equals(array2)).IsFalse();
        await Assert.That(array1 == array2).IsFalse();
        await Assert.That(array1 != array2).IsTrue();
    }

    /// <summary>
    /// Tests that two EquatableArray instances with different lengths are not equal.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task EquatableArray_WithDifferentLengths_AreNotEqual()
    {
        var array1 = new EquatableArray<string>(new[] { "a", "b", "c" });
        var array2 = new EquatableArray<string>(new[] { "a", "b" });

        await Assert.That(array1.Equals(array2)).IsFalse();
        await Assert.That(array1 == array2).IsFalse();
        await Assert.That(array1 != array2).IsTrue();
    }

    /// <summary>
    /// Tests that two default EquatableArray instances are equal.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task EquatableArray_DefaultInstances_AreEqual()
    {
        var array1 = default(EquatableArray<string>);
        var array2 = default(EquatableArray<string>);

        await Assert.That(array1.Equals(array2)).IsTrue();
        await Assert.That(array1 == array2).IsTrue();
        await Assert.That(array1 != array2).IsFalse();
        await Assert.That(array1.GetHashCode()).IsEqualTo(array2.GetHashCode());
    }

    /// <summary>
    /// Tests that default EquatableArray and one with null array are equal.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task EquatableArray_NullArrays_AreEqual()
    {
        var array1 = new EquatableArray<string>(null!);
        var array2 = default(EquatableArray<string>);

        await Assert.That(array1.Equals(array2)).IsTrue();
        await Assert.That(array1 == array2).IsTrue();
        await Assert.That(array1 != array2).IsFalse();
    }

    /// <summary>
    /// Tests that EquatableArray with null array and one with values are not equal.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task EquatableArray_NullVsNonNull_AreNotEqual()
    {
        var array1 = new EquatableArray<string>(null!);
        var array2 = new EquatableArray<string>(new[] { "a" });

        await Assert.That(array1.Equals(array2)).IsFalse();
        await Assert.That(array2.Equals(array1)).IsFalse();
    }

    /// <summary>
    /// Tests that Length property returns correct value.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task EquatableArray_Length_ReturnsCorrectValue()
    {
        var array1 = new EquatableArray<string>(new[] { "a", "b", "c" });
        var array2 = default(EquatableArray<string>);

        await Assert.That(array1.Length).IsEqualTo(3);
        await Assert.That(array2.Length).IsEqualTo(0);
    }

    /// <summary>
    /// Tests that indexer returns correct values.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task EquatableArray_Indexer_ReturnsCorrectValue()
    {
        var array = new EquatableArray<string>(new[] { "a", "b", "c" });

        await Assert.That(array[0]).IsEqualTo("a");
        await Assert.That(array[1]).IsEqualTo("b");
        await Assert.That(array[2]).IsEqualTo("c");
    }

    /// <summary>
    /// Tests that GetEnumerator returns all elements.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task EquatableArray_GetEnumerator_ReturnsAllElements()
    {
        var array = new EquatableArray<string>(new[] { "a", "b", "c" });
        var list = new List<string>(array);

        await Assert.That(list.Count).IsEqualTo(3);
        await Assert.That(list[0]).IsEqualTo("a");
        await Assert.That(list[1]).IsEqualTo("b");
        await Assert.That(list[2]).IsEqualTo("c");
    }

    /// <summary>
    /// Tests that GetEnumerator works with null array.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task EquatableArray_NullArray_GetEnumerator_ReturnsEmpty()
    {
        var array = new EquatableArray<string>(null!);
        var list = new List<string>(array);

        await Assert.That(list.Count).IsEqualTo(0);
    }

    /// <summary>
    /// Tests that explicit IEnumerable.GetEnumerator works.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task EquatableArray_ExplicitIEnumerable_GetEnumerator_Works()
    {
        var array = new EquatableArray<string>(new[] { "a", "b", "c" });
        IEnumerable enumerable = array;
        var list = enumerable.Cast<string>().ToList();

        await Assert.That(list.Count).IsEqualTo(3);
        await Assert.That(list[0]).IsEqualTo("a");
        await Assert.That(list[1]).IsEqualTo("b");
        await Assert.That(list[2]).IsEqualTo("c");
    }

    /// <summary>
    /// Tests that Equals with object parameter works correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task EquatableArray_EqualsObject_WorksCorrectly()
    {
        var array1 = new EquatableArray<string>(new[] { "a", "b", "c" });
        var array2 = new EquatableArray<string>(new[] { "a", "b", "c" });
        object array2Obj = array2;
        object nullObj = null!;
        object stringObj = "test";

        await Assert.That(array1.Equals(array2Obj)).IsTrue();
        await Assert.That(array1.Equals(nullObj)).IsFalse();
        await Assert.That(array1.Equals(stringObj)).IsFalse();
    }

    /// <summary>
    /// Tests that GetHashCode returns 0 for null array.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task EquatableArray_NullArray_GetHashCode_ReturnsZero()
    {
        var array = new EquatableArray<string>(null!);

        await Assert.That(array.GetHashCode()).IsEqualTo(0);
    }

    /// <summary>
    /// Tests that GetHashCode is consistent.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task EquatableArray_GetHashCode_IsConsistent()
    {
        var array = new EquatableArray<string>(new[] { "a", "b", "c" });

        var hash1 = array.GetHashCode();
        var hash2 = array.GetHashCode();

        await Assert.That(hash1).IsEqualTo(hash2);
    }

    /// <summary>
    /// Tests that GetHashCode is different for arrays with different values.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task EquatableArray_DifferentArrays_GetHashCode_IsDifferent()
    {
        var array1 = new EquatableArray<string>(new[] { "a", "b", "c" });
        var array2 = new EquatableArray<string>(new[] { "x", "y", "z" });

        var hash1 = array1.GetHashCode();
        var hash2 = array2.GetHashCode();

        // Different arrays should likely have different hash codes (though not guaranteed)
        await Assert.That(hash1).IsNotEqualTo(hash2);
    }

    /// <summary>
    /// Tests that empty arrays are equal.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task EquatableArray_EmptyArrays_AreEqual()
    {
        var array1 = new EquatableArray<string>(Array.Empty<string>());
        var array2 = new EquatableArray<string>(Array.Empty<string>());

        await Assert.That(array1.Equals(array2)).IsTrue();
        await Assert.That(array1 == array2).IsTrue();
        await Assert.That(array1.GetHashCode()).IsEqualTo(array2.GetHashCode());
    }

    /// <summary>
    /// Tests that GetHashCode consistency is maintained across equal arrays.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task EquatableArray_EqualArrays_GetHashCode_AreEqual()
    {
        var array1 = new EquatableArray<string>(new[] { "a", "b", "c" });
        var array2 = new EquatableArray<string>(new[] { "a", "b", "c" });

        await Assert.That(array1.GetHashCode()).IsEqualTo(array2.GetHashCode());
    }
}
