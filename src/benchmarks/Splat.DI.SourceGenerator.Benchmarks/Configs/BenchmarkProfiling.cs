// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Splat.DI.SourceGenerator.Benchmarks.Configs;

/// <summary>Reads whether the benchmark config attaches its profiler.</summary>
/// <remarks>
/// An A/B comparison measures timing only. It sets <c>BENCHMARK_PROFILERS</c> to <c>false</c>, which skips the
/// second pass the profiler adds to every benchmark.
/// </remarks>
public static class BenchmarkProfiling
{
    /// <summary>The environment variable that turns the profiler off when set to <c>false</c>.</summary>
    private const string VariableName = "BENCHMARK_PROFILERS";

    /// <summary>Gets a value indicating whether the profiler is attached.</summary>
    public static bool Enabled =>
        !string.Equals(Environment.GetEnvironmentVariable(VariableName), "false", StringComparison.OrdinalIgnoreCase);
}
