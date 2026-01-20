# Splat.DI.SourceGenerator: AOT-Compatible Dependency Injection Source Generator

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Prerequisites and Environment Setup
- **CRITICAL**: Requires .NET 8.0, 9.0, and 10.0 SDKs (all three required for full build). Install with:
  ```bash
  # Install .NET 8.0
  curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version latest --channel 8.0
  # Install .NET 9.0
  curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version latest --channel 9.0
  # Install .NET 10.0
  curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version latest --channel 10.0
  export PATH="$HOME/.dotnet:$PATH"

  # Check installations
  dotnet --info
  ```
- **Platform Support**: This project builds on Windows, Linux, and macOS with cross-platform support.
- **Development Tools**: Visual Studio 2022, VS Code with C# extension, or JetBrains Rider.
- **Roslyn Version**: Uses Microsoft.CodeAnalysis 4.14.0 for IIncrementalGenerator support.
- **Testing Framework**: TUnit with Microsoft.Testing.Platform (not xUnit).
- **Snapshot Testing**: Uses Verify.SourceGenerators for snapshot-based testing.
- **Note on Cloning the Repository**:
  When cloning the Splat.DI.SourceGenerator repository, use a full clone instead of a shallow one (e.g., avoid --depth=1). This project uses Nerdbank.GitVersioning for automatic version calculation based on Git history. Shallow clones lack the necessary commit history, which can cause build errors or force the tool to perform an extra fetch step to deepen the repository. To ensure smooth builds:
   ```bash
   git clone https://github.com/reactiveui/Splat.DI.SourceGenerator.git
   ```
   If you've already done a shallow clone, deepen it with:
   ```bash
   git fetch --unshallow
   ```
   This prevents exceptions like "Shallow clone lacks the objects required to calculate version height."

### Development Workflow
- Full solution restore and build:
  ```bash
  cd src
  dotnet restore Splat.DI.SourceGenerator.slnx
  dotnet build Splat.DI.SourceGenerator.slnx --configuration Release
  ```
  Build time: **30-60 seconds**. Set timeout to 5+ minutes for full solution builds.

- **Note:** This project uses the modern `.slnx` (XML-based solution file) format instead of the legacy `.sln` format. The `.slnx` format provides better performance, cleaner diffs, and improved tooling support in Visual Studio 2022 17.10+.

- Individual project builds (faster for development):
  ```bash
  cd src
  dotnet build Splat.DependencyInjection.SourceGenerator/Splat.DependencyInjection.SourceGenerator.csproj --configuration Release
  dotnet build Splat.DependencyInjection.Analyzer/Splat.DependencyInjection.Analyzer.csproj --configuration Release
  ```

- **Source Generator Development Workflow**:
  - **Incremental generators** must use value-equatable POCOs (no ISymbol/SyntaxNode in pipeline outputs)
  - **Code generation** uses string interpolation (not SyntaxFactory) for performance
  - **Analyzer project** is separate from generator project following Roslyn best practices
  - **Generic-first API** ensures AOT compatibility and eliminates boxing

### Testing (Microsoft Testing Platform with TUnit)

This project uses **Microsoft Testing Platform (MTP)** with the **TUnit** testing framework. Test commands differ significantly from traditional VSTest.

See: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test?tabs=dotnet-test-with-mtp

**CRITICAL:** The working folder must be the `./src` folder. These commands won't function properly without the correct working folder.

**IMPORTANT:**
- Do NOT use `--no-build` flag when running tests. Always build before testing to ensure all code changes (including test changes) are compiled. Using `--no-build` can cause tests to run against stale binaries and produce misleading results.
- Use `--output Detailed` to see Console.WriteLine output from tests. This must be placed BEFORE any `--` separator:
  ```bash
  dotnet test --output Detailed -- --treenode-filter "..."
  ```

#### Test Commands

```bash
# Run all tests in the solution
cd src
dotnet test --solution Splat.DI.SourceGenerator.slnx -c Release

# Run all tests in a specific project
dotnet test --project Splat.DependencyInjection.Analyzer.Tests/Splat.DependencyInjection.Analyzer.Tests.csproj -c Release
dotnet test --project Splat.DependencyInjection.SourceGenerator.Tests/Splat.DependencyInjection.SourceGenerator.Tests.csproj -c Release

# Run a single test method using treenode-filter
# Syntax: /{AssemblyName}/{Namespace}/{ClassName}/{TestMethodName}
dotnet test --project Splat.DependencyInjection.Analyzer.Tests/Splat.DependencyInjection.Analyzer.Tests.csproj -- --treenode-filter "/*/*/*/MyTestMethod"

# Run all tests in a specific class
dotnet test --project Splat.DependencyInjection.Analyzer.Tests/Splat.DependencyInjection.Analyzer.Tests.csproj -- --treenode-filter "/*/*/MyClassName/*"

# Run tests in a specific namespace
dotnet test --project Splat.DependencyInjection.SourceGenerator.Tests/Splat.DependencyInjection.SourceGenerator.Tests.csproj -- --treenode-filter "/*/MyNamespace/*/*"

# Filter by test property (e.g., Category)
dotnet test --solution Splat.DI.SourceGenerator.slnx -- --treenode-filter "/*/*/*/*[Category=Integration]"

# Run tests with code coverage (Microsoft Code Coverage)
dotnet test --solution Splat.DI.SourceGenerator.slnx -- --coverage --coverage-output-format cobertura

# Run tests with detailed output
dotnet test --solution Splat.DependencyInjection.SourceGenerator.sln -- --output Detailed

# List all available tests without running them
dotnet test --project Splat.DependencyInjection.Analyzer.Tests/Splat.DependencyInjection.Analyzer.Tests.csproj -- --list-tests

# Fail fast (stop on first failure)
dotnet test --solution Splat.DependencyInjection.SourceGenerator.sln -- --fail-fast

# Control parallel test execution
dotnet test --solution Splat.DependencyInjection.SourceGenerator.sln -- --maximum-parallel-tests 4

# Generate TRX report
dotnet test --solution Splat.DependencyInjection.SourceGenerator.sln -- --report-trx

# Disable logo for cleaner output
dotnet test --project Splat.DependencyInjection.Analyzer.Tests/Splat.DependencyInjection.Analyzer.Tests.csproj -- --disable-logo

# Combine options: coverage + TRX report + detailed output
dotnet test --solution Splat.DependencyInjection.SourceGenerator.sln -- --coverage --coverage-output-format cobertura --report-trx --output Detailed
```

#### Alternative: Using `dotnet run` for single project
```bash
# Run tests using dotnet run (easier for passing flags)
cd src
dotnet run --project Splat.DependencyInjection.Analyzer.Tests/Splat.DependencyInjection.Analyzer.Tests.csproj -c Release -- --treenode-filter "/*/*/*/MyTest"

# Disable logo for cleaner output
dotnet run --project Splat.DependencyInjection.SourceGenerator.Tests/Splat.DependencyInjection.SourceGenerator.Tests.csproj -- --disable-logo --treenode-filter "/*/*/*/Test1"
```

#### TUnit Treenode-Filter Syntax

The `--treenode-filter` follows the pattern: `/{AssemblyName}/{Namespace}/{ClassName}/{TestMethodName}`

**Examples:**
- Single test: `--treenode-filter "/*/*/*/MyTestMethod"`
- All tests in class: `--treenode-filter "/*/*/MyClassName/*"`
- All tests in namespace: `--treenode-filter "/*/MyNamespace/*/*"`
- Filter by property: `--treenode-filter "/*/*/*/*[Category=Integration]"`
- Multiple wildcards: `--treenode-filter "/*/*/MyTests*/*"`

**Note:** Use single asterisks (`*`) to match segments. Double asterisks (`/**`) are not supported in treenode-filter.

#### Key TUnit Command-Line Flags

- `--treenode-filter` - Filter tests by path pattern or properties (syntax: `/{Assembly}/{Namespace}/{Class}/{Method}`)
- `--list-tests` - Display available tests without running
- `--fail-fast` - Stop after first failure
- `--maximum-parallel-tests` - Limit concurrent execution (default: processor count)
- `--coverage` - Enable Microsoft Code Coverage
- `--coverage-output-format` - Set coverage format (cobertura, xml, coverage)
- `--report-trx` - Generate TRX format reports
- `--output` - Control verbosity (Normal or Detailed)
- `--no-progress` - Suppress progress reporting
- `--disable-logo` - Remove TUnit logo display
- `--diagnostic` - Enable diagnostic logging (Trace level)
- `--timeout` - Set global test timeout
- `--reflection` - Enable reflection mode instead of source generation

See https://tunit.dev/docs/reference/command-line-flags for complete TUnit flag reference.

#### Key Configuration Files

- `src/testconfig.json` - Configures test execution (`"parallel": false`) and code coverage (Cobertura format)
- `src/Directory.Build.props` - Enables `TestingPlatformDotnetTestSupport` for test projects

#### TUnit Testing Framework Specifics

- **Attributes**: Uses `[Test]` attribute instead of `[Fact]`/`[Theory]`
- **Lifecycle Hooks**: Uses `[Before(Test)]` and `[After(Test)]` hooks instead of IDisposable
- **Assertions**: Uses `await Assert.That(x).IsEqualTo(y)` instead of `Assert.Equal(y, x)`
- **Output**: No `ITestOutputHelper` - TUnit uses Microsoft.Testing.Platform for output
- **Parallel Execution**: Configured in `testconfig.json`

#### Snapshot Testing with Verify

- Tests use Verify.SourceGenerators for snapshot comparisons
- Snapshots are stored in `*.verified.cs` files
- Run `dotnet test` to validate snapshots
- To accept new snapshots temporarily:
  1. Enable `VerifierSettings.AutoVerify()` in `ModuleInitializer.cs`
  2. Run tests to accept all snapshots
  3. Disable `VerifierSettings.AutoVerify()` after accepting

## Validation and Quality Assurance

### Code Style and Analysis Enforcement
- **EditorConfig Compliance**: Repository uses a comprehensive `.editorconfig` with detailed rules for C# formatting, naming conventions, and code analysis.
- **StyleCop Analyzers**: Enforces consistent C# code style with `stylecop.analyzers`.
- **Roslynator Analyzers**: Additional code quality rules with `Roslynator.Analyzers`.
- **Analysis Level**: Set to `latest` with enhanced .NET analyzers enabled.
- **CRITICAL**: All code must comply with ReactiveUI contribution guidelines: [https://www.reactiveui.net/contribute/index.html](https://www.reactiveui.net/contribute/index.html).

## C# Style Guide
**General Rule**: Follow "Visual Studio defaults" with the following specific requirements:

### Brace Style
- **Allman style braces**: Each brace begins on a new line.
- **Single line statement blocks**: Can go without braces but must be properly indented on its own line and not nested in other statement blocks that use braces.
- **Exception**: A `using` statement is permitted to be nested within another `using` statement by starting on the following line at the same indentation level, even if the nested `using` contains a controlled block.

### Indentation and Spacing
- **Indentation**: Four spaces (no tabs).
- **Spurious free spaces**: Avoid, e.g., `if (someVar == 0)...` where dots mark spurious spaces.
- **Empty lines**: Avoid more than one empty line at any time between members of a type.
- **Labels**: Indent one less than the current indentation (for `goto` statements).

### Field and Property Naming
- **Internal and private fields**: Use `_camelCase` prefix with `readonly` where possible.
- **Static fields**: `readonly` should come after `static` (e.g., `static readonly` not `readonly static`).
- **Public fields**: Use PascalCasing with no prefix (use sparingly).
- **Constants**: Use PascalCasing for all constant local variables and fields (except interop code, where names and values must match the interop code exactly).
- **Fields placement**: Specify fields at the top within type declarations.

### Visibility and Modifiers
- **Always specify visibility**: Even if it's the default (e.g., `private string _foo` not `string _foo`).
- **Visibility first**: Should be the first modifier (e.g., `public abstract` not `abstract public`).
- **Modifier order**: `public`, `private`, `protected`, `internal`, `static`, `extern`, `new`, `virtual`, `abstract`, `sealed`, `override`, `readonly`, `unsafe`, `volatile`, `async`.

### Namespace and Using Statements
- **Namespace imports**: At the top of the file, outside of `namespace` declarations.
- **Sorting**: System namespaces alphabetically first, then third-party namespaces alphabetically.
- **Global using directives**: Use where appropriate to reduce repetition across files.
- **Placement**: Use `using` directives outside `namespace` declarations.

### Type Usage and Variables
- **Language keywords**: Use instead of BCL types (e.g., `int`, `string`, `float` instead of `Int32`, `String`, `Single`) for type references and method calls (e.g., `int.Parse` instead of `Int32.Parse`).
- **var usage**: Encouraged for large return types or refactoring scenarios; use full type names for clarity when needed.
- **this. avoidance**: Avoid `this.` unless absolutely necessary.
- **nameof(...)**: Use instead of string literals whenever possible and relevant.

### Code Patterns and Features
- **Method groups**: Use where appropriate.
- **Pattern matching**: Use C# 7+ pattern matching, including recursive, tuple, positional, type, relational, and list patterns for expressive conditional logic.
- **Inline out variables**: Use C# 7 inline variable feature with `out` parameters.
- **Non-ASCII characters**: Use Unicode escape sequences (`\uXXXX`) instead of literal characters to avoid garbling by tools or editors.
- **Modern C# features (C# 8–12)**:
  - Enable nullable reference types to reduce null-related errors.
  - Use ranges (`..`) and indices (`^`) for concise collection slicing.
  - Employ `using` declarations for automatic resource disposal.
  - Declare static local functions to avoid state capture.
  - Prefer switch expressions over statements for concise control flow.
  - Use records and record structs for data-centric types with value semantics.
  - Apply init-only setters for immutable properties.
  - Utilize target-typed `new` expressions to reduce verbosity.
  - Declare static anonymous functions or lambdas to prevent state capture.
  - Use file-scoped namespace declarations for concise syntax.
  - Apply `with` expressions for nondestructive mutation.
  - Use raw string literals (`"""`) for multi-line or complex strings.
  - Mark required members with the `required` modifier.
  - Use primary constructors to centralize initialization logic.
  - Employ collection expressions (`[...]`) for concise array/list/span initialization.
  - Add default parameters to lambda expressions to reduce overloads.

### Documentation Requirements
- **XML comments**: All publicly exposed methods and properties must have .NET XML comments, including protected methods of public classes.
- **Documentation culture**: Use `en-US` as specified in `src/stylecop.json`.

### File Style Precedence
- **Existing style**: If a file differs from these guidelines (e.g., private members named `m_member` instead of `_member`), the existing style in that file takes precedence.
- **Consistency**: Maintain consistency within individual files.

## Example Code Structure

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Win32;

namespace System.Collections.Generic;

public partial class ObservableLinkedList<T> : INotifyCollectionChanged, INotifyPropertyChanged
{
    private ObservableLinkedListNode<T>? _head;
    private int _count;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableLinkedList{T}"/> class.
    /// </summary>
    /// <param name="items">The items to initialize the list with.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is null.</exception>
    public ObservableLinkedList(IEnumerable<T> items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        foreach (var item in items)
        {
            AddLast(item);
        }
    }

    /// <summary>
    /// Occurs when the collection changes.
    /// </summary>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// Gets the number of elements in the list.
    /// </summary>
    public int Count
    {
        get => _count;
    }

    /// <summary>
    /// Adds a new node containing the specified value at the end of the list.
    /// </summary>
    /// <param name="value">The value to add.</param>
    /// <returns>The new node that was added.</returns>
    public ObservableLinkedListNode AddLast(T value)
    {
        var newNode = new ObservableLinkedListNode(this, value);
        InsertNodeBefore(_head, newNode);
        return newNode;
    }

    /// <summary>
    /// Raises the CollectionChanged event.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        CollectionChanged?.Invoke(this, e);
    }

    private void InsertNodeBefore(ObservableLinkedListNode<T>? node, ObservableLinkedListNode<T> newNode)
    {
        // Implementation details...
    }
}
```

### Notes
- **EditorConfig**: The `.editorconfig` at the root of the ReactiveUI repository enforces formatting and analysis rules, replacing the previous `analyzers.ruleset`. Update `.editorconfig` as needed to support modern C# features, such as nullable reference types.
- **Example Updates**: The example incorporates modern C# practices like file-scoped namespaces and nullable reference types. Refer to Microsoft documentation for further integration of C# 8–12 features.

### Code Formatting (Fast - Always Run)
- **ALWAYS** run formatting before committing:
  ```bash
  cd src
  dotnet format whitespace --verify-no-changes
  dotnet format style --verify-no-changes
  ```
  Time: **2-5 seconds per command**.

### Code Analysis Validation
- **Run analyzers** to check StyleCop and code quality compliance:
  ```bash
  cd src
  dotnet build --configuration Release --verbosity normal
  ```
  This runs all analyzers (StyleCop SA*, Roslynator RCS*, .NET CA*) and treats warnings as errors.
- **Analyzer Configuration**:
  - StyleCop settings in `src/stylecop.json`
  - EditorConfig rules in `.editorconfig` (root level)
  - Analyzer packages in `src/Directory.Build.props`
  - All code must follow the **ReactiveUI C# Style Guide** detailed above

## Key Projects and Structure

### Core Projects (Priority Order)
1. **Splat.DependencyInjection.SourceGenerator** (`Splat.DependencyInjection.SourceGenerator.csproj`) - IIncrementalGenerator implementation
   - Entry point: `Generator.cs`
   - Generates compile-time DI registrations for Splat
   - Uses generic-first API for AOT compatibility
   - Target framework: netstandard2.0
   - Output: Analyzer component packaged to analyzers/dotnet/cs

2. **Splat.DependencyInjection.Analyzer** (`Splat.DependencyInjection.Analyzer.csproj`) - Roslyn analyzer and code fix providers
   - Real-time diagnostics for DI attributes
   - Code fixes for common issues (SPLATDI001-007)
   - Target framework: netstandard2.0
   - Output: Analyzer component packaged with source generator

### Test Projects
3. **Splat.DependencyInjection.SourceGenerator.Tests** - Source generator snapshot tests
   - Uses TUnit testing framework
   - Uses Verify.SourceGenerators for snapshot comparison
   - 342 tests validating generated code
   - Target framework: net8.0

4. **Splat.DependencyInjection.Analyzer.Tests** - Analyzer and code fix tests
   - Uses TUnit testing framework
   - Tests all diagnostic analyzers (SPLATDI001-007)
   - Tests all code fix providers
   - 45 tests validating analyzer behavior
   - Target framework: net8.0

### Project Structure
```
src/
├── Splat.DependencyInjection.SourceGenerator/
│   ├── Generator.cs                        # IIncrementalGenerator entry point
│   ├── Constants.cs                        # Attribute definitions with [Embedded]
│   ├── DiagnosticWarnings.cs              # Shared diagnostic descriptors
│   ├── Models/
│   │   ├── RegistrationInfo.cs            # Base POCO record
│   │   ├── TransientRegistrationInfo.cs   # Transient registration data
│   │   ├── LazySingletonRegistrationInfo.cs
│   │   ├── ConstantRegistrationInfo.cs
│   │   ├── ConstructorParameter.cs
│   │   ├── PropertyInjection.cs
│   │   └── EquatableArray.cs              # Value-equatable array wrapper
│   └── CodeGeneration/
│       └── CodeGenerator.cs               # String-based code generation
├── Splat.DependencyInjection.Analyzer/
│   ├── Analyzers/
│   │   ├── ConstructorAnalyzer.cs         # SPLATDI001, SPLATDI003, SPLATDI004
│   │   └── PropertyAnalyzer.cs            # SPLATDI002
│   └── CodeFixes/
│       ├── ConstructorCodeFixProvider.cs  # Adds [DependencyInjectionConstructor]
│       └── PropertyCodeFixProvider.cs     # Fixes property setter accessibility
├── Splat.DependencyInjection.SourceGenerator.Tests/
│   ├── TestBase.cs                        # TUnit test base with [Before]/[After] hooks
│   ├── TestHelper.cs                      # Static helper methods
│   └── ModuleInitializer.cs               # Verify.SourceGenerators initialization
└── Splat.DependencyInjection.Analyzer.Tests/
    ├── TestUtilities.cs                   # Newline-agnostic source comparison
    └── VerifyTests/                       # Analyzer and code fix tests
```

## Common Development Tasks

### Making Changes to Source Generator
1. **Understand incremental generator pipeline**:
   - Predicate functions (fast syntax checks)
   - Transform functions (semantic analysis + POCO extraction)
   - Generation functions (string-based code output)

2. **CRITICAL: Value-Equatable Models**:
   - NEVER include ISymbol or SyntaxNode in pipeline output models
   - ALL pipeline models must implement IEquatable<T>
   - Use `EquatableArray<T>` for array equality in records
   - Extract strings from symbols using `ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)`

3. **Code Generation Best Practices**:
   - Use StringBuilder with raw string literals (`"""`)
   - NEVER use SyntaxFactory (3-5x slower)
   - Use generic-first API: `resolver.Register<T>()` not `Register(factory, typeof(T))`
   - Minimize allocations in hot paths

4. **Testing Source Generator Changes**:
   ```bash
   cd src
   dotnet test Splat.DependencyInjection.SourceGenerator.Tests --configuration Release
   ```
   - Review `.verified.cs` snapshot diffs carefully
   - Ensure generic-first API is used in generated code
   - Verify C# 7.3 compatibility (netstandard2.0 target)

### Making Changes to Analyzer
1. **Analyzer Development**:
   - Analyzers provide real-time feedback to users
   - Use `RegisterSymbolAction` for efficient symbol analysis
   - Check attribute presence using `GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == Constants.AttributeName)`
   - Report diagnostics via `context.ReportDiagnostic()`

2. **Code Fix Development**:
   - Provide code fixes for all fixable diagnostics
   - Use `CodeAction.Create()` to register fixes
   - Test code fixes thoroughly with `VerifyCodeFixAsync()`

3. **Testing Analyzer Changes**:
   ```bash
   cd src
   dotnet test Splat.DependencyInjection.Analyzer.Tests --configuration Release
   ```
   - Use `TestUtilities.AreEquivalent()` for newline-agnostic comparison
   - Verify diagnostics are reported at correct locations
   - Verify code fixes produce expected output

### Adding New Features
1. **Follow coding standards** - see ReactiveUI guidelines: https://www.reactiveui.net/contribute/index.html
2. **Ensure StyleCop compliance** - all code must pass StyleCop analyzers (SA* rules)
3. **Run code analysis** - `dotnet build` must complete without analyzer warnings
4. **Add unit tests** - all features require test coverage (100% for critical paths)
5. **Update documentation** - especially for public APIs with XML doc comments
6. **Update README** - if user-facing behavior changes
7. **Test AOT compatibility** - ensure generated code works with Native AOT

### Working with Diagnostics
- **Diagnostic IDs**: SPLATDI001 through SPLATDI007
- **Severity Levels**: Warning or Error based on impact
- **Code Fixes**: Provide automatic fixes where possible
- **Test Coverage**: Every diagnostic must have corresponding test

### Updating Generated Code
- **Always use generic-first API**: `resolver.Register<T>()` not `Register(factory, typeof(T))`
- **Constructor injection**: Resolve parameters via `resolver.GetService(typeof(T))`
- **Property injection**: Use object initializer syntax `{ Prop = value }`
- **Lazy singletons**: Create `Lazy<T>` wrapper and register both `Lazy<T>` and `T`
- **Contracts**: Pass contract string as second parameter to Register

## Target Framework Support

### Source Generator and Analyzer
- **netstandard2.0**: Required for Roslyn analyzers and source generators
- **Language Version**: C# 7.3 minimum for target framework compatibility
- **Generated Code**: Must be compatible with C# 7.3 (no file-scoped namespaces, no init properties)

### Test Projects
- **net8.0**: Modern .NET with TUnit and Verify.SourceGenerators support
- **Language Version**: Latest C# features available

### AOT Compatibility
- **Generated code** must work with Native AOT and trimming
- **Generic-first API** ensures no boxing for value types
- **No reflection** in generated code paths
- **Splat 15.0.0+** required for generic-first resolver support

## Build Timing and Expectations

| Operation | Time | Notes |
|-----------|------|-------|
| **Single Project Restore** | 10-20 seconds | Fast operation |
| **Single Project Build** | 10-30 seconds | Usually quick |
| **Full Solution Restore** | 30 seconds | Small solution |
| **Full Solution Build** | 30-60 seconds | All projects |
| **Analyzer Test Suite** | 5-10 seconds | 45 tests |
| **Generator Test Suite** | 20-40 seconds | 342 snapshot tests |
| **Code Formatting** | 2-5 seconds | Always run |

## Performance Characteristics

### Source Generator Performance
- **Incremental builds** should only regenerate when relevant files change
- **Cache-friendly pipeline** eliminates unnecessary recompilation
- **Value-equatable POCOs** enable Roslyn incremental caching
- **10-100x faster** than reflection-based DI at runtime

### Generated Code Performance
- **Zero reflection** overhead (all work done at compile-time)
- **Generic-first API** eliminates boxing for value types
- **Approximately 100x faster** than reflection-based registration
- **AOT compatible** works with Native AOT and trimming

### Analyzer Performance
- **Real-time feedback** with minimal IDE overhead
- **Efficient symbol analysis** using RegisterSymbolAction
- **Incremental analysis** only reruns on changed files

## Migration and Compatibility

### Version Compatibility
- **Semantic versioning** is followed for breaking changes
- **Splat dependency**: Requires Splat 15.0.0 or later
- **Roslyn version**: Microsoft.CodeAnalysis 4.14.0

### Breaking Changes
- **Version 3.0**: Migrated to IIncrementalGenerator, requires Splat 15.0.0+
- **Migration guide** provided in README.md

## CI/CD Integration

### GitHub Actions
- Uses reactiveui/actions-common reusable workflow
- Runs on multiple platforms (Windows, Linux, macOS)
- Includes code coverage reporting via Codecov
- Publishes packages to NuGet

### Local Development
- **Build locally** before pushing changes
- **Run tests** for affected components
- **Format code** before every commit
- **Check analyzer warnings** before committing
- **Review snapshot diffs** carefully

### Code Coverage
- **Target**: High coverage for critical paths
- **Configuration**: `src/testconfig.json`
- **Format**: Cobertura format for Codecov
- **Exclusions**: Test projects excluded from coverage

## Troubleshooting

### Common Issues
1. **"IIncrementalGenerator not found" errors**: Ensure Microsoft.CodeAnalysis 4.14.0 is installed
2. **StyleCop violations**: Check `.editorconfig` rules and `src/stylecop.json`
3. **Missing dependencies**: Run `dotnet restore` in `src` directory
4. **Test failures**: May require snapshot updates or code changes

### Source Generator Specific Issues
1. **Generator not running**: Ensure `SetupIOC()` is called in consuming project
2. **Attributes not found**: Check `using static Splat.SplatRegistrations;` directive
3. **Type resolution errors**: Verify fully qualified type names in generated code
4. **Pipeline not caching**: Ensure POCOs don't contain ISymbol/SyntaxNode references

### Analyzer Specific Issues
1. **Diagnostics not appearing**: Ensure analyzer DLL is packaged correctly
2. **Code fix not available**: Check if diagnostic is marked as fixable
3. **Multiple diagnostics**: Analyzer may detect multiple issues simultaneously

### Quick Fixes
- **Format issues**: Run `dotnet format whitespace` and `dotnet format style`
- **StyleCop violations**: Check `.editorconfig` rules and `src/stylecop.json` configuration
- **Analyzer warnings**: Build with `--verbosity normal` to see detailed analyzer messages
- **Missing XML documentation**: All public APIs require XML doc comments per StyleCop rules
- **Package restore issues**: Clear NuGet cache with `dotnet nuget locals all --clear`
- **Snapshot mismatches**: Review diff carefully, accept if correct, fix if incorrect

### When to Escalate
- **Incremental generator** not caching properly across builds
- **Performance regressions** in generated code execution
- **Test failures** that persist across platforms
- **Breaking changes** affecting Splat compatibility
- **AOT compatibility** issues with newer .NET versions

## Resources

### Splat.DI.SourceGenerator
- **Main Repository**: https://github.com/reactiveui/Splat.DI.SourceGenerator
- **Repository README**: https://github.com/reactiveui/Splat.DI.SourceGenerator#readme
- **Issues & Bug Reports**: https://github.com/reactiveui/Splat.DI.SourceGenerator/issues
- **NuGet Package**: https://www.nuget.org/packages/Splat.DependencyInjection.SourceGenerator
- **Code Coverage**: https://codecov.io/gh/reactiveui/Splat.DI.SourceGenerator
- **GitHub Actions (CI/CD)**: https://github.com/reactiveui/Splat.DI.SourceGenerator/actions

### Governance & Contributing
- **Contribution Hub**: https://www.reactiveui.net/contribute/index.html
- **ReactiveUI Repository README**: https://github.com/reactiveui/ReactiveUI#readme
- **Contributing Guidelines**: https://github.com/reactiveui/ReactiveUI/blob/main/CONTRIBUTING.md
- **Code of Conduct**: https://github.com/reactiveui/ReactiveUI/blob/main/CODE_OF_CONDUCT.md

### Engineering & Style
- **ReactiveUI Coding/Style Guidance** (start here): https://www.reactiveui.net/contribute/
- **Build & Project Structure Reference**: https://github.com/reactiveui/ReactiveUI#readme

### Documentation & Samples
- **Documentation Home**: https://www.reactiveui.net/
- **Handbook** (core concepts): https://www.reactiveui.net/docs/

### Ecosystem
- **Splat** (core dependency): https://github.com/reactiveui/splat
- **ReactiveUI** (MVVM framework): https://github.com/reactiveui/ReactiveUI
- **DynamicData** (reactive collections): https://github.com/reactivemarbles/DynamicData

### Source Generators & Roslyn
- **Incremental Generators Cookbook**: https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.cookbook.md
- **Source Generators**: https://learn.microsoft.com/dotnet/csharp/roslyn-sdk/source-generators-overview
- **IIncrementalGenerator**: https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md
- **Roslyn Analyzers**: https://learn.microsoft.com/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix

### AOT & Trimming
- **.NET Native AOT Overview**: https://learn.microsoft.com/dotnet/core/deploying/native-aot/
- **Prepare Libraries for Trimming**: https://learn.microsoft.com/dotnet/core/deploying/trimming/prepare-libraries-for-trimming
- **Trimming Options (MSBuild)**: https://learn.microsoft.com/dotnet/core/deploying/trimming/trimming-options
- **Fixing Trim Warnings**: https://learn.microsoft.com/dotnet/core/deploying/trimming/trim-warnings

### Testing
- **TUnit Documentation**: https://github.com/thomhurst/TUnit
- **Verify.SourceGenerators**: https://github.com/VerifyTests/Verify.SourceGenerators
- **Microsoft.Testing.Platform**: https://learn.microsoft.com/dotnet/core/testing/unit-testing-platform-intro

### Copilot Coding Agent
- **Best Practices for Copilot Coding Agent**: https://gh.io/copilot-coding-agent-tips

### CI & Misc
- **GitHub Actions** (builds and workflow runs): https://github.com/reactiveui/Splat.DI.SourceGenerator/actions
- **ReactiveUI Website Source** (useful for docs cross-refs): https://github.com/reactiveui/website
