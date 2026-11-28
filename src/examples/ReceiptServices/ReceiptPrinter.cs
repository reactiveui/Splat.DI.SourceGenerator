// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Splat;

namespace ReceiptServices;

/// <summary>Formats a receipt with the shop name and the next receipt number.</summary>
internal sealed class ReceiptPrinter
{
    /// <summary>Initializes a new instance of the <see cref="ReceiptPrinter"/> class.</summary>
    internal ReceiptPrinter()
        : this(new Lazy<ReceiptNumbers>(static () => new()))
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ReceiptPrinter"/> class.</summary>
    /// <param name="numbers">The shared counter, created when the first receipt is printed.</param>
    [DependencyInjectionConstructor]
    internal ReceiptPrinter(Lazy<ReceiptNumbers> numbers) => Numbers = numbers;

    /// <summary>Gets or sets the shop identified on each receipt.</summary>
    [DependencyInjectionProperty]
    public Shop? Shop { get; set; }

    /// <summary>Gets the counter selected by the constructor.</summary>
    internal Lazy<ReceiptNumbers> Numbers { get; }
}
