// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Splat.DependencyInjection.SourceGenerator.Models;

/// <summary>How a registration creates its instances.</summary>
internal enum RegistrationKind
{
    /// <summary>A new instance per resolution, from <c>Register</c>.</summary>
    Transient = 0,

    /// <summary>One instance, created on first resolution, from <c>RegisterLazySingleton</c>.</summary>
    LazySingleton = 1,
}
