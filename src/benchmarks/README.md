# Benchmarks

`Splat.DI.SourceGenerator.Benchmarks` measures two things: what a consumer's build pays while the generator runs, and
what a consumer's application pays in the code the generator wrote.

## Running them

```sh
cd src/benchmarks/Splat.DI.SourceGenerator.Benchmarks
dotnet run -c Release -- --filter '*' --artifacts ~/.cache/splat-bench/run
```

Release configuration is required. Every run attaches BenchmarkDotNet's memory diagnoser and an EventPipe profiler
with the `GcVerbose` profile plus CPU sampling (`Configs/ProfilerConfig.cs`), so each case leaves a `.nettrace` and a
`.speedscope.json` in the artifacts folder. Set `BENCHMARK_PROFILERS=false` for a timing-only A/B run.

The benchmark project builds against Roslyn 5.9 (BenchmarkDotNet needs 5.6 or newer); the generator itself is built
against 4.14 and runs on any newer compiler, as it does on a current SDK.

## Reading the allocations

The trace's allocation ticks carry stacks, which is what tells the generator's allocations from Roslyn's:

```sh
# Allocation types, sites and inclusive frames for one case
dotnet run ~/source/rxui/tools/nettrace-analyzer.cs -- --top 60 '<artifacts>/...Cold(Registrations_ 128)-*.nettrace'

# Everything allocated beneath one generator method
dotnet run --project ~/source/rxui/tools/TraceFocus -c Release -- --file '<artifacts>/...speedscope.json' \
    --profile alloc --no-default-includes --include TryExtractConstructorParameters
```

## The cases

`GenerationBenchmarks` runs the generator over a corpus with 1, 16 or 128 registrations. Registrations cycle through
a parameterless type, constructor and property injection, a lazy singleton with a mode taking a collection, and a
contract taking a lazy dependency; a third of the types have two constructors, one marked. Every service file also
calls Splat's own `resolver.Register<T>(() => ...)`.

- `Cold` builds a fresh driver per operation: a whole pass.
- `EditUnrelated` reruns a primed driver after an edit to a file with no registrations.
- `EditBootstrapper` reruns it after an edit that moves every registration down a line without changing one.

`RuntimeBenchmarks` resolves services through the code the generator wrote for the benchmark project itself.
`Setup` makes every registration on a new resolver; the `Resolve` cases ask for a service.

## Results

Measured on an AMD Ryzen 7 5800X, .NET 10.0.12, with the profilers attached. The baseline is the generator before
this rewrite, built from the same benchmark sources.

| Generation | Registrations | Before | After |
|------------|--------------:|-------:|------:|
| `Cold` | 1 | 1,466.7 us / 332.8 KB | 906.4 us / 242.0 KB |
| `EditUnrelated` | 1 | 710.0 us / 155.7 KB | 396.2 us / 105.3 KB |
| `EditBootstrapper` | 1 | 724.0 us / 166.4 KB | 387.0 us / 107.3 KB |
| `Cold` | 16 | 4,689.2 us / 770.5 KB | 2,203.7 us / 432.5 KB |
| `EditUnrelated` | 16 | 3,302.5 us / 519.6 KB | 1,515.2 us / 267.8 KB |
| `EditBootstrapper` | 16 | 3,301.8 us / 605.5 KB | 1,561.8 us / 281.4 KB |
| `Cold` | 128 | 21,045.1 us / 3,979.9 KB | 9,583.4 us / 1,824.4 KB |
| `EditUnrelated` | 128 | 18,523.2 us / 3,128.3 KB | 8,226.4 us / 1,475.9 KB |
| `EditBootstrapper` | 128 | 18,523.8 us / 3,811.3 KB | 8,471.5 us / 1,554.0 KB |

| Runtime | Before | After |
|---------|-------:|------:|
| `Setup` | 1,701.9 ns / 8,144 B | 1,639.1 ns / 8,128 B |
| `ResolveLeaf` | 27.9 ns / 24 B | 28.9 ns / 24 B |
| `ResolveBranch` | 68.8 ns / 48 B | 69.0 ns / 48 B |
| `ResolveTree` | 201.8 ns / 200 B | 203.1 ns / 200 B |
| `ResolveSingleton` | 25.0 ns / 0 B | 25.3 ns / 0 B |
| `ResolveLazyConsumer` | 64.8 ns / 24 B | 65.0 ns / 24 B |
| `ResolveKeyed` | 61.5 ns / 48 B | 64.9 ns / 48 B |

The baseline generator also wrote code that does not compile for a lazy singleton given a thread safety mode
(`PublicationOnly` instead of the enum member), which the corpus has; its generation figures are still comparable,
since the generator never compiles its own output.

## Where the generation time went, and where it goes now

In the baseline trace, 86% of an unrelated edit's allocation was the transform binding calls: a `CreateSyntaxProvider`
transform re-runs for every matching node whenever the compilation changes, and the old predicate let through every
call named `Register`, including Splat's own `resolver.Register<T>(() => new T(...))`. Binding those lambdas and their
overloads dominated the trace. The predicate now requires a type argument list and rejects lambda arguments, so those
calls never reach the transform, and a receiver not spelled `SplatRegistrations` is bound on its own before the call.

What remains of a pass, by allocation beneath each generator frame in the `Cold`/128 trace:

- Binding the marker call (`GetSymbolInfo`). Required: the type arguments, overload and argument bindings come from it.
- Building each registered type's member table on first touch (`GetMembers(".ctor")`, beneath
  `TryExtractConstructorParameters`). Required by any inspection of a type's constructors; looking constructors up by
  name avoids building a list of all members as well.
- Binding attributes (`GetAttributes`, beneath `HasAttribute`). Required to tell our attribute from another, including
  through a using alias. The walk stops at the first base type from a reference, whose members are never loaded, and
  a type from a reference with several constructors is turned away without decoding theirs.
- The registrations file string (`PooledBuilder.ToStringAndReturn`). One string of the file's size is the least
  `AddSource` accepts; the builder behind it is pooled per thread and reused across passes.
- Type names (`ToDisplayString`), about a twentieth of the constructor-extraction figure. Each is kept in the model; a lazy's and
  a collection's type argument is cut from the name already formatted rather than formatted again. A per-compilation
  name cache was considered and rejected: it would cost a dictionary and its entries per compilation to save a small
  share, and hit only when many registrations share a dependency.
- The models: one `RegistrationInfo` and one `RegistrationSite` per registration, one array of `ConstructorParameter`
  values (structs) per registration with parameters, and nothing for a type without injected properties. The split
  into `RegistrationSite` costs one object per registration and is what lets `EditBootstrapper` skip regeneration.
- The graph checks allocate their lookups only when there is something to look for, and are a fraction of a percent.

## What the generated code allocates

Resolving a service allocates the objects it constructs and nothing else from the generated code: `ResolveLeaf` is
the `Leaf` (24 B), `ResolveBranch` the `Branch` and its `Leaf`, `ResolveSingleton` nothing once created.
`ResolveTree` also includes the collection Splat returns from `GetServices`. Every figure includes Splat's lookup.

`Setup` allocates one closure for `resolver` and every lazy, one delegate for each registration that captures, and
for each lazy singleton the `Lazy<T>` and three delegates. A registration with nothing to resolve captures nothing, so
its delegate is created once for the life of the application. The rest is Splat's registration storage.

Resolution through the resolver cannot be avoided: constructing a dependency directly would ignore a registration the
application makes later, which Splat's semantics allow.
