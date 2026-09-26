// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.Tracing;

using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;

using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing.Parsers;

namespace Splat.DI.SourceGenerator.Benchmarks.Configs;

/// <summary>The memory diagnoser, and an EventPipe trace of CPU samples and verbose GC events.</summary>
/// <remarks>
/// A config keeps one EventPipe profiler, and the CPU and GC profiles both enable the runtime provider. So the
/// sample profiler is added beside the <see cref="EventPipeProfile.GcVerbose"/> profile, and the runtime provider
/// carries the keywords of both. The trace's allocation ticks carry stacks, which is what attributes an allocation
/// to the generator rather than to Roslyn.
/// </remarks>
public class ProfilerConfig : ManualConfig
{
    /// <summary>The provider that samples CPU stacks.</summary>
    private const string SampleProfilerProviderName = "Microsoft-DotNETCore-SampleProfiler";

    /// <summary>The runtime keywords of both profiles: the CPU profile's defaults, which include GC, plus GC handles.</summary>
    private const ClrTraceEventParser.Keywords RuntimeKeywords =
        ClrTraceEventParser.Keywords.Default | ClrTraceEventParser.Keywords.GCHandle;

    /// <summary>Initializes a new instance of the <see cref="ProfilerConfig"/> class.</summary>
    public ProfilerConfig()
    {
        Add(DefaultConfig.Instance);
        _ = AddDiagnoser(MemoryDiagnoser.Default);
        _ = AddColumn(CategoriesColumn.Default);
        _ = AddExporter(MarkdownExporter.GitHub);

        if (!BenchmarkProfiling.Enabled)
        {
            return;
        }

        EventPipeProvider[] providers =
        [
            new(SampleProfilerProviderName, EventLevel.Informational),
            new(ClrTraceEventParser.ProviderName, EventLevel.Verbose, (long)RuntimeKeywords),
        ];

        _ = AddDiagnoser(new EventPipeProfiler(EventPipeProfile.GcVerbose, providers));
    }
}
