// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace TestApp;

/// <summary>
/// Defines a service contract for retrieving messages.
/// </summary>
public interface IService
{
    /// <summary>
    /// Gets a message from the service.
    /// </summary>
    /// <returns>A string containing the service message.</returns>
    string GetMessage();
}
