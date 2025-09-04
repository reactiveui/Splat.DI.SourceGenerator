# Splat DI Source Generator: Dependency Injection Code Generation

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Prerequisites and Environment Setup
- **CRITICAL**: Requires .NET 9.0 SDK (not .NET 8.0). Install with:
  ```bash
  curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version latest --channel 9.0
  export PATH="$HOME/.dotnet:$PATH"
  ```
- **Platform Support**: This project **builds fully only on Windows**. Linux/macOS have partial support due to ILRepack tooling requirements.
- **Development Tools**: Visual Studio 2022 or VS Code with C# extension.

### Code Style and Analysis Enforcement
- **EditorConfig Compliance**: Repository uses comprehensive `.editorconfig` with 500+ rules for C# formatting, naming conventions, and code analysis
- **StyleCop Analyzers**: Enforces consistent C# code style with `stylecop.analyzers` (v1.2.0-beta.556)
- **Roslynator Analyzers**: Additional code quality rules with `Roslynator.Analyzers` (v4.14.0)
- **Analysis Level**: Set to `latest` with enhanced .NET analyzers enabled
- **CRITICAL**: All code must comply with **ReactiveUI contribution guidelines**: https://www.reactiveui.net/contribute/index.html

### Code Formatting (Fast - Always Run)
- **ALWAYS** run formatting before committing:
  ```bash
  cd src
  dotnet format whitespace --verify-no-changes
  dotnet format style --verify-no-changes
  ```
  Time: **2-5 seconds per command**.

## Project Overview

### What is Splat DI Source Generator?
This project is a source generator that produces Splat-based registrations for both constructor and property injection. It eliminates the need for reflection by using C# Source Generation to create dependency injection registrations at compile time.

### Key Features
- **Constructor Injection**: Automatic registration generation based on constructor parameters
- **Property Injection**: Support for `[DependencyInjectionProperty]` attribute on properties
- **Lazy Singleton Support**: `RegisterLazySingleton<TInterface, TImplementation>()` for singleton instances
- **No Reflection**: Full native speed through compile-time code generation
- **Splat Integration**: Seamless integration with the Splat service location framework

## Common Development Tasks

### Source Generator Development
1. **Follow Roslyn Source Generator best practices** - see Microsoft documentation on source generators
2. **Ensure StyleCop compliance** - all code must pass StyleCop analyzers (SA* rules)
3. **Run code analysis** - `dotnet build` must complete without analyzer warnings
4. **Add unit tests** - use Microsoft.CodeAnalysis.Testing for source generator tests
5. **Update documentation** - especially for public APIs with XML doc comments
6. **Test generated code** - verify the output compiles and behaves correctly

### Adding New Features
1. **Follow coding standards** - see ReactiveUI guidelines: https://www.reactiveui.net/contribute/index.html
2. **Ensure cross-platform compatibility** - while builds require Windows, generated code should work everywhere
3. **Add comprehensive tests** - test both the generator and the generated code
4. **Update README.md** - document new attributes or registration methods
5. **Consider performance** - source generators run during compilation

### Testing Source Generators
- Use `Microsoft.CodeAnalysis.Testing` framework for testing source generators
- Test both successful generation and error cases
- Verify generated code compiles and produces expected registrations
- Test edge cases like multiple constructors, missing dependencies, etc.

## CI/CD Integration

### GitHub Actions (Windows-based)
- Uses `reactiveui/actions-common` workflow
- Requires Windows runner for full build due to ILRepack tooling
- Installs all workloads automatically
- Runs comprehensive test suite and uploads coverage

### Local Development
- **Use** Linux/macOS for quick iteration on core source generator logic
- **Format code** before every commit
- **Test generated output** when changing generation logic
- **Full builds require Windows** due to IL merging requirements

## Troubleshooting

### Common Issues
1. **"ILRepack not found" errors**: Platform limitation - use Windows for full builds
2. **"Invalid framework identifier" errors**: Use explicit `-p:TargetFramework=netstandard2.0`
3. **Source generator not running**: Clean and rebuild, ensure generator is referenced correctly
4. **Build hangs**: Normal for large builds - wait up to 45 minutes
5. **Test failures**: May be platform-specific - verify on Windows

### Quick Fixes
- **Format issues**: Run `dotnet format whitespace` and `dotnet format style`
- **StyleCop violations**: Check `.editorconfig` rules and `src/stylecop.json` configuration
- **Analyzer warnings**: Build with `--verbosity normal` to see detailed analyzer messages
- **Missing XML documentation**: All public APIs require XML doc comments per StyleCop rules
- **Package restore issues**: Clear NuGet cache with `dotnet nuget locals all --clear`
- **Generator not working**: Verify `<Analyzer Include="..." />` references in consuming projects

### When to Escalate
- **Source generator compilation errors** affecting code generation
- **Cross-platform compatibility** issues affecting generated code
- **Performance regressions** in generator execution time
- **Test failures** that persist across platforms
- **Build system changes** affecting CI/CD pipeline

## Development Patterns

### Source Generator Structure
- **Incremental Generators**: Use `IIncrementalGenerator` for better performance
- **Syntax Receivers**: Implement efficient syntax filtering for DI attributes
- **Code Generation**: Generate clean, readable C# code with proper formatting
- **Error Handling**: Provide clear diagnostics for invalid usage patterns

### Dependency Injection Patterns
- **Constructor Injection**: Primary pattern for mandatory dependencies
- **Property Injection**: For optional dependencies with `[DependencyInjectionProperty]`
- **Lazy Dependencies**: Use `Lazy<T>` for expensive-to-create dependencies
- **Service Location**: Integration with Splat's `Locator.Current`

## Resources

### Governance & Contributing
- **Contribution Hub**: https://www.reactiveui.net/contribute/index.html
- **ReactiveUI Repository README**: https://github.com/reactiveui/ReactiveUI#readme
- **Contributing Guidelines**: https://github.com/reactiveui/ReactiveUI/blob/main/CONTRIBUTING.md
- **Code of Conduct**: https://github.com/reactiveui/ReactiveUI/blob/main/CODE_OF_CONDUCT.md

### Engineering & Style
- **ReactiveUI Coding/Style Guidance** (start here): https://www.reactiveui.net/contribute/
- **Build & Project Structure Reference**: https://github.com/reactiveui/ReactiveUI#readme

### Source Generator Resources
- **Source Generators Documentation**: https://learn.microsoft.com/dotnet/csharp/roslyn-sdk/source-generators-overview
- **Microsoft.CodeAnalysis.Testing**: https://github.com/dotnet/roslyn-sdk/tree/main/src/Microsoft.CodeAnalysis.Testing
- **Incremental Generators**: https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md
- **Source Generator Cookbook**: https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md

### Ecosystem
- **Splat** (service location/DI and logging): https://github.com/reactiveui/splat
- **DynamicData** (reactive collections): https://github.com/reactivemarbles/DynamicData
- **ReactiveUI.SourceGenerators**: https://github.com/reactiveui/ReactiveUI.SourceGenerators

### Source Generators & AOT/Trimming
- **ReactiveUI.SourceGenerators**: https://github.com/reactiveui/ReactiveUI.SourceGenerators
- **.NET Native AOT Overview**: https://learn.microsoft.com/dotnet/core/deploying/native-aot/
- **Prepare Libraries for Trimming**: https://learn.microsoft.com/dotnet/core/deploying/trimming/prepare-libraries-for-trimming
- **Trimming Options (MSBuild)**: https://learn.microsoft.com/dotnet/core/deploying/trimming/trimming-options
- **Fixing Trim Warnings**: https://learn.microsoft.com/dotnet/core/deploying/trimming/trim-warnings

### Copilot Coding Agent
- **Best Practices for Copilot Coding Agent**: https://gh.io/copilot-coding-agent-tips

### CI & Misc
- **GitHub Actions** (Windows builds and workflow runs): https://github.com/reactiveui/Splat.DI.SourceGenerator/actions
- **ReactiveUI Website Source** (useful for docs cross-refs): https://github.com/reactiveui/website