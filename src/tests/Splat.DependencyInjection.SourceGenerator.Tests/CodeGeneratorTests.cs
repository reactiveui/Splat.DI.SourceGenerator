// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;

using Splat.DependencyInjection.SourceGenerator.CodeGeneration;
using Splat.DependencyInjection.SourceGenerator.Models;

namespace Splat.DependencyInjection.SourceGenerator.Tests;

/// <summary>
/// Tests for CodeGenerator.
/// </summary>
public class CodeGeneratorTests
{
    /// <summary>
    /// Verifies GenerateSetupIOCMethod output structure.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task GenerateSetupIOCMethod_BasicStructure()
    {
        var transients = ImmutableArray<TransientRegistrationInfo>.Empty;
        var lazySingletons = ImmutableArray<LazySingletonRegistrationInfo>.Empty;

        var result = CodeGenerator.GenerateSetupIOCMethod(transients, lazySingletons);

        await Assert.That(result).Contains("internal static partial class SplatRegistrations");
        await Assert.That(result).Contains("static partial void SetupIOCInternal(Splat.IDependencyResolver resolver)");
    }

    /// <summary>
    /// Verifies GenerateTransientRegistration output with various options.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task GenerateTransientRegistration_VariousScenarios()
    {
        var sb = new StringBuilder();

        // 1. Simple registration
        var reg1 = new TransientRegistrationInfo(
            InterfaceTypeFullName: "IService",
            ConcreteTypeFullName: "Service",
            ConstructorParameters: new EquatableArray<ConstructorParameter>([]),
            PropertyInjections: new EquatableArray<PropertyInjection>([]),
            ContractValue: null,
            InvocationLocation: Location.None);

        CodeGenerator.GenerateTransientRegistration(sb, reg1);
        var result1 = sb.ToString();
        await Assert.That(result1).Contains("resolver.Register<IService>(() => new Service());");

        sb.Clear();

        // 2. Complex registration with params, props and contract
        var reg2 = new TransientRegistrationInfo(
            InterfaceTypeFullName: "IService",
            ConcreteTypeFullName: "Service",
            ConstructorParameters: new EquatableArray<ConstructorParameter>([
                new ConstructorParameter("p1", "IParam1", false, null, false, null),
                new ConstructorParameter("p2", "IEnumerable<IParam2>", false, null, true, "IParam2")
            ]),
            PropertyInjections: new EquatableArray<PropertyInjection>([
                new PropertyInjection("Prop1", "IProp1", Location.None)
            ]),
            ContractValue: "\"MyContract\"",
            InvocationLocation: Location.None);

        CodeGenerator.GenerateTransientRegistration(sb, reg2);
        var result2 = sb.ToString();

        await Assert.That(result2).Contains("resolver.GetService<IParam1>(\"MyContract\")");
        await Assert.That(result2).Contains("resolver.GetServices<IParam2>(\"MyContract\")");
        await Assert.That(result2).Contains("Prop1 = resolver.GetService<IProp1>(\"MyContract\")");
        await Assert.That(result2).Contains(", \"MyContract\");");
    }

    /// <summary>
    /// Verifies GenerateLazySingletonRegistration output with various options.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task GenerateLazySingletonRegistration_VariousScenarios()
    {
        var sb = new StringBuilder();

        // 1. Complex registration
        var reg = new LazySingletonRegistrationInfo(
            InterfaceTypeFullName: "IService",
            ConcreteTypeFullName: "Service",
            ConstructorParameters: new EquatableArray<ConstructorParameter>([
                new ConstructorParameter("p1", "IParam1", false, null, false, null)
            ]),
            PropertyInjections: new EquatableArray<PropertyInjection>([]),
            ContractValue: null,
            LazyThreadSafetyMode: "global::System.Threading.LazyThreadSafetyMode.ExecutionAndPublication",
            InvocationLocation: Location.None);

        CodeGenerator.GenerateLazySingletonRegistration(sb, reg);
        var result = sb.ToString();

        await Assert.That(result).Contains("global::System.Lazy<IService> lazy = new global::System.Lazy<IService>");
        await Assert.That(result).Contains("global::System.Threading.LazyThreadSafetyMode.ExecutionAndPublication");
        await Assert.That(result).Contains("resolver.Register<global::System.Lazy<IService>>(() => lazy);");
        await Assert.That(result).Contains("resolver.Register<IService>(() => lazy.Value);");
    }

    /// <summary>
    /// Verifies GetResolutionString returns a GetService call for a simple type without contract.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task GetResolutionString_SimpleType_NoContract()
    {
        var result = CodeGenerator.GetResolutionString("global::MyApp.IService", false, null, null);

        await Assert.That(result).IsEqualTo(
            "resolver.GetService<global::MyApp.IService>() ?? throw new global::System.InvalidOperationException(\"Dependency 'global::MyApp.IService' not registered with Splat resolver.\")");
    }

    /// <summary>
    /// Verifies GetResolutionString returns a GetService call with contract parameter.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task GetResolutionString_SimpleType_WithContract()
    {
        var result = CodeGenerator.GetResolutionString("global::MyApp.IService", false, null, "\"key1\"");

        await Assert.That(result).Contains("resolver.GetService<global::MyApp.IService>(\"key1\")");
        await Assert.That(result).Contains("with contract");
    }

    /// <summary>
    /// Verifies GetResolutionString returns a GetServices call for a collection type without contract.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task GetResolutionString_CollectionType_NoContract()
    {
        var result = CodeGenerator.GetResolutionString("global::System.Collections.Generic.IEnumerable<global::MyApp.IService>", true, "global::MyApp.IService", null);

        await Assert.That(result).IsEqualTo("resolver.GetServices<global::MyApp.IService>()");
    }

    /// <summary>
    /// Verifies GetResolutionString returns a GetServices call for a collection type with contract.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task GetResolutionString_CollectionType_WithContract()
    {
        var result = CodeGenerator.GetResolutionString("global::System.Collections.Generic.IEnumerable<global::MyApp.IService>", true, "global::MyApp.IService", "\"key1\"");

        await Assert.That(result).IsEqualTo("resolver.GetServices<global::MyApp.IService>(\"key1\")");
    }

    /// <summary>
    /// Verifies GetConstructorArguments returns empty string for no parameters.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task GetConstructorArguments_Empty_ReturnsEmpty()
    {
        var result = CodeGenerator.GetConstructorArguments(new EquatableArray<ConstructorParameter>([]), null);

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    /// <summary>
    /// Verifies GetConstructorArguments produces comma-separated resolution strings for multiple parameters.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task GetConstructorArguments_MultipleParams_CommaSeparated()
    {
        var parameters = new EquatableArray<ConstructorParameter>([
            new ConstructorParameter("a", "IServiceA", false, null, false, null),
            new ConstructorParameter("b", "IServiceB", false, null, false, null)
        ]);

        var result = CodeGenerator.GetConstructorArguments(parameters, null);

        await Assert.That(result).Contains("resolver.GetService<IServiceA>()");
        await Assert.That(result).Contains("resolver.GetService<IServiceB>()");
        await Assert.That(result).Contains(", ");
    }

    /// <summary>
    /// Verifies GetConstructorArguments handles a collection parameter correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task GetConstructorArguments_CollectionParam_UsesGetServices()
    {
        var parameters = new EquatableArray<ConstructorParameter>([
            new ConstructorParameter("items", "global::System.Collections.Generic.IEnumerable<IItem>", false, null, true, "IItem")
        ]);

        var result = CodeGenerator.GetConstructorArguments(parameters, null);

        await Assert.That(result).IsEqualTo("resolver.GetServices<IItem>()");
    }

    /// <summary>
    /// Verifies GetPropertyInitializer returns empty string for no properties.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task GetPropertyInitializer_Empty_ReturnsEmpty()
    {
        var result = CodeGenerator.GetPropertyInitializer(new EquatableArray<PropertyInjection>([]), null);

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    /// <summary>
    /// Verifies GetPropertyInitializer produces correct object initializer syntax.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task GetPropertyInitializer_SingleProperty_ReturnsInitializer()
    {
        var properties = new EquatableArray<PropertyInjection>([
            new PropertyInjection("Logger", "ILogger", Location.None)
        ]);

        var result = CodeGenerator.GetPropertyInitializer(properties, null);

        await Assert.That(result).Contains("Logger = resolver.GetService<ILogger>()");
        await Assert.That(result).StartsWith(" { ");
        await Assert.That(result).EndsWith(" }");
    }

    /// <summary>
    /// Verifies GetPropertyInitializer uses contract value when provided.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task GetPropertyInitializer_WithContract_PassesContract()
    {
        var properties = new EquatableArray<PropertyInjection>([
            new PropertyInjection("Logger", "ILogger", Location.None)
        ]);

        var result = CodeGenerator.GetPropertyInitializer(properties, "\"named\"");

        await Assert.That(result).Contains("resolver.GetService<ILogger>(\"named\")");
    }

    /// <summary>
    /// Verifies GetPropertyInitializer produces comma-separated initializers for multiple properties.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task GetPropertyInitializer_MultipleProperties_CommaSeparated()
    {
        var properties = new EquatableArray<PropertyInjection>([
            new PropertyInjection("Logger", "ILogger", Location.None),
            new PropertyInjection("Cache", "ICache", Location.None)
        ]);

        var result = CodeGenerator.GetPropertyInitializer(properties, null);

        await Assert.That(result).Contains("Logger = ");
        await Assert.That(result).Contains("Cache = ");
        await Assert.That(result).Contains(", ");
    }
}
