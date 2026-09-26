// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Splat.DI.SourceGenerator.Benchmarks.Runtime;

/// <summary>A service registered as a lazy singleton.</summary>
public interface ISingleton
{
    /// <summary>Gets the name of the implementation.</summary>
    string Name { get; }
}
