// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using Splat.DependencyInjection.SourceGenerator.Models;

namespace Splat.DependencyInjection.SourceGenerator;

/// <summary>
/// Reports the diagnostics that need the whole registration graph - SPLATDI005, SPLATDI006 and SPLATDI007 - which the
/// per-type analyzers cannot see.
/// </summary>
/// <remarks>
/// Each check first looks for anything to report and returns before building a lookup when there is nothing, which is
/// the case for a project that builds cleanly.
/// </remarks>
internal static class RegistrationValidator
{
    /// <summary>The DFS state of a node on the current path.</summary>
    private const int OnPath = 1;

    /// <summary>The DFS state of a node whose descendants are all visited.</summary>
    private const int Visited = 2;

    /// <summary>The number of registrations that makes a service registered more than once.</summary>
    private const int Duplicate = 2;

    /// <summary>The entries each registration adds to the lookup of constructed types: its service and its type.</summary>
    private const int NamesPerRegistration = 2;

    /// <summary>Reports the graph diagnostics.</summary>
    /// <param name="context">The context diagnostics are reported to.</param>
    /// <param name="sites">The registrations and where they are made, in source order.</param>
    internal static void ReportDiagnostics(SourceProductionContext context, ImmutableArray<RegistrationSite?> sites)
    {
        if (sites.IsEmpty)
        {
            return;
        }

        var ordered = TransientsFirst(sites);
        ReportDuplicateRegistrations(context, ordered);
        ReportLazyParametersNotRegisteredLazy(context, ordered);
        ReportCircularDependencies(context, ordered);
    }

    /// <summary>Orders the registrations the way the generated code makes them: transients, then lazy singletons.</summary>
    /// <param name="sites">The registrations in source order.</param>
    /// <returns>The registrations in registration order.</returns>
    /// <remarks>Returns the input unchanged, without copying, when it is already in that order.</remarks>
    internal static ImmutableArray<RegistrationSite?> TransientsFirst(ImmutableArray<RegistrationSite?> sites)
    {
        var sawLazySingleton = false;
        var needsReorder = false;
        foreach (var site in sites)
        {
            var isLazySingleton = site!.Registration.Kind == RegistrationKind.LazySingleton;
            needsReorder |= sawLazySingleton && !isLazySingleton;
            sawLazySingleton |= isLazySingleton;
        }

        if (!needsReorder)
        {
            return sites;
        }

        var builder = ImmutableArray.CreateBuilder<RegistrationSite?>(sites.Length);
        AddKind(builder, sites, RegistrationKind.Transient);
        AddKind(builder, sites, RegistrationKind.LazySingleton);
        return builder.MoveToImmutable();
    }

    /// <summary>Reports SPLATDI006 at every registration of a type and contract that is registered more than once.</summary>
    /// <param name="context">The context diagnostics are reported to.</param>
    /// <param name="sites">The registrations in registration order.</param>
    internal static void ReportDuplicateRegistrations(in SourceProductionContext context, ImmutableArray<RegistrationSite?> sites)
    {
        if (sites.Length < Duplicate)
        {
            return;
        }

        var counts = new Dictionary<(string Service, string? Contract), int>(sites.Length);
        var duplicated = false;
        foreach (var site in sites)
        {
            var key = Key(site!.Registration);
            _ = counts.TryGetValue(key, out var count);
            counts[key] = count + 1;
            duplicated |= count != 0;
        }

        if (!duplicated)
        {
            return;
        }

        // Each duplicated type is reported as a group, at the position of its first registration.
        for (var i = 0; i < sites.Length; i++)
        {
            var key = Key(sites[i]!.Registration);
            if (counts[key] < Duplicate)
            {
                continue;
            }

            counts[key] = 0;
            for (var j = i; j < sites.Length; j++)
            {
                var site = sites[j]!;
                if (Key(site.Registration) == key)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticWarnings.InterfaceRegisteredMultipleTimes,
                        site.Location.ToLocation(),
                        site.Registration.InterfaceTypeFullName));
                }
            }
        }
    }

    /// <summary>Reports SPLATDI007 for a <see cref="Lazy{T}"/> parameter whose type is not a lazy singleton.</summary>
    /// <param name="context">The context diagnostics are reported to.</param>
    /// <param name="sites">The registrations in registration order.</param>
    internal static void ReportLazyParametersNotRegisteredLazy(in SourceProductionContext context, ImmutableArray<RegistrationSite?> sites)
    {
        HashSet<string>? lazilyRegistered = null;
        foreach (var site in sites)
        {
            var registration = site!.Registration;
            var parameters = registration.ConstructorParameters;
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (parameter.Kind != DependencyKind.Lazy)
                {
                    continue;
                }

                lazilyRegistered ??= LazilyRegisteredTypes(sites);
                if (!lazilyRegistered.Contains(parameter.InnerTypeFullName!))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticWarnings.LazyParameterNotRegisteredLazy,
                        site.Location.ToLocation(),
                        registration.ConcreteTypeFullName,
                        parameter.InnerTypeFullName));
                }
            }
        }
    }

    /// <summary>
    /// Reports SPLATDI005, once per type, at the first registration of each constructed type that takes part in a
    /// dependency cycle through its constructor.
    /// </summary>
    /// <param name="context">The context diagnostics are reported to.</param>
    /// <param name="sites">The registrations in registration order.</param>
    /// <remarks>
    /// A lazy or collection parameter defers its resolution until after construction, so it cannot close a cycle and
    /// is left out of the graph.
    /// </remarks>
    internal static void ReportCircularDependencies(in SourceProductionContext context, ImmutableArray<RegistrationSite?> sites)
    {
        if (!HasServiceParameter(sites))
        {
            return;
        }

        // Each type a constructor can ask for - a registered service or a constructed type - maps to the type
        // constructed for it.
        var constructedFor = new Dictionary<string, string>(sites.Length * NamesPerRegistration, StringComparer.Ordinal);
        var firstLocation = new Dictionary<string, LocationInfo>(sites.Length, StringComparer.Ordinal);
        foreach (var site in sites)
        {
            var registration = site!.Registration;
            constructedFor[registration.InterfaceTypeFullName] = registration.ConcreteTypeFullName;
            constructedFor[registration.ConcreteTypeFullName] = registration.ConcreteTypeFullName;
            if (!firstLocation.ContainsKey(registration.ConcreteTypeFullName))
            {
                firstLocation.Add(registration.ConcreteTypeFullName, site.Location);
            }
        }

        var graph = BuildGraph(sites, constructedFor);
        foreach (var concreteType in FindNodesInCycles(graph))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticWarnings.ConstructorsMustNotHaveCircularDependency,
                firstLocation[concreteType].ToLocation()));
        }
    }

    /// <summary>Builds the graph of constructed types, each pointing at the constructed types it depends on.</summary>
    /// <param name="sites">The registrations in registration order.</param>
    /// <param name="constructedFor">The constructed type for each type a constructor can ask for.</param>
    /// <returns>Every constructed type, in registration order, with its dependencies; <see langword="null"/> for none.</returns>
    internal static Dictionary<string, List<string>?> BuildGraph(ImmutableArray<RegistrationSite?> sites, Dictionary<string, string> constructedFor)
    {
        var graph = new Dictionary<string, List<string>?>(sites.Length, StringComparer.Ordinal);
        foreach (var site in sites)
        {
            var registration = site!.Registration;
            _ = graph.TryGetValue(registration.ConcreteTypeFullName, out var dependencies);
            var parameters = registration.ConstructorParameters;
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (parameter.Kind == DependencyKind.Service
                    && constructedFor.TryGetValue(parameter.TypeFullName, out var dependency)
                    && (dependencies is null || !dependencies.Contains(dependency)))
                {
                    (dependencies ??= []).Add(dependency);
                }
            }

            graph[registration.ConcreteTypeFullName] = dependencies;
        }

        return graph;
    }

    /// <summary>Finds the nodes that lie on at least one cycle.</summary>
    /// <param name="graph">The graph, each node pointing at nodes of the same graph.</param>
    /// <returns>The nodes on a cycle, in the order the search reached them.</returns>
    /// <remarks>
    /// A depth-first search from each node in turn. Reaching a node still on the path closes a cycle, and every node
    /// from it to the top of the path lies on that cycle.
    /// </remarks>
    internal static List<string> FindNodesInCycles(Dictionary<string, List<string>?> graph)
    {
        var inCycle = new List<string>();
        var state = new Dictionary<string, int>(graph.Count, StringComparer.Ordinal);
        var path = new List<string>();

        foreach (var node in graph.Keys)
        {
            if (!state.ContainsKey(node))
            {
                Visit(node);
            }
        }

        return inCycle;

        void Visit(string node)
        {
            state[node] = OnPath;
            path.Add(node);

            var dependencies = graph[node];
            if (dependencies is not null)
            {
                foreach (var next in dependencies)
                {
                    _ = state.TryGetValue(next, out var nextState);
                    if (nextState == 0)
                    {
                        Visit(next);
                    }
                    else if (nextState == OnPath)
                    {
                        AddCycle(path, next, inCycle);
                    }
                }
            }

            path.RemoveAt(path.Count - 1);
            state[node] = Visited;
        }
    }

    /// <summary>Adds the nodes of a cycle the search has closed.</summary>
    /// <param name="path">The search's path, from its root to the node that reached <paramref name="start"/>.</param>
    /// <param name="start">The node on the path that the cycle returns to.</param>
    /// <param name="inCycle">The nodes found on a cycle so far, in the order found.</param>
    internal static void AddCycle(List<string> path, string start, List<string> inCycle)
    {
        for (var i = path.LastIndexOf(start); i < path.Count; i++)
        {
            if (!inCycle.Contains(path[i]))
            {
                inCycle.Add(path[i]);
            }
        }
    }

    /// <summary>Tests whether any constructor takes a service directly, the only kind of edge a cycle can use.</summary>
    /// <param name="sites">The registrations.</param>
    /// <returns><see langword="true"/> when a constructor takes a service directly.</returns>
    internal static bool HasServiceParameter(ImmutableArray<RegistrationSite?> sites)
    {
        foreach (var site in sites)
        {
            var parameters = site!.Registration.ConstructorParameters;
            for (var i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].Kind == DependencyKind.Service)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>Collects the types <c>RegisterLazySingleton</c> registers, as services and as constructed types.</summary>
    /// <param name="sites">The registrations.</param>
    /// <returns>The types.</returns>
    internal static HashSet<string> LazilyRegisteredTypes(ImmutableArray<RegistrationSite?> sites)
    {
        var types = new HashSet<string>(StringComparer.Ordinal);
        foreach (var site in sites)
        {
            var registration = site!.Registration;
            if (registration.Kind != RegistrationKind.LazySingleton)
            {
                continue;
            }

            _ = types.Add(registration.InterfaceTypeFullName);
            _ = types.Add(registration.ConcreteTypeFullName);
        }

        return types;
    }

    /// <summary>Adds the registrations of one kind, in order.</summary>
    /// <param name="builder">The builder to add to.</param>
    /// <param name="sites">The registrations.</param>
    /// <param name="kind">The kind to add.</param>
    private static void AddKind(ImmutableArray<RegistrationSite?>.Builder builder, ImmutableArray<RegistrationSite?> sites, RegistrationKind kind)
    {
        foreach (var site in sites)
        {
            if (site!.Registration.Kind != kind)
            {
                continue;
            }

            builder.Add(site);
        }
    }

    /// <summary>Gets the key two registrations of the same service share.</summary>
    /// <param name="registration">The registration.</param>
    /// <returns>The service type and contract.</returns>
    private static (string Service, string? Contract) Key(RegistrationInfo registration) =>
        (registration.InterfaceTypeFullName, registration.ContractValue);
}
