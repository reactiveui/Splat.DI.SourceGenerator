// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using Microsoft.CodeAnalysis;

namespace Splat.DependencyInjection.SourceGenerator.Models;

/// <summary>
/// Represents a property injection for dependency injection.
/// This is a cache-friendly POCO - contains only primitive data and Location for diagnostics.
/// </summary>
internal sealed record PropertyInjection(
    string PropertyName,
    string TypeFullName,
    Location PropertyLocation) : IEquatable<PropertyInjection>;
