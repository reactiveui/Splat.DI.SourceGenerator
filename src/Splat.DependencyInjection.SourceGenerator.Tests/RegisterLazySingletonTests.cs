// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using TUnit.Core;

namespace Splat.DependencyInjection.SourceGenerator.Tests;

/// <summary>
/// Tests for the RegisterLazySingleton method source generation.
/// Validates lazy singleton registration scenarios with different thread safety modes and injection patterns.
/// </summary>
[InheritsTests]
public class RegisterLazySingletonTests() : TestBase("RegisterLazySingleton")
{
    /// <summary>
    /// Validates that lazy singleton registration works with multiple property injection and different thread safety modes.
    /// </summary>
    /// <param name="mode">The lazy thread safety mode to test (PublicationOnly, ExecutionAndPublication, or None).</param>
    /// <param name="contract">The contract name parameter to test (empty, "Test1", or "Test2").</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [Arguments(LazyThreadSafetyMode.PublicationOnly, "")]
    [Arguments(LazyThreadSafetyMode.PublicationOnly, "Test1")]
    [Arguments(LazyThreadSafetyMode.PublicationOnly, "Test2")]
    [Arguments(LazyThreadSafetyMode.ExecutionAndPublication, "")]
    [Arguments(LazyThreadSafetyMode.ExecutionAndPublication, "Test1")]
    [Arguments(LazyThreadSafetyMode.ExecutionAndPublication, "Test2")]
    [Arguments(LazyThreadSafetyMode.None, "")]
    [Arguments(LazyThreadSafetyMode.None, "Test1")]
    [Arguments(LazyThreadSafetyMode.None, "Test2")]
    public Task ConstructionAndMultiplePropertyInjectionWithLazyMode(LazyThreadSafetyMode mode, string contract)
    {
        var arguments = string.IsNullOrWhiteSpace(contract) ?
            $"LazyThreadSafetyMode.{mode}" :
            $"\"{contract}\", LazyThreadSafetyMode.{mode}";

        var source = $$"""
            using System;
            using System.Threading;
            using Splat;

            namespace Test
            {
                public static class DIRegister
                {
                    static DIRegister()
                    {
                        SplatRegistrations.RegisterLazySingleton<ITest, TestConcrete>({{arguments}});
                    }
                }

                public interface ITest { }
                public class TestConcrete : ITest
                {
                    public TestConcrete(IService1 service1, IService2 service)
                    {
                    }

                    [DependencyInjectionProperty]
                    public IServiceProperty1 ServiceProperty1 { get; set; }

                    [DependencyInjectionProperty]
                    public IServiceProperty2 ServiceProperty2 { get; set; }

                    [DependencyInjectionProperty]
                    internal IServiceProperty3 ServiceProperty3 { get; set; }
                }

                public interface IService1 { }
                public interface IService2 { }
                public interface IServiceProperty1 { }
                public interface IServiceProperty2 { }
                public interface IServiceProperty3 { }
            }
            """;

        return TestHelper.TestPass(source, contract, mode, GetType());
    }

    /// <summary>
    /// Validates that lazy parameter injection works when the dependency is properly registered as a lazy singleton.
    /// </summary>
    /// <param name="contract">The contract name parameter to test (empty, "Test1", or "Test2").</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [Arguments("")]
    [Arguments("Test1")]
    [Arguments("Test2")]
    public Task LazyParameterRegisteredLazy(string contract)
    {
        var arguments = string.IsNullOrWhiteSpace(contract) ?
            string.Empty :
            $"\"{contract}\"";

        var source = $$"""
            using System;
            using System.Threading;
            using Splat;

            namespace Test
            {
                public static class DIRegister
                {
                    static DIRegister()
                    {
                        SplatRegistrations.Register<ITest, TestConcrete>({{arguments}});
                        SplatRegistrations.Register<IService1, Service1>({{arguments}});
                        SplatRegistrations.RegisterLazySingleton<IService2, Service2>({{arguments}});
                    }
                }

                public interface ITest { }
                public class TestConcrete : ITest
                {
                    public TestConcrete(IService1 service1, Lazy<IService2> service)
                    {
                    }

                    [DependencyInjectionProperty]
                    public IServiceProperty1 ServiceProperty1 { get; set; }

                    [DependencyInjectionProperty]
                    public IServiceProperty2 ServiceProperty2 { get; set; }

                    [DependencyInjectionProperty]
                    internal IServiceProperty3 ServiceProperty3 { get; set; }
                }

                public interface IService1 { }
                public class Service1 : IService1 { }
                public interface IService2 { }
                public class Service2 : IService2 { }
                public interface IServiceProperty1 { }
                public interface IServiceProperty2 { }
                public interface IServiceProperty3 { }
            }
            """;

        return TestHelper.TestPass(source, contract, GetType());
    }
}
