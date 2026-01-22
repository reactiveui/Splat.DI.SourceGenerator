// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace TestApp;

/// <summary>
/// Provides a concrete implementation of <see cref="IService"/>.
/// </summary>
public class ServiceImplementation : IService
{
    /// <summary>
    /// Gets a greeting message from the service.
    /// </summary>
    /// <returns>A string containing "Hello from DI!".</returns>
    public string GetMessage() => "Hello from DI!";
}
