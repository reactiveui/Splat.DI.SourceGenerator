﻿//HintName: Splat.DI.Reg.g.cs

// <auto-generated />
namespace Splat
{
    internal static partial class SplatRegistrations
    {
        static partial void SetupIOCInternal(Splat.IDependencyResolver resolver) 
        {
            {
                global::System.Lazy<Test.ITest> lazy = new global::System.Lazy<Test.ITest>(() => new global::Test.TestConcrete((global::Test.IService1)resolver.GetService(typeof(global::Test.IService1))));
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<Test.ITest>));
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::Test.ITest));
            }
        }
    }
}