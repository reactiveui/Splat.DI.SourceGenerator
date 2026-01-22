// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
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
    /// <summary>
    /// Main entry point for the application.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    public static void Main(string[] args)
    {
        // Register dependencies using source generator
        SplatRegistrations.Register<IService, ServiceImplementation>();
        SplatRegistrations.Register<ILogger, ConsoleLogger>();
        SplatRegistrations.RegisterLazySingleton<ServiceWithDependency>();

        // Setup IOC
        SplatRegistrations.SetupIOC();

        // Test resolution
        var service = Locator.Current.GetService<IService>();
        var logger = Locator.Current.GetService<ILogger>();
        var serviceWithDep = Locator.Current.GetService<ServiceWithDependency>();

        if (service != null)
        {
            Console.WriteLine(service.GetMessage());
        }

        if (serviceWithDep != null)
        {
            serviceWithDep.DoWork();
        }

        Console.WriteLine("TestApp completed successfully!");
    }
}
