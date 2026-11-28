// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace ReceiptServices;

/// <summary>Assigns unique receipt numbers within a shop.</summary>
internal sealed class ReceiptNumbers : IReceiptNumbers
{
    /// <summary>The last number assigned.</summary>
    private int _last;

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Next() => Interlocked.Increment(ref _last);
}
