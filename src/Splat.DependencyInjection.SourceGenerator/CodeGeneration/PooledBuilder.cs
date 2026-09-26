// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Text;

namespace Splat.DependencyInjection.SourceGenerator.CodeGeneration;

/// <summary>Hands out reusable string builders so a generation pass reuses buffers instead of allocating per file.</summary>
/// <remarks>
/// <para>
/// Each generated file used to be built in its own fresh builder, so every pass allocated a builder and its grown
/// chunk chain and threw them away. Renting from a per-thread slot lets one pass reuse the buffer, and a builder that
/// has already grown to fit the registrations file starts large enough for the next pass.
/// </para>
/// <para>
/// Thread-static because source-output callbacks can run concurrently, and because renting must not need a lock to be
/// worth doing. A builder is only reused after <see cref="ToStringAndReturn"/> hands it back, so two live rents never
/// share one. The generator writes one file at a time, so one slot per thread is enough.
/// </para>
/// </remarks>
internal static class PooledBuilder
{
    /// <summary>
    /// The largest builder worth keeping. One outsized file would otherwise pin its whole chunk chain for the life of
    /// the thread, which in a build host outlives the compilation that needed it.
    /// </summary>
    internal const int MaxRetainedCapacity = 256 * 1024;

    /// <summary>The builder this thread last handed back.</summary>
    [ThreadStatic]
    private static StringBuilder? _cached;

    /// <summary>Rents a builder, empty and ready to write.</summary>
    /// <param name="capacity">The capacity the caller expects to need.</param>
    /// <returns>An empty builder.</returns>
    internal static StringBuilder Rent(int capacity)
    {
        var cached = _cached;
        if (cached is null)
        {
            return new(capacity);
        }

        _cached = null;
        _ = cached.EnsureCapacity(capacity);
        return cached;
    }

    /// <summary>Materializes a rented builder's content and hands the builder back for reuse.</summary>
    /// <param name="builder">The rented builder, which the caller must not touch afterwards.</param>
    /// <returns>The accumulated string.</returns>
    internal static string ToStringAndReturn(StringBuilder builder)
    {
        var result = builder.ToString();
        if (builder.Capacity <= MaxRetainedCapacity)
        {
            _ = builder.Clear();
            _cached = builder;
        }

        return result;
    }
}
