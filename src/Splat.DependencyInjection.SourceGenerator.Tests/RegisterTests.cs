// Copyright (c) 2019-2021 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

using VerifyXunit;

using Xunit;
using Xunit.Abstractions;

using static ICSharpCode.Decompiler.IL.Transforms.Stepper;

namespace Splat.DependencyInjection.SourceGenerator.Tests
{
    [UsesVerify]
    public sealed class RegisterTests : TestBase
    {
        public RegisterTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper, "Register")
        {
        }

        [Theory]
        [InlineData("")]
        [InlineData("Test1")]
        [InlineData("Test2")]
        public Task LazyParameterConstantNotRegisteredLazyFail(string contract)
        {
            string arguments;
            if (string.IsNullOrWhiteSpace(contract))
            {
                arguments = string.Empty;
            }
            else
            {
                arguments = $"\"{contract}\"";
            }

            string constantArguments;
            if (string.IsNullOrWhiteSpace(contract))
            {
                constantArguments = "new Service1()";
            }
            else
            {
                constantArguments = $"new Service1(), \"{contract}\"";
            }

            var source = @$"
using System;
using System.Threading;
using Splat;

namespace Test
{{
    public static class DIRegister
    {{
        static DIRegister()
        {{
            SplatRegistrations.Register<ITest, TestConcrete>({arguments});
            SplatRegistrations.RegisterConstant({constantArguments});
        }}
    }}

    public interface ITest {{ }}
    public class TestConcrete : ITest
    {{
        public TestConcrete(Lazy<Service1> service1)
        {{
        }}
    }}

    public class Service1 {{ }}
}}";

            return TestHelper.TestFail(source, contract);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Test1")]
        [InlineData("Test2")]
        public Task LazyParameterNotRegisteredLazyFail(string contract)
        {
            string arguments;
            if (string.IsNullOrWhiteSpace(contract))
            {
                arguments = string.Empty;
            }
            else
            {
                arguments = $"\"{contract}\"";
            }

            var source = @$"
using System;
using System.Threading;
using Splat;

namespace Test
{{
    public static class DIRegister
    {{
        static DIRegister()
        {{
            SplatRegistrations.Register<ITest, TestConcrete>({arguments});
            SplatRegistrations.Register<IService1, Service1>({arguments});
            SplatRegistrations.Register<IService2, Service2>({arguments});
        }}
    }}

    public interface ITest {{ }}
    public class TestConcrete : ITest
    {{
        public TestConcrete(IService1 service1, Lazy<IService2> service)
        {{
        }}

        [DependencyInjectionProperty]
        public IServiceProperty1 ServiceProperty1 {{ get; set; }}

        [DependencyInjectionProperty]
        public IServiceProperty2 ServiceProperty2 {{ get; set; }}

        [DependencyInjectionProperty]
        internal IServiceProperty3 ServiceProperty3 {{ get; set; }}
    }}

    public interface IService1 {{ }}
    public class Service1 : IService1 {{ }}
    public interface IService2 {{ }}
    public class Service2 : IService2 {{ }}
    public interface IServiceProperty1 {{ }}
    public interface IServiceProperty2 {{ }}
    public interface IServiceProperty3 {{ }}
}}";

            return TestHelper.TestFail(source, contract);
        }
    }
}
