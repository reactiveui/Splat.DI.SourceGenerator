# Register receipt services

Run `dotnet run --project examples/ReceiptServices -f net11.0` from `src`. Use `net10.0` to run on .NET 10. The project references the generator and analyzer from this checkout.

| Example | What to look for |
| --- | --- |
| [Register and resolve services](ReceiptServices/Program.cs) | Select concrete types or interface mappings. Give a registration a contract name, choose a transient or lazy singleton lifetime, register a constant, and use either the current resolver or a separate resolver. |
| [Construct a receipt printer](ReceiptServices/ReceiptPrinter.cs) | Select one of two constructors with `DependencyInjectionConstructor`, defer a dependency with `Lazy<T>` and inject a property with `DependencyInjectionProperty`. |
| [Assign receipt numbers](ReceiptServices/ReceiptNumbers.cs) | Check that two printers use the same sales counter and that named sales channels have separate sequences. |

For an interface with one default singleton, use `SplatRegistrations.RegisterLazySingleton<IReceiptNumbers, ReceiptNumbers>()`. Pass `LazyThreadSafetyMode.ExecutionAndPublication` as its only argument to specify how the instance is initialized. These are alternatives to the example's default transient registration.
