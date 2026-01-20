; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 3.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
SPLATDI001 | Compiler | Error | Multiple constructors detected without [DependencyInjectionConstructor] attribute
SPLATDI002 | Compiler | Error | Property with [DependencyInjectionProperty] lacks accessible setter
SPLATDI003 | Compiler | Error | Multiple constructors marked with [DependencyInjectionConstructor]
SPLATDI004 | Compiler | Error | Constructor marked with [DependencyInjectionConstructor] is not accessible
SPLATDI005 | Compiler | Error | Constructor parameters must not have circular dependencies
SPLATDI006 | Compiler | Warning | Interface has been registered multiple times
SPLATDI007 | Compiler | Error | Constructor has a lazy parameter not registered with RegisterLazySingleton
