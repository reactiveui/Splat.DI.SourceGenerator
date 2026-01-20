// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Splat.DependencyInjection.SourceGenerator.Models;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Splat.DependencyInjection.SourceGenerator.Tests;

/// <summary>
/// Tests for Generator code generation methods.
/// Tests the internal code generation helpers directly for better coverage.
/// </summary>
public class GeneratorCodeGenerationTests
{
    /// <summary>
    /// Tests that GenerateTransientRegistration generates correct code for a simple registration with no dependencies.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task GenerateTransientRegistration_NoDependencies_GeneratesCorrectCode()
    {
        var registration = new TransientRegistrationInfo(
            InterfaceTypeFullName: "global::Test.IService",
            ConcreteTypeFullName: "global::Test.Service",
            ConstructorParameters: default,
            PropertyInjections: default,
            ContractValue: null,
            InvocationLocation: Location.None);

        var sb = new StringBuilder();
        Generator.GenerateTransientRegistration(sb, registration);

        var result = sb.ToString();
        await Assert.That(result).Contains("resolver.Register<global::Test.IService>");
        await Assert.That(result).Contains("new global::Test.Service()");
    }

    /// <summary>
    /// Tests that GenerateTransientRegistration generates correct code with constructor parameters.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task GenerateTransientRegistration_WithConstructorParameters_GeneratesCorrectCode()
    {
        var registration = new TransientRegistrationInfo(
            InterfaceTypeFullName: "global::Test.IService",
            ConcreteTypeFullName: "global::Test.Service",
            ConstructorParameters: new EquatableArray<ConstructorParameter>(new[]
            {
                new ConstructorParameter("dep1", "global::Test.IDep1", false, null),
                new ConstructorParameter("dep2", "global::Test.IDep2", false, null)
            }),
            PropertyInjections: default,
            ContractValue: null,
            InvocationLocation: Location.None);

        var sb = new StringBuilder();
        Generator.GenerateTransientRegistration(sb, registration);

        var result = sb.ToString();
        await Assert.That(result).Contains("(global::Test.IDep1)resolver.GetService(typeof(global::Test.IDep1))");
        await Assert.That(result).Contains("(global::Test.IDep2)resolver.GetService(typeof(global::Test.IDep2))");
    }

    /// <summary>
    /// Tests that GenerateTransientRegistration generates correct code with property injections.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task GenerateTransientRegistration_WithPropertyInjections_GeneratesCorrectCode()
    {
        var registration = new TransientRegistrationInfo(
            InterfaceTypeFullName: "global::Test.IService",
            ConcreteTypeFullName: "global::Test.Service",
            ConstructorParameters: default,
            PropertyInjections: new EquatableArray<PropertyInjection>(new[]
            {
                new PropertyInjection("Prop1", "global::Test.IProp1", Location.None),
                new PropertyInjection("Prop2", "global::Test.IProp2", Location.None)
            }),
            ContractValue: null,
            InvocationLocation: Location.None);

        var sb = new StringBuilder();
        Generator.GenerateTransientRegistration(sb, registration);

        var result = sb.ToString();
        await Assert.That(result).Contains("{ Prop1 = (global::Test.IProp1)resolver.GetService(typeof(global::Test.IProp1)), Prop2 = (global::Test.IProp2)resolver.GetService(typeof(global::Test.IProp2)) }");
    }

    /// <summary>
    /// Tests that GenerateTransientRegistration generates correct code with a contract value.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task GenerateTransientRegistration_WithContract_GeneratesCorrectCode()
    {
        var registration = new TransientRegistrationInfo(
            InterfaceTypeFullName: "global::Test.IService",
            ConcreteTypeFullName: "global::Test.Service",
            ConstructorParameters: default,
            PropertyInjections: default,
            ContractValue: "\"MyContract\"",
            InvocationLocation: Location.None);

        var sb = new StringBuilder();
        Generator.GenerateTransientRegistration(sb, registration);

        var result = sb.ToString();
        await Assert.That(result).Contains(", \"MyContract\"");
    }

    /// <summary>
    /// Tests that GenerateLazySingletonRegistration generates correct code for a simple lazy singleton.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task GenerateLazySingletonRegistration_Simple_GeneratesCorrectCode()
    {
        var registration = new LazySingletonRegistrationInfo(
            InterfaceTypeFullName: "global::Test.IService",
            ConcreteTypeFullName: "global::Test.Service",
            ConstructorParameters: default,
            PropertyInjections: default,
            ContractValue: null,
            LazyThreadSafetyMode: null,
            InvocationLocation: Location.None);

        var sb = new StringBuilder();
        Generator.GenerateLazySingletonRegistration(sb, registration);

        var result = sb.ToString();
        await Assert.That(result).Contains("global::System.Lazy<global::Test.IService> lazy");
        await Assert.That(result).Contains("new global::System.Lazy<global::Test.IService>");
        await Assert.That(result).Contains("resolver.Register<global::System.Lazy<global::Test.IService>>(() => lazy");
        await Assert.That(result).Contains("resolver.Register<global::Test.IService>(() => lazy.Value");
    }

    /// <summary>
    /// Tests that GenerateLazySingletonRegistration generates correct code with thread safety mode.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task GenerateLazySingletonRegistration_WithThreadSafetyMode_GeneratesCorrectCode()
    {
        var registration = new LazySingletonRegistrationInfo(
            InterfaceTypeFullName: "global::Test.IService",
            ConcreteTypeFullName: "global::Test.Service",
            ConstructorParameters: default,
            PropertyInjections: default,
            ContractValue: null,
            LazyThreadSafetyMode: "global::System.Threading.LazyThreadSafetyMode.ExecutionAndPublication",
            InvocationLocation: Location.None);

        var sb = new StringBuilder();
        Generator.GenerateLazySingletonRegistration(sb, registration);

        var result = sb.ToString();
        await Assert.That(result).Contains(", global::System.Threading.LazyThreadSafetyMode.ExecutionAndPublication");
    }

    /// <summary>
    /// Tests that GenerateSetupIOCMethod generates the full SetupIOCInternal method.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task GenerateSetupIOCMethod_Empty_GeneratesCorrectStructure()
    {
        var transients = ImmutableArray<TransientRegistrationInfo>.Empty;
        var lazySingletons = ImmutableArray<LazySingletonRegistrationInfo>.Empty;

        var result = Generator.GenerateSetupIOCMethod(transients, lazySingletons);

        await Assert.That(result).Contains("// <auto-generated/>");
        await Assert.That(result).Contains("#nullable enable annotations");
        await Assert.That(result).Contains("namespace Splat");
        await Assert.That(result).Contains("internal static partial class SplatRegistrations");
        await Assert.That(result).Contains("static partial void SetupIOCInternal(Splat.IDependencyResolver resolver)");
    }

    /// <summary>
    /// Tests that GenerateSetupIOCMethod includes GeneratedCodeAttribute with version.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task GenerateSetupIOCMethod_IncludesGeneratedCodeAttribute()
    {
        var transients = ImmutableArray<TransientRegistrationInfo>.Empty;
        var lazySingletons = ImmutableArray<LazySingletonRegistrationInfo>.Empty;

        var result = Generator.GenerateSetupIOCMethod(transients, lazySingletons);

        await Assert.That(result).Contains("[global::System.CodeDom.Compiler.GeneratedCodeAttribute(");
        await Assert.That(result).Contains("Splat.DependencyInjection.SourceGenerator");
    }

    /// <summary>
    /// Tests that GenerateSetupIOCMethod generates registrations for transients and lazy singletons.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task GenerateSetupIOCMethod_WithRegistrations_GeneratesAllRegistrations()
    {
        var transients = ImmutableArray.Create(
            new TransientRegistrationInfo(
                "global::Test.IService1",
                "global::Test.Service1",
                default,
                default,
                null,
                Location.None));

        var lazySingletons = ImmutableArray.Create(
            new LazySingletonRegistrationInfo(
                "global::Test.IService2",
                "global::Test.Service2",
                default,
                default,
                null,
                null,
                Location.None));

        var result = Generator.GenerateSetupIOCMethod(transients, lazySingletons);

        await Assert.That(result).Contains("resolver.Register<global::Test.IService1>");
        await Assert.That(result).Contains("new global::Test.Service1()");
        await Assert.That(result).Contains("global::System.Lazy<global::Test.IService2>");
        await Assert.That(result).Contains("new global::Test.Service2()");
    }
}
