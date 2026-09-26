// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Splat.DependencyInjection.SourceGenerator.Models;

/// <summary>How a constructor parameter is resolved.</summary>
internal enum DependencyKind
{
    /// <summary>One registered service, which must exist.</summary>
    Service = 0,

    /// <summary>A <see cref="System.Lazy{T}"/> of a service, itself registered by <c>RegisterLazySingleton</c>.</summary>
    Lazy = 1,

    /// <summary>An <see cref="System.Collections.Generic.IEnumerable{T}"/> of every registration of a service.</summary>
    Collection = 2,
}
