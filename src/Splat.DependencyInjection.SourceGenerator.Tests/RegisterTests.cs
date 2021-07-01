using System;
using System.Threading.Tasks;

using VerifyXunit;

using Xunit;
using Xunit.Abstractions;

namespace Splat.DependencyInjection.SourceGenerator.Tests
{
    [UsesVerify]
    public sealed class RegisterTests : TestBase
    {
        public RegisterTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper, "Register")
        {
        }
    }
}
