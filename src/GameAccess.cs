using HarmonyLib;
using UnityEngine;

namespace FriendlyTurretCoverage
{
    /// <summary>Non-public game members the mod needs, bound once through Harmony's AccessTools.</summary>
    internal static class GameAccess
    {
        /// <summary>The top-down camera that renders the sector map.</summary>
        public static readonly AccessTools.FieldRef<MinimapControl, Camera> SectorMapCam =
            AccessTools.FieldRefAccess<MinimapControl, Camera>("bigCam");

        /// <summary>The camera that renders the game in space.</summary>
        public static readonly AccessTools.FieldRef<MinimapControl, Camera> MainCam =
            AccessTools.FieldRefAccess<MinimapControl, Camera>("mainCamera");

        /// <summary>The flight HUD. The game hides it on the sector map and while docked.</summary>
        public static readonly AccessTools.FieldRef<MinimapControl, GameObject> PlayerUI =
            AccessTools.FieldRefAccess<MinimapControl, GameObject>("playerUI");

        /// <summary>
        /// Where a weapon takes its ammunition from. On a station it is the station's item stock,
        /// and <c>Weapon.PayCost</c> won't fire while it holds less than one shot's worth.
        /// </summary>
        public static readonly AccessTools.FieldRef<Weapon, AmmoBuffer> Ammo =
            AccessTools.FieldRefAccess<Weapon, AmmoBuffer>("ammoBuffer");
    }
}
