// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;

namespace Splat.DI.SourceGenerator.Benchmarks.Runtime;

/// <summary>Measures the code the generator wrote for <see cref="RuntimeRegistrations"/>.</summary>
/// <remarks>
/// <see cref="Setup"/> is what an application pays once at start-up. The <c>Resolve</c> cases are what it pays each
/// time it asks the resolver for a service, which runs the factory the generator wrote. Every figure includes
/// Splat's own lookup, which the generated code does not control; read one case against another.
/// </remarks>
[DebuggerDisplay("RuntimeBenchmarks")]
public class RuntimeBenchmarks : IDisposable
{
    /// <summary>The resolver the generated registrations were made on.</summary>
    private ModernDependencyResolver _resolver = null!;

    /// <summary>The resolver <see cref="Setup"/> made last.</summary>
    private ModernDependencyResolver? _lastSetup;

    /// <summary>Makes the registrations once for the resolve cases.</summary>
    /// <exception cref="InvalidOperationException">The generated registrations do not resolve.</exception>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _resolver = new();
        SplatRegistrations.SetupIOC(_resolver);
        if (_resolver.GetService<ITree>() is not Tree { Injected: not null } || _resolver.GetService<IKeyed>(RuntimeRegistrations.Contract) is null)
        {
            throw new InvalidOperationException("The generated registrations did not resolve.");
        }
    }

    /// <summary>Makes every registration on a new resolver.</summary>
    /// <returns>The resolver.</returns>
    [Benchmark]
    public ModernDependencyResolver Setup()
    {
        _lastSetup = new();
        SplatRegistrations.SetupIOC(_lastSetup);
        return _lastSetup;
    }

    /// <summary>Resolves a service with no dependencies.</summary>
    /// <returns>The service.</returns>
    [Benchmark(Baseline = true)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ILeaf? ResolveLeaf() => _resolver.GetService<ILeaf>();

    /// <summary>Resolves a service with one constructor dependency.</summary>
    /// <returns>The service.</returns>
    [Benchmark]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IBranch? ResolveBranch() => _resolver.GetService<IBranch>();

    /// <summary>Resolves a service with constructor, collection and property dependencies.</summary>
    /// <returns>The service.</returns>
    [Benchmark]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ITree? ResolveTree() => _resolver.GetService<ITree>();

    /// <summary>Resolves a lazy singleton after its first creation.</summary>
    /// <returns>The service.</returns>
    [Benchmark]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ISingleton? ResolveSingleton() => _resolver.GetService<ISingleton>();

    /// <summary>Resolves a service that takes a lazy singleton.</summary>
    /// <returns>The service.</returns>
    [Benchmark]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ILazyConsumer? ResolveLazyConsumer() => _resolver.GetService<ILazyConsumer>();

    /// <summary>Resolves a service and its dependency under a contract.</summary>
    /// <returns>The service.</returns>
    [Benchmark]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IKeyed? ResolveKeyed() => _resolver.GetService<IKeyed>(RuntimeRegistrations.Contract);

    /// <summary>Releases the resolvers.</summary>
    [GlobalCleanup]
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases the resolvers.</summary>
    /// <param name="disposing">Whether the call comes from <see cref="Dispose()"/> rather than a finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        _resolver.Dispose();
        _lastSetup?.Dispose();
    }
}
