//HintName: Splat.DI.Extensions.Registrations.SourceGenerated.cs
namespace Splat
{
    internal static partial class SplatRegistrations
    {
        static SplatRegistrations()
        {
            {
                global::System.Lazy<Test.ITest1> lazy = new global::System.Lazy<Test.ITest1>(() => new global::Test.TestConcrete1((global::Test.IService1)Splat.Locator.Current.GetService(typeof(global::Test.IService1)), (global::Test.IService2)Splat.Locator.Current.GetService(typeof(global::Test.IService2))));
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<Test.ITest1>), "Test1");
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::Test.ITest1), "Test1");
            }
            {
                global::System.Lazy<Test.ITest2> lazy = new global::System.Lazy<Test.ITest2>(() => new global::Test.TestConcrete2((global::Test.IService1)Splat.Locator.Current.GetService(typeof(global::Test.IService1)), (global::Test.IService2)Splat.Locator.Current.GetService(typeof(global::Test.IService2))));
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<Test.ITest2>), "Test1");
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::Test.ITest2), "Test1");
            }
            {
                global::System.Lazy<Test.ITest3> lazy = new global::System.Lazy<Test.ITest3>(() => new global::Test.TestConcrete3((global::Test.IService1)Splat.Locator.Current.GetService(typeof(global::Test.IService1)), (global::Test.IService2)Splat.Locator.Current.GetService(typeof(global::Test.IService2))));
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<Test.ITest3>), "Test1");
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::Test.ITest3), "Test1");
            }
        }
    }
}