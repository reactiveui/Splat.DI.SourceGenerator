﻿//HintName: Splat.DI.Reg.g.cs

// <auto-generated />
namespace Splat
{
    internal static partial class SplatRegistrations
    {
        static partial void SetupIOCInternal(Splat.IDependencyResolver resolver) 
        {
            Splat.Locator.CurrentMutable.Register(() => new global::Test.TestConcrete1(), typeof(global::Test.ITest), "Test1");
            Splat.Locator.CurrentMutable.Register(() => new global::Test.TestConcrete2(), typeof(global::Test.ITest), "Test1");
        }
    }
}