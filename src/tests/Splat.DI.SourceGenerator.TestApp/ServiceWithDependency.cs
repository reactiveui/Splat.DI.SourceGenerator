// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace TestApp;

/// <summary>A service that demonstrates constructor dependency injection.</summary>
/// <param name="logger">The logger instance to use for logging operations.</param>
[DebuggerDisplay("ServiceWithDependency: {_logger}")]
public class ServiceWithDependency(ILogger logger)
{
    /// <summary>The logger the work is reported to.</summary>
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>Performs work and logs the operation using the injected logger.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoWork() => _logger.Log("Working...");
}
