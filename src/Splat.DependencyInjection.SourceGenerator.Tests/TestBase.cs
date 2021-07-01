using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;

using NuGet.LibraryModel;
using NuGet.Versioning;
using ReactiveMarbles.NuGet.Helpers;
using ReactiveMarbles.SourceGenerator.TestNuGetHelper.Compilation;

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

        [Fact]
        public Task ConstructionInjection()
        {
            var source = @$"
using System;
using Splat;

namespace Test
{{
    public static class DIRegister
    {{
        static DIRegister()
        {{
            SplatRegistrations.{_testMethod}<ITest, TestConcrete>();
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

            return TestPass(source);
        }

        [Fact]
        public Task ConstructionAndPropertyInjection()
        {
            var source = @$"
using System;
using Splat;

namespace Test
{{
    public static class DIRegister
    {{
        static DIRegister()
        {{
            SplatRegistrations.{_testMethod}<ITest, TestConcrete>();
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

            return TestPass(source);
        }

        [Fact]
        public Task ConstructionAndNonPublicPropertyInjectionFail()
        {
            var source = @$"
using System;
using Splat;

namespace Test
{{
    public static class DIRegister
    {{
        static DIRegister()
        {{
            SplatRegistrations.{_testMethod}<ITest, TestConcrete>();
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

            return TestFail(source);
        }

        [Fact]
        public Task ConstructionAndNonPublicPropertySetterInjectionFail()
        {
            var source = @$"
using System;
using Splat;

namespace Test
{{
    public static class DIRegister
    {{
        static DIRegister()
        {{
            SplatRegistrations.{_testMethod}<ITest, TestConcrete>();
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

            return TestFail(source);
        }

        [Fact]
        public Task ConstructionAndInternalPropertyInjection()
        {
            var source = @$"
using System;
using Splat;

namespace Test
{{
    public static class DIRegister
    {{
        static DIRegister()
        {{
            SplatRegistrations.{_testMethod}<ITest, TestConcrete>();
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

            return TestPass(source);
        }


        [Fact]
        public Task ConstructionAndInternalSetterPropertyInjection()
        {
            var source = @$"
using System;
using Splat;

namespace Test
{{
    public static class DIRegister
    {{
        static DIRegister()
        {{
            SplatRegistrations.{_testMethod}<ITest, TestConcrete>();
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

            return TestPass(source);
        }

        [Fact]
        public Task ConstructionAndMultiplePropertyInjection()
        {
            var source = @$"
using System;
using Splat;

namespace Test
{{
    public static class DIRegister
    {{
        static DIRegister()
        {{
            SplatRegistrations.{_testMethod}<ITest, TestConcrete>();
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

            return TestPass(source);
        }

        [Fact]
        public Task MultipleConstructorWithoutAttributeFail()
        {
            var source = @$"
using System;
using Splat;

namespace Test
{{
    public static class DIRegister
    {{
        static DIRegister()
        {{
            SplatRegistrations.{_testMethod}<ITest, TestConcrete>();
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

            return TestFail(source);
        }

        [Fact]
        public Task MultipleConstructorWithAttribute()
        {
            var source = @$"
using System;
using Splat;

namespace Test
{{
    public static class DIRegister
    {{
        static DIRegister()
        {{
            SplatRegistrations.{_testMethod}<ITest, TestConcrete>();
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

            return TestPass(source);
        }

        [Fact]
        public Task MultipleConstructorWithMultipleAttributesFail()
        {
            var source = @$"
using System;
using Splat;

namespace Test
{{
    public static class DIRegister
    {{
        static DIRegister()
        {{
            SplatRegistrations.{_testMethod}<ITest, TestConcrete>();
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

            return TestFail(source);
        }

        [Fact]
        public Task EmptyConstructor()
        {
            var source = @$"
using System;
using Splat;

namespace Test
{{
    public static class DIRegister
    {{
        static DIRegister()
        {{
            SplatRegistrations.{_testMethod}<ITest, TestConcrete>();
        }}
    }}

    public interface ITest {{ }}
    public class TestConcrete : ITest {{ }}
}}";

            return TestPass(source);
        }

        protected Task TestFail(string source, [CallerFilePath] string file = "")
        {
            var utility = new SourceGeneratorUtility(x => TestOutputHelper.WriteLine(x));

            GeneratorDriver? driver = null;

            Assert.Throws<InvalidOperationException>(() => utility.RunGenerator<Generator>(EventCompiler, out var _, out var _, out driver, source));

            return Verifier.Verify(driver, sourceFile: file);
        }

        protected Task TestPass(string source, [CallerFilePath] string file = "")
        {
            var utility = new SourceGeneratorUtility(x => TestOutputHelper.WriteLine(x));

            utility.RunGenerator<Generator>(EventCompiler, out var _, out var _, out var driver, source);

            return Verifier.Verify(driver, sourceFile: file);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                EventCompiler?.Dispose();
            }
        }
    }
}
