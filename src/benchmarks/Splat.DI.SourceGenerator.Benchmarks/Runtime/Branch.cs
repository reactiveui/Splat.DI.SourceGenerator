// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Splat.DI.SourceGenerator.Benchmarks.Runtime;

/// <summary>The implementation of <see cref="IBranch"/>.</summary>
/// <param name="leaf">The dependency.</param>
[DebuggerDisplay("{Name}")]
public sealed class Branch(ILeaf leaf) : IBranch
{
    /// <inheritdoc/>
    public string Name => nameof(Branch);

    /// <summary>Gets the dependency.</summary>
    public ILeaf Dependency { get; } = leaf;
}
