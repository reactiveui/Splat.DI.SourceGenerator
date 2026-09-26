// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Splat.DI.SourceGenerator.Benchmarks.Runtime;

/// <summary>The implementation of <see cref="ITree"/>.</summary>
/// <param name="branch">The constructor dependency.</param>
/// <param name="leaves">The collection dependency.</param>
[DebuggerDisplay("{Name}")]
public sealed class Tree(IBranch branch, IEnumerable<ILeaf> leaves) : ITree
{
    /// <inheritdoc/>
    public string Name => nameof(Tree);

    /// <summary>Gets the constructor dependency.</summary>
    public IBranch Branch { get; } = branch;

    /// <summary>Gets the collection dependency.</summary>
    public IEnumerable<ILeaf> Leaves { get; } = leaves;

    /// <summary>Gets or sets the injected property.</summary>
    [DependencyInjectionProperty]
    public ILeaf? Injected { get; set; }
}
