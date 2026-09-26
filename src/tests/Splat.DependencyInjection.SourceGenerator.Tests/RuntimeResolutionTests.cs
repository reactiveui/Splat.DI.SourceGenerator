// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Splat.DependencyInjection.SourceGenerator.Tests;

/// <summary>Runs registrations the generator wrote into this test assembly against Splat, as an application would.</summary>
/// <remarks>
/// The test project also runs the generator as an analyzer, so the calls in <see cref="Declare"/> generate this
/// assembly's own <c>SetupIOCInternal</c>.
/// </remarks>
public sealed class RuntimeResolutionTests
{
    /// <summary>The contract <see cref="View"/> is registered under; the generated registrations read it.</summary>
    internal const string ViewContract = "V1";

    /// <summary>
    /// A service registered under a contract resolves when its dependency is registered without one: the contract
    /// names the service's registration, not its dependencies' (issue #305).
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ResolvesContractedServiceWithUncontractedDependency()
    {
        using var resolver = new ModernDependencyResolver();
        SplatRegistrations.SetupIOC(resolver);

        var view = resolver.GetService<ViewBase>(ViewContract);

        await Assert.That(view).IsTypeOf<View>();
        await Assert.That(((View)view!).Service).IsNotNull();
    }

    /// <summary>Declares the registrations; the generator reads the calls, which do nothing at run time.</summary>
    internal static void Declare()
    {
        SplatRegistrations.Register<Service>();
        SplatRegistrations.Register<ViewBase, View>(ViewContract);
    }

    /// <summary>A dependency registered without a contract.</summary>
    [System.Diagnostics.DebuggerDisplay("{Name}")]
    public sealed class Service
    {
        /// <summary>Gets the name of the service.</summary>
        public string Name => nameof(Service);
    }

    /// <summary>The type a view is registered as.</summary>
    [System.Diagnostics.DebuggerDisplay("{Name}")]
    public class ViewBase
    {
        /// <summary>Gets the name of the view.</summary>
        public virtual string Name => nameof(ViewBase);
    }

    /// <summary>A view registered under a contract, taking a dependency registered without one.</summary>
    /// <param name="service">The dependency.</param>
    [System.Diagnostics.DebuggerDisplay("{Name}")]
    public sealed class View(Service service) : ViewBase
    {
        /// <inheritdoc/>
        public override string Name => nameof(View);

        /// <summary>Gets the dependency.</summary>
        public Service Service { get; } = service;
    }
}
