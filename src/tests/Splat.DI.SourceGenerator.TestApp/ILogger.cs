// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace TestApp;

/// <summary>Defines a logger contract for writing log messages.</summary>
public interface ILogger
{
    /// <summary>Logs a message to the output.</summary>
    /// <param name="message">The message to log.</param>
    void Log(string message);
}
