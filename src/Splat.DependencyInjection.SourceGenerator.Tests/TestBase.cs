// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using TUnit.Core;

namespace Splat.DependencyInjection.SourceGenerator.Tests;

/// <summary>
/// Base class for source generator snapshot tests.
/// Provides common test patterns for verifying generated code against snapshots.
/// </summary>
/// <param name="testMethod">The name of the registration method being tested (Register or RegisterLazySingleton).</param>
public abstract class TestBase(string testMethod)
{
    /// <summary>
    /// Initializes resources before each test.
    /// </summary>
    /// <returns>A task representing the asynchronous initialization operation.</returns>
    [Before(Test)]
    public Task SetupAsync() => TestHelper.InitializeAsync();

    /// <summary>
    /// Cleans up resources after each test.
    /// </summary>
    /// <returns>A task representing the asynchronous cleanup operation.</returns>
    [After(Test)]
    public Task CleanupAsync()
    {
        // No cleanup needed with static TestHelper methods
        return Task.CompletedTask;
    }

    /// <summary>
    /// Validates that basic constructor injection works with different contract parameters.
    /// </summary>
    /// <param name="contractParameter">The contract name parameter to test (empty, "Test1", or "Test2").</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [Arguments("")]
    [Arguments("Test1")]
    [Arguments("Test2")]
    public Task ConstructionInjection(string contractParameter)
    {
        var arguments = string.IsNullOrWhiteSpace(contractParameter) ? string.Empty : '"' + contractParameter + '"';
        var source = $$"""

            using System;
            using Splat;

            namespace Test
            {
                public static class DIRegister
                {
                    static DIRegister()
                    {
                        SplatRegistrations.{{testMethod}}<ITest, TestConcrete>({{arguments}});
                    }
                }

                public interface ITest { }
                public class TestConcrete : ITest
                {
                    public TestConcrete(IService1 service1, IService2 service)
                    {
                    }
                }

                public interface IService1 { }
                public interface IService2 { }
            }
            """;

        return TestHelper.TestPass(source, contractParameter, GetType());
    }

    /// <summary>
    /// Validates that circular dependencies between registered types are detected and fail appropriately.
    /// </summary>
    /// <param name="contractParameter">The contract name parameter to test (empty, "Test1", or "Test2").</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [Arguments("")]
    [Arguments("Test1")]
    [Arguments("Test2")]
    public Task CircularDependencyFail(string contractParameter)
    {
        var arguments = string.IsNullOrWhiteSpace(contractParameter) ? string.Empty : '"' + contractParameter + '"';
        var source = $$"""

            using System;
            using Splat;

            namespace Test
            {
                public static class DIRegister
                {
                    static DIRegister()
                    {
                        SplatRegistrations.{{testMethod}}<ITest1, TestConcrete1>({{arguments}});
                        SplatRegistrations.{{testMethod}}<ITest2, TestConcrete2>();
                    }
                }

                public interface ITest1 { }
                public class TestConcrete1 : ITest1
                {
                    public TestConcrete1(ITest2 service1, IService2 service2)
                    {
                    }
                }

                public interface ITest2 { }
                public class TestConcrete2 : ITest2
                {
                    public TestConcrete2(ITest1 service1, IService2 service2)
                    {
                    }
                }

                public interface IService1 { }
                public interface IService2 { }
            }
            """;

        return TestHelper.TestFail(source, contractParameter, GetType());
    }

    /// <summary>
    /// Validates that multiple classes can be registered simultaneously with constructor injection.
    /// </summary>
    /// <param name="contractParameter">The contract name parameter to test (empty, "Test1", or "Test2").</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [Arguments("")]
    [Arguments("Test1")]
    [Arguments("Test2")]
    public Task MultiClassesRegistrations(string contractParameter)
    {
        var arguments = string.IsNullOrWhiteSpace(contractParameter) ? string.Empty : '"' + contractParameter + '"';
        var source = $$"""

            using System;
            using Splat;

            namespace Test
            {
                public static class DIRegister
                {
                    static DIRegister()
                    {
                        SplatRegistrations.{{testMethod}}<ITest1, TestConcrete1>({{arguments}});
                        SplatRegistrations.{{testMethod}}<ITest2, TestConcrete2>({{arguments}});
                        SplatRegistrations.{{testMethod}}<ITest3, TestConcrete3>({{arguments}});
                    }
                }

                public interface ITest1 { }
                public class TestConcrete1 : ITest1
                {
                    public TestConcrete1(IService1 service1, IService2 service)
                    {
                    }
                }

                public interface ITest2 { }
                public class TestConcrete2 : ITest2
                {
                    public TestConcrete2(IService1 service1, IService2 service)
                    {
                    }
                }

                public interface ITest3 { }
                public class TestConcrete3 : ITest3
                {
                    public TestConcrete3(IService1 service1, IService2 service)
                    {
                    }
                }

                public interface IService1 { }
                public interface IService2 { }
            }
            """;

        return TestHelper.TestPass(source, contractParameter, GetType());
    }

    /// <summary>
    /// Validates that both constructor injection and public property injection work together.
    /// </summary>
    /// <param name="contractParameter">The contract name parameter to test (empty, "Test1", or "Test2").</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [Arguments("")]
    [Arguments("Test1")]
    [Arguments("Test2")]
    public Task ConstructionAndPropertyInjection(string contractParameter)
    {
        var arguments = string.IsNullOrWhiteSpace(contractParameter) ? string.Empty : '"' + contractParameter + '"';
        var source = $$"""

            using System;
            using Splat;

            namespace Test
            {
                public static class DIRegister
                {
                    static DIRegister()
                    {
                        SplatRegistrations.{{testMethod}}<ITest, TestConcrete>({{arguments}});
                    }
                }

                public interface ITest { }
                public class TestConcrete : ITest
                {
                    public TestConcrete(IService1 service1, IService2 service)
                    {
                    }

                    [DependencyInjectionProperty]
                    public IServiceProperty ServiceProperty { get; set; }
                }

                public interface IService1 { }
                public interface IService2 { }
                public interface IServiceProperty { }
            }
            """;

        return TestHelper.TestPass(source, contractParameter, GetType());
    }

    /// <summary>
    /// Validates that property injection fails when the property is not public.
    /// </summary>
    /// <param name="contractParameter">The contract name parameter to test (empty, "Test1", or "Test2").</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [Arguments("")]
    [Arguments("Test1")]
    [Arguments("Test2")]
    public Task ConstructionAndNonPublicPropertyInjectionFail(string contractParameter)
    {
        var arguments = string.IsNullOrWhiteSpace(contractParameter) ? string.Empty : '"' + contractParameter + '"';
        var source = $$"""

            using System;
            using Splat;

            namespace Test
            {
                public static class DIRegister
                {
                    static DIRegister()
                    {
                        SplatRegistrations.{{testMethod}}<ITest, TestConcrete>({{arguments}});
                    }
                }

                public interface ITest { }
                public class TestConcrete : ITest
                {
                    public TestConcrete(IService1 service1, IService2 service)
                    {
                    }

                    [DependencyInjectionProperty]
                    protected IServiceProperty ServiceProperty { get; set; }
                }

                public interface IService1 { }
                public interface IService2 { }
                public interface IServiceProperty { }
            }
            """;

        return TestHelper.TestFail(source, contractParameter, GetType());
    }

    /// <summary>
    /// Validates that property injection fails when the property setter is not public.
    /// </summary>
    /// <param name="contractParameter">The contract name parameter to test (empty, "Test1", or "Test2").</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [Arguments("")]
    [Arguments("Test1")]
    [Arguments("Test2")]
    public Task ConstructionAndNonPublicPropertySetterInjectionFail(string contractParameter)
    {
        var arguments = string.IsNullOrWhiteSpace(contractParameter) ? string.Empty : '"' + contractParameter + '"';
        var source = $$"""

            using System;
            using Splat;

            namespace Test
            {
                public static class DIRegister
                {
                    static DIRegister()
                    {
                        SplatRegistrations.{{testMethod}}<ITest, TestConcrete>({{arguments}});
                    }
                }

                public interface ITest { }
                public class TestConcrete : ITest
                {
                    public TestConcrete(IService1 service1, IService2 service)
                    {
                    }

                    [DependencyInjectionProperty]
                    public IServiceProperty ServiceProperty { get; private set; }
                }

                public interface IService1 { }
                public interface IService2 { }
                public interface IServiceProperty { }
            }
            """;

        return TestHelper.TestFail(source, contractParameter, GetType());
    }

    /// <summary>
    /// Validates that property injection works with internal properties.
    /// </summary>
    /// <param name="contractParameter">The contract name parameter to test (empty, "Test1", or "Test2").</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [Arguments("")]
    [Arguments("Test1")]
    [Arguments("Test2")]
    public Task ConstructionAndInternalPropertyInjection(string contractParameter)
    {
        var arguments = string.IsNullOrWhiteSpace(contractParameter) ? string.Empty : '"' + contractParameter + '"';
        var source = $$"""

            using System;
            using Splat;

            namespace Test
            {
                public static class DIRegister
                {
                    static DIRegister()
                    {
                        SplatRegistrations.{{testMethod}}<ITest, TestConcrete>({{arguments}});
                    }
                }

                public interface ITest { }
                public class TestConcrete : ITest
                {
                    public TestConcrete(IService1 service1, IService2 service)
                    {
                    }

                    [DependencyInjectionProperty]
                    internal IServiceProperty ServiceProperty { get; set; }
                }

                public interface IService1 { }
                public interface IService2 { }
                public interface IServiceProperty { }
            }
            """;

        return TestHelper.TestPass(source, contractParameter, GetType());
    }

    /// <summary>
    /// Validates that property injection works with internal properties when using single type argument registration.
    /// </summary>
    /// <param name="contractParameter">The contract name parameter to test (empty, "Test1", or "Test2").</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [Arguments("")]
    [Arguments("Test1")]
    [Arguments("Test2")]
    public Task ConstructionAndInternalPropertyInjectionTypeArgument(string contractParameter)
    {
        var arguments = string.IsNullOrWhiteSpace(contractParameter) ? string.Empty : '"' + contractParameter + '"';
        var source = $$"""

            using System;
            using Splat;

            namespace Test
            {
                public static class DIRegister
                {
                    static DIRegister()
                    {
                        SplatRegistrations.{{testMethod}}<TestConcrete>({{arguments}});
                    }
                }

                public interface ITest { }
                public class TestConcrete : ITest
                {
                    public TestConcrete(IService1 service1, IService2 service)
                    {
                    }

                    [DependencyInjectionProperty]
                    internal IServiceProperty ServiceProperty { get; set; }
                }

                public interface IService1 { }
                public interface IService2 { }
                public interface IServiceProperty { }
            }
            """;

        return TestHelper.TestPass(source, contractParameter, GetType());
    }

    /// <summary>
    /// Validates that property injection works with public properties having internal setters.
    /// </summary>
    /// <param name="contractParameter">The contract name parameter to test (empty, "Test1", or "Test2").</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [Arguments("")]
    [Arguments("Test1")]
    [Arguments("Test2")]
    public Task ConstructionAndInternalSetterPropertyInjection(string contractParameter)
    {
        var arguments = string.IsNullOrWhiteSpace(contractParameter) ? string.Empty : '"' + contractParameter + '"';
        var source = $$"""

            using System;
            using Splat;

            namespace Test
            {
                public static class DIRegister
                {
                    static DIRegister()
                    {
                        SplatRegistrations.{{testMethod}}<ITest, TestConcrete>({{arguments}});
                    }
                }

                public interface ITest { }
                public class TestConcrete : ITest
                {
                    public TestConcrete(IService1 service1, IService2 service)
                    {
                    }

                    [DependencyInjectionProperty]
                    public IServiceProperty ServiceProperty { get; internal set; }
                }

                public interface IService1 { }
                public interface IService2 { }
                public interface IServiceProperty { }
            }
            """;

        return TestHelper.TestPass(source, contractParameter, GetType());
    }

    /// <summary>
    /// Validates that multiple properties can be injected simultaneously with different accessibility levels.
    /// </summary>
    /// <param name="contractParameter">The contract name parameter to test (empty, "Test1", or "Test2").</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [Arguments("")]
    [Arguments("Test1")]
    [Arguments("Test2")]
    public Task ConstructionAndMultiplePropertyInjection(string contractParameter)
    {
        var arguments = string.IsNullOrWhiteSpace(contractParameter) ? string.Empty : '"' + contractParameter + '"';

        var source = $$"""

            using System;
            using Splat;

            namespace Test
            {
                public static class DIRegister
                {
                    static DIRegister()
                    {
                        SplatRegistrations.{{testMethod}}<ITest, TestConcrete>({{arguments}});
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

        return TestHelper.TestPass(source, contractParameter, GetType());
    }

    /// <summary>
    /// Validates that multiple constructors without the DependencyInjectionConstructor attribute cause a failure.
    /// </summary>
    /// <param name="contractParameter">The contract name parameter to test (empty, "Test1", or "Test2").</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [Arguments("")]
    [Arguments("Test1")]
    [Arguments("Test2")]
    public Task MultipleConstructorWithoutAttributeFail(string contractParameter)
    {
        var arguments = string.IsNullOrWhiteSpace(contractParameter) ? string.Empty : '"' + contractParameter + '"';
        var source = $$"""

            using System;
            using Splat;

            namespace Test
            {
                public static class DIRegister
                {
                    static DIRegister()
                    {
                        SplatRegistrations.{{testMethod}}<ITest, TestConcrete>({{arguments}});
                    }
                }

                public interface ITest { }
                public class TestConcrete : ITest
                {
                    public TestConcrete(IService1 service1, IService2 service)
                    {
                    }

                    public TestConcrete(IService1 service1)
                    {
                    }
                }

                public interface IService1 { }
                public interface IService2 { }
            }
            """;

        return TestHelper.TestFail(source, contractParameter, GetType());
    }

    /// <summary>
    /// Validates that multiple constructors pass when not using DI registration (non-DI scenario).
    /// </summary>
    /// <param name="contractParameter">The contract name parameter to test (empty, "Test1", or "Test2").</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [Arguments("")]
    [Arguments("Test1")]
    [Arguments("Test2")]
    public Task MultipleConstructorWithoutAttributeNonDIPass(string contractParameter)
    {
        var arguments = string.IsNullOrWhiteSpace(contractParameter) ? string.Empty : '"' + contractParameter + '"';
        var source = $$"""

            using System;

            namespace Test
            {
                public interface ITest { }
                public class TestConcrete : ITest
                {
                    public static TestConcrete Instance = new(default!);
                    public TestConcrete(IService1 service1, IService2 service)
                    {
                        Instance.{{testMethod}}<ITest, TestConcrete>({{arguments}});
                    }

                    public TestConcrete(IService1 service1)
                    {
                    }
                    public void {{testMethod}}<T1, T2>(params object[] args)
                    {
                    }
                }

                public interface IService1 { }
                public interface IService2 { }
            }
            """;

        return TestHelper.TestPass(source, contractParameter, GetType());
    }

    /// <summary>
    /// Validates that multiple constructors work when one is marked with the DependencyInjectionConstructor attribute.
    /// </summary>
    /// <param name="contractParameter">The contract name parameter to test (empty, "Test1", or "Test2").</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [Arguments("")]
    [Arguments("Test1")]
    [Arguments("Test2")]
    public Task MultipleConstructorWithAttribute(string contractParameter)
    {
        var arguments = string.IsNullOrWhiteSpace(contractParameter) ? string.Empty : '"' + contractParameter + '"';
        var source = $$"""

            using System;
            using Splat;

            namespace Test
            {
                public static class DIRegister
                {
                    static DIRegister()
                    {
                        SplatRegistrations.{{testMethod}}<ITest, TestConcrete>({{arguments}});
                    }
                }

                public interface ITest { }
                public class TestConcrete : ITest
                {
                    public TestConcrete(IService1 service1, IService2 service)
                    {
                    }

                    [DependencyInjectionConstructor]
                    public TestConcrete(IService1 service1)
                    {
                    }
                }

                public interface IService1 { }
                public interface IService2 { }
            }
            """;

        return TestHelper.TestPass(source, contractParameter, GetType());
    }

    /// <summary>
    /// Validates that multiple constructors with multiple DependencyInjectionConstructor attributes cause a failure.
    /// </summary>
    /// <param name="contractParameter">The contract name parameter to test (empty, "Test1", or "Test2").</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [Arguments("")]
    [Arguments("Test1")]
    [Arguments("Test2")]
    public Task MultipleConstructorWithMultipleAttributesFail(string contractParameter)
    {
        var arguments = string.IsNullOrWhiteSpace(contractParameter) ? string.Empty : '"' + contractParameter + '"';
        var source = $$"""

            using System;
            using Splat;

            namespace Test
            {
                public static class DIRegister
                {
                    static DIRegister()
                    {
                        SplatRegistrations.{{testMethod}}<ITest, TestConcrete>({{arguments}});
                    }
                }

                public interface ITest { }
                public class TestConcrete : ITest
                {
                    [DependencyInjectionConstructor]
                    public TestConcrete(IService1 service1, IService2 service)
                    {
                    }

                    [DependencyInjectionConstructor]
                    public TestConcrete(IService1 service1)
                    {
                    }
                }

                public interface IService1 { }
                public interface IService2 { }
            }
            """;

        return TestHelper.TestFail(source, contractParameter, GetType());
    }

    /// <summary>
    /// Validates that classes with no constructor parameters can be registered successfully.
    /// </summary>
    /// <param name="contractParameter">The contract name parameter to test (empty, "Test1", or "Test2").</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [Arguments("")]
    [Arguments("Test1")]
    [Arguments("Test2")]
    public Task EmptyConstructor(string contractParameter)
    {
        var arguments = string.IsNullOrWhiteSpace(contractParameter) ? string.Empty : '"' + contractParameter + '"';
        var source = $$"""

            using System;
            using Splat;

            namespace Test
            {
                public static class DIRegister
                {
                    static DIRegister()
                    {
                        SplatRegistrations.{{testMethod}}<ITest, TestConcrete>({{arguments}});
                    }
                }

                public interface ITest { }
                public class TestConcrete : ITest { }
            }
            """;

        return TestHelper.TestPass(source, contractParameter, GetType());
    }

    /// <summary>
    /// Validates that registering the same interface multiple times with different implementations causes a failure.
    /// </summary>
    /// <param name="contractParameter">The contract name parameter to test (empty, "Test1", or "Test2").</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    [Arguments("")]
    [Arguments("Test1")]
    [Arguments("Test2")]
    public Task InterfaceRegisteredMultipleTimes(string contractParameter)
    {
        var arguments = string.IsNullOrWhiteSpace(contractParameter) ? string.Empty : '"' + contractParameter + '"';
        var source = $$"""

            using System;
            using Splat;

            namespace Test
            {
                public static class DIRegister
                {
                    static DIRegister()
                    {
                        SplatRegistrations.{{testMethod}}<ITest, TestConcrete1>({{arguments}});
                        SplatRegistrations.{{testMethod}}<ITest, TestConcrete2>({{arguments}});
                    }
                }

                public interface ITest { }
                public class TestConcrete1 : ITest { }
                public class TestConcrete2 : ITest { }
            }
            """;

        return TestHelper.TestFail(source, contractParameter, GetType());
    }
}
