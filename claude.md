# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Test Commands

This project uses **Microsoft Testing Platform (MTP)** with the **TUnit** testing framework. Test commands differ significantly from traditional VSTest.

See: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test?tabs=dotnet-test-with-mtp

### Prerequisites

```powershell
# Check .NET installation (.NET 8.0, 9.0, and 10.0 required)
dotnet --info

# Restore NuGet packages
cd src
dotnet restore Splat.DependencyInjection.SourceGenerator.sln
```

### Build Commands

**CRITICAL:** The working folder must be `./src` folder. These commands won't function properly without the correct working folder.

```powershell
# Build the solution
dotnet build Splat.DependencyInjection.SourceGenerator.sln -c Release

# Build with warnings as errors (includes StyleCop violations)
dotnet build Splat.DependencyInjection.SourceGenerator.sln -c Release -warnaserror

# Clean the solution
dotnet clean Splat.DependencyInjection.SourceGenerator.sln
```

### Test Commands (Microsoft Testing Platform)

**CRITICAL:** This repository uses MTP configured in `testconfig.json`. All TUnit-specific arguments must be passed after `--`:

The working folder must be `./src` folder. These commands won't function properly without the correct working folder.

**IMPORTANT:**
- Do NOT use `--no-build` flag when running tests. Always build before testing to ensure all code changes (including test changes) are compiled. Using `--no-build` can cause tests to run against stale binaries and produce misleading results.
- Use `--output Detailed` to see Console.WriteLine output from tests. This must be placed BEFORE any `--` separator:
  ```powershell
  dotnet test --output Detailed -- --treenode-filter "..."
  ```

```powershell
# Run all tests in the solution
dotnet test --solution Splat.DependencyInjection.SourceGenerator.sln -c Release

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
dotnet test --solution Splat.DependencyInjection.SourceGenerator.sln -- --treenode-filter "/*/*/*/*[Category=Integration]"

# Run tests with code coverage (Microsoft Code Coverage)
dotnet test --solution Splat.DependencyInjection.SourceGenerator.sln -- --coverage --coverage-output-format cobertura

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

**Alternative: Using `dotnet run` for single project**
```powershell
# Run tests using dotnet run (easier for passing flags)
dotnet run --project Splat.DependencyInjection.Analyzer.Tests/Splat.DependencyInjection.Analyzer.Tests.csproj -c Release -- --treenode-filter "/*/*/*/MyTest"

# Disable logo for cleaner output
dotnet run --project Splat.DependencyInjection.SourceGenerator.Tests/Splat.DependencyInjection.SourceGenerator.Tests.csproj -- --disable-logo --treenode-filter "/*/*/*/Test1"
```

### TUnit Treenode-Filter Syntax

The `--treenode-filter` follows the pattern: `/{AssemblyName}/{Namespace}/{ClassName}/{TestMethodName}`

**Examples:**
- Single test: `--treenode-filter "/*/*/*/MyTestMethod"`
- All tests in class: `--treenode-filter "/*/*/MyClassName/*"`
- All tests in namespace: `--treenode-filter "/*/MyNamespace/*/*"`
- Filter by property: `--treenode-filter "/*/*/*/*[Category=Integration]"`
- Multiple wildcards: `--treenode-filter "/*/*/MyTests*/*"`

**Note:** Use single asterisks (`*`) to match segments. Double asterisks (`/**`) are not supported in treenode-filter.

### Key TUnit Command-Line Flags

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

### Key Configuration Files

- `src/testconfig.json` - Configures test execution (`"parallel": false`) and code coverage (Cobertura format)
- `src/Directory.Build.props` - Enables `TestingPlatformDotnetTestSupport` for test projects
- `.github/COPILOT_INSTRUCTIONS.md` - Comprehensive development guidelines

## Architecture Overview

### Core Project Structure

Splat.DI.SourceGenerator is a high-performance C# source generator that produces compile-time dependency injection registrations for Splat. It eliminates runtime reflection, provides full native AOT support, and includes intelligent analyzers with automatic code fixes.

**Generator Project (`Splat.DependencyInjection.SourceGenerator/`)**
- `Generator.cs` - IIncrementalGenerator entry point with CreateSyntaxProvider pipeline
- `Models/` - Value-equatable POCO records (no ISymbol/SyntaxNode references)
  - `RegistrationInfo.cs` - Base record for all registration types
  - `TransientRegistrationInfo.cs`, `LazySingletonRegistrationInfo.cs`, `ConstantRegistrationInfo.cs`
  - `ConstructorParameter.cs`, `PropertyInjection.cs`
  - `EquatableArray.cs` - Value-equatable array wrapper for pipeline caching
- `CodeGeneration/CodeGenerator.cs` - String-based code generation (not SyntaxFactory)
- `Constants.cs` - Attribute definitions with `[Embedded]` attribute
- `DiagnosticWarnings.cs` - Shared diagnostic descriptors

**Analyzer Project (`Splat.DependencyInjection.Analyzer/`)**
- `Analyzers/ConstructorAnalyzer.cs` - Detects multiple constructors without `[DependencyInjectionConstructor]` (SPLATDI001, SPLATDI003, SPLATDI004)
- `Analyzers/PropertyAnalyzer.cs` - Validates property injection setters (SPLATDI002)
- `CodeFixes/ConstructorCodeFixProvider.cs` - Adds `[DependencyInjectionConstructor]` attribute
- `CodeFixes/PropertyCodeFixProvider.cs` - Fixes property setter accessibility

**Test Projects**
- `Splat.DependencyInjection.SourceGenerator.Tests/` - 342 snapshot tests using Verify.SourceGenerators
- `Splat.DependencyInjection.Analyzer.Tests/` - 45 analyzer and code fix tests

### Key Architectural Patterns

**Incremental Generator Pipeline (IIncrementalGenerator)**
- **Predicate functions** - Fast syntax-only checks (e.g., `IsRegisterInvocation`)
- **Transform functions** - Semantic analysis + POCO extraction (no ISymbol in output!)
- **Generation functions** - String-based code output using StringBuilder

**Value-Equatable Models (Critical for Caching)**
- ALL pipeline models must implement `IEquatable<T>`
- NEVER include ISymbol or SyntaxNode references in pipeline outputs
- Use `EquatableArray<T>` for array equality in records
- Extract strings from symbols using `ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)`

**Generic-First API (AOT Compatible)**
- Generated code uses `resolver.Register<T>()` instead of `Register(factory, typeof(T))`
- Eliminates boxing for value types
- Full Native AOT and trimming support

**Code Generation Strategy**
- Uses StringBuilder with raw string literals (`"""`)
- 3-5x faster than SyntaxFactory approach
- No dependency on ReactiveMarbles.RoslynHelpers or ILRepack

**Analyzer Separation (Roslyn Best Practice)**
- Generator focuses on code generation only
- Separate analyzer project provides real-time diagnostics
- Code fix providers offer automatic fixes (Quick Actions)

### Diagnostic IDs

| ID | Severity | Description | Code Fix Available |
|----|----------|-------------|--------------------|
| SPLATDI001 | Warning | Multiple constructors without `[DependencyInjectionConstructor]` | ✅ Yes |
| SPLATDI002 | Error | Property lacks accessible setter | ✅ Yes |
| SPLATDI003 | Error | Multiple constructors marked | ❌ Manual fix |
| SPLATDI004 | Error | Constructor not accessible | ✅ Yes |

## Code Style & Quality Requirements

**CRITICAL:** All code must comply with ReactiveUI contribution guidelines: https://www.reactiveui.net/contribute/index.html

### Style Enforcement

- EditorConfig rules (`.editorconfig`) - comprehensive C# formatting and naming conventions
- StyleCop Analyzers - builds fail on violations
- Roslynator Analyzers - additional code quality rules
- Analysis level: latest with enhanced .NET analyzers
- **All public APIs require XML documentation comments** (including protected methods of public classes)

### C# Style Rules

- **Braces:** Allman style (each brace on new line)
- **Indentation:** 4 spaces, no tabs
- **Fields:** `_camelCase` for private/internal, `readonly` where possible, `static readonly` (not `readonly static`)
- **Visibility:** Always explicit (e.g., `private string _foo` not `string _foo`), visibility first modifier
- **Namespaces:** File-scoped preferred, imports outside namespace, sorted (system then third-party)
- **Types:** Use keywords (`int`, `string`) not BCL types (`Int32`, `String`)
- **Modern C#:** Use nullable reference types, pattern matching, switch expressions, records, init setters, target-typed new, collection expressions, file-scoped namespaces, primary constructors
- **Avoid `this.`** unless necessary
- **Use `nameof()`** instead of string literals
- **Use `var`** when it improves readability or aids refactoring

See `.github/COPILOT_INSTRUCTIONS.md` for complete style guide.

## Testing Guidelines

- Unit tests use **TUnit** framework with **Microsoft Testing Platform**
- Test projects: `Splat.DependencyInjection.Analyzer.Tests` and `Splat.DependencyInjection.SourceGenerator.Tests`
- Coverage configured in `src/testconfig.json` (Cobertura format)
- Parallel test execution disabled (`"parallel": false` in testconfig.json)
- Snapshot testing uses Verify.SourceGenerators with `*.verified.cs` files
- Always write unit tests for new features or bug fixes
- Follow existing test patterns in test projects
- Use `TestUtilities.AreEquivalent()` for newline-agnostic source code comparison

### TUnit Testing Framework

- Uses `[Test]` attribute instead of `[Fact]`/`[Theory]`
- Uses `[Before(Test)]` and `[After(Test)]` hooks instead of IDisposable
- Uses `await Assert.That(x).IsEqualTo(y)` instead of `Assert.Equal(y, x)`
- No `ITestOutputHelper` - TUnit uses Microsoft.Testing.Platform for output

## Common Tasks

### Adding a New Feature to Source Generator

1. **Design incremental pipeline** - predicate → transform → generate
2. **Create value-equatable POCOs** - no ISymbol/SyntaxNode references
3. Create failing tests first (snapshot tests in SourceGenerator.Tests)
4. Implement minimal functionality in Generator.cs
5. Update code generation in CodeGenerator.cs (use StringBuilder, not SyntaxFactory)
6. Ensure generic-first API usage: `resolver.Register<T>()` not `typeof(T)`
7. Verify snapshots match expected output (C# 7.3 compatible)
8. Add XML documentation to all public APIs
9. Run formatting validation before committing

### Adding a New Analyzer Diagnostic

1. Add diagnostic descriptor to `DiagnosticWarnings.cs`
2. Create analyzer in `Splat.DependencyInjection.Analyzer/Analyzers/`
3. Implement `DiagnosticAnalyzer` using `RegisterSymbolAction` or `RegisterSyntaxNodeAction`
4. Create corresponding code fix provider if fixable
5. Add tests in `Splat.DependencyInjection.Analyzer.Tests/`
6. Use `TestUtilities.AreEquivalent()` for source comparison
7. Update README.md with new diagnostic ID

### Fixing Bugs

1. Create reproduction test (use Verify snapshots)
2. Fix with minimal changes
3. Ensure pipeline still caches properly (POCOs value-equatable)
4. Verify no regression in existing tests
5. Accept snapshot changes if expected

### Updating Generated Code Format

1. Modify code generation in `CodeGeneration/CodeGenerator.cs`
2. Use raw string literals (`"""`) for multi-line code
3. Ensure generic-first API: `resolver.Register<T>()` not `typeof(T)`
4. Run all snapshot tests - expect 342 failing tests
5. Review each `.verified.cs` diff carefully
6. Accept snapshots only if changes are correct
7. Ensure C# 7.3 compatibility (no file-scoped namespaces, no init properties)

## What to Avoid

- **ISymbol/SyntaxNode in pipeline outputs** - breaks incremental caching
- **Runtime reflection** in generated code - breaks AOT compatibility
- **SyntaxFactory for code generation** - 3-5x slower than StringBuilder
- **Type-based API** - use `resolver.Register<T>()` not `Register(factory, typeof(T))`
- **Diagnostics in generator** - use separate analyzer project instead
- **Heavy dependencies** - keep generator lightweight (netstandard2.0 target)
- **Breaking changes** to generated code format without major version bump
- **Non-value-equatable models** in pipeline - breaks caching

## Important Notes

- **Value-Equatable POCOs:** CRITICAL for incremental generator caching - never include ISymbol/SyntaxNode
- **Generic-First API:** All generated code must use `resolver.Register<T>()` for AOT compatibility
- **String-Based Generation:** Use StringBuilder with raw string literals, not SyntaxFactory
- **Separate Analyzer:** Diagnostics in separate project following Roslyn best practices
- **No shallow clones:** Repository requires full clone for git version information used by Nerdbank.GitVersioning
- **Required .NET SDKs:** .NET 8.0, 9.0, and 10.0 (all three required for full build)
- **Snapshot Testing:** Review `.verified.cs` diffs carefully before accepting
- **Comprehensive Instructions:** `.github/COPILOT_INSTRUCTIONS.md` contains detailed development guidelines
- **Code Formatting:** Always run `dotnet format whitespace` and `dotnet format style` before committing

**Philosophy:** Generate simple, efficient, AOT-compatible dependency injection code at compile-time. Minimize runtime overhead, maximize build performance with incremental caching, and provide excellent developer experience with real-time diagnostics and automatic code fixes.
