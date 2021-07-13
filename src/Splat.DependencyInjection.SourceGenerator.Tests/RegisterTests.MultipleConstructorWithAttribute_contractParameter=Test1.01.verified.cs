//HintName: Splat.DI.Extensions.Registrations.SourceGenerated.cs
namespace Splat
{
    internal static partial class SplatRegistrations
    {
        static SplatRegistrations()
        {
            Splat.Locator.CurrentMutable.Register(() => new global::Test.TestConcrete((global::Test.IService1)Splat.Locator.Current.GetService(typeof(global::Test.IService1))), typeof(global::Test.ITest), "Test1");
        }
    }
}