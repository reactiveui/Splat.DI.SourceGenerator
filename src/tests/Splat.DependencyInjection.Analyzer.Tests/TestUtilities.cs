// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis;

namespace Splat.DependencyInjection.Analyzer.Tests;

/// <summary>Utilities for testing analyzers and code fixes.</summary>
internal static class TestUtilities
{
    /// <summary>
    /// Normalizes whitespace and line endings in source code for comparison.
    /// Removes leading/trailing whitespace per line and normalizes to LF line endings.
    /// </summary>
    /// <param name="source">The source code to normalize.</param>
    /// <returns>Normalized source code suitable for comparison.</returns>
    internal static string NormalizeWhitespace(string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        var lines = source.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        for (var i = 0; i < lines.Length; i++)
        {
            lines[i] = lines[i].Trim();
        }

        return string.Join('\n', lines);
    }

    /// <summary>
    /// Compares two source code strings with whitespace normalization.
    /// Ignores differences in line endings and per-line leading/trailing whitespace.
    /// Writes expected and actual to the current test's output when they don't match for easier debugging.
    /// </summary>
    /// <param name="expected">The expected source code.</param>
    /// <param name="actual">The actual source code.</param>
    /// <returns>True if the sources are equivalent after normalization.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool AreEquivalent(string expected, string actual) =>
        AreEquivalent(expected, actual, TestContext.Current?.OutputWriter ?? TextWriter.Null);

    /// <summary>
    /// Compares two source code strings with whitespace normalization.
    /// Ignores differences in line endings and per-line leading/trailing whitespace.
    /// Writes expected and actual to <paramref name="output"/> when they don't match for easier debugging.
    /// </summary>
    /// <param name="expected">The expected source code.</param>
    /// <param name="actual">The actual source code.</param>
    /// <param name="output">The writer that receives the failure details.</param>
    /// <returns>True if the sources are equivalent after normalization.</returns>
    internal static bool AreEquivalent(string expected, string actual, TextWriter output)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expected);
        ArgumentException.ThrowIfNullOrWhiteSpace(actual);
        ArgumentNullException.ThrowIfNull(output);
        var result = NormalizeWhitespace(expected) == NormalizeWhitespace(actual);

        if (!result)
        {
            output.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            output.WriteLine("║ TEST FAILURE: Source code comparison failed                  ║");
            output.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
            output.WriteLine();
            output.WriteLine("=== EXPECTED ===");
            output.WriteLine(expected);
            output.WriteLine();
            output.WriteLine("=== ACTUAL ===");
            output.WriteLine(actual);
            output.WriteLine();
            output.WriteLine("═══════════════════════════════════════════════════════════════");
        }

        return result;
    }

    /// <summary>Gets the first difference between two normalized source code strings for debugging.</summary>
    /// <param name="expected">The expected source code.</param>
    /// <param name="actual">The actual source code.</param>
    /// <returns>Description of the first difference, or empty string if sources are equivalent.</returns>
    internal static string GetFirstDifference(string expected, string actual)
    {
        var normalizedExpected = NormalizeWhitespace(expected);
        var normalizedActual = NormalizeWhitespace(actual);

        if (normalizedExpected == normalizedActual)
        {
            return string.Empty;
        }

        var expectedLines = normalizedExpected.Split('\n');
        var actualLines = normalizedActual.Split('\n');

        for (int i = 0; i < Math.Max(expectedLines.Length, actualLines.Length); i++)
        {
            var expectedLine = i < expectedLines.Length ? expectedLines[i] : string.Empty;
            var actualLine = i < actualLines.Length ? actualLines[i] : string.Empty;

            if (expectedLine != actualLine)
            {
                return $"Line {i + 1} differs:\nExpected: {expectedLine}\nActual: {actualLine}";
            }
        }

        return "Sources differ but no specific line difference found";
    }

    /// <summary>Finds the loaded System.Runtime assembly, if any.</summary>
    /// <returns>The System.Runtime assembly, or <see langword="null"/> when it is not loaded.</returns>
    internal static Assembly? FindSystemRuntimeAssembly()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.GetName().Name == "System.Runtime")
            {
                return assembly;
            }
        }

        return null;
    }

    /// <summary>Gets the first descendant node of the requested type.</summary>
    /// <typeparam name="TNode">The syntax node type to find.</typeparam>
    /// <param name="root">The node to search beneath.</param>
    /// <returns>The first matching descendant.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no matching node exists.</exception>
    internal static TNode FirstDescendant<TNode>(SyntaxNode root)
        where TNode : SyntaxNode
    {
        ArgumentNullException.ThrowIfNull(root);
        foreach (var node in root.DescendantNodes())
        {
            if (node is TNode match)
            {
                return match;
            }
        }

        throw new InvalidOperationException($"No {typeof(TNode).Name} found.");
    }

    /// <summary>Gets the first method with the given name declared on a type.</summary>
    /// <param name="type">The type to search.</param>
    /// <param name="name">The method name.</param>
    /// <returns>The first matching method.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no matching method exists.</exception>
    internal static IMethodSymbol FirstMethod(INamespaceOrTypeSymbol type, string name)
    {
        ArgumentNullException.ThrowIfNull(type);
        foreach (var member in type.GetMembers(name))
        {
            if (member is IMethodSymbol method)
            {
                return method;
            }
        }

        throw new InvalidOperationException($"No method named {name} found.");
    }

    /// <summary>Gets the first constructor with the given number of parameters.</summary>
    /// <param name="type">The type to search.</param>
    /// <param name="parameterCount">The number of parameters the constructor must have.</param>
    /// <returns>The first matching constructor.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no matching constructor exists.</exception>
    internal static IMethodSymbol ConstructorWithParameterCount(INamedTypeSymbol type, int parameterCount)
    {
        ArgumentNullException.ThrowIfNull(type);
        foreach (var constructor in type.Constructors)
        {
            if (constructor.Parameters.Length == parameterCount)
            {
                return constructor;
            }
        }

        throw new InvalidOperationException($"No constructor with {parameterCount} parameters found.");
    }

    /// <summary>Gets the first constructor that is written in source rather than implicitly declared.</summary>
    /// <param name="type">The type to search.</param>
    /// <returns>The first explicitly declared constructor.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no explicit constructor exists.</exception>
    internal static IMethodSymbol FirstExplicitConstructor(INamedTypeSymbol type)
    {
        ArgumentNullException.ThrowIfNull(type);
        foreach (var constructor in type.Constructors)
        {
            if (!constructor.IsImplicitlyDeclared)
            {
                return constructor;
            }
        }

        throw new InvalidOperationException("No explicit constructor found.");
    }
}
