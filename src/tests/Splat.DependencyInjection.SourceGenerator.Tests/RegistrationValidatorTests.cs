// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Splat.DependencyInjection.SourceGenerator.Tests;

/// <summary>Tests the diagnostics that need the whole registration graph.</summary>
public sealed class RegistrationValidatorTests
{
    /// <summary>The line of <see cref="Template"/> the registration calls are written after.</summary>
    private const string Marker = "// registrations";

    /// <summary>The file the registration calls are written into, after <see cref="Marker"/>.</summary>
    private const string Template = """
        using System;
        using System.Collections.Generic;
        using R = Splat.SplatRegistrations;

        namespace T
        {
            public interface IA { }
            public interface IB { }
            public sealed class A : IA { }
            public sealed class B : IB { }
            public sealed class LazyA : IB { }

            public interface ILazyUser { }
            public sealed class LazyUser : ILazyUser { public LazyUser(Lazy<IA> a) { } }

            public interface ICycle1 { }
            public interface ICycle2 { }
            public sealed class Cycle1 : ICycle1 { public Cycle1(ICycle2 other) { } }
            public sealed class Cycle2 : ICycle2 { public Cycle2(ICycle1 other) { } }

            public interface ISelf { }
            public interface ISelfAlias { }
            public sealed class Self : ISelf, ISelfAlias { public Self(ISelf self) { } }

            public interface ITop { }
            public interface ILeft { }
            public interface IRight { }
            public sealed class Top : ITop { public Top(ILeft left, IRight right) { } }
            public sealed class Left : ILeft { public Left(IA a) { } }
            public sealed class Right : IRight { public Right(IA a) { } }

            public interface ITwice { }
            public sealed class Twice : ITwice { public Twice(IA first, IA second) { } }

            public interface IMissing { }
            public sealed class NeedsMissing : IB { public NeedsMissing(IMissing missing) { } }

            public interface ICollector { }
            public sealed class Collector : ICollector { public Collector(IEnumerable<IA> all) { } }

            public static class Bootstrapper
            {
                public static void Register()
                {
                    // registrations
                }
            }
        }
        """;

    /// <summary>The diagnostics reported for a set of registrations, in the order they are reported.</summary>
    /// <param name="registrations">The registration calls, one per line.</param>
    /// <param name="expected">The diagnostic IDs, each with the number of the registration line it is reported at.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("R.Register<IA, A>();", "")]
    [Arguments("R.Register<IA, A>(); R.Register<IA, A>(\"c\");", "")]
    [Arguments("R.Register<IA, A>();\nR.Register<IA, A>();", "SPLATDI006@1 SPLATDI006@2")]
    [Arguments("R.RegisterLazySingleton<IA, A>();\nR.Register<IB, B>();\nR.Register<IA, A>();", "SPLATDI006@3 SPLATDI006@1")]
    [Arguments("R.Register<IA, A>();\nR.Register<IB, B>();\nR.Register<IA, A>();\nR.Register<IB, B>();", "SPLATDI006@1 SPLATDI006@3 SPLATDI006@2 SPLATDI006@4")]
    [Arguments("R.Register<ILazyUser, LazyUser>();", "SPLATDI007@1")]
    [Arguments("R.Register<ILazyUser, LazyUser>();\nR.RegisterLazySingleton<IA, A>();", "")]
    [Arguments("R.Register<ILazyUser, LazyUser>();\nR.RegisterLazySingleton<A>();", "SPLATDI007@1")]
    [Arguments("R.Register<ILazyUser, LazyUser>();\nR.RegisterLazySingleton<IB, LazyA>();", "SPLATDI007@1")]
    [Arguments("R.Register<ICycle1, Cycle1>();\nR.Register<ICycle2, Cycle2>();", "SPLATDI005@1 SPLATDI005@2")]
    [Arguments("R.Register<ISelf, Self>();", "SPLATDI005@1")]
    [Arguments("R.Register<ISelf, Self>();\nR.Register<ISelfAlias, Self>();", "SPLATDI005@1")]
    [Arguments("R.Register<ITop, Top>();\nR.Register<ILeft, Left>();\nR.Register<IRight, Right>();\nR.Register<IA, A>();", "")]
    [Arguments("R.Register<ITwice, Twice>();\nR.Register<IA, A>();", "")]
    [Arguments("R.Register<IB, NeedsMissing>();", "")]
    [Arguments("R.Register<ICollector, Collector>();\nR.Register<IA, A>();", "")]
    public async Task ReportsGraphDiagnostics(string registrations, string expected)
    {
        var markerAt = Template.IndexOf(Marker, StringComparison.Ordinal);
        var firstLine = CountLines(Template, markerAt) + 1;
        var source = Template.Insert(markerAt + Marker.Length, $"\n{registrations}");

        var run = TestHelper.Run(source);

        await Assert.That(run.DiagnosticLines(firstLine)).IsEqualTo(expected);
    }

    /// <summary>Counts the line breaks before a position.</summary>
    /// <param name="text">The text.</param>
    /// <param name="end">The position.</param>
    /// <returns>The zero-based line of the position.</returns>
    private static int CountLines(string text, int end)
    {
        var lines = 0;
        for (var i = 0; i < end; i++)
        {
            if (text[i] == '\n')
            {
                lines++;
            }
        }

        return lines;
    }
}
