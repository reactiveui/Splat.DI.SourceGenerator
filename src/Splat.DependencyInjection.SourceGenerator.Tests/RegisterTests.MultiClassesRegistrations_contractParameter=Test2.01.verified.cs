﻿//HintName: Splat.DI.Extensions.Registrations.SourceGenerated.cs

// <auto-generated />
namespace Splat
{
    internal static partial class SplatRegistrations
    {
        static partial void SetupIOCInternal()
        {
            Splat.Locator.CurrentMutable.Register(() => new global::Test.TestConcrete1((global::Test.IService1)Splat.Locator.Current.GetService(typeof(global::Test.IService1)), (global::Test.IService2)Splat.Locator.Current.GetService(typeof(global::Test.IService2))), typeof(global::Test.ITest1), "Test2");
            Splat.Locator.CurrentMutable.Register(() => new global::Test.TestConcrete2((global::Test.IService1)Splat.Locator.Current.GetService(typeof(global::Test.IService1)), (global::Test.IService2)Splat.Locator.Current.GetService(typeof(global::Test.IService2))), typeof(global::Test.ITest2), "Test2");
            Splat.Locator.CurrentMutable.Register(() => new global::Test.TestConcrete3((global::Test.IService1)Splat.Locator.Current.GetService(typeof(global::Test.IService1)), (global::Test.IService2)Splat.Locator.Current.GetService(typeof(global::Test.IService2))), typeof(global::Test.ITest3), "Test2");
        }
    }
}