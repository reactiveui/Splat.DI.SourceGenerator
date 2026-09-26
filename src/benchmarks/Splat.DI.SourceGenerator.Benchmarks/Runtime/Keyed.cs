// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Splat.DI.SourceGenerator.Benchmarks.Runtime;

/// <summary>The implementation of <see cref="IKeyed"/>.</summary>
/// <param name="leaf">The dependency, resolved under the same contract.</param>
[DebuggerDisplay("{Name}")]
public sealed class Keyed(ILeaf leaf) : IKeyed
{
    /// <inheritdoc/>
    public string Name => nameof(Keyed);

    /// <summary>Gets the dependency.</summary>
    public ILeaf Dependency { get; } = leaf;
}
