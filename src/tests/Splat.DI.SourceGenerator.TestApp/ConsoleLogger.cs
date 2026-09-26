// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace TestApp;

/// <summary>An <see cref="ILogger"/> that writes each message to a text writer, such as the console's output.</summary>
/// <param name="output">The writer each message is written to.</param>
[DebuggerDisplay("ConsoleLogger")]
public class ConsoleLogger(TextWriter output) : ILogger
{
    /// <summary>Logs a message with a [LOG] prefix.</summary>
    /// <param name="message">The message to log.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Log(string message) => output.WriteLine($"[LOG] {message}");
}
