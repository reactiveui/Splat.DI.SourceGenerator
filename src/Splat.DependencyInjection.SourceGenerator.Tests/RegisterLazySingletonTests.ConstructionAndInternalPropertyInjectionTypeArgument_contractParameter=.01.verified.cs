﻿//HintName: Splat.DI.Extensions.Registrations.SourceGenerated.cs

// <auto-generated />
namespace Splat
{
    internal static partial class SplatRegistrations
    {
        static partial void SetupIOCInternal()
        {
            {
                global::System.Lazy<Test.TestConcrete> lazy = new global::System.Lazy<Test.TestConcrete>(() => new global::Test.TestConcrete((global::Test.IService1)Splat.Locator.Current.GetService(typeof(global::Test.IService1)), (global::Test.IService2)Splat.Locator.Current.GetService(typeof(global::Test.IService2))){ ServiceProperty=(global::Test.IServiceProperty)Splat.Locator.Current.GetService(typeof(global::Test.IServiceProperty))} );
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<Test.TestConcrete>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::Test.TestConcrete));
            }
        }
    }
}