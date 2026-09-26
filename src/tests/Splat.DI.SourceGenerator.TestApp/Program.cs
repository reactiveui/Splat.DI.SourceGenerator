// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Splat;

namespace TestApp;

/// <summary>
/// Entry point for the Splat DI Source Generator test application.
/// This application demonstrates the source generator's ability to work alongside ReactiveUI.SourceGenerators
/// without causing duplicate GeneratedCodeAttribute errors (CS0579).
/// </summary>
public static class Program
{
    /// <summary>Main entry point for the application.</summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    public static void Main(string[] args)
    {
        var output = Console.Out;

        // Register dependencies using source generator
        SplatRegistrations.RegisterConstant(output);
        SplatRegistrations.Register<IService, ServiceImplementation>();
        SplatRegistrations.Register<ILogger, ConsoleLogger>();
        SplatRegistrations.RegisterLazySingleton<ServiceWithDependency>();

        // Setup IOC
        SplatRegistrations.SetupIOC();

        // Test resolution
        var service = Locator.Current.GetService<IService>();
        var serviceWithDependency = Locator.Current.GetService<ServiceWithDependency>();

        if (service is not null)
        {
            output.WriteLine(service.GetMessage());
        }

        serviceWithDependency?.DoWork();

        output.WriteLine("TestApp completed successfully!");
    }
}
