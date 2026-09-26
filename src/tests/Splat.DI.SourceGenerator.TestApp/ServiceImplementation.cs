// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace TestApp;

/// <summary>Provides a concrete implementation of <see cref="IService"/>.</summary>
public class ServiceImplementation : IService
{
    /// <summary>Gets a greeting message from the service.</summary>
    /// <returns>A string containing "Hello from DI!".</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string GetMessage() => "Hello from DI!";
}
