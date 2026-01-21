[![Build](https://github.com/reactiveui/Splat.DI.SourceGenerator/actions/workflows/ci-build.yml/badge.svg)](https://github.com/reactiveui/Splat.DI.SourceGenerator/actions/workflows/ci-build.yml)
[![Code Coverage](https://codecov.io/gh/reactiveui/Splat.DI.SourceGenerator/branch/main/graph/badge.svg?token=dmQeHH4Us8)](https://codecov.io/gh/reactiveui/Splat.DI.SourceGenerator)
[![NuGet](https://img.shields.io/nuget/v/Splat.DependencyInjection.SourceGenerator.svg)](https://www.nuget.org/packages/Splat.DependencyInjection.SourceGenerator/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Splat.DependencyInjection.SourceGenerator.svg)](https://www.nuget.org/packages/Splat.DependencyInjection.SourceGenerator/)
<br>
<a href="https://reactiveui.net/slack">
        <img src="https://img.shields.io/badge/chat-slack-blue.svg">
</a>

# Splat Dependency Injection Source Generator

A high-performance C# source generator that produces compile-time dependency injection registrations for [Splat](https://github.com/reactiveui/splat). Eliminates runtime reflection, provides full native AOT support, and includes intelligent analyzers with automatic code fixes.

## What does it do?

This source generator produces dependency injection registrations for Splat at compile-time based on your constructor and property injection requirements. It uses an incremental source generator to provide fast builds with zero runtime reflection overhead.

Key features:
- Zero reflection - All registrations generated at compile-time
- Native AOT compatible - Works with trimming and AOT compilation
- Built-in analyzers - Real-time diagnostics and automatic code fixes
- Constructor injection - Automatic dependency resolution
- Property injection - Attribute-based property initialization
- Lazy singletons - Thread-safe lazy initialization with configurable modes
- Contract support - Named registrations for multiple implementations
- Incremental compilation - Fast builds that only regenerate when needed

## How do I install?

[Always Be NuGetting](https://nuget.org/packages/Splat.DependencyInjection.SourceGenerator/). Package contains:

| Package                          | NuGet                            |
| -------------------------------- | -------------------------------- |
| [Splat.DependencyInjection.SourceGenerator][Core] | [![CoreBadge]][Core] |

[Core]: https://www.nuget.org/packages/Splat.DependencyInjection.SourceGenerator/
[CoreBadge]: https://img.shields.io/nuget/v/Splat.DependencyInjection.SourceGenerator.svg

### Requirements

- .NET SDK: Any version supporting C# 7.3 or later
- Target Frameworks: .NET Framework 4.6.2+, .NET 8+, .NET 9+, .NET 10+
- Splat: Version 19.1.1 or later (supports modern generic-first resolvers)

### Installation

Add the package to your project:

```xml
<PackageReference Include="Splat.DependencyInjection.SourceGenerator" Version="{latest version}" PrivateAssets="all" />
```

Note: The `PrivateAssets="all"` attribute prevents the source generator from being transitively referenced by projects that depend on yours. This is the recommended configuration for source generators.

## How to Use

### Register Your Dependencies

Use the `SplatRegistrations` static class to register services. The source generator will detect these calls and generate the implementation at compile-time.

**Transient Registration (New Instance Each Time)**

```csharp
using static Splat.SplatRegistrations;

// Register with interface and implementation
Register<IToaster, Toaster>();
Register<IMessageService, MessageService>();

// Register concrete type only (when no interface)
Register<DatabaseContext>();
```

**Lazy Singleton Registration (Single Lazy Instance)**

```csharp
// Basic lazy singleton
RegisterLazySingleton<IDatabase, SqliteDatabase>();

// With thread safety mode
RegisterLazySingleton<ICache, MemoryCache>(LazyThreadSafetyMode.PublicationOnly);
```

Thread safety modes:
- `LazyThreadSafetyMode.ExecutionAndPublication` (default) - Full thread safety with locks
- `LazyThreadSafetyMode.PublicationOnly` - Multiple threads may initialize, first wins
- `LazyThreadSafetyMode.None` - No thread safety (single-threaded scenarios only)

**Constant Registration (Pre-Created Instance)**

```csharp
// Register an existing instance
var config = new Configuration { ApiUrl = "https://api.example.com" };
RegisterConstant<IConfiguration>(config);
```

**Named Contracts (Multiple Implementations)**

```csharp
// Register multiple implementations with different contracts
Register<ILogger, FileLogger>("file");
Register<ILogger, ConsoleLogger>("console");
Register<ILogger, CloudLogger>("cloud");

// Retrieve by contract
var fileLogger = resolver.GetService<ILogger>("file");
```

### Initialize the Container

Call `SetupIOC()` once during application startup in each assembly that uses `SplatRegistrations`:

```csharp
using Splat;
using static Splat.SplatRegistrations;

// In your application entry point
public class App
{
    public void ConfigureServices()
    {
        // Register all dependencies
        Register<IUserService, UserService>();
        Register<IAuthService, AuthService>();
        RegisterLazySingleton<IDatabase, AppDatabase>();

        // Initialize the container (generates and executes registrations)
        SetupIOC();
    }
}
```

For unit tests, pass a custom resolver:

```csharp
[Test]
public void TestDependencies()
{
    var resolver = new ModernDependencyResolver();
    SetupIOC(resolver); // Use test-specific resolver

    var service = resolver.GetService<IUserService>();
    Assert.NotNull(service);
}
```

### Constructor Injection

The source generator automatically resolves constructor parameters.

**Single Constructor**

```csharp
public class UserService : IUserService
{
    private readonly IDatabase _database;
    private readonly ILogger _logger;

    // Automatically detected - no attribute needed
    public UserService(IDatabase database, ILogger logger)
    {
        _database = database;
        _logger = logger;
    }
}
```

**Multiple Constructors**

Use `[DependencyInjectionConstructor]` to specify which constructor to use:

```csharp
using static Splat.SplatRegistrations;

public class AuthService : IAuthService
{
    private readonly IDatabase _database;
    private readonly ILogger _logger;

    // Empty constructor for testing
    public AuthService()
    {
        _database = new InMemoryDatabase();
        _logger = new NullLogger();
    }

    // Production constructor - marked for DI
    [DependencyInjectionConstructor]
    public AuthService(IDatabase database, ILogger logger)
    {
        _database = database;
        _logger = logger;
    }
}
```

If you forget the attribute with multiple constructors, the analyzer will warn you and offer a code fix to add it automatically.

**Lazy Dependencies**

Inject `Lazy<T>` for on-demand initialization:

```csharp
public class ExpensiveService
{
    private readonly Lazy<IDatabase> _database;

    public ExpensiveService(Lazy<IDatabase> database)
    {
        _database = database; // Not initialized yet
    }

    public void DoWork()
    {
        // Database initialized only when first accessed
        _database.Value.ExecuteQuery("...");
    }
}

// Register the dependency as a lazy singleton
RegisterLazySingleton<IDatabase, AppDatabase>();
Register<IExpensiveService, ExpensiveService>();
```

### Property Injection

Mark properties with `[DependencyInjectionProperty]` for initialization after construction.

```csharp
using static Splat.SplatRegistrations;

public class ViewModelBase
{
    // Property injection - must have public or internal setter
    [DependencyInjectionProperty]
    public INavigationService Navigation { get; set; }

    [DependencyInjectionProperty]
    public ILogger Logger { get; internal set; } // Internal setters supported
}
```

The analyzer will:
- Warn if property doesn't have a public/internal setter
- Offer code fix to change `private set` to `public set` or `internal set`
- Offer code fix to add missing setter to read-only properties

### Complete Example

```csharp
using Splat;
using static Splat.SplatRegistrations;

// Models
public interface IDatabase { }
public interface ILogger { }
public interface IUserService { }

public class SqliteDatabase : IDatabase { }
public class FileLogger : ILogger { }

public class UserService : IUserService
{
    private readonly IDatabase _database;

    // Constructor injection
    public UserService(IDatabase database)
    {
        _database = database;
    }

    // Property injection
    [DependencyInjectionProperty]
    public ILogger Logger { get; set; }
}

// Application startup
public class Program
{
    public static void Main()
    {
        // Register dependencies
        RegisterLazySingleton<IDatabase, SqliteDatabase>();
        Register<ILogger, FileLogger>();
        Register<IUserService, UserService>();

        // Initialize container
        SetupIOC();

        // Resolve services
        var userService = Locator.Current.GetService<IUserService>();
    }
}
```

## Built-in Analyzers and Code Fixes

The package includes intelligent analyzers that provide real-time feedback:

| Diagnostic ID | Severity | Description | Code Fix |
|--------------|----------|-------------|----------|
| SPLATDI001 | Warning | Multiple constructors without `[DependencyInjectionConstructor]` attribute | Adds attribute to selected constructor |
| SPLATDI002 | Error | Property with `[DependencyInjectionProperty]` lacks accessible setter | Changes setter to `public` or `internal` |
| SPLATDI003 | Error | Multiple constructors marked with `[DependencyInjectionConstructor]` | Manual fix required |
| SPLATDI004 | Error | Constructor marked with `[DependencyInjectionConstructor]` is not accessible | Changes to `public` or `internal` |

The analyzer detects issues in real-time and offers automatic fixes via Quick Actions (Ctrl+. or Cmd+.).

## How It Works

The source generator follows a four-step process:

1. Compile-Time Detection - Scans for `SplatRegistrations.Register()` calls during compilation
2. Metadata Extraction - Analyzes constructor parameters and property injection requirements
3. Code Generation - Generates optimized registration code with no reflection
4. Incremental Builds - Only regenerates when relevant code changes

Generated code example:

```csharp
// Generated by Splat.DependencyInjection.SourceGenerator
static partial void SetupIOCInternal(IDependencyResolver resolver)
{
    // Transient registration
    resolver.Register<IUserService>(() => new UserService(
        (IDatabase)resolver.GetService(typeof(IDatabase)),
        (ILogger)resolver.GetService(typeof(ILogger))
    ) {
        Navigation = (INavigationService)resolver.GetService(typeof(INavigationService))
    });

    // Lazy singleton registration
    {
        var lazy = new Lazy<IDatabase>(() => new SqliteDatabase(),
            LazyThreadSafetyMode.ExecutionAndPublication);
        resolver.Register<Lazy<IDatabase>>(() => lazy);
        resolver.Register<IDatabase>(() => lazy.Value);
    }
}
```

## Performance Benefits

Compared to reflection-based DI:
- Approximately 100x faster registration execution (no runtime reflection)
- Approximately 10-100x faster incremental builds (only processes changed files)
- Full AOT support (works with Native AOT and trimming)
- Zero runtime overhead (all work done at compile-time)

## Troubleshooting

**Generator doesn't seem to run?**

Ensure you called `SetupIOC()` in your startup code. The generator only produces code for assemblies that use `SplatRegistrations`.

**"Multiple constructors" warning?**

Add `[DependencyInjectionConstructor]` to the constructor you want used. Use the Quick Fix to add automatically.

**Property injection not working?**

Ensure the property has `[DependencyInjectionProperty]` and a `public` or `internal` setter. The analyzer will warn if the setter is missing or inaccessible.

**Lazy dependencies not resolving?**

Make sure you registered the dependency with `RegisterLazySingleton`, not `Register`. Only lazy singletons can be injected as `Lazy<T>`.

## Migration from Version 1.x to 2.x

Version 2.1.1 includes breaking changes:

- Requires Splat 19.1.1 or later for generic-first resolver support
- Migrated from legacy `ISourceGenerator` to modern `IIncrementalGenerator`
- Updated to Roslyn 4.14.0
- Removed support for .NET Standard 2.0 and .NET 6
- Minimum supported frameworks: .NET Framework 4.6.2+, .NET 8+, .NET 9+, .NET 10+

New features in 2.1.1:
- 10-100x faster incremental builds (only processes changed files)
- Cache-friendly pipeline eliminates unnecessary recompilation
- Built-in analyzers with real-time diagnostics and automatic code fixes
- Full Native AOT and trimming support with generic-first API

## Support

If you have questions or need help:

- Check existing [GitHub Issues](https://github.com/reactiveui/Splat.DI.SourceGenerator/issues)
- Ask on [Stack Overflow](https://stackoverflow.com/questions/tagged/splat) with the `splat` tag
- Join our [Slack community](https://reactiveui.net/slack)

Please do not open GitHub issues for general support questions.

## Contribute

We welcome contributions! Here's how you can help:

1. Report Issues: Found a bug? [Open an issue](https://github.com/reactiveui/Splat.DI.SourceGenerator/issues/new)
2. Submit PRs: Improvements are always welcome
3. Documentation: Help improve our examples and docs
4. Testing: Add test cases for edge scenarios

See our [contribution guidelines](CONTRIBUTING.md) for details.

## Sponsorship

The core team members and contributors work on this project in their free time. If Splat.DI.SourceGenerator increases your productivity, please consider supporting the project:

[Become a sponsor](https://github.com/sponsors/reactivemarbles)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
