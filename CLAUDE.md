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
dotnet restore Splat.DI.SourceGenerator.slnx
```

**Note:** This project uses the modern `.slnx` (XML-based solution file) format instead of the legacy `.sln` format. The `.slnx` format provides better performance, cleaner diffs, and improved tooling support in Visual Studio 2022 17.10+.

### Build Commands

**CRITICAL:** The working folder must be `./src` folder. These commands won't function properly without the correct working folder.

```powershell
# Build the solution
dotnet build Splat.DI.SourceGenerator.slnx -c Release

# Build with warnings as errors (includes StyleSharp/PerformanceSharp/SecuritySharp violations)
dotnet build Splat.DI.SourceGenerator.slnx -c Release -warnaserror

# Clean the solution
dotnet clean Splat.DI.SourceGenerator.slnx
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
dotnet test --solution Splat.DI.SourceGenerator.slnx -- --output Detailed

# List all available tests without running them
dotnet test --project Splat.DependencyInjection.Analyzer.Tests/Splat.DependencyInjection.Analyzer.Tests.csproj -- --list-tests

# Fail fast (stop on first failure)
dotnet test --solution Splat.DI.SourceGenerator.slnx -- --fail-fast

# Control parallel test execution
dotnet test --solution Splat.DI.SourceGenerator.slnx -- --maximum-parallel-tests 4

# Generate TRX report
dotnet test --solution Splat.DI.SourceGenerator.slnx -- --report-trx

# Disable logo for cleaner output
dotnet test --project Splat.DependencyInjection.Analyzer.Tests/Splat.DependencyInjection.Analyzer.Tests.csproj -- --disable-logo

# Combine options: coverage + TRX report + detailed output
dotnet test --solution Splat.DI.SourceGenerator.slnx -- --coverage --coverage-output-format cobertura --report-trx --output Detailed
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

- `src/Splat.DI.SourceGenerator.slnx` - Modern XML-based solution file (Visual Studio 2022 17.10+)
- `src/testconfig.json` - Configures test execution (`"parallel": false`) and code coverage (Cobertura format)
- `src/Directory.Build.props` - Enables `TestingPlatformDotnetTestSupport` for test projects
- `.github/COPILOT_INSTRUCTIONS.md` - Comprehensive development guidelines

## Architecture Overview

### Core Project Structure

Splat.DI.SourceGenerator is a high-performance C# source generator that produces compile-time dependency injection registrations for Splat. It eliminates runtime reflection, provides full native AOT support, and includes intelligent analyzers with automatic code fixes.

**Generator Project (`Splat.DependencyInjection.SourceGenerator/`)**
- `Generator.cs` - IIncrementalGenerator entry point: one syntax provider for both marker methods, then a split
  into a code output (registrations only) and a diagnostics output (registrations with locations)
- `RoslynHelpers.cs` - The syntax predicate and small symbol helpers
- `MetadataExtractor.cs` - The transform: turns a bound marker call into a `RegistrationSite`
- `WellKnownSymbols.cs` - The symbols extraction compares against, cached per compilation in a `ConditionalWeakTable`
- `RegistrationValidator.cs` - SPLATDI005/006/007, which need the whole registration graph
- `Models/` - Value-equatable pipeline models (no ISymbol, SyntaxNode or Location)
  - `RegistrationInfo.cs` - Everything the generated code needs for one registration (`RegistrationKind` says which)
  - `RegistrationSite.cs` - A registration plus its `LocationInfo`, for the diagnostics
  - `LocationInfo.cs` - A location held as values (path, span, line span)
  - `ConstructorParameter.cs`, `PropertyInjection.cs` - `readonly record struct`s
  - `EquatableArray.cs` - Value-equatable array wrapper with a cached hash, read by index
- `CodeGeneration/` - Emission through `SourceWriter` (see Writing Generated Code)
  - `CodeGenerator.cs` - The registrations file
  - `MarkerSource.cs` - The fixed marker methods and attributes, added in post-initialization
  - `SourceWriter.cs`, `SourceWriterExtensions.cs`, `PooledBuilder.cs` - The writer and its pooled builder
- `Constants.cs` - Names matched against and file names (linked into the analyzer, so constants only)
- `DiagnosticWarnings.cs` - Shared diagnostic descriptors

**Analyzer Project (`Splat.DependencyInjection.Analyzer/`)**
- `Analyzers/ConstructorAnalyzer.cs` - Detects multiple constructors without `[DependencyInjectionConstructor]` (SPLATDI001, SPLATDI003, SPLATDI004)
- `Analyzers/PropertyAnalyzer.cs` - Validates property injection setters (SPLATDI002)
- `CodeFixes/ConstructorCodeFixProvider.cs` - Adds `[DependencyInjectionConstructor]` attribute
- `CodeFixes/PropertyCodeFixProvider.cs` - Fixes property setter accessibility

**Test Projects**
- `Splat.DependencyInjection.SourceGenerator.Tests/` - Snapshot tests (`GeneratorSnapshot`) plus unit tests of
  the pipeline, extraction, validation, emission and caching
- `Splat.DependencyInjection.Analyzer.Tests/` - Analyzer and code fix tests

**Benchmarks (`benchmarks/Splat.DI.SourceGenerator.Benchmarks/`)** - see `benchmarks/README.md`

### Key Architectural Patterns

**Incremental Generator Pipeline (IIncrementalGenerator)**
- **Predicate** (`RoslynHelpers.IsRegistrationInvocation`) - syntax only, allocation free. Every marker is generic
  and cannot infer its type arguments, and none takes a lambda, so calls without a type argument list or with a
  lambda argument (Splat's own `resolver.Register<T>(() => ...)`) never reach the transform.
- **Transform** (`MetadataExtractor.Extract`) - binds the call. A `CreateSyntaxProvider` transform re-runs for every
  matching node on every compilation change, so it turns calls away as early and cheaply as it can.
- **Outputs** - the generated file is built from `RegistrationInfo` alone, so an edit that only moves a registration
  regenerates nothing. The graph diagnostics are reported from their own output, which also carries the locations.
  With no registrations, no registrations file is added (the partial method is removed by the compiler).
- Steps carry tracking names (`Generator.SitesStep` and so on); `GeneratorTests` asserts what an edit re-runs.

**Value-Equatable Models (Critical for Caching)**
- Pipeline models are `sealed record` or `readonly record struct` types with value equality
- NEVER include ISymbol, SyntaxNode or `Location` in pipeline outputs; use `LocationInfo`
- Use `EquatableArray<T>` for array equality in records
- Extract strings from symbols using `ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)`
- Records on netstandard2.0 need `IsExternalInit`, which is inlined in `src/Polyfills/` and linked into the
  generator and analyzer projects, as elsewhere in rxui

**Generic-First API (AOT Compatible)**
- Generated code uses `resolver.Register<T>()` instead of `Register(factory, typeof(T))`
- Eliminates boxing for value types
- Full Native AOT and trimming support

**What the generated code allocates**
- Resolving a service allocates the objects it constructs and nothing else from the generated code; Splat's
  `GetServices` collection is Splat's. A missing dependency throws through a `ThrowNotRegistered` helper written once
  per file, so factories stay small.
- A factory that needs nothing from the resolver captures nothing, so its delegate is cached once by the compiler.
- `SetupIOC` allocates one closure (for `resolver` and every lazy), one delegate per capturing registration, and for
  each lazy singleton its `Lazy<T>` and three delegates. Lazies are method-level locals (`lazy0`, `lazy1`, ...), not
  locals of a block per registration: a block adds a closure object each.

**Interceptors are not used.** The marker calls are empty methods that registration never runs through, and
`SetupIOC` is a direct call to the generated method, so there is no call to redirect. Intercepting the marker calls
would change when and on which resolver registration happens.

### Writing Generated Code

`SourceWriter` owns the layout of every generated file. An emitter says what it writes, and the writer decides the
indentation.

- **The writer tracks the level.** A line is indented when its first character is written. A blank line carries no
  whitespace. Every line ends with `\n`. No string literal in an emitter starts with spaces.
- **Blocks change the level.** `OpenBlock` and `CloseBlock` write the braces and move one level in and out.
  `OpenContinuation`/`CloseContinuation` put the arguments of a multi-line call one level deeper.
- **Each emitter method writes at the level it is given** and leaves it as it found it; its doc comment says where
  it leaves the writer.
- **C# constructs have names** in `SourceWriterExtensions` (namespaces, continuations, argument separators). Domain
  shapes live in `CodeGenerator` (`AppendFactoryCall`, `AppendFactory`, `AppendService`).
- **Fixed text is written with `Lines`** from a raw string literal whose indentation is relative to column zero.
- `SourceWriter.Rent` takes its `StringBuilder` from `PooledBuilder`'s per-thread slot, and `ToStringAndReturn`
  gives it back.

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
- StyleSharp.Analyzers (SST), PerformanceSharp.Analyzers (PSH) and SecuritySharp.Analyzers (SES) - builds fail on
  violations. Rule docs: https://github.com/glennawatson/RoslynCommonAnalyzers/tree/main/docs/rules
- Fix the code. Do not suppress rules, do not disable them in `.editorconfig`, and do not use
  `#pragma warning disable` (the `#pragma` for obsolete members inside the generated output is part of what the
  generator emits, not our source)
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
- Snapshot tests compare every generated registrations file with a `*.verified.cs` snapshot beside the test class,
  through `GeneratorSnapshot.cs`. A snapshot is named `{type}.{method}_{arguments}#{hint name}.verified.cs`; generator
  diagnostics go to `...#Diagnostics.verified.txt`. The fixed marker source and embedded attribute are stored once, by
  `SnapshotTests.WritesFixedSources`. An output that differs or has no snapshot is written beside it as `.received`
  and fails the test, as does a snapshot the run no longer produces
- To accept new or changed snapshots, run the tests with `ACCEPT_SNAPSHOTS=1`, then run them again without it
- Snapshot paths must fit the Windows path limit on a CI runner (`D:\a\{repo}\{repo}\...`, under 256 characters);
  `SnapshotTests.SnapshotPathsFitWindowsLimit` checks this, so keep test names and arguments short
- Line endings are LF everywhere (`.gitattributes`); the analyzers fail the build on CRLF
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
5. Update code generation in CodeGenerator.cs through `SourceWriter` (not a raw StringBuilder, not SyntaxFactory)
6. Ensure generic-first API usage: `resolver.Register<T>()` not `typeof(T)`
7. Verify snapshots match expected output; a passing snapshot test also requires the generated code to compile
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

1. Create reproduction test (use snapshots)
2. Fix with minimal changes
3. Ensure pipeline still caches properly (POCOs value-equatable)
4. Verify no regression in existing tests
5. Accept snapshot changes if expected

### Updating Generated Code Format

1. Modify code generation in `CodeGeneration/CodeGenerator.cs` through `SourceWriter`
2. Write fixed multi-line text as raw string literals (`"""`) passed to `Lines`
3. Ensure generic-first API: `resolver.Register<T>()` not `typeof(T)`
4. Run all snapshot tests - expect every `Splat.DI.Reg.g` snapshot to fail
5. Review each received file carefully and accept (`ACCEPT_SNAPSHOTS=1`) only if the changes are correct
6. Keep file-scoped namespaces, init properties and similar newer syntax out of the generated code
7. Measure with the benchmarks (see `benchmarks/README.md`)

## What to Avoid

- **ISymbol/SyntaxNode in pipeline outputs** - breaks incremental caching
- **Runtime reflection** in generated code - breaks AOT compatibility
- **SyntaxFactory or a raw StringBuilder for code generation** - write through `SourceWriter`
- **LINQ in the generator** - use loops; the transform runs for every marker call on every compilation change
- **Type-based API** - use `resolver.Register<T>()` not `Register(factory, typeof(T))`
- **Diagnostics in generator** - use separate analyzer project instead
- **Heavy dependencies** - keep generator lightweight (netstandard2.0 target)
- **Breaking changes** to generated code format without major version bump
- **Non-value-equatable models** in pipeline - breaks caching

## Important Notes

- **Value-Equatable POCOs:** CRITICAL for incremental generator caching - never include ISymbol/SyntaxNode
- **Generic-First API:** All generated code must use `resolver.Register<T>()` for AOT compatibility
- **Generation:** Write through `SourceWriter`, not SyntaxFactory
- **Separate Analyzer:** Diagnostics in separate project following Roslyn best practices
- **No shallow clones:** Repository requires full clone for git version information used by MinVer
- **Required .NET SDKs:** .NET 8.0, 9.0, and 10.0 (all three required for full build)
- **Snapshot Testing:** Review `.verified.cs` diffs carefully before accepting
- **Comprehensive Instructions:** `.github/COPILOT_INSTRUCTIONS.md` contains detailed development guidelines
- **Code Formatting:** Always run `dotnet format whitespace` and `dotnet format style` before committing

**Philosophy:** Generate simple, efficient, AOT-compatible dependency injection code at compile-time. Minimize runtime overhead, maximize build performance with incremental caching, and provide excellent developer experience with real-time diagnostics and automatic code fixes.
