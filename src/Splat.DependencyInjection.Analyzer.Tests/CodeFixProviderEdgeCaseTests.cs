// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Splat.DependencyInjection.Analyzer.CodeFixes;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace Splat.DependencyInjection.Analyzer.Tests;

/// <summary>
/// Tests for edge cases in code fix providers.
/// </summary>
public class CodeFixProviderEdgeCaseTests
{
    /// <summary>
    /// Tests that ConstructorCodeFixProvider works when constructor has no leading trivia.
    /// This hits the specific branch in AddAttributeAsync.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task ConstructorCodeFix_NoLeadingTrivia()
    {
        const string code = """
            using Splat;
            public class Test {
                public Test() {}
                public Test(int i) {}
            }
            
            public class Startup {
                public void Configure() {
                    SplatRegistrations.Register<Test>();
                }
            }
            """;

        var fixedCode = await CodeFixTestHelper.ApplyCodeFixAsync<
            Analyzers.ConstructorAnalyzer,
            ConstructorCodeFixProvider>(code);

        await Assert.That(fixedCode).Contains("[DependencyInjectionConstructor]");
    }

    /// <summary>
    /// Tests that ConstructorCodeFixProvider works when constructor has existing attributes but no leading trivia.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Test]
    public async Task ConstructorCodeFix_ExistingAttribute_NoLeadingTrivia()
    {
        const string code = """
            using Splat;
            using System;
            public class Test {
                [Obsolete]public Test() {}
                public Test(int i) {}
            }

            public class Startup {
                public void Configure() {
                    SplatRegistrations.Register<Test>();
                }
            }
            """;

        var fixedCode = await CodeFixTestHelper.ApplyCodeFixAsync<
            Analyzers.ConstructorAnalyzer,
            ConstructorCodeFixProvider>(code);

        await Assert.That(fixedCode).Contains("[DependencyInjectionConstructor]");
        await Assert.That(fixedCode).Contains("[Obsolete]public Test()");
    }
}
