﻿//HintName: Splat.DI.Extensions.Registrations.SourceGenerated.cs
namespace Splat
{
    internal static partial class SplatRegistrations
    {
        static SplatRegistrations()
        {
            Splat.Locator.CurrentMutable.Register(() => new global::Test.TestConcrete((global::Test.IService1)Splat.Locator.Current.GetService(typeof(global::Test.IService1)), (global::Test.IService2)Splat.Locator.Current.GetService(typeof(global::Test.IService2))){ ServiceProperty1=(global::Test.IServiceProperty1)Splat.Locator.Current.GetService(typeof(global::Test.IServiceProperty1)), ServiceProperty2=(global::Test.IServiceProperty2)Splat.Locator.Current.GetService(typeof(global::Test.IServiceProperty2)), ServiceProperty3=(global::Test.IServiceProperty3)Splat.Locator.Current.GetService(typeof(global::Test.IServiceProperty3))} , typeof(global::Test.ITest), "Test1");
        }
    }
}