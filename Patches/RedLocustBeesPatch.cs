using HarmonyLib;

namespace LethalFauna.Patches
{
    [HarmonyPatch(typeof(RedLocustBees))]
    internal class RedLocustBeesPatch
    {
        // these two methods dont account for a hive being destroyed, so lets fix that
        [HarmonyPatch("IsHiveMissing")]
        [HarmonyPrefix]
        static bool IsHiveMissingPatch(RedLocustBees __instance, ref bool __result)
        {
            if (__instance.hive == null)
            {
                __result = true;
                return false;
            }
            return true;
        }

        [HarmonyPatch("IsHivePlacedAndInLOS")]
        [HarmonyPrefix]
        static bool IsHivePlacedAndInLOSPatch(RedLocustBees __instance, ref bool __result)
        {
            if (__instance.hive == null)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}
