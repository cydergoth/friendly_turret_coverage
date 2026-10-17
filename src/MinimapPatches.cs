using System;
using HarmonyLib;

namespace FriendlyTurretCoverage
{
    /// <summary>Hooks the scanner, which the game builds together with the sector map, so the overlay is added with them.</summary>
    [HarmonyPatch(typeof(MinimapControl))]
    internal static class MinimapPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        private static void Start_Postfix(MinimapControl __instance)
        {
            try
            {
                CoverageOverlay.Create(__instance);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Failed to create the turret coverage overlay: " + e);
            }
        }
    }
}
