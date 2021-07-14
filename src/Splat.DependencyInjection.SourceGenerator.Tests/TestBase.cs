// Copyright (c) 2019-2021 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using ICSharpCode.Decompiler.Metadata;

using Microsoft.CodeAnalysis;

using NuGet.LibraryModel;
using NuGet.Versioning;

using ReactiveMarbles.NuGet.Helpers;
using ReactiveMarbles.SourceGenerator.TestNuGetHelper.Compilation;

using VerifyTests;

using VerifyXunit;

using Xunit;
using Xunit.Abstractions;

namespace Splat.DependencyInjection.SourceGenerator.Tests
{
    public abstract class TestBase : IAsyncLifetime, IDisposable
    {
#pragma warning disable CS0618 // Type or member is obsolete
        private static readonly LibraryRange _splatLibrary = new("Splat", VersionRange.AllStableFloating, LibraryDependencyTarget.Package);
#pragma warning restore CS0618 // Type or member is obsolete

        private readonly string _testMethod;

        protected TestBase(ITestOutputHelper testOutputHelper, string testMethod)
        {
            TestOutputHelper = testOutputHelper;
            _testMethod = testMethod;
        }

        protected ITestOutputHelper TestOutputHelper { get; }

        protected EventBuilderCompiler? EventCompiler { get; private set; }

        public async Task InitializeAsync()
        {
            var targetFrameworks = "netstandard2.0".ToFrameworks();

            var inputGroup = await NuGetPackageHelper.DownloadPackageFilesAndFolder(_splatLibrary, targetFrameworks, packageOutputDirectory: null).ConfigureAwait(false);

            var framework = targetFrameworks[0];
            EventCompiler = new EventBuilderCompiler(inputGroup, inputGroup, framework);
        }

        public Task DisposeAsync()
        {
            EventCompiler?.Dispose();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Test1")]
        [InlineData("Test2")]
        public Task ConstructionInjection(string contractParameter)
        {
            var arguments = string.IsNullOrWhiteSpace(contractParameter) ? string.Empty : '"' + contractParameter + '"';
            var source = @$"
using System;
using Splat;

namespace Test
{{
    public static class DIRegister
    {{
        static DIRegister()
        {{
            SplatRegistrations.{_testMethod}<ITest, TestConcrete>({arguments});
        }}
    }}

    public interface ITest {{ }}
    public class TestConcrete : ITest
    {{
        public TestConcrete(IService1 service1, IService2 service)
        {{
        }}
    }}

    public interface IService1 {{ }}
    public interface IService2 {{ }}
}}";

            return TestPass(source, contractParameter);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Test1")]
        [InlineData("Test2")]
        public Task CircularDependencyFail(string contractParameter)
        {
            var arguments = string.IsNullOrWhiteSpace(contractParameter) ? string.Empty : '"' + contractParameter + '"';
            var source = $@"
using System;
using Splat;

namespace Test
{{
    public static class DIRegister
    {{
        static DIRegister()
        {{
            SplatRegistrations.{_testMethod}<ITest1, TestConcrete1>({arguments});
            SplatRegistrations.{_testMethod}<ITest2, TestConcrete2>();        
        }}
    }}

    public interface ITest1 {{ }}
    public class TestConcrete1 : ITest1
    {{
        public TestConcrete1(ITest2 service1, IService2 service2)
        {{
        }}
    }}

    public interface ITest2 {{ }}
    public class TestConcrete2 : ITest2
    {{
        public TestConcrete2(ITest1 service1, IService2 service2)
        {{
        }}
    }}

    public interface IService1 {{ }}
    public interface IService2 {{ }}
}}";

            return TestFail(source, contractParameter);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Test1")]
        [InlineData("Test2")]
        public Task MultiClassesRegistrations(string contractParameter)
        {
            var arguments = string.IsNullOrWhiteSpace(contractParameter) ? string.Empty : '"' + contractParameter + '"';
            var source = @$"
using System;
using Splat;

namespace Test
{{
    public static class DIRegister
    {{
        static DIRegister()
        {{
            SplatRegistrations.{_testMethod}<ITest1, TestConcrete1>({arguments});
            SplatRegistrations.{_testMethod}<ITest2, TestConcrete2>({arguments});
            SplatRegistrations.{_testMethod}<ITest3, TestConcrete3>({arguments});
        }}
    }}

    public interface ITest1 {{ }}
    public class TestConcrete1 : ITest1
    {{
        public TestConcrete1(IService1 service1, IService2 service)
        {{
        }}
    }}

    public interface ITest2 {{ }}
    public class TestConcrete2 : ITest2
    {{
        public TestConcrete2(IService1 service1, IService2 service)
        {{
        }}
    }}

    public interface ITest3 {{ }}
    public class TestConcrete3 : ITest3
    {{
        public TestConcrete3(IService1 service1, IService2 service)
        {{
        }}
    }}

    public interface IService1 {{ }}
    public interface IService2 {{ }}
}}";

            return TestPass(source, contractParameter);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Test1")]
        [InlineData("Test2")]
        public Task ConstructionAndPropertyInjection(string contractParameter)
        {
            var arguments = string.IsNullOrWhiteSpace(contractParameter) ? string.Empty : '"' + contractParameter + '"';
            var source = @$"
using System;
using Splat;

namespace Test
{{
    public static class DIRegister
    {{
        static DIRegister()
        {{
            SplatRegistrations.{_testMethod}<ITest, TestConcrete>({arguments});
        }}
    }}

    public interface ITest {{ }}
    public class TestConcrete : ITest
    {{
        public TestConcrete(IService1 service1, IService2 service)
        {{
        }}

        [DependencyInjectionProperty]
        public IServiceProperty ServiceProperty {{ get; set; }}
    }}

    public interface IService1 {{ }}
    public interface IService2 {{ }}
    public interface IServiceProperty {{ }}
}}";

            return TestPass(source, contractParameter);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Test1")]
        [InlineData("Test2")]
        public Task ConstructionAndNonPublicPropertyInjectionFail(string contractParameter)
        {
            var arguments = string.IsNullOrWhiteSpace(contractParameter) ? string.Empty : '"' + contractParameter + '"';
            var source = @$"
using System;
using Splat;

namespace Test
{{
    public static class DIRegister
    {{
        static DIRegister()
        {{
            SplatRegistrations.{_testMethod}<ITest, TestConcrete>({arguments});
        }}
    }}

    public interface ITest {{ }}
    public class TestConcrete : ITest
    {{
        public TestConcrete(IService1 service1, IService2 service)
        {{
        }}

        [DependencyInjectionProperty]
        protected IServiceProperty ServiceProperty {{ get; set; }}
    }}

    public interface IService1 {{ }}
    public interface IService2 {{ }}
    public interface IServiceProperty {{ }}
}}";

            return TestFail(source, contractParameter);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Test1")]
        [InlineData("Test2")]
        public Task ConstructionAndNonPublicPropertySetterInjectionFail(string contractParameter)
        {
            var arguments = string.IsNullOrWhiteSpace(contractParameter) ? string.Empty : '"' + contractParameter + '"';
            var source = @$"
using System;
using Splat;

namespace Test
{{
    public static class DIRegister
    {{
        static DIRegister()
        {{
            SplatRegistrations.{_testMethod}<ITest, TestConcrete>({arguments});
        }}
    }}

    public interface ITest {{ }}
    public class TestConcrete : ITest
    {{
        public TestConcrete(IService1 service1, IService2 service)
        {{
        }}

        [DependencyInjectionProperty]
        public IServiceProperty ServiceProperty {{ get; private set; }}
    }}

    public interface IService1 {{ }}
    public interface IService2 {{ }}
    public interface IServiceProperty {{ }}
}}";

            return TestFail(source, contractParameter);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Test1")]
        [InlineData("Test2")]
        public Task ConstructionAndInternalPropertyInjection(string contractParameter)
        {
            var arguments = string.IsNullOrWhiteSpace(contractParameter) ? string.Empty : '"' + contractParameter + '"';
            var source = @$"
using System;
using Splat;

namespace Test
{{
    public static class DIRegister
    {{
        static DIRegister()
        {{
            SplatRegistrations.{_testMethod}<ITest, TestConcrete>({arguments});
        }}
    }}

    public interface ITest {{ }}
    public class TestConcrete : ITest
    {{
        public TestConcrete(IService1 service1, IService2 service)
        {{
        }}

        [DependencyInjectionProperty]
        internal IServiceProperty ServiceProperty {{ get; set; }}
    }}

    public interface IService1 {{ }}
    public interface IService2 {{ }}
    public interface IServiceProperty {{ }}
}}";

            return TestPass(source, contractParameter);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Test1")]
        [InlineData("Test2")]
        public Task ConstructionAndInternalSetterPropertyInjection(string contractParameter)
        {
            var arguments = string.IsNullOrWhiteSpace(contractParameter) ? string.Empty : '"' + contractParameter + '"';
            var source = @$"
using System;
using Splat;

namespace Test
{{
    public static class DIRegister
    {{
        static DIRegister()
        {{
            SplatRegistrations.{_testMethod}<ITest, TestConcrete>({arguments});
        }}
    }}

    public interface ITest {{ }}
    public class TestConcrete : ITest
    {{
        public TestConcrete(IService1 service1, IService2 service)
        {{
        }}

        [DependencyInjectionProperty]
        public IServiceProperty ServiceProperty {{ get; internal set; }}
    }}

    public interface IService1 {{ }}
    public interface IService2 {{ }}
    public interface IServiceProperty {{ }}
}}";

            return TestPass(source, contractParameter);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Test1")]
        [InlineData("Test2")]
        public Task ConstructionAndMultiplePropertyInjection(string contractParameter)
        {
            var arguments = string.IsNullOrWhiteSpace(contractParameter) ? string.Empty : '"' + contractParameter + '"';

            var source = @$"
using System;
using Splat;

namespace Test
{{
    public static class DIRegister
    {{
        static DIRegister()
        {{
            SplatRegistrations.{_testMethod}<ITest, TestConcrete>({arguments});
        }}
    }}

    public interface ITest {{ }}
    public class TestConcrete : ITest
    {{
        public TestConcrete(IService1 service1, IService2 service)
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
    public interface IService2 {{ }}
    public interface IServiceProperty1 {{ }}
    public interface IServiceProperty2 {{ }}
    public interface IServiceProperty3 {{ }}
}}";

            return TestPass(source, contractParameter);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Test1")]
        [InlineData("Test2")]
        public Task MultipleConstructorWithoutAttributeFail(string contractParameter)
        {
            var arguments = string.IsNullOrWhiteSpace(contractParameter) ? string.Empty : '"' + contractParameter + '"';
            var source = @$"
using System;
using Splat;

namespace Test
{{
    public static class DIRegister
    {{
        static DIRegister()
        {{
            SplatRegistrations.{_testMethod}<ITest, TestConcrete>({arguments});
        }}
    }}

    public interface ITest {{ }}
    public class TestConcrete : ITest
    {{
        public TestConcrete(IService1 service1, IService2 service)
        {{
        }}

        public TestConcrete(IService1 service1)
        {{
        }}
    }}

    public interface IService1 {{ }}
    public interface IService2 {{ }}
}}";

            return TestFail(source, contractParameter);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Test1")]
        [InlineData("Test2")]
        public Task MultipleConstructorWithAttribute(string contractParameter)
        {
            var arguments = string.IsNullOrWhiteSpace(contractParameter) ? string.Empty : '"' + contractParameter + '"';
            var source = @$"
using System;
using Splat;

namespace Test
{{
    public static class DIRegister
    {{
        static DIRegister()
        {{
            SplatRegistrations.{_testMethod}<ITest, TestConcrete>({arguments});
        }}
    }}

    public interface ITest {{ }}
    public class TestConcrete : ITest
    {{
        public TestConcrete(IService1 service1, IService2 service)
        {{
        }}

        [DependencyInjectionConstructor]
        public TestConcrete(IService1 service1)
        {{
        }}
    }}

    public interface IService1 {{ }}
    public interface IService2 {{ }}
}}";

            return TestPass(source, contractParameter);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Test1")]
        [InlineData("Test2")]
        public Task MultipleConstructorWithMultipleAttributesFail(string contractParameter)
        {
            var arguments = string.IsNullOrWhiteSpace(contractParameter) ? string.Empty : '"' + contractParameter + '"';
            var source = @$"
using System;
using Splat;

namespace Test
{{
    public static class DIRegister
    {{
        static DIRegister()
        {{
            SplatRegistrations.{_testMethod}<ITest, TestConcrete>({arguments});
        }}
    }}

    public interface ITest {{ }}
    public class TestConcrete : ITest
    {{
        [DependencyInjectionConstructor]
        public TestConcrete(IService1 service1, IService2 service)
        {{
        }}

        [DependencyInjectionConstructor]
        public TestConcrete(IService1 service1)
        {{
        }}
    }}

    public interface IService1 {{ }}
    public interface IService2 {{ }}
}}";

            return TestFail(source, contractParameter);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Test1")]
        [InlineData("Test2")]
        public Task EmptyConstructor(string contractParameter)
        {
            var arguments = string.IsNullOrWhiteSpace(contractParameter) ? string.Empty : '"' + contractParameter + '"';
            var source = @$"
using System;
using Splat;

namespace Test
{{
    public static class DIRegister
    {{
        static DIRegister()
        {{
            SplatRegistrations.{_testMethod}<ITest, TestConcrete>({arguments});
        }}
    }}

    public interface ITest {{ }}
    public class TestConcrete : ITest {{ }}
}}";

            return TestPass(source, contractParameter);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Test1")]
        [InlineData("Test2")]
        public Task InterfaceRegisteredMultipleTimes(string contractParameter)
        {
            var arguments = string.IsNullOrWhiteSpace(contractParameter) ? string.Empty : '"' + contractParameter + '"';
            var source = @$"
using System;
using Splat;

namespace Test
{{
    public static class DIRegister
    {{
        static DIRegister()
        {{
            SplatRegistrations.{_testMethod}<ITest, TestConcrete1>({arguments});
            SplatRegistrations.{_testMethod}<ITest, TestConcrete2>({arguments});
        }}
    }}

    public interface ITest {{ }}
    public class TestConcrete1 : ITest {{ }}
    public class TestConcrete2 : ITest {{ }}
}}";

            return TestFail(source, contractParameter);
        }

        protected Task TestFail(string source, string contractParameter, [CallerFilePath] string file = "")
        {
            if (EventCompiler is null)
            {
                throw new InvalidOperationException("Must have valid compiler instance.");
            }

            var utility = new SourceGeneratorUtility(x => TestOutputHelper.WriteLine(x));

            GeneratorDriver? driver = null;

            Assert.Throws<InvalidOperationException>(() => utility.RunGenerator<Generator>(EventCompiler, out var _, out var _, out driver, source));

            VerifySettings settings = new();
            settings.UseParameters(contractParameter);

            return Verifier.Verify(driver, settings, sourceFile: file);
        }

        protected Task TestPass(string source, string contractParameter, [CallerFilePath] string file = "")
        {
            var driver = Generate(source);
            VerifySettings settings = new();
            settings.UseParameters(contractParameter);

            return Verifier.Verify(driver, settings, sourceFile: file);
        }

        protected Task TestPass(string source, string contractParameter, LazyThreadSafetyMode mode, [CallerFilePath] string file = "")
        {
            var driver = Generate(source);

            VerifySettings settings = new();
            settings.UseParameters(contractParameter, mode);

            return Verifier.Verify(driver, settings, sourceFile: file);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                EventCompiler?.Dispose();
            }
        }

        private GeneratorDriver Generate(string source)
        {
            if (EventCompiler is null)
            {
                throw new InvalidOperationException("Must have valid compiler instance.");
            }

            var utility = new SourceGeneratorUtility(x => TestOutputHelper.WriteLine(x));

            utility.RunGenerator<Generator>(EventCompiler, out var _, out var _, out var driver, source);

            return driver;
        }
    }
}
