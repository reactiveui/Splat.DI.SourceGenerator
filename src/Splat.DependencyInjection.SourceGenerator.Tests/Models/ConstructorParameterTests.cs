// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Splat.DependencyInjection.SourceGenerator.Models;

namespace Splat.DependencyInjection.SourceGenerator.Tests.Models;

/// <summary>
/// Tests for the ConstructorParameter model class.
/// Ensures proper equality and record behavior for constructor parameter metadata.
/// </summary>
public class ConstructorParameterTests
{
    /// <summary>
    /// Tests that two ConstructorParameter instances with the same values are equal.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task ConstructorParameter_WithSameValues_AreEqual()
    {
        var param1 = new ConstructorParameter("service", "global::Test.IService", false, null, false, null);
        var param2 = new ConstructorParameter("service", "global::Test.IService", false, null, false, null);

        await Assert.That(param1).IsEqualTo(param2);
        await Assert.That(param1.GetHashCode()).IsEqualTo(param2.GetHashCode());
    }

    /// <summary>
    /// Tests that two ConstructorParameter instances with different parameter names are not equal.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task ConstructorParameter_WithDifferentParameterName_AreNotEqual()
    {
        var param1 = new ConstructorParameter("service1", "global::Test.IService", false, null, false, null);
        var param2 = new ConstructorParameter("service2", "global::Test.IService", false, null, false, null);

        await Assert.That(param1).IsNotEqualTo(param2);
    }

    /// <summary>
    /// Tests that two ConstructorParameter instances with different type names are not equal.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task ConstructorParameter_WithDifferentTypeName_AreNotEqual()
    {
        var param1 = new ConstructorParameter("service", "global::Test.IService1", false, null, false, null);
        var param2 = new ConstructorParameter("service", "global::Test.IService2", false, null, false, null);

        await Assert.That(param1).IsNotEqualTo(param2);
    }

    /// <summary>
    /// Tests that two ConstructorParameter instances with different IsLazy values are not equal.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task ConstructorParameter_WithDifferentIsLazy_AreNotEqual()
    {
        var param1 = new ConstructorParameter("service", "global::Test.IService", false, null, false, null);
        var param2 = new ConstructorParameter("service", "global::Test.IService", true, "global::Test.IService", false, null);

        await Assert.That(param1).IsNotEqualTo(param2);
    }

    /// <summary>
    /// Tests that two ConstructorParameter instances with different LazyInnerType values are not equal.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task ConstructorParameter_WithDifferentLazyInnerType_AreNotEqual()
    {
        var param1 = new ConstructorParameter("service", "global::System.Lazy<global::Test.IService1>", true, "global::Test.IService1", false, null);
        var param2 = new ConstructorParameter("service", "global::System.Lazy<global::Test.IService2>", true, "global::Test.IService2", false, null);

        await Assert.That(param1).IsNotEqualTo(param2);
    }

    /// <summary>
    /// Tests that ConstructorParameter properties can be accessed correctly for non-lazy parameters.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task ConstructorParameter_NonLazy_Properties_AreAccessible()
    {
        var param = new ConstructorParameter("myParam", "global::Test.IMyType", false, null, false, null);

        await Assert.That(param.ParameterName).IsEqualTo("myParam");
        await Assert.That(param.TypeFullName).IsEqualTo("global::Test.IMyType");
        await Assert.That(param.IsLazy).IsFalse();
        await Assert.That(param.LazyInnerType).IsNull();
    }

    /// <summary>
    /// Tests that ConstructorParameter properties can be accessed correctly for lazy parameters.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task ConstructorParameter_Lazy_Properties_AreAccessible()
    {
        var param = new ConstructorParameter("myParam", "global::System.Lazy<global::Test.IMyType>", true, "global::Test.IMyType", false, null);

        await Assert.That(param.ParameterName).IsEqualTo("myParam");
        await Assert.That(param.TypeFullName).IsEqualTo("global::System.Lazy<global::Test.IMyType>");
        await Assert.That(param.IsLazy).IsTrue();
        await Assert.That(param.LazyInnerType).IsEqualTo("global::Test.IMyType");
    }
}
