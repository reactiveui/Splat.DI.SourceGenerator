// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace TestApp;

/// <summary>
/// A service that demonstrates constructor dependency injection.
/// </summary>
public class ServiceWithDependency
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceWithDependency"/> class.
    /// </summary>
    /// <param name="logger">The logger instance to use for logging operations.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
    public ServiceWithDependency(ILogger logger)
    {
        _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Performs work and logs the operation using the injected logger.
    /// </summary>
    public void DoWork()
    {
        _logger.Log("Working...");
    }
}
