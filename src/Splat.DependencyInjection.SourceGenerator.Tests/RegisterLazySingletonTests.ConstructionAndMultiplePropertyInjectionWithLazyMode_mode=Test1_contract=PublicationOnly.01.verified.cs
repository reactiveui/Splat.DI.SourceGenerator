//HintName: Splat.DI.Extensions.Registrations.SourceGenerated.cs
namespace Splat
{
    internal static partial class SplatRegistrations
    {
        static SplatRegistrations()
        {
            {
                global::System.Lazy<Test.ITest> lazy = new global::System.Lazy<Test.ITest>(() => new global::Test.TestConcrete((global::Test.IService1)Splat.Locator.Current.GetService(typeof(global::Test.IService1)), (global::Test.IService2)Splat.Locator.Current.GetService(typeof(global::Test.IService2))){ ServiceProperty1=(global::Test.IServiceProperty1)Splat.Locator.Current.GetService(typeof(global::Test.IServiceProperty1)), ServiceProperty2=(global::Test.IServiceProperty2)Splat.Locator.Current.GetService(typeof(global::Test.IServiceProperty2)), ServiceProperty3=(global::Test.IServiceProperty3)Splat.Locator.Current.GetService(typeof(global::Test.IServiceProperty3))} , System.Threading.LazyThreadSafetyMode.PublicationOnly);
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<Test.ITest>), "Test1");
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::Test.ITest), "Test1");
            }
        }
    }
}