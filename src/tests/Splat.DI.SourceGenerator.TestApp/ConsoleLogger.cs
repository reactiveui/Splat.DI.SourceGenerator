// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace TestApp;

/// <summary>
/// Provides a console-based implementation of <see cref="ILogger"/>.
/// </summary>
public class ConsoleLogger : ILogger
{
    /// <summary>
    /// Logs a message to the console output with a [LOG] prefix.
    /// </summary>
    /// <param name="message">The message to log to the console.</param>
    public void Log(string message) => Console.WriteLine($"[LOG] {message}");
}
