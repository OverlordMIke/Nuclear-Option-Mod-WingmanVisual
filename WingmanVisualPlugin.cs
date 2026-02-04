using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Mirage;
using System.Linq;
using System.Collections.Generic;

namespace WingmanVisual
{
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class WingmanVisual : BaseUnityPlugin
    {
        // Mod identification
        private const string MyGUID = "com.gnol.wingmanvisual";
        private const string PluginName = "WingmanVisual";
        private const string VersionString = "1.0.0";

        public static ManualLogSource Log { get; private set; }

        // Config entries
        private const string CurrentConfigVersion = "c1.0";
        public static ConfigEntry<bool> Enabled { get; set; }

        internal static ConfigEntry<KeyboardShortcut> AddSpectatedToWing;

        public static string CurrentlySpectating = "";

        public static HashSet<string> wingMembers = new HashSet<string>();

        private void Awake()
        {
            var go = this.gameObject;
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.SetActive(true);
            Log = Logger;
            Logger.LogInfo($"{PluginName} v{VersionString} is loading...");

            var savedVersion = Config.Bind(
                section: "Internal",
                key: "ConfigVersion",
                defaultValue: "",
                "Do not touch - used for auto-updating config."
            ).Value;

            Enabled = Config.Bind(
                section: "General",
                key: "Enabled",
                defaultValue: true,
                configDescription: new ConfigDescription(
                    "Whether the mod is active."
                )
            );

            AddSpectatedToWing = Config.Bind(
                section: "Controls",
                key: "AddSpectatedToWing",
                defaultValue: new KeyboardShortcut(KeyCode.P),
                configDescription: new ConfigDescription(
                    "When spectating anything or anyone, press this key to add them to your wing.\n" +
                    "Examples of keys you can enter:\n" +
                    "- Letters: A, B, C, ...\n" +
                    "- Numbers: Alpha0, Alpha1, ...\n" +
                    "- Function keys: F1, F2, ...\n" +
                    "- Special keys: Space, Enter, Escape, Tab\n" +
                    "- Modifiers: LeftShift, RightShift, LeftControl, RightControl\n" +
                    "- Arrow keys: UpArrow, DownArrow, LeftArrow, RightArrow"
                )
            );


            if (savedVersion != CurrentConfigVersion)
            {
                Logger.LogInfo($"Config version changed ({savedVersion} -> {CurrentConfigVersion}). Updating config.");

                var savedVersionConfig = Config.Bind(
                    section: "Internal",
                    key: "ConfigVersion",
                    defaultValue: CurrentConfigVersion,
                    configDescription: new ConfigDescription(
                        "Do not touch - used for auto-updating config."
                    )
                );

                savedVersionConfig.Value = CurrentConfigVersion;

                Config.Save();

                Logger.LogInfo("Config automatically updated.");
            }

            var harmony = new Harmony(MyGUID);
            harmony.PatchAll();

            Logger.LogInfo($"{PluginName} v{VersionString} loaded successfully. State: {(Enabled.Value ? "Enabled" : "Disabled")}");
        }

        private void Update()
        {
            if (AddSpectatedToWing.Value.IsDown())
            {
                if (!string.IsNullOrWhiteSpace(CurrentlySpectating))
                {
                    if (wingMembers.Remove(CurrentlySpectating))
                    {
                        Log.LogInfo($"Removed {CurrentlySpectating} from wing.\nAll members:");
                        Log.LogInfo(string.Join(", ", wingMembers));
                    }
                    else
                    {
                        wingMembers.Add(CurrentlySpectating);
                        Log.LogInfo($"Added {CurrentlySpectating} to wing.\nAll members:");
                        Log.LogInfo(string.Join(", ", wingMembers));
                    }
                }
            }
        }
        public static string StripAircraftName(string unformatted)
        {
            string input = unformatted;
            string output;

            int bracketIndex = input.LastIndexOf('[');
            if (bracketIndex > 0)
            {
                output = input.Substring(0, bracketIndex).TrimEnd();
            }
            else
            {
                output = input;
            }
            return output;
        }

    }

    [HarmonyPatch(typeof(MapIcon), nameof(MapIcon.UpdateColor))]
    internal static class MapIcon_UpdateColor_Patch
    {
        static void Postfix(MapIcon __instance)
        {
            var unitIcon = __instance as UnitMapIcon;
            if (unitIcon == null)
                return;

            if (unitIcon.unit == null || unitIcon.iconImage == null)
                return;

            string unitName = WingmanVisual.StripAircraftName(unitIcon.unit.unitName);
            bool isFriend = WingmanVisual.wingMembers.Contains(unitName);

            if (isFriend)
            {
                Color green = new Color(1.0f, 0.27f, 0.63f);
                green.a = unitIcon.iconImage.color.a;

                unitIcon.iconImage.color = green;
            }
        }
    }

    [HarmonyPatch(typeof(CameraStateManager), nameof(CameraStateManager.SetFollowingUnit))]
    internal static class CameraStateManager_SetFollowingUnit_Patch
    {
        static void Postfix(Unit unit)
        {
            if (unit != null)
            {
                string username = WingmanVisual.StripAircraftName(unit.unitName);
                WingmanVisual.CurrentlySpectating = username;
            }
        }
    }
}