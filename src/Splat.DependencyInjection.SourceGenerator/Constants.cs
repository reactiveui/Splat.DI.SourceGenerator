// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Splat.DependencyInjection.SourceGenerator;

/// <summary>The names the generator and the analyzers match against, and the names of the generated files.</summary>
/// <remarks>Linked into the analyzer project, so it holds constants only.</remarks>
internal static class Constants
{
    /// <summary>The class name of the SplatRegistrations marker class.</summary>
    public const string ClassName = "SplatRegistrations";

    /// <summary>The namespace containing the SplatRegistrations class.</summary>
    public const string NamespaceName = "Splat";

    /// <summary>Fully qualified display string for the DependencyInjectionConstructorAttribute.</summary>
    public const string ConstructorAttribute = "global::Splat.DependencyInjectionConstructorAttribute";

    /// <summary>Metadata name for resolving the DependencyInjectionConstructorAttribute via compilation.</summary>
    public const string ConstructorAttributeMetadataName = "Splat.DependencyInjectionConstructorAttribute";

    /// <summary>Metadata name for resolving the DependencyInjectionPropertyAttribute via compilation.</summary>
    public const string PropertyAttributeMetadataName = "Splat.DependencyInjectionPropertyAttribute";

    /// <summary>Short name of the DependencyInjectionConstructor attribute (without namespace).</summary>
    public const string ConstructorAttributeShortName = "DependencyInjectionConstructor";

    /// <summary>Method name for transient registrations.</summary>
    public const string MethodNameRegister = "Register";

    /// <summary>Method name for lazy singleton registrations.</summary>
    public const string MethodNameRegisterLazySingleton = "RegisterLazySingleton";

    /// <summary>Parameter name for the contract key in registration methods.</summary>
    public const string ParameterNameContract = "contract";

    /// <summary>Parameter name for the thread safety mode in lazy singleton methods.</summary>
    public const string ParameterNameMode = "mode";

    /// <summary>Metadata name for resolving <see cref="System.Lazy{T}"/> via compilation.</summary>
    public const string LazyMetadataName = "System.Lazy`1";

    /// <summary>Metadata name for resolving IEnumerable via compilation.</summary>
    public const string EnumerableMetadataName = "System.Collections.Generic.IEnumerable`1";

    /// <summary>Metadata name for resolving <see cref="System.Threading.LazyThreadSafetyMode"/> via compilation.</summary>
    public const string LazyThreadSafetyModeMetadataName = "System.Threading.LazyThreadSafetyMode";

    /// <summary>File name for the generated marker methods and attributes source.</summary>
    public const string ExtensionMethodFileName = "Splat.DI.g.cs";

    /// <summary>File name for the generated registration implementation source.</summary>
    public const string RegistrationFileName = "Splat.DI.Reg.g.cs";
}
