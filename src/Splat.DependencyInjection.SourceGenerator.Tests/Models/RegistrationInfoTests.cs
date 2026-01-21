// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

using Splat.DependencyInjection.SourceGenerator.Models;

namespace Splat.DependencyInjection.SourceGenerator.Tests.Models;

/// <summary>
/// Tests for the RegistrationInfo base record and its derived types.
/// Ensures proper equality and record behavior for registration metadata.
/// </summary>
public class RegistrationInfoTests
{
    /// <summary>
    /// Tests that two TransientRegistrationInfo instances with the same values are equal.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task TransientRegistrationInfo_WithSameValues_AreEqual()
    {
        var location = Location.None;
        var constructorParams = new EquatableArray<ConstructorParameter>(new[]
        {
            new ConstructorParameter("param1", "global::System.String", false, null, false, null)
        });
        var propertyInjections = new EquatableArray<PropertyInjection>(new[]
        {
            new PropertyInjection("Prop1", "global::System.Int32", location)
        });

        var reg1 = new TransientRegistrationInfo(
            "global::Test.IService",
            "global::Test.Service",
            constructorParams,
            propertyInjections,
            "contract1",
            location);

        var reg2 = new TransientRegistrationInfo(
            "global::Test.IService",
            "global::Test.Service",
            constructorParams,
            propertyInjections,
            "contract1",
            location);

        await Assert.That(reg1).IsEqualTo(reg2);
        await Assert.That(reg1.GetHashCode()).IsEqualTo(reg2.GetHashCode());
    }

    /// <summary>
    /// Tests that two TransientRegistrationInfo instances with different interface types are not equal.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task TransientRegistrationInfo_WithDifferentInterfaceType_AreNotEqual()
    {
        var location = Location.None;
        var constructorParams = default(EquatableArray<ConstructorParameter>);
        var propertyInjections = default(EquatableArray<PropertyInjection>);

        var reg1 = new TransientRegistrationInfo(
            "global::Test.IService1",
            "global::Test.Service",
            constructorParams,
            propertyInjections,
            null,
            location);

        var reg2 = new TransientRegistrationInfo(
            "global::Test.IService2",
            "global::Test.Service",
            constructorParams,
            propertyInjections,
            null,
            location);

        await Assert.That(reg1).IsNotEqualTo(reg2);
    }

    /// <summary>
    /// Tests that two LazySingletonRegistrationInfo instances with the same values are equal.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task LazySingletonRegistrationInfo_WithSameValues_AreEqual()
    {
        var location = Location.None;
        var constructorParams = default(EquatableArray<ConstructorParameter>);
        var propertyInjections = default(EquatableArray<PropertyInjection>);

        var reg1 = new LazySingletonRegistrationInfo(
            "global::Test.IService",
            "global::Test.Service",
            constructorParams,
            propertyInjections,
            "contract1",
            "PublicationOnly",
            location);

        var reg2 = new LazySingletonRegistrationInfo(
            "global::Test.IService",
            "global::Test.Service",
            constructorParams,
            propertyInjections,
            "contract1",
            "PublicationOnly",
            location);

        await Assert.That(reg1).IsEqualTo(reg2);
        await Assert.That(reg1.GetHashCode()).IsEqualTo(reg2.GetHashCode());
    }

    /// <summary>
    /// Tests that two LazySingletonRegistrationInfo instances with different thread safety modes are not equal.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task LazySingletonRegistrationInfo_WithDifferentThreadSafetyMode_AreNotEqual()
    {
        var location = Location.None;
        var constructorParams = default(EquatableArray<ConstructorParameter>);
        var propertyInjections = default(EquatableArray<PropertyInjection>);

        var reg1 = new LazySingletonRegistrationInfo(
            "global::Test.IService",
            "global::Test.Service",
            constructorParams,
            propertyInjections,
            null,
            "PublicationOnly",
            location);

        var reg2 = new LazySingletonRegistrationInfo(
            "global::Test.IService",
            "global::Test.Service",
            constructorParams,
            propertyInjections,
            null,
            "ExecutionAndPublication",
            location);

        await Assert.That(reg1).IsNotEqualTo(reg2);
    }

    /// <summary>
    /// Tests that TransientRegistrationInfo properties can be accessed correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task TransientRegistrationInfo_Properties_AreAccessible()
    {
        var location = Location.None;
        var constructorParams = new EquatableArray<ConstructorParameter>(new[]
        {
            new ConstructorParameter("param1", "global::System.String", false, null, false, null)
        });
        var propertyInjections = new EquatableArray<PropertyInjection>(new[]
        {
            new PropertyInjection("Prop1", "global::System.Int32", location)
        });

        var reg = new TransientRegistrationInfo(
            "global::Test.IMyService",
            "global::Test.MyService",
            constructorParams,
            propertyInjections,
            "myContract",
            location);

        await Assert.That(reg.InterfaceTypeFullName).IsEqualTo("global::Test.IMyService");
        await Assert.That(reg.ConcreteTypeFullName).IsEqualTo("global::Test.MyService");
        await Assert.That(reg.ConstructorParameters.Length).IsEqualTo(1);
        await Assert.That(reg.PropertyInjections.Length).IsEqualTo(1);
        await Assert.That(reg.ContractValue).IsEqualTo("myContract");
        await Assert.That(reg.InvocationLocation).IsEqualTo(location);
    }

    /// <summary>
    /// Tests that LazySingletonRegistrationInfo properties can be accessed correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task LazySingletonRegistrationInfo_Properties_AreAccessible()
    {
        var location = Location.None;
        var constructorParams = default(EquatableArray<ConstructorParameter>);
        var propertyInjections = default(EquatableArray<PropertyInjection>);

        var reg = new LazySingletonRegistrationInfo(
            "global::Test.IMySingleton",
            "global::Test.MySingleton",
            constructorParams,
            propertyInjections,
            "mySingletonContract",
            "PublicationOnly",
            location);

        await Assert.That(reg.InterfaceTypeFullName).IsEqualTo("global::Test.IMySingleton");
        await Assert.That(reg.ConcreteTypeFullName).IsEqualTo("global::Test.MySingleton");
        await Assert.That(reg.ContractValue).IsEqualTo("mySingletonContract");
        await Assert.That(reg.LazyThreadSafetyMode).IsEqualTo("PublicationOnly");
        await Assert.That(reg.InvocationLocation).IsEqualTo(location);
    }

    /// <summary>
    /// Tests that registrations with null contract values are handled correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task RegistrationInfo_WithNullContract_AreEqual()
    {
        var location = Location.None;
        var constructorParams = default(EquatableArray<ConstructorParameter>);
        var propertyInjections = default(EquatableArray<PropertyInjection>);

        var reg1 = new TransientRegistrationInfo(
            "global::Test.IService",
            "global::Test.Service",
            constructorParams,
            propertyInjections,
            null,
            location);

        var reg2 = new TransientRegistrationInfo(
            "global::Test.IService",
            "global::Test.Service",
            constructorParams,
            propertyInjections,
            null,
            location);

        await Assert.That(reg1).IsEqualTo(reg2);
    }

    /// <summary>
    /// Tests that TransientRegistrationInfo and LazySingletonRegistrationInfo with same base properties are not equal.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task DifferentRegistrationTypes_AreNotEqual()
    {
        var location = Location.None;
        var constructorParams = default(EquatableArray<ConstructorParameter>);
        var propertyInjections = default(EquatableArray<PropertyInjection>);

        RegistrationInfo reg1 = new TransientRegistrationInfo(
            "global::Test.IService",
            "global::Test.Service",
            constructorParams,
            propertyInjections,
            null,
            location);

        RegistrationInfo reg2 = new LazySingletonRegistrationInfo(
            "global::Test.IService",
            "global::Test.Service",
            constructorParams,
            propertyInjections,
            null,
            null,
            location);

        await Assert.That(reg1).IsNotEqualTo(reg2);
    }
}
