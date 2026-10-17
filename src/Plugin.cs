using System;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace FriendlyTurretCoverage
{
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "starvalor.friendlyturretcoverage";
        public const string Name = "Friendly Turret Coverage";
        public const string Version = "1.0.0";

        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> ShowCoverage;
        internal static ConfigEntry<KeyboardShortcut> ToggleKey;
        internal static ConfigEntry<bool> ColorByType;
        internal static ConfigEntry<Color> SingleColor;
        internal static ConfigEntry<float> FillOpacity;
        internal static ConfigEntry<float> EdgeOpacity;
        internal static ConfigEntry<bool> AdditiveBlending;
        internal static ConfigEntry<bool> ShowBlocking;
        internal static ConfigEntry<bool> IncludeUnpowered;
        internal static ConfigEntry<bool> AmmoWarning;
        internal static ConfigEntry<float> LowAmmoSeconds;
        internal static ConfigEntry<float> ArcStepDegrees;
        internal static ConfigEntry<float> RefreshSeconds;
        internal static ConfigEntry<float> HoverPixels;
        internal static ConfigEntry<float> PanelOffsetX;
        internal static ConfigEntry<float> PanelOffsetY;
        internal static readonly ConfigEntry<bool>[] KindShown = new ConfigEntry<bool>[DamageKinds.Count];
        internal static readonly ConfigEntry<Color>[] KindColor = new ConfigEntry<Color>[DamageKinds.Count];

        private void Awake()
        {
            Log = Logger;
            ShowCoverage = Config.Bind("General", "ShowCoverage", false,
                "Show the fields of fire of friendly station turrets. The 'Turret coverage' toggle on screen changes this.");
            ToggleKey = Config.Bind("General", "ToggleKey", KeyboardShortcut.Empty,
                "Optional key that switches the coverage overlay on and off, for example F7.");
            ColorByType = Config.Bind("General", "ColorByType", true,
                "Draw each damage type in its own colour instead of one colour for all. The 'Colour by type' toggle on screen changes this.");
            SingleColor = Config.Bind("General", "SingleColor", new Color(0.35f, 0.8f, 1f),
                "Colour of every field of fire when ColorByType is off (RRGGBBAA).");
            FillOpacity = Config.Bind("General", "FillOpacity", 0.12f,
                "Opacity of one field of fire. Where fields overlap they add up and look denser.");
            EdgeOpacity = Config.Bind("General", "EdgeOpacity", 0.45f,
                "Opacity of the soft edge drawn along the end of each field of fire.");
            AdditiveBlending = Config.Bind("General", "AdditiveBlending", false,
                "Add the colours of overlapping fields together (they get brighter and whiter) instead of mixing them.");
            ShowBlocking = Config.Bind("General", "ShowBlocking", true,
                "Cut each field of fire short where asteroids or stations block the turret's line of fire.");
            IncludeUnpowered = Config.Bind("General", "IncludeUnpowered", false,
                "Also show turrets that are switched off or have no power. They don't fire.");
            AmmoWarning = Config.Bind("General", "AmmoWarning", true,
                "Flash a warning sign on turrets whose weapons need ammunition: yellow when it is running low, red when the station has run out and they no longer fire.");
            LowAmmoSeconds = Config.Bind("General", "LowAmmoSeconds", 30f,
                "A turret gets the yellow warning when its station's ammunition would last less than this many seconds with every powered turret using it firing non-stop. 0 shows only the red one.");

            string[] keys = { "Beam", "Kinetic", "Missile", "PointDefence", "Repair" };
            Color[] colors =
            {
                new Color(0.3f, 0.75f, 1f),
                new Color(1f, 0.65f, 0.15f),
                new Color(1f, 0.25f, 0.25f),
                new Color(0.8f, 0.35f, 1f),
                new Color(0.3f, 1f, 0.4f),
            };
            for (int i = 0; i < DamageKinds.Count; i++)
            {
                KindShown[i] = Config.Bind("DamageTypes", keys[i], true,
                    $"Show turrets with {DamageKinds.Describe((DamageKind)i)}. The toggle on screen changes this.");
                KindColor[i] = Config.Bind("Colors", keys[i], colors[i],
                    $"Colour of {DamageKinds.Describe((DamageKind)i)} when ColorByType is on (RRGGBBAA).");
            }

            ArcStepDegrees = Config.Bind("Advanced", "ArcStepDegrees", 1f,
                "Angle between the line-of-fire checks that shape each field of fire. Smaller is smoother but slower.");
            RefreshSeconds = Config.Bind("Advanced", "RefreshSeconds", 2f,
                "How often each field of fire is checked again for asteroids and stations in the way.");
            HoverPixels = Config.Bind("Advanced", "HoverPixels", 24f,
                "How close to a turret, in screen pixels, the mouse must be to show its traverse sweep.");
            PanelOffsetX = Config.Bind("Advanced", "PanelOffsetX", 0f,
                "Moves the on-screen toggles right (or left, if negative) from their place above the scanner.");
            PanelOffsetY = Config.Bind("Advanced", "PanelOffsetY", 0f,
                "Moves the on-screen toggles up (or down, if negative) from their place above the scanner.");

            try
            {
                // Bind the game's non-public members now, so a game update that renames one
                // disables the mod at startup instead of failing while the overlay is drawn.
                RuntimeHelpers.RunClassConstructor(typeof(GameAccess).TypeHandle);
            }
            catch (Exception e)
            {
                Log.LogError($"{Name} is not compatible with this version of the game and is disabled: {e}");
                return;
            }

            // Lets a config change made while the game runs (for example with Configuration Manager) show at once.
            Config.SettingChanged += (sender, args) => CoverageOverlay.Instance?.ApplyConfig();

            var harmony = new Harmony(Guid);
            harmony.PatchAll(typeof(MinimapPatches));
            Log.LogInfo($"{Name} {Version} loaded");
        }
    }
}
