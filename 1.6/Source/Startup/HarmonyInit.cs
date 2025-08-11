using Verse;
using HarmonyLib;

namespace BetterInfChemreactor.Startup
{
    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            var harmony = new Harmony("com.BetterInfChemreactor");
            harmony.PatchAll();
        }
    }
}
