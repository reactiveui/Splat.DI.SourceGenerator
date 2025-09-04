[![Build](https://github.com/reactiveui/Splat.DI.SourceGenerator/actions/workflows/ci-build.yml/badge.svg)](https://github.com/reactiveui/Splat.DI.SourceGenerator/actions/workflows/ci-build.yml) [![Pull Requests](https://img.shields.io/github/issues-pr/reactiveui/Splat.DI.SourceGenerator.svg)](https://github.com/reactiveui/Splat.DI.SourceGenerator/pulls) [![Issues](https://img.shields.io/github/issues/reactiveui/Splat.DI.SourceGenerator.svg)](https://github.com/reactiveui/Splat.DI.SourceGenerator/issues) ![License](https://img.shields.io/github/license/reactiveui/Splat.DI.SourceGenerator.svg) ![Size](https://img.shields.io/github/repo-size/reactiveui/Splat.DI.SourceGenerator.svg) [![codecov](https://codecov.io/gh/reactiveui/Splat.DI.SourceGenerator/branch/main/graph/badge.svg?token=dmQeHH4Us8)](https://codecov.io/gh/reactiveui/Splat.DI.SourceGenerator)

# Splat Source Generator

This project is a high-performance source generator that produces Splat-based registrations for both constructor and property injection using modern incremental source generation.

## ⚡ Performance Benefits

This generator uses Roslyn's modern incremental model with efficient pipeline chaining and immutable records, significantly improving Visual Studio performance by:

- **Caching intermediate results** - Only re-processes changed files instead of regenerating everything
- **Reducing memory usage** - Uses efficient data structures and avoids unnecessary allocations
- **Avoiding unnecessary re-computations** - Leverages Roslyn's caching to skip unchanged code paths
- **Providing immediate feedback** - Fast incremental compilation during editing

Compatible with Visual Studio 17.10+ and modern .NET development environments.

# Installation

## NuGet Packages

Make sure your project is using the newer `PackageReference` inside your CSPROJ. The older style is buggy and should be moved away from regardless. See here for discussions how to [upgrade](https://docs.microsoft.com/en-us/nuget/consume-packages/migrate-packages-config-to-package-reference).

Install the following packages:

| Name                          | Platform          | NuGet                            |
| ----------------------------- | ----------------- | -------------------------------- |
| [Splat.DependencyInjection.SourceGenerator][Core]       | Core - Library     | [![CoreBadge]][Core]             |


[Core]: https://www.nuget.org/packages/Splat.DependencyInjection.SourceGenerator/
[CoreBadge]:https://img.shields.io/nuget/v/Splat.DependencyInjection.SourceGenerator.svg

## What does it do?

Generates high-performance dependency injection registrations for Splat based on your constructors and properties. It uses modern incremental source generation instead of reflection, providing full native speed with excellent IDE performance.

## Installation
Include the following in your .csproj file

```xml
<PackageReference Include="Splat.DependencyInjection.SourceGenerator" Version="{latest version}" PrivateAssets="all" />
```

The `PrivateAssets` will prevent the Source generator from being inherited into other projects.

## How to use

### Registration

Register your dependencies using the `SplatRegistrations` class.

There are three main registration methods:

#### `Register<TInterface, TConcrete>()`
Generates a new instance each time. Use generic parameters, first for the interface type, second for the concrete type.

```cs
SplatRegistrations.Register<IMenuUseCase, MenuUseCase>();
SplatRegistrations.Register<IOtherDependency, OtherDependency>();
```

#### `RegisterLazySingleton<TInterface, TConcrete>()`
Creates a lazy singleton instance. Use generic parameters, first for the interface type, second for the concrete type.

```cs
SplatRegistrations.RegisterLazySingleton<IMessagesSqlDataSource, MessagesSqlDataSource>();
```

You can also specify thread safety mode:

```cs
SplatRegistrations.RegisterLazySingleton<IService, Service>(LazyThreadSafetyMode.ExecutionAndPublication);
```

#### `RegisterConstant<T>(instance)`
Registers a pre-created instance as a constant.

```cs
var config = new Configuration();
SplatRegistrations.RegisterConstant<IConfiguration>(config);
```

### Setup

You must call either `SplatRegistrations.SetupIOC()` or with the specialization `SplatRegistrations.SetupIOC(resolver)` once during your application start. This must be done in each assembly where you use SplatRegistrations.

```cs
// Use default Splat locator
SplatRegistrations.SetupIOC();

// Or use a specific resolver (mainly for unit tests)
SplatRegistrations.SetupIOC(customResolver);
```

### Constructor Injection

If there are more than one constructor use the `[DependencyInjectionConstructor]` attribute to signify which one should be used.

```cs
[DependencyInjectionConstructor]
public AuthApi(
    Lazy<IJsonService> jsonService,
    ILogService logService)
    : base(jsonService)
{
}
```

You don't need to decorate when there is only one constructor.

### Property Injection

Use the `[DependencyInjectionProperty]` above a property to be initialized. It must have a `public` or `internal` setter.

```cs
public class MySpecialClass
{
    [DependencyInjectionProperty]
    public IService MyService { get; set; }
    
    [DependencyInjectionProperty]
    internal IInternalService InternalService { get; set; }
}
```

### Contracts

You can use contracts (string-based registration keys) with any registration method:

```cs
SplatRegistrations.Register<IService, ServiceA>("ServiceA");
SplatRegistrations.Register<IService, ServiceB>("ServiceB");
SplatRegistrations.RegisterLazySingleton<IConfig, Config>("DefaultConfig");
SplatRegistrations.RegisterConstant<string>("MyValue", "MyContract");
```

## Architecture

This source generator uses modern Roslyn incremental generation techniques:

- **Incremental Pipeline**: Uses `IIncrementalGenerator` for optimal performance
- **Efficient Syntax Detection**: Only processes method calls that match registration patterns
- **Immutable Data Transfer**: Uses C# records for efficient data flow between pipeline stages
- **Cache-Friendly Design**: Pure transforms and value-based equality for maximum caching benefits
- **Memory Efficient**: Early filtering and minimal allocations in hot paths

The generator targets `netstandard2.0` and leverages PolySharp for modern C# language features while maintaining broad compatibility.