//HintName: Splat.DI.Extensions.Registrations.SourceGenerated.cs
namespace Splat
{
    internal static partial class SplatRegistrations
    {
        static SplatRegistrations()
        {
            Splat.Locator.CurrentMutable.Register(() => new global::Test.TestConcrete1(), typeof(global::Test.ITest), "Test2");
            Splat.Locator.CurrentMutable.Register(() => new global::Test.TestConcrete2(), typeof(global::Test.ITest), "Test2");
        }
    }
}