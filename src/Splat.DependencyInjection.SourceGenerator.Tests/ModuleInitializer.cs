// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

[assembly: NotInParallel]

/// <summary>
/// Initialize for the module.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1050:Declare types in namespaces", Justification = "By Design")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1110:Declare type inside namespace", Justification = "By Design")]
public static class ModuleInitializer
{
    /// <summary>
    /// Initializes the source generators.
    /// </summary>
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Initialize();
        VerifierSettings.UseSplitModeForUniqueDirectory();
    }
}
