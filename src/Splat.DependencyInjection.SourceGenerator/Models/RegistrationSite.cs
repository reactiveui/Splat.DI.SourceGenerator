// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Splat.DependencyInjection.SourceGenerator.Models;

/// <summary>A registration and the call that made it.</summary>
/// <param name="Registration">The registration.</param>
/// <param name="Location">Where the registration call is.</param>
/// <remarks>
/// The generated code is built from <see cref="Registration"/> alone, so it is not rebuilt when only
/// <see cref="Location"/> moves. The graph diagnostics need both.
/// </remarks>
internal sealed record RegistrationSite(RegistrationInfo Registration, LocationInfo Location);
