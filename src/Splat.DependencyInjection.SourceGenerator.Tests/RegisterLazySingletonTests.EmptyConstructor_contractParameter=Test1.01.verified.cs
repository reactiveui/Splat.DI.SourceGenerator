//HintName: Splat.DI.Extensions.Registrations.SourceGenerated.cs
namespace Splat
{
    internal static partial class SplatRegistrations
    {
        static SplatRegistrations()
        {
            {
                global::System.Lazy<Test.ITest> lazy = new global::System.Lazy<Test.ITest>(() => new global::Test.TestConcrete());
                Splat.Locator.CurrentMutable.Register(() => lazy, typeof(global::System.Lazy<Test.ITest>), "Test1");
                Splat.Locator.CurrentMutable.Register(() => lazy.Value, typeof(global::Test.ITest), "Test1");
            }
        }
    }
}