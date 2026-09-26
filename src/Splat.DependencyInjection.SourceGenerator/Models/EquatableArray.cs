// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;

namespace Splat.DependencyInjection.SourceGenerator.Models;

/// <summary>A value-equatable wrapper around an array, so a record holding one compares by content.</summary>
/// <typeparam name="T">The element type, which must be equatable.</typeparam>
/// <remarks>
/// <para>
/// The hash is computed once at construction: the pipeline hashes a model each time it compares a run's output with
/// the last, and the array never changes.
/// </para>
/// <para>
/// It is read by index, which allocates nothing. It deliberately does not implement
/// <see cref="System.Collections.Generic.IEnumerable{T}"/>, whose enumerator a caller would allocate on every loop.
/// </para>
/// </remarks>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>
    where T : IEquatable<T>
{
    /// <summary>The multiplier used in the deterministic hash-combine loop.</summary>
    private const int HashMultiplier = 31;

    /// <summary>The seed of the deterministic hash-combine loop.</summary>
    private const int HashSeed = 17;

    /// <summary>The underlying array, or null if default-constructed.</summary>
    private readonly T[]? _array;

    /// <summary>The hash code, computed once at construction.</summary>
    private readonly int _hashCode;

    /// <summary>Initializes a new instance of the <see cref="EquatableArray{T}"/> struct.</summary>
    /// <param name="array">The array to wrap, which the caller must not change afterwards.</param>
    public EquatableArray(T[] array)
    {
        _array = array;
        _hashCode = ComputeHashCode(array);
    }

    /// <summary>Gets an empty array.</summary>
    internal static EquatableArray<T> Empty { get; } = new([]);

    /// <summary>Gets the number of elements.</summary>
    internal int Length => _array?.Length ?? 0;

    /// <summary>Gets the element at the specified index.</summary>
    /// <param name="index">The zero-based index of the element to get.</param>
    internal T this[int index] => _array![index];

    /// <summary>Determines whether two arrays are equal.</summary>
    /// <param name="left">The first array to compare.</param>
    /// <param name="right">The second array to compare.</param>
    /// <returns><see langword="true"/> if the arrays are equal; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

    /// <summary>Determines whether two arrays are not equal.</summary>
    /// <param name="left">The first array to compare.</param>
    /// <param name="right">The second array to compare.</param>
    /// <returns><see langword="true"/> if the arrays are not equal; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);

    /// <summary>Indicates whether this array has the same elements as another.</summary>
    /// <param name="other">The array to compare with this one.</param>
    /// <returns><see langword="true"/> if the arrays are equal; otherwise, <see langword="false"/>.</returns>
    public bool Equals(EquatableArray<T> other)
    {
        var array = _array ?? [];
        var otherArray = other._array ?? [];
        if (ReferenceEquals(array, otherArray))
        {
            return true;
        }

        if (array.Length != otherArray.Length || _hashCode != other._hashCode)
        {
            return false;
        }

        for (var i = 0; i < array.Length; i++)
        {
            if (!array[i].Equals(otherArray[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Determines whether the specified object is an equal array.</summary>
    /// <param name="obj">The object to compare with this array.</param>
    /// <returns><see langword="true"/> if the object is an equal array; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    /// <summary>Returns the hash code computed at construction.</summary>
    /// <returns>A hash code for the array.</returns>
    public override int GetHashCode() => _hashCode;

    /// <summary>Computes a deterministic hash code for an array.</summary>
    /// <param name="array">The array to hash.</param>
    /// <returns>A hash code for the array.</returns>
    private static int ComputeHashCode(T[] array)
    {
        unchecked
        {
            var hash = HashSeed;
            for (var i = 0; i < array.Length; i++)
            {
                hash = (hash * HashMultiplier) + array[i].GetHashCode();
            }

            return hash;
        }
    }
}
