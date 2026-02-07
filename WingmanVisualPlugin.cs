using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.Networking;
using Mirage;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace WingmanVisual
{
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class WingmanVisual : BaseUnityPlugin
    {
        // Mod identification
        private const string MyGUID = "com.gnol.wingmanvisual";
        private const string PluginName = "WingmanVisual";
        private const string VersionString = "1.1.2";

        public static ManualLogSource Log { get; private set; }

        // Config entries
        private const string CurrentConfigVersion = "c1.2";

        public static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<KeyboardShortcut> AddSpectatedToWing;
        internal static ConfigEntry<KeyboardShortcut> AddRemoveFriend;
        public static ConfigEntry<string> FriendsConfigString;
        public static ConfigEntry<string> WingColorHexConfig;
        public static ConfigEntry<string> FriendColorHexConfig;
        public static ConfigEntry<string> EnemyFriendColorHexConfig;
        public static ConfigEntry<bool> ShouldVoiceSocialFeatures;

        // Audio Files
        private static Dictionary<string, AudioClip> loadedClips = new Dictionary<string, AudioClip>();

        private static string soundsFolder = Path.Combine(Paths.PluginPath, "WingmanVisual", "sounds");

        // Variables
        public static string CurrentlySpectating = "";

        public static string SpectateTargetFaction = "";

        public static bool IsSpectating = false;

        public static HashSet<string> wingMembers = new HashSet<string>();

        public static HashSet<string> friendsList = new HashSet<string>();

        public static Color defaultWingColor = new Color(193, 128, 255, 255);
        public static Color wingColor;

        public static Color defaultFriendColor = new Color(255, 241, 43, 255);
        public static Color friendColor;

        public static Color defaultEnemyFriendColor = new Color(255, 179, 179, 255);
        public static Color enemyFriendColor;

        public static string ourFaction;

        public static bool IsChatOpen = false;

        private void Awake()
        {
            var go = this.gameObject;
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.SetActive(true);
            Log = Logger;
            Logger.LogInfo($"{PluginName} v{VersionString} is loading...");

            LoadCustomSounds();

            var savedVersion = Config.Bind(
                section: "Internal",
                key: "ConfigVersion",
                defaultValue: "",
                configDescription: new ConfigDescription(
                    "Do not touch - used for auto-updating config.\n" +
                    "If you accidentally change this, just set it back to the above default.\n" +
                    "Alternatively, you can delete this config and launch the game to generate a clean one.",
                    null,
                    new ConfigurationManagerAttributes { Browsable = false }
                )
            ).Value;

            Enabled = Config.Bind(
                section: "General",
                key: "Enabled",
                defaultValue: true,
                configDescription: new ConfigDescription(
                    "Whether the mod is active.\n" +
                    "---"
                )
            );

            AddSpectatedToWing = Config.Bind(
                section: "Controls",
                key: "AddSpectatedToWing",
                defaultValue: new KeyboardShortcut(KeyCode.P),
                configDescription: new ConfigDescription(
                    "When spectating anything or anyone, press this key to add or remove them from your wing.\n" +
                    "Examples of keys you can enter:\n" +
                    "- Letters: A, B, C, ...\n" +
                    "- Numbers: Alpha0, Alpha1, ...\n" +
                    "- Function keys: F1, F2, ...\n" +
                    "- Special keys: Space, Enter, Escape, Tab\n" +
                    "- Modifiers: LeftShift, RightShift, LeftControl, RightControl\n" +
                    "- Arrow keys: UpArrow, DownArrow, LeftArrow, RightArrow\n" +
                    "---"
                )
            );

            AddRemoveFriend = Config.Bind(
                section: "Controls",
                key: "AddSpectatedToFriends",
                defaultValue: new KeyboardShortcut(KeyCode.O),
                configDescription: new ConfigDescription(
                    "When spectating anything or anyone, press this key to add or remove them from your friends list.\n" +
                    "Examples of keys you can enter:\n" +
                    "- Letters: A, B, C, ...\n" +
                    "- Numbers: Alpha0, Alpha1, ...\n" +
                    "- Function keys: F1, F2, ...\n" +
                    "- Special keys: Space, Enter, Escape, Tab\n" +
                    "- Modifiers: LeftShift, RightShift, LeftControl, RightControl\n" +
                    "- Arrow keys: UpArrow, DownArrow, LeftArrow, RightArrow\n" +
                    "---"
                )
            );

            FriendsConfigString = Config.Bind(
                section: "Social",
                key: "FriendsList",
                defaultValue: "",
                configDescription: new ConfigDescription(
                    "This can be populated manually or you can add people in-game via keybind.\n" +
                    "Usernames are CASE-SENSITIVE. Separate with a comma.\n" +
                    "eg: FriendsList = Jessie, James, GiovanniSakaki\n" +
                    "---"
                )
            );
            LoadFriendsList();
            FriendsConfigString.SettingChanged += ConfigSettingChanged;

            WingColorHexConfig = Config.Bind(
                section: "Colors",
                key: "WingColor",
                defaultValue: "C180FF",
                configDescription: new ConfigDescription(
                    "Color of Wing members on the map.\n" +
                    "Hex color. With or without the #.\n" +
                    "---"
                )
            );
            wingColor = ExtractColorFromConfig(WingColorHexConfig.Value, defaultWingColor);
            WingColorHexConfig.SettingChanged += ConfigSettingChanged;

            FriendColorHexConfig = Config.Bind(
                section: "Colors",
                key: "FriendColor",
                defaultValue: "FFF12B",
                configDescription: new ConfigDescription(
                    "Color of friends on the map.\n" +
                    "Hex color. With or without the #.\n" +
                    "---"
                )
            );
            friendColor = ExtractColorFromConfig(FriendColorHexConfig.Value, defaultFriendColor);
            FriendColorHexConfig.SettingChanged += ConfigSettingChanged;

            EnemyFriendColorHexConfig = Config.Bind(
                section: "Colors",
                key: "EnemyFriendColor",
                defaultValue: "FFB3B3",
                configDescription: new ConfigDescription(
                    "Color of friends on the map when they are on the opposing team.\n" +
                    "Hex color. With or without the #.\n" +
                    "---"
                )
            );
            enemyFriendColor = ExtractColorFromConfig(EnemyFriendColorHexConfig.Value, defaultEnemyFriendColor);
            EnemyFriendColorHexConfig.SettingChanged += ConfigSettingChanged;

            ShouldVoiceSocialFeatures = Config.Bind(
                section: "General",
                key: "ShouldVoiceSocialFeatures",
                defaultValue: true,
                configDescription: new ConfigDescription(
                    "Set to 'true' to hear audio feedback for social features.\n" +
                    "---"
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
                        "Do not touch - used for auto-updating config.\n" +
                        "If you accidentally change this, just set it back to the above default.\n" +
                        "Alternatively, you can delete this config and launch the game to generate a clean one.",
                        null,
                        new ConfigurationManagerAttributes { Browsable = false }
                    )
                );

                savedVersionConfig.Value = CurrentConfigVersion;

                Config.Save();

                Logger.LogInfo("Config automatically updated.");
            }

            if (Enabled.Value)
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
                var harmony = new Harmony(MyGUID);
                harmony.PatchAll();
            }

            Logger.LogInfo($"{PluginName} v{VersionString} loaded successfully. State: {(Enabled.Value ? "Enabled" : "Disabled")}");
        }

        private void onDestroy()
        {
            if (!Enabled.Value)
                return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!Enabled.Value)
                return;

            string sceneName = scene.name.ToLowerInvariant();

            if (sceneName.Contains("mainmenu"))
            {
                wingMembers.Clear();
                CurrentlySpectating = "";
            }
        }

        private void Update()
        {
            if (!Enabled.Value)
                return;
            if (!IsSpectating || IsChatOpen)
                return;
            if (AddSpectatedToWing.Value.IsDown())
            {
                if (!string.IsNullOrWhiteSpace(CurrentlySpectating))
                {
                    if (string.Equals(SpectateTargetFaction, ourFaction, StringComparison.OrdinalIgnoreCase))
                    {
                        if (wingMembers.Remove(CurrentlySpectating))
                        {
                            Log.LogInfo($"Removed {CurrentlySpectating} from wing.\nAll members:");
                            Log.LogInfo(string.Join(", ", wingMembers));
                            if (ShouldVoiceSocialFeatures.Value)
                                PlayCustomSound("wingmanRemoved");
                        }
                        else
                        {
                            wingMembers.Add(CurrentlySpectating);
                            Log.LogInfo($"Added {CurrentlySpectating} to wing.\nAll members:");
                            Log.LogInfo(string.Join(", ", wingMembers));
                            if (ShouldVoiceSocialFeatures.Value)
                                PlayCustomSound("wingmanAdded");
                        }
                    }
                }
            }
            else if (AddRemoveFriend.Value.IsDown())
            {
                if (!string.IsNullOrWhiteSpace(CurrentlySpectating))
                {
                    if (friendsList.Remove(CurrentlySpectating))
                    {
                        Log.LogInfo($"Removed {CurrentlySpectating} from friends list.\nFriends:");
                        Log.LogInfo(string.Join(", ", friendsList));
                        if (ShouldVoiceSocialFeatures.Value)
                            PlayCustomSound("friendRemoved");
                    }
                    else
                    {
                        friendsList.Add(CurrentlySpectating);
                        Log.LogInfo($"Added {CurrentlySpectating} to friends list.\nFriends:");
                        Log.LogInfo(string.Join(", ", friendsList));
                        if (ShouldVoiceSocialFeatures.Value)
                            PlayCustomSound("friendAdded");
                    }
                    SaveFriendsListToConfig();
                }
            }
        }

        private void ConfigSettingChanged(object sender, System.EventArgs e)
        {
            SettingChangedEventArgs settingChangedEventArgs = e as SettingChangedEventArgs;

            if (settingChangedEventArgs == null)
            {
                return;
            }

            if (settingChangedEventArgs.ChangedSetting.Definition.Key == "WingColor")
            {
                wingColor = ExtractColorFromConfig(WingColorHexConfig.Value, defaultWingColor);
            }

            if (settingChangedEventArgs.ChangedSetting.Definition.Key == "FriendColor")
            {
                wingColor = ExtractColorFromConfig(WingColorHexConfig.Value, defaultWingColor);
            }

            if (settingChangedEventArgs.ChangedSetting.Definition.Key == "EnemyFriendColor")
            {
                wingColor = ExtractColorFromConfig(WingColorHexConfig.Value, defaultWingColor);
            }

            if (settingChangedEventArgs.ChangedSetting.Definition.Key == "FriendsList")
            {
                LoadFriendsList();
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

        private Color ExtractColorFromConfig(string rawHex, Color fallbackColor)
        {
            if (string.IsNullOrWhiteSpace(rawHex))
                return fallbackColor;

            string hex = rawHex.StartsWith("#") ? rawHex.Substring(1) : rawHex;

            if (hex.Length != 6)
            {
                Log.LogInfo("Error parsing hex from config. Using fallback color.");
                return fallbackColor;
            }

            try
            {
                byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

                return new Color(
                    r / 255f,
                    g / 255f,
                    b / 255f,
                    1f
                );
            }
            catch
            {
                Log.LogInfo("Error parsing hex from config. Using fallback color.");
                return fallbackColor;
            }
        }

        private void LoadFriendsList()
        {
            friendsList.Clear();

            string value = FriendsConfigString.Value;
            if (string.IsNullOrEmpty(value))
                return;

            string[] parts = value.Split(',');

            for (int i = 0; i < parts.Length; i++)
            {
                string name = parts[i].Trim();
                if (!string.IsNullOrEmpty(name))
                {
                    friendsList.Add(name); // HashSet is case-sensitive by default
                }
            }
        }

        private void SaveFriendsListToConfig()
        {
            // Serialize HashSet to comma-separated string
            string[] array = new string[friendsList.Count];
            friendsList.CopyTo(array);

            FriendsConfigString.Value = string.Join(",", array);

            // Force write to disk
            Config.Save();
        }

        private void LoadCustomSounds()
        {
            var soundList = new List<KeyValuePair<string, string>>();

            soundList.Add(new KeyValuePair<string, string>("wingmanAdded", "wing_added.mp3"));
            soundList.Add(new KeyValuePair<string, string>("wingmanRemoved", "wing_removed.mp3"));
            soundList.Add(new KeyValuePair<string, string>("friendAdded", "friend_added.mp3"));
            soundList.Add(new KeyValuePair<string, string>("friendRemoved", "friend_removed.mp3"));

            foreach (KeyValuePair<string, string> entry in soundList)
            {
                string key = entry.Key;
                string filename = entry.Value;
                string fullPath = Path.Combine(soundsFolder, filename);

                if (!File.Exists(fullPath))
                {
                    Logger.LogWarning("Sound file not found: " + fullPath);
                    continue;
                }

                Logger.LogInfo("Attempting to load: " + fullPath);

                using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip("file://" + fullPath, AudioType.MPEG))
                {
                    uwr.SendWebRequest();

                    while (!uwr.isDone) { }

                    if (uwr.result == UnityWebRequest.Result.Success)
                    {
                        AudioClip clip = DownloadHandlerAudioClip.GetContent(uwr);
                        if (clip != null)
                        {
                            loadedClips.Add(key, clip);
                            Logger.LogInfo("Successfully loaded sound: " + filename);
                        }
                        else
                        {
                            Logger.LogError("Clip was null after loading: " + filename);
                        }
                    }
                    else
                    {
                        Logger.LogError("Failed to load " + filename + ": " + uwr.error);
                    }
                }
            }
        }

        public static void PlayCustomSound(string soundKey)
        {
            if (loadedClips == null)
            {
                return;
            }

            AudioClip clip;
            if (!loadedClips.TryGetValue(soundKey, out clip) || clip == null)
            {
                Log.LogWarning("Cannot play sound - not loaded or null: " + soundKey);
                return;
            }

            SoundManager.PlayInterfaceOneShot(clip);

            // SoundManager.PlayMenuOneShot(clip);
            // SoundManager.PlayRadarWarningOneShot(clip);
            // SoundManager.PlayEffectOneShot(clip);
        }
    }

    [HarmonyPatch(typeof(MapIcon), nameof(MapIcon.UpdateColor))]
    [HarmonyAfter("com.hellcat92.vanillaiconsplus_1.5.1")]
    internal static class MapIcon_UpdateColor_Patch
    {
        static void Postfix(MapIcon __instance)
        {
            if (!WingmanVisual.Enabled.Value)
                return;

            var unitIcon = __instance as UnitMapIcon;
            if (unitIcon == null)
                return;

            if (unitIcon.unit == null || unitIcon.iconImage == null)
                return;

            string unitNameRaw = unitIcon.unit?.unitName ?? "";
            string unitName = WingmanVisual.StripAircraftName(unitNameRaw);
            string unitFaction = unitIcon.unit?.NetworkHQ?.name ?? "";
            bool isWing = WingmanVisual.wingMembers.Contains(unitName);
            bool isFriend = WingmanVisual.friendsList.Contains(unitName);
            WingmanVisual.ourFaction = SceneSingleton<DynamicMap>.i?.HQ?.name ?? "NULL";
            if (isFriend)
            {
                Color col = WingmanVisual.friendColor;
                if (!string.IsNullOrWhiteSpace(WingmanVisual.ourFaction))
                {
                    if (!string.IsNullOrWhiteSpace(unitFaction))
                    {
                        if (!string.Equals(unitFaction, WingmanVisual.ourFaction, StringComparison.OrdinalIgnoreCase))
                        {
                            col = WingmanVisual.enemyFriendColor;
                        }
                    }
                }
                col.a = unitIcon.iconImage.color.a;
                unitIcon.iconImage.color = col;
            }
            else if (isWing)
            {
                Color col = WingmanVisual.wingColor;
                col.a = unitIcon.iconImage.color.a;
                unitIcon.iconImage.color = col;
            }
        }
    }

    [HarmonyPatch(typeof(CameraStateManager), nameof(CameraStateManager.SetFollowingUnit))]
    internal static class CameraStateManager_SetFollowingUnit_Patch
    {
        static void Postfix(Unit unit)
        {
            if (!WingmanVisual.Enabled.Value)
                return;

            if (unit != null)
            {
                string username = WingmanVisual.StripAircraftName(unit.unitName);
                string faction = unit.NetworkHQ.name;
                WingmanVisual.CurrentlySpectating = username;
                WingmanVisual.SpectateTargetFaction = faction;
            }
        }
    }

    [HarmonyPatch(typeof(GameplayUI), nameof(GameplayUI.ShowSpectatorPanel))]
    internal static class GameplayUI_ShowSpectatorPanel_Patch
    {
        static void Postfix()
        {
            if (!WingmanVisual.Enabled.Value)
                return;

            WingmanVisual.IsSpectating = true;
        }
    }

    [HarmonyPatch(typeof(GameplayUI), nameof(GameplayUI.HideSpectatorPanel))]
    internal static class GameplayUI_HideSpectatorPanel_Patch
    {
        static void Postfix()
        {
            if (!WingmanVisual.Enabled.Value)
                return;

            WingmanVisual.IsSpectating = false;
            WingmanVisual.CurrentlySpectating = "";
        }
    }

    [HarmonyPatch(typeof(ChatBox), "OnEnable")]
    static class ChatBox_OnEnable_Patch
    {
        static void Postfix()
        {
            if (!WingmanVisual.Enabled.Value)
                return;

            WingmanVisual.IsChatOpen = true;
        }
    }

    [HarmonyPatch(typeof(ChatBox), nameof(ChatBox.OnDisable))]
    static class ChatBox_OnDisable_Patch
    {
        static void Postfix()
        {
            if (!WingmanVisual.Enabled.Value)
                return;

            WingmanVisual.IsChatOpen = false;
        }
    }
}