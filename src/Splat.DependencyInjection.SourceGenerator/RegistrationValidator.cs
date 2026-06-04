// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;

using Splat.DependencyInjection.SourceGenerator.Models;

namespace Splat.DependencyInjection.SourceGenerator;

/// <summary>
/// Validates the collected registrations and reports the SPLATDI diagnostics that require the full
/// registration graph (SPLATDI005, SPLATDI006, SPLATDI007) and therefore cannot be detected by the
/// per-type analyzers.
/// </summary>
internal static class RegistrationValidator
{
    /// <summary>
    /// Reports graph-level registration diagnostics.
    /// </summary>
    /// <param name="context">The source production context used to report diagnostics.</param>
    /// <param name="transients">The transient registrations.</param>
    /// <param name="lazySingletons">The lazy singleton registrations.</param>
    public static void ReportDiagnostics(
        SourceProductionContext context,
        ImmutableArray<TransientRegistrationInfo> transients,
        ImmutableArray<LazySingletonRegistrationInfo> lazySingletons)
    {
        var all = transients.Cast<RegistrationInfo>()
            .Concat(lazySingletons.Cast<RegistrationInfo>())
            .ToList();

        if (all.Count == 0)
        {
            return;
        }

        ReportDuplicateInterfaceRegistrations(context, all);
        ReportLazyParametersNotRegisteredLazy(context, all, lazySingletons);
        ReportCircularDependencies(context, all);
    }

    /// <summary>
    /// Reports SPLATDI006 when the same interface (and contract) is registered more than once.
    /// </summary>
    /// <param name="context">The source production context.</param>
    /// <param name="all">All registrations.</param>
    private static void ReportDuplicateInterfaceRegistrations(SourceProductionContext context, List<RegistrationInfo> all)
    {
        foreach (var group in all.GroupBy(r => (r.InterfaceTypeFullName, r.ContractValue)))
        {
            if (group.Count() <= 1)
            {
                continue;
            }

            foreach (var registration in group)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticWarnings.InterfaceRegisteredMultipleTimes,
                    registration.InvocationLocation,
                    registration.InterfaceTypeFullName));
            }
        }
    }

    /// <summary>
    /// Reports SPLATDI007 when a constructor takes a <see cref="System.Lazy{T}"/> parameter whose
    /// inner type is not registered via RegisterLazySingleton.
    /// </summary>
    /// <param name="context">The source production context.</param>
    /// <param name="all">All registrations.</param>
    /// <param name="lazySingletons">The lazy singleton registrations.</param>
    private static void ReportLazyParametersNotRegisteredLazy(
        SourceProductionContext context,
        List<RegistrationInfo> all,
        ImmutableArray<LazySingletonRegistrationInfo> lazySingletons)
    {
        var lazilyRegistered = new HashSet<string>();
        foreach (var lazySingleton in lazySingletons)
        {
            lazilyRegistered.Add(lazySingleton.InterfaceTypeFullName);
            lazilyRegistered.Add(lazySingleton.ConcreteTypeFullName);
        }

        foreach (var registration in all)
        {
            foreach (var parameter in registration.ConstructorParameters)
            {
                if (!parameter.IsLazy || parameter.LazyInnerType is null)
                {
                    continue;
                }

                if (lazilyRegistered.Contains(parameter.LazyInnerType))
                {
                    continue;
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticWarnings.LazyParameterNotRegisteredLazy,
                    registration.InvocationLocation,
                    registration.ConcreteTypeFullName,
                    parameter.LazyInnerType));
            }
        }
    }

    /// <summary>
    /// Reports SPLATDI005 for registrations whose concrete types form a circular dependency through
    /// their (non-lazy, non-collection) constructor parameters.
    /// </summary>
    /// <param name="context">The source production context.</param>
    /// <param name="all">All registrations.</param>
    private static void ReportCircularDependencies(SourceProductionContext context, List<RegistrationInfo> all)
    {
        // Map each requested type (interface or concrete) to the concrete that satisfies it.
        var requestedToConcrete = new Dictionary<string, string>();
        var concreteToLocation = new Dictionary<string, Location>();
        foreach (var registration in all)
        {
            requestedToConcrete[registration.InterfaceTypeFullName] = registration.ConcreteTypeFullName;
            requestedToConcrete[registration.ConcreteTypeFullName] = registration.ConcreteTypeFullName;
            if (!concreteToLocation.ContainsKey(registration.ConcreteTypeFullName))
            {
                concreteToLocation[registration.ConcreteTypeFullName] = registration.InvocationLocation;
            }
        }

        // Build the dependency graph among concrete types. Lazy and collection parameters defer
        // resolution and therefore break a cycle, so they are excluded.
        var graph = new Dictionary<string, HashSet<string>>();
        foreach (var registration in all)
        {
            if (!graph.TryGetValue(registration.ConcreteTypeFullName, out var dependencies))
            {
                dependencies = new HashSet<string>();
                graph[registration.ConcreteTypeFullName] = dependencies;
            }

            foreach (var parameter in registration.ConstructorParameters)
            {
                if (parameter.IsLazy || parameter.IsCollection)
                {
                    continue;
                }

                if (requestedToConcrete.TryGetValue(parameter.TypeFullName, out var dependencyConcrete))
                {
                    dependencies.Add(dependencyConcrete);
                }
            }
        }

        foreach (var concrete in FindNodesInCycles(graph))
        {
            if (concreteToLocation.TryGetValue(concrete, out var location))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticWarnings.ConstructorsMustNotHaveCircularDependency,
                    location));
            }
        }
    }

    /// <summary>
    /// Returns the set of graph nodes that participate in at least one cycle.
    /// </summary>
    /// <param name="graph">The dependency graph (concrete type to its concrete dependencies).</param>
    /// <returns>The concrete types that are part of a cycle.</returns>
    private static HashSet<string> FindNodesInCycles(Dictionary<string, HashSet<string>> graph)
    {
        var inCycle = new HashSet<string>();
        var state = new Dictionary<string, int>(); // 0 = unvisited, 1 = on stack, 2 = done.
        var stack = new List<string>();

        void Visit(string node)
        {
            state[node] = 1;
            stack.Add(node);

            if (graph.TryGetValue(node, out var dependencies))
            {
                foreach (var next in dependencies)
                {
                    if (!graph.ContainsKey(next))
                    {
                        continue;
                    }

                    var nextState = state.TryGetValue(next, out var value) ? value : 0;
                    if (nextState == 0)
                    {
                        Visit(next);
                    }
                    else if (nextState == 1)
                    {
                        var index = stack.LastIndexOf(next);
                        for (var i = index; i < stack.Count; i++)
                        {
                            inCycle.Add(stack[i]);
                        }
                    }
                }
            }

            stack.RemoveAt(stack.Count - 1);
            state[node] = 2;
        }

        foreach (var node in graph.Keys)
        {
            if (!state.ContainsKey(node))
            {
                Visit(node);
            }
        }

        return inCycle;
    }
}
