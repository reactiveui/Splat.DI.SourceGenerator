// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Splat.DI.SourceGenerator.Benchmarks.Runtime;

/// <summary>Makes the registrations the runtime benchmarks resolve.</summary>
internal static class RuntimeRegistrations
{
    /// <summary>The contract <see cref="IKeyed"/> and its dependency are registered under.</summary>
    internal const string Contract = "keyed";

    /// <summary>Declares the registrations; the generator reads the calls, which do nothing at run time.</summary>
    internal static void Declare()
    {
        SplatRegistrations.Register<ILeaf, Leaf>();
        SplatRegistrations.Register<ILeaf, Leaf>(Contract);
        SplatRegistrations.Register<IBranch, Branch>();
        SplatRegistrations.Register<ITree, Tree>();
        SplatRegistrations.RegisterLazySingleton<ISingleton, Singleton>();
        SplatRegistrations.Register<ILazyConsumer, LazyConsumer>();
        SplatRegistrations.Register<IKeyed, Keyed>(Contract);
    }
}
