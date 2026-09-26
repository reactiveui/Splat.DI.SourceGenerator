// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Splat.DependencyInjection.SourceGenerator.Models;

/// <summary>A property the generated factory sets in its object initializer.</summary>
/// <param name="PropertyName">The name of the property.</param>
/// <param name="TypeFullName">The fully qualified type of the property.</param>
/// <remarks>A value type, so a registration's properties are one array rather than one object each.</remarks>
internal readonly record struct PropertyInjection(string PropertyName, string TypeFullName);
