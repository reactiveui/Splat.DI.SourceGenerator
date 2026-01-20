// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Splat.DependencyInjection.SourceGenerator.Models;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Splat.DependencyInjection.SourceGenerator.Tests.Models;

/// <summary>
/// Tests for the PropertyInjection model class.
/// Ensures proper equality and record behavior for property injection metadata.
/// </summary>
public class PropertyInjectionTests
{
    /// <summary>
    /// Tests that two PropertyInjection instances with the same values are equal.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task PropertyInjection_WithSameValues_AreEqual()
    {
        var location = Location.None;
        var prop1 = new PropertyInjection("Service", "global::Test.IService", location);
        var prop2 = new PropertyInjection("Service", "global::Test.IService", location);

        await Assert.That(prop1).IsEqualTo(prop2);
        await Assert.That(prop1.GetHashCode()).IsEqualTo(prop2.GetHashCode());
    }

    /// <summary>
    /// Tests that two PropertyInjection instances with different property names are not equal.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task PropertyInjection_WithDifferentPropertyName_AreNotEqual()
    {
        var location = Location.None;
        var prop1 = new PropertyInjection("Service1", "global::Test.IService", location);
        var prop2 = new PropertyInjection("Service2", "global::Test.IService", location);

        await Assert.That(prop1).IsNotEqualTo(prop2);
    }

    /// <summary>
    /// Tests that two PropertyInjection instances with different type names are not equal.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task PropertyInjection_WithDifferentTypeName_AreNotEqual()
    {
        var location = Location.None;
        var prop1 = new PropertyInjection("Service", "global::Test.IService1", location);
        var prop2 = new PropertyInjection("Service", "global::Test.IService2", location);

        await Assert.That(prop1).IsNotEqualTo(prop2);
    }

    /// <summary>
    /// Tests that PropertyInjection properties can be accessed correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task PropertyInjection_Properties_AreAccessible()
    {
        var location = Location.None;
        var prop = new PropertyInjection("MyProperty", "global::Test.IMyType", location);

        await Assert.That(prop.PropertyName).IsEqualTo("MyProperty");
        await Assert.That(prop.TypeFullName).IsEqualTo("global::Test.IMyType");
        await Assert.That(prop.PropertyLocation).IsEqualTo(location);
    }
}
