// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ReceiptServices;

/// <summary>Supplies receipt numbers for one sales channel.</summary>
internal interface IReceiptNumbers
{
    /// <summary>Assigns a receipt number.</summary>
    /// <returns>The next number in the channel's sequence.</returns>
    int Next();
}
