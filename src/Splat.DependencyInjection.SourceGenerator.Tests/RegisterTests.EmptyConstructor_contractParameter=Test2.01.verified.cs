//HintName: Splat.DI.Extensions.Registrations.SourceGenerated.cs
namespace Splat
{
    internal static partial class SplatRegistrations
    {
        static SplatRegistrations()
        {
            Splat.Locator.CurrentMutable.Register(() => new global::Test.TestConcrete(), typeof(global::Test.ITest), "Test2");
        }
    }
}