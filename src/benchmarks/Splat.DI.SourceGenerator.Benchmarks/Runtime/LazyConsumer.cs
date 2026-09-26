// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Splat.DI.SourceGenerator.Benchmarks.Runtime;

/// <summary>The implementation of <see cref="ILazyConsumer"/>.</summary>
/// <param name="singleton">The lazy dependency.</param>
[DebuggerDisplay("{Name}")]
public sealed class LazyConsumer(Lazy<ISingleton> singleton) : ILazyConsumer
{
    /// <inheritdoc/>
    public string Name => nameof(LazyConsumer);

    /// <summary>Gets the dependency.</summary>
    public Lazy<ISingleton> Dependency { get; } = singleton;
}
