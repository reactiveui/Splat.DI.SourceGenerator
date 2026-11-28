// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Splat;

namespace ReceiptServices;

/// <summary>Configures and resolves receipt services with generated registrations.</summary>
internal static class Program
{
    /// <summary>Identifies receipts issued in the warehouse.</summary>
    internal const string Warehouse = "warehouse";

    /// <summary>Identifies receipts issued by the online shop.</summary>
    internal const string Online = "online";

    /// <summary>Checks constructor and property injection, named services and service lifetimes.</summary>
    /// <exception cref="InvalidOperationException">The generated registrations do not satisfy the receipt scenario.</exception>
    internal static void Main()
    {
        var shop = new Shop("Market");
        SplatRegistrations.RegisterConstant(shop);
        SplatRegistrations.RegisterConstant(shop, "head-office");
        SplatRegistrations.Register<ReceiptPrinter>();
        SplatRegistrations.Register<ReceiptPrinter>("counter");
        SplatRegistrations.RegisterLazySingleton<ReceiptNumbers>();
        SplatRegistrations.RegisterLazySingleton<ReceiptNumbers>("returns");
        SplatRegistrations.Register<IReceiptNumbers, ReceiptNumbers>();
        SplatRegistrations.Register<IReceiptNumbers, ReceiptNumbers>("batch");
        SplatRegistrations.RegisterLazySingleton<IReceiptNumbers, ReceiptNumbers>(Warehouse);
        SplatRegistrations.RegisterLazySingleton<IReceiptNumbers, ReceiptNumbers>(Online, LazyThreadSafetyMode.ExecutionAndPublication);
        SplatRegistrations.SetupIOC();
        CheckShop(shop);
        CheckBranch();
        CheckChannels();
    }

    /// <summary>Checks the injected shop and shared counter on two separate printers.</summary>
    /// <param name="shop">The constant registered with the current resolver.</param>
    /// <exception cref="InvalidOperationException">The printers do not receive the expected shop or deferred counter.</exception>
    private static void CheckShop(Shop shop)
    {
        var first = AppLocator.Current.GetService<ReceiptPrinter>();
        var second = AppLocator.Current.GetService<ReceiptPrinter>("counter");
        if (first is null || second is null || first == second || first.Shop != shop
            || AppLocator.Current.GetService<Shop>("head-office") != shop || first.Numbers.IsValueCreated)
        {
            throw new InvalidOperationException("Each receipt needs its own printer with the registered shop and a deferred counter.");
        }

        CheckNumbers(first, second);
    }

    /// <summary>Checks the shared sales counter and the named returns counter.</summary>
    /// <param name="first">The first sales printer.</param>
    /// <param name="second">The second sales printer.</param>
    /// <exception cref="InvalidOperationException">The named and default counters do not have independent sequences.</exception>
    private static void CheckNumbers(ReceiptPrinter first, ReceiptPrinter second)
    {
        const int SecondReceipt = 2;
        if (first.Numbers.Value.Next() != 1 || second.Numbers.Value.Next() != SecondReceipt
            || AppLocator.Current.GetService<ReceiptNumbers>("returns")?.Next() != 1)
        {
            throw new InvalidOperationException("The printers should share one counter and the returns desk should have its own.");
        }
    }

    /// <summary>Installs the same generated registrations in an independent resolver.</summary>
    /// <exception cref="InvalidOperationException">The branch uses the wrong shop or receipt sequence.</exception>
    private static void CheckBranch()
    {
        using var separate = new ModernDependencyResolver();
        separate.RegisterConstant(new Shop("Branch"));
        SplatRegistrations.SetupIOC(separate);
        var branch = separate.GetService<ReceiptPrinter>();
        if (branch?.Shop?.Name != "Branch" || branch.Numbers.Value.Next() != 1)
        {
            throw new InvalidOperationException("The branch resolver should supply its own shop and receipt numbers.");
        }
    }

    /// <summary>Checks fresh batch sequences and shared sequences for named channels.</summary>
    /// <exception cref="InvalidOperationException">The registrations returned a counter with the wrong lifetime.</exception>
    private static void CheckChannels()
    {
        const int SecondReceipt = 2;
        foreach (var channel in new[] { null, "batch", Warehouse, Online })
        {
            var first = AppLocator.Current.GetService<IReceiptNumbers>(channel);
            var second = AppLocator.Current.GetService<IReceiptNumbers>(channel);
            var expected = channel is Warehouse or Online ? SecondReceipt : 1;
            if (first?.Next() != 1 || second?.Next() != expected)
            {
                throw new InvalidOperationException("Only the shared sales channels should continue the previous receipt sequence.");
            }
        }
    }
}
