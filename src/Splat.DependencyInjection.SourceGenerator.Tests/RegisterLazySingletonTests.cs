using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ReactiveMarbles.SourceGenerator.TestNuGetHelper.Compilation;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

namespace Splat.DependencyInjection.SourceGenerator.Tests
{
    [UsesVerify]
    public class RegisterLazySingletonTests : TestBase
    {
        public RegisterLazySingletonTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper, "RegisterLazySingleton")
        {
        }
    }
}
