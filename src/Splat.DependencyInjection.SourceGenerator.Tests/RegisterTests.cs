// Copyright (c) 2019-2021 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using VerifyXunit;

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
