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
}
