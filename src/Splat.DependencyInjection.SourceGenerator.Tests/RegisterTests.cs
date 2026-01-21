// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using TUnit.Core;

namespace Splat.DependencyInjection.SourceGenerator.Tests;

/// <summary>
/// Tests for the Register method source generation.
/// Validates transient registration scenarios including constructor injection, property injection, and contract parameters.
/// </summary>
[InheritsTests]
public sealed class RegisterTests() : TestBase("Register")
{
    /// <summary>
    /// Validates that lazy parameter injection fails when the dependency is registered as a constant instead of lazy singleton.
    /// </summary>
    /// <param name="contract">The contract name parameter to test (empty, "Test1", or "Test2").</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [Arguments("")]
    [Arguments("Test1")]
    [Arguments("Test2")]
    public Task LazyParameterConstantNotRegisteredLazyFail(string contract)
    {
        var arguments = string.IsNullOrWhiteSpace(contract)
            ? string.Empty
            : $"\"{contract}\"";

        var constantArguments = string.IsNullOrWhiteSpace(contract)
            ? "new Service1()"
            : $"new Service1(), \"{contract}\"";

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
                        SplatRegistrations.RegisterConstant({{constantArguments}});
                    }
                }

                public interface ITest { }
                public class TestConcrete : ITest
                {
                    public TestConcrete(Lazy<Service1> service1)
                    {
                    }
                }

                public class Service1 { }
            }
            """;

        return TestHelper.TestFail(source, contract, GetType());
    }

    /// <summary>
    /// Validates that lazy parameter injection fails when the dependency is not registered as a lazy singleton.
    /// </summary>
    /// <param name="contract">The contract name parameter to test (empty, "Test1", or "Test2").</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [Arguments("")]
    [Arguments("Test1")]
    [Arguments("Test2")]
    public Task LazyParameterNotRegisteredLazyFail(string contract)
    {
        var arguments = string.IsNullOrWhiteSpace(contract)
            ? string.Empty
            : $"\"{contract}\"";

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
                        SplatRegistrations.Register<IService2, Service2>({{arguments}});
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

        return TestHelper.TestFail(source, contract, GetType());
    }

    /// <summary>
    /// Validates that IEnumerable{T} dependency injection generates GetServices{T}() calls.
    /// </summary>
    /// <param name="contract">The contract name parameter to test (empty, "Test1", or "Test2").</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [Arguments("")]
    [Arguments("Test1")]
    [Arguments("Test2")]
    public Task IEnumerableDependency(string contract)
    {
        var arguments = string.IsNullOrWhiteSpace(contract)
            ? string.Empty
            : $"\"{contract}\"";

        var source = $$"""
            using System;
            using System.Collections.Generic;
            using Splat;

            namespace Test
            {
                public static class DIRegister
                {
                    static DIRegister()
                    {
                        SplatRegistrations.Register<ITest, TestConcrete>({{arguments}});
                        SplatRegistrations.Register<IService, ServiceA>({{arguments}});
                        SplatRegistrations.Register<IService, ServiceB>({{arguments}});
                    }
                }

                public interface ITest { }
                public class TestConcrete : ITest
                {
                    public TestConcrete(IEnumerable<IService> services)
                    {
                    }
                }

                public interface IService { }
                public class ServiceA : IService { }
                public class ServiceB : IService { }
            }
            """;

        return TestHelper.TestPass(source, contract, GetType());
    }

    /// <summary>
    /// Validates that generic concrete types with single type parameter are handled correctly.
    /// </summary>
    /// <param name="contract">The contract name parameter to test (empty, "Test1", or "Test2").</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [Arguments("")]
    [Arguments("Test1")]
    [Arguments("Test2")]
    public Task GenericConcreteType_SingleTypeParameter(string contract)
    {
        var arguments = string.IsNullOrWhiteSpace(contract)
            ? string.Empty
            : $"\"{contract}\"";

        var source = $$"""
            using System;
            using Splat;

            namespace Test
            {
                public static class DIRegister
                {
                    static DIRegister()
                    {
                        SplatRegistrations.Register<IAppUpdateService, AppUpdateService<string>>({{arguments}});
                        SplatRegistrations.Register<IConfig, Config>({{arguments}});
                    }
                }

                public interface IAppUpdateService { }
                public class AppUpdateService<TSettings> : IAppUpdateService
                {
                    public AppUpdateService(IConfig config)
                    {
                    }
                }

                public interface IConfig { }
                public class Config : IConfig { }
            }
            """;

        return TestHelper.TestPass(source, contract, GetType());
    }

    /// <summary>
    /// Validates that generic concrete types with multiple type parameters are handled correctly.
    /// </summary>
    /// <param name="contract">The contract name parameter to test (empty, "Test1", or "Test2").</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [Arguments("")]
    [Arguments("Test1")]
    [Arguments("Test2")]
    public Task GenericConcreteType_MultipleTypeParameters(string contract)
    {
        var arguments = string.IsNullOrWhiteSpace(contract)
            ? string.Empty
            : $"\"{contract}\"";

        var source = $$"""
            using System;
            using Splat;

            namespace Test
            {
                public static class DIRegister
                {
                    static DIRegister()
                    {
                        SplatRegistrations.Register<ICache, Cache<string, int>>({{arguments}});
                    }
                }

                public interface ICache { }
                public class Cache<TKey, TValue> : ICache
                {
                    public Cache()
                    {
                    }
                }
            }
            """;

        return TestHelper.TestPass(source, contract, GetType());
    }

    /// <summary>
    /// Validates that nested generic types are handled correctly.
    /// </summary>
    /// <param name="contract">The contract name parameter to test (empty, "Test1", or "Test2").</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [Arguments("")]
    [Arguments("Test1")]
    [Arguments("Test2")]
    public Task GenericConcreteType_NestedGeneric(string contract)
    {
        var arguments = string.IsNullOrWhiteSpace(contract)
            ? string.Empty
            : $"\"{contract}\"";

        var source = $$"""
            using System;
            using System.Collections.Generic;
            using Splat;

            namespace Test
            {
                public static class DIRegister
                {
                    static DIRegister()
                    {
                        SplatRegistrations.Register<IRepository, Repository<List<string>>>({{arguments}});
                    }
                }

                public interface IRepository { }
                public class Repository<T> : IRepository
                {
                    public Repository()
                    {
                    }
                }
            }
            """;

        return TestHelper.TestPass(source, contract, GetType());
    }
}