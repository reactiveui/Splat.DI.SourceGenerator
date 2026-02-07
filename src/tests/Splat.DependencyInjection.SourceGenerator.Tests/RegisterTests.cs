// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

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

    /// <summary>
    /// Validates that property injection with contracts generates correct GetService calls.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public Task PropertyInjectionWithContract()
    {
        var source = """
            using System;
            using Splat;

            namespace Test
            {
                public static class DIRegister
                {
                    static DIRegister()
                    {
                        SplatRegistrations.Register<ITest, TestConcrete>("TestContract");
                        SplatRegistrations.Register<IService1, Service1>("TestContract");
                        SplatRegistrations.Register<IServiceProperty1, ServiceProperty1>("TestContract");
                    }
                }

                public interface ITest { }
                public class TestConcrete : ITest
                {
                    public TestConcrete(IService1 service1)
                    {
                    }

                    [DependencyInjectionProperty]
                    public IServiceProperty1 ServiceProperty1 { get; set; }
                }

                public interface IService1 { }
                public class Service1 : IService1 { }
                public interface IServiceProperty1 { }
                public class ServiceProperty1 : IServiceProperty1 { }
            }
            """;

        return TestHelper.TestPass(source, "TestContract", GetType());
    }

    /// <summary>
    /// Validates that contract keys from a different namespace are fully qualified in generated code.
    /// This is a regression test for the issue where Keys.Key1 from a different namespace
    /// causes CS0103 "The name 'Keys' does not exist in the current context" in generated code.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public Task ContractKeyFromDifferentNamespace()
    {
        var source = """
            using System;
            using Splat;
            using Test.Keys;

            namespace Test.Keys
            {
                public static class RegistrationKeys
                {
                    public static string Key1 = "Key1";
                    public static string Key2 = "Key2";
                }
            }

            namespace Test.Services
            {
                using Test.Keys;

                public static class DIRegister
                {
                    static DIRegister()
                    {
                        SplatRegistrations.Register<IService, ServiceImpl>(RegistrationKeys.Key1);
                    }
                }

                public interface IService { }
                public class ServiceImpl : IService { }
            }
            """;

        return TestHelper.TestPass(source, "Key1", GetType());
    }

    /// <summary>
    /// Validates that contract keys from a nested static class in a different namespace are fully qualified.
    /// This tests the scenario where the key is accessed via a simple name after a using directive.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public Task ContractKeyFromDifferentNamespaceWithSimpleName()
    {
        var source = """
            using System;
            using Splat;
            using ConsoleApp.NS1;

            namespace ConsoleApp.NS1
            {
                public static class Keys
                {
                    public static string Key1 = "Key1";
                    public static string Key2 = "Key2";
                }
            }

            namespace ConsoleApp.NS2
            {
                using ConsoleApp.NS1;

                public class Class1
                {
                    static Class1()
                    {
                        SplatRegistrations.Register<Class1>(Keys.Key1);
                    }
                }
            }
            """;

        return TestHelper.TestPass(source, "Key1", GetType());
    }

    /// <summary>
    /// Validates that contract keys from a method invocation are preserved as-is in generated code.
    /// This is a regression test for the issue where ToDisplayString on an IMethodSymbol produces
    /// a signature-like string (e.g., "global::Test.ContractHelper.GetContractKey()") that may not
    /// be a valid expression. The original invocation expression should be preserved verbatim.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public Task ContractKeyFromMethodInvocation()
    {
        var source = """
            using System;
            using Splat;

            namespace Test
            {
                public static class ContractHelper
                {
                    public static string GetContractKey() => "DynamicKey";
                }

                public static class DIRegister
                {
                    static DIRegister()
                    {
                        SplatRegistrations.Register<IService, ServiceImpl>(ContractHelper.GetContractKey());
                    }
                }

                public interface IService { }
                public class ServiceImpl : IService { }
            }
            """;

        return TestHelper.TestPass(source, "MethodResult", GetType());
    }

    /// <summary>
    /// Validates that contract keys from a static property in a different namespace are fully qualified.
    /// This tests the IPropertySymbol path (as opposed to IFieldSymbol) to ensure properties
    /// are handled the same way fields are when fully qualifying references.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public Task ContractKeyFromPropertyInDifferentNamespace()
    {
        var source = """
            using System;
            using Splat;

            namespace Test.Config
            {
                public static class Settings
                {
                    public static string ContractName { get; } = "MyContract";
                }
            }

            namespace Test.Services
            {
                using Test.Config;

                public static class DIRegister
                {
                    static DIRegister()
                    {
                        SplatRegistrations.Register<IService, ServiceImpl>(Settings.ContractName);
                    }
                }

                public interface IService { }
                public class ServiceImpl : IService { }
            }
            """;

        return TestHelper.TestPass(source, "ContractName", GetType());
    }

    /// <summary>
    /// Validates that contract keys using const string fields are handled correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public Task ContractKeyFromConstField()
    {
        var source = """
            using System;
            using Splat;

            namespace Test
            {
                public static class Constants
                {
                    public const string ServiceKey = "MyServiceKey";
                }

                public static class DIRegister
                {
                    static DIRegister()
                    {
                        SplatRegistrations.Register<IService, ServiceImpl>(Constants.ServiceKey);
                    }
                }

                public interface IService { }
                public class ServiceImpl : IService { }
            }
            """;

        return TestHelper.TestPass(source, "MyServiceKey", GetType());
    }
}