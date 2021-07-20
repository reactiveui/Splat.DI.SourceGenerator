// Copyright (c) 2019-2021 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

using NuGet.LibraryModel;
using NuGet.Versioning;

using ReactiveMarbles.NuGet.Helpers;
using ReactiveMarbles.SourceGenerator.TestNuGetHelper.Compilation;

using VerifyTests;

using VerifyXunit;

using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Splat.DependencyInjection.SourceGenerator.Tests
{
    public class TestHelper : IDisposable
    {
#pragma warning disable CS0618 // Type or member is obsolete
        private static readonly LibraryRange _splatLibrary = new("Splat", VersionRange.AllStableFloating, LibraryDependencyTarget.Package);
#pragma warning restore CS0618 // Type or member is obsolete

        public TestHelper(ITestOutputHelper testOutput)
        {
            TestOutputHelper = testOutput ?? throw new ArgumentNullException(nameof(testOutput));
        }

        protected EventBuilderCompiler? EventCompiler { get; private set; }

        protected ITestOutputHelper TestOutputHelper { get; private set; }

        public async Task InitializeAsync()
        {
            var targetFrameworks = "netstandard2.0".ToFrameworks();

            var inputGroup = await NuGetPackageHelper.DownloadPackageFilesAndFolder(_splatLibrary, targetFrameworks, packageOutputDirectory: null).ConfigureAwait(false);

            var framework = targetFrameworks[0];
            EventCompiler = new(inputGroup, inputGroup, framework);
        }

        public Task TestFail(string source, string contractParameter, [CallerFilePath] string file = "")
        {
            if (EventCompiler is null)
            {
                throw new InvalidOperationException("Must have valid compiler instance.");
            }

            var utility = new SourceGeneratorUtility(x => TestOutputHelper.WriteLine(x));

            GeneratorDriver? driver = null;

            Assert.Throws<InvalidOperationException>(() => utility.RunGenerator<Generator>(EventCompiler, out _, out _, out driver, source));

            VerifySettings settings = new();
            settings.UseParameters(contractParameter);
            settings.AutoVerify();
            return Verifier.Verify(driver, settings, sourceFile: file);
        }

        public Task TestPass(string source, string contractParameter, [CallerFilePath] string file = "")
        {
            var driver = Generate(source);
            VerifySettings settings = new();
            settings.UseParameters(contractParameter);
            return Verifier.Verify(driver, settings, sourceFile: file);
        }

        public Task TestPass(string source, string contractParameter, LazyThreadSafetyMode mode, [CallerFilePath] string file = "")
        {
            var driver = Generate(source);

            VerifySettings settings = new();
            settings.UseParameters(contractParameter, mode);
            return Verifier.Verify(driver, settings, sourceFile: file);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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

            utility.RunGenerator<Generator>(EventCompiler, out _, out _, out var driver, source);

            return driver;
        }
    }
}
