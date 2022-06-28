[![Build](https://github.com/reactiveui/Splat.DI.SourceGenerator/actions/workflows/ci-build.yml/badge.svg)](https://github.com/reactiveui/Splat.DI.SourceGenerator/actions/workflows/ci-build.yml) [![Pull Requests](https://img.shields.io/github/issues-pr/reactiveui/Splat.DI.SourceGenerator.svg)](https://github.com/reactiveui/Splat.DI.SourceGenerator/pulls) [![Issues](https://img.shields.io/github/issues/reactiveui/Splat.DI.SourceGenerator.svg)](https://github.com/reactiveui/Splat.DI.SourceGenerator/issues) ![License](https://img.shields.io/github/license/reactiveui/Splat.DI.SourceGenerator.svg) ![Size](https://img.shields.io/github/repo-size/reactiveui/Splat.DI.SourceGenerator.svg) [![codecov](https://codecov.io/gh/reactiveui/Splat.DI.SourceGenerator/branch/main/graph/badge.svg?token=dmQeHH4Us8)](https://codecov.io/gh/reactiveui/Splat.DI.SourceGenerator)

# Splat Source Generator

This project is a source generator which produces Splat based registrations for both constructor and property injection.

# Installation

## NuGet Packages

Make sure your project is using the newer `PackageReference` inside your CSPROJ. The older style is buggy and should be moved away from regardless. See here for discussions how to [upgrade](https://docs.microsoft.com/en-us/nuget/consume-packages/migrate-packages-config-to-package-reference).

Install the following packages:

| Name                          | Platform          | NuGet                            |
| ----------------------------- | ----------------- | -------------------------------- |
| [Splat.DependencyInjection.SourceGenerator][Core]       | Core - Libary     | [![CoreBadge]][Core]             |


[Core]: https://www.nuget.org/packages/Splat.DependencyInjection.SourceGenerator/
[CoreBadge]:https://img.shields.io/nuget/v/Splat.DependencyInjection.SourceGenerator.svg

## What does it do?

ObservableEvents generator registrations for Splat based on your constructors and properties. It will not use reflection and instead uses Source Generation. You should get full native speed.

## Installation
Include the following in your .csproj file

```xml
<PackageReference Include="Splat.DependencyInjection.SourceGenerator" Version="{latest version}" PrivateAssets="all" />
```

The `PrivateAssets` will prevent the Source generator from being inherited into other projects.

## How to use

### Registration

Register your dependencies using the `SplatRegistrations` class.

There are two methods. 

`Register()` will generate a new instance each time. Use generic parameters, first for the interface type, second for the concrete type.

```cs
    SplatRegistrations.Register<IMenuUseCase, MenuUseCase>();
    SplatRegistrations.Register<IOtherDependency, OtherDependency>();
```

`RegisterLazySingleton()` will have a lazy instance. Use generic parameters, first for the interface type, second for the concrete type.

```cs
    SplatRegistrations.RegisterLazySingleton<IMessagesSqlDataSource, MessagesSqlDataSource>();
```

You must call either `SplatRegistrations.SetupIOC()` or with the specialisation `SplatRegistrations.SetupIOC(resolver)` once during your application start. This must be done in each assembly where you use SplatRegistrations.

The resolver version of `SetupIOC` is used mainly for unit tests.

### Constructor Injection
If there are more than one constructor use the `[DependencyInjectionConstructor]` attribute to signify which one should be used.

```cs
    [DependencyInjectionConstructor]
    public AuthApi(
        Lazy<IJsonService> jsonService,
        : base(jsonService)
    {
    }
```

You don't need to decorate when there is only one constructor. 

### Property Injection

Use the `[DependencyInjectionProperty]` above a property to be initialized. It must be `public` or `internal` setter.

```cs
public class MySpecialClass
{
    [DependencyInjectionProperty]
    public IService MyService { get; set; }
}
