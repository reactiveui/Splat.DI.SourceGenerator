﻿//HintName: Splat.DI.Extensions.Registrations.SourceGenerated.cs

// <auto-generated />
namespace Splat
{
    internal static partial class SplatRegistrations
    {
        static partial void SetupIOCInternal()
        {
            {
                global::System.Lazy<Test.ITest> lazy = new global::System.Lazy<Test.ITest>(() => new global::Test.TestConcrete((global::Test.IService1)Splat.Locator.Current.GetService(typeof(global::Test.IService1)), (global::Test.IService2)Splat.Locator.Current.GetService(typeof(global::Test.IService2))){ ServiceProperty=(global::Test.IServiceProperty)Splat.Locator.Current.GetService(typeof(global::Test.IServiceProperty))} );
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<Test.ITest>), "Test2");
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::Test.ITest), "Test2");
            }
        }
    }
}