// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Linq;

namespace Splat.DependencyInjection.Analyzer.Tests;

/// <summary>
/// Utilities for testing analyzers and code fixes.
/// </summary>
public static class TestUtilities
{
    /// <summary>
    /// Normalizes whitespace and line endings in source code for comparison.
    /// Removes leading/trailing whitespace per line and normalizes to LF line endings.
    /// </summary>
    /// <param name="source">The source code to normalize.</param>
    /// <returns>Normalized source code suitable for comparison.</returns>
    public static string NormalizeWhitespace(string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        return string.Join(
            "\n",
            source.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None)
                .Select(line => line.Trim()));
    }

    /// <summary>
    /// Compares two source code strings with whitespace normalization.
    /// Ignores differences in line endings and per-line leading/trailing whitespace.
    /// Outputs expected and actual to console when they don't match for easier debugging.
    /// </summary>
    /// <param name="expected">The expected source code.</param>
    /// <param name="actual">The actual source code.</param>
    /// <returns>True if the sources are equivalent after normalization.</returns>
    public static bool AreEquivalent(string expected, string actual)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expected);
        ArgumentException.ThrowIfNullOrWhiteSpace(actual);
        var result = NormalizeWhitespace(expected) == NormalizeWhitespace(actual);

        if (!result)
        {
            System.Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            System.Console.WriteLine("║ TEST FAILURE: Source code comparison failed                  ║");
            System.Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
            System.Console.WriteLine();
            System.Console.WriteLine("=== EXPECTED ===");
            System.Console.WriteLine(expected);
            System.Console.WriteLine();
            System.Console.WriteLine("=== ACTUAL ===");
            System.Console.WriteLine(actual);
            System.Console.WriteLine();
            System.Console.WriteLine("═══════════════════════════════════════════════════════════════");
        }

        return result;
    }

    /// <summary>
    /// Gets the first difference between two normalized source code strings for debugging.
    /// </summary>
    /// <param name="expected">The expected source code.</param>
    /// <param name="actual">The actual source code.</param>
    /// <returns>Description of the first difference, or empty string if sources are equivalent.</returns>
    public static string GetFirstDifference(string expected, string actual)
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
}
