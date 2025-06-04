using BaboonAPI.Hooks.Initializer;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.IO;
using TootTallyCore.Utils.TootTallyModules;
using TootTallySettings;
using UnityEngine;
using TootTallyCore.Utils.TootTallyNotifs;

namespace TootTallyGameTweaks
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("TootTallySettings", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin, ITootTallyModule
    {
        public static Plugin Instance;

        private const string CONFIG_NAME = "GameTweaks.cfg";
        private Harmony _harmony;
        public ConfigEntry<bool> ModuleConfigEnabled { get; set; }
        public bool IsConfigInitialized { get; set; }

        //Change this name to whatever you want
        public string Name { get => "GameTweaks"; set => Name = value; }

        public static TootTallySettingPage settingPage;

        public static void LogInfo(string msg) => Instance.Logger.LogInfo(msg);
        public static void LogError(string msg) => Instance.Logger.LogError(msg);

        private void Awake()
        {
            if (Instance != null) return;
            Instance = this;
            _harmony = new Harmony(Info.Metadata.GUID);

            GameInitializationEvent.Register(Info, TryInitialize);
        }

        private void TryInitialize()
        {
            // Bind to the TTModules Config for TootTally
            ModuleConfigEnabled = TootTallyCore.Plugin.Instance.Config.Bind("Modules", "GameTweaks", true, "Various game tweaks, improvements and QoL features.");
            TootTallyModuleManager.AddModule(this);
            TootTallySettings.Plugin.Instance.AddModuleToSettingPage(this);
        }

        public void LoadModule()
        {
            string configPath = Path.Combine(Paths.BepInExRootPath, "config/");
            ConfigFile config = new ConfigFile(configPath + CONFIG_NAME, true);
            ChampMeterSize = config.Bind("General", "ChampMeterSize", 1f, "Resize the champ meter to make it less intrusive.");
            SyncDuringSong = config.Bind("General", "Sync During Song", false, "Allow the game to sync during a song, may cause lags but prevent desyncs.");
            ShowTromboner = config.Bind("General", "Show Tromboner", true, "Show or hides the Tromboner during gameplay.");
            RandomizeKey = config.Bind("General", "RandomizeKey", KeyCode.F5, "Press that key to randomize.");
            MuteButtonTransparency = config.Bind("General", "MuteBtnAlpha", .25f, "Change the transparency of the mute button.");
            TouchScreenMode = config.Bind("Misc", "TouchScreenMode", false, "Tweaks for touchscreen users.");
            OverwriteNoteSpacing = config.Bind("NoteSpacing", "OverwriteNoteSpacing", false, "Make the note spacing always the same.");
            NoteSpacing = config.Bind("NoteSpacing", "NoteSpacing", 280.ToString(), "Note Spacing Value");
            ShowCardAnimation = config.Bind("Misc", "ShowCardAnimation", true, "Show or skip the animation when opening cards.");
            ShowLyrics = config.Bind("Misc", "ShowLyrics", true, "Show or remove Lyrics from songs.");
            OptimizeGame = config.Bind("Misc", "OptimizeGame", false, "Instantiate and destroy notes as they enter and leave the screen.");
            ExperimentalSync = config.Bind("Misc", "ExperiementalSync", false, "Attempt at fixing syncing issues with very long charts.");
            ResyncStrength = config.Bind("Misc", "ResyncStrength", 60f, "Increase the strength of the resync: higher means resync will be more aggressive. RECOMMENDED: 40-80");
            SliderSamplePoints = config.Bind("Misc", "SliderSamplePoints", 8f, "Increase or decrease the quality of slides.");
            RememberMyBoner = config.Bind("RMB", "RememberMyBoner", true, "Remembers the things you selected in the character selection screen.");
            TootRainbow = config.Bind("RMB", "TootRainbow", false, "Remembers the tootrainbow you selected.");
            LongTrombone = config.Bind("RMB", "LongTrombone", false, "Remembers the longtrombone you selected.");
            CharacterID = config.Bind("RMB", "CharacterID", 0, "Remembers the character you selected.");
            TromboneID = config.Bind("RMB", "TromboneID", 0, "Remembers the trombone you selected.");
            VibeID = config.Bind("RMB", "VibeID", 0, "Remembers the vibe you selected.");
            SoundID = config.Bind("RMB", "SoundID", 0, "Remembers the sound you selected.");
            ShowConfetti = config.Bind("Misc", "Show Confetti", true, "Show or remove the confetti in the score screen.");

            settingPage = TootTallySettingsManager.AddNewPage("GameTweaks", "Game Tweaks", 40f, new Color(0, 0, 0, 0));
            settingPage?.AddSlider("Champ Meter Size", 0, 1, ChampMeterSize, false);
            settingPage?.AddSlider("Mute Btn Alpha", 0, 1, MuteButtonTransparency, false);
            settingPage?.AddToggle("Show Tromboner", ShowTromboner);
            settingPage?.AddToggle("Sync During Song", SyncDuringSong);
            settingPage?.AddToggle("Experiemental Syncing", ExperimentalSync, OnExperimentalSyncToggle);
            OnExperimentalSyncToggle(ExperimentalSync.Value);
            settingPage?.AddToggle("Touchscreen Mode", TouchScreenMode, (value) => GlobalVariables.localsettings.mousecontrolmode = value ? 0 : 1);
            settingPage?.AddToggle("Show Card Animation", ShowCardAnimation);
            settingPage?.AddToggle("Overwrite Note Spacing", OverwriteNoteSpacing, OnOverwriteNoteSpacingToggle);
            OnOverwriteNoteSpacingToggle(OverwriteNoteSpacing.Value);
            settingPage?.AddToggle("Show Lyrics", ShowLyrics);
            settingPage?.AddToggle("Optimize Game", OptimizeGame, OnOptimizeGameToggle);
            OnOptimizeGameToggle(OptimizeGame.Value);
            settingPage?.AddToggle("Remember My Boner", RememberMyBoner);
            settingPage?.AddToggle("Show Confetti", ShowConfetti);

            TootTallySettings.Plugin.TryAddThunderstoreIconToPageButton(Instance.Info.Location, Name, settingPage);

            _harmony.PatchAll(typeof(GameTweaksPatches));
            LogInfo($"Module loaded!");
        }

        public void UnloadModule()
        {
            _harmony.UnpatchSelf();
            settingPage.Remove();
            LogInfo($"Module unloaded!");
        }

        public void OnExperimentalSyncToggle(bool value)
        {
            if (value)
            {
                settingPage?.AddSlider("ResyncStrength", 1, 240, ResyncStrength, true);
            }
            else
            {
                settingPage?.RemoveSettingObjectFromList("ResyncStrength");
            }
        }

        public void OnOptimizeGameToggle(bool value)
        {
            if (value)
            {
                settingPage?.AddSlider("SliderSamplePoints", 2, 50, SliderSamplePoints, true);
            }
            else
            {
                settingPage?.RemoveSettingObjectFromList("SliderSamplePoints");
            }
        }

        public void OnOverwriteNoteSpacingToggle(bool value)
        {
            if (value)
                settingPage?.AddTextField("NoteSpacing", NoteSpacing.Value, false, OnNoteSpacingSubmit);
            else
                settingPage?.RemoveSettingObjectFromList("NoteSpacing");
        }

        public void OnNoteSpacingSubmit(string value)
        {
            if (int.TryParse(value, out var num) && num > 0)
                NoteSpacing.Value = num.ToString();
            else
                TootTallyNotifManager.DisplayNotif("Value has to be a positive integer.");
        }

        public ConfigEntry<float> ChampMeterSize { get; set; }
        public ConfigEntry<float> MuteButtonTransparency { get; set; }
        public ConfigEntry<bool> SyncDuringSong { get; set; }
        public ConfigEntry<KeyCode> RandomizeKey { get; set; }
        public ConfigEntry<bool> TouchScreenMode { get; set; }
        public ConfigEntry<bool> OverwriteNoteSpacing { get; set; }
        public ConfigEntry<string> NoteSpacing { get; set; }
        public ConfigEntry<bool> ShowTromboner { get; set; }
        public ConfigEntry<bool> ShowCardAnimation { get; set; }
        public ConfigEntry<bool> ShowLyrics { get; set; }
        public ConfigEntry<bool> OptimizeGame { get; set; }
        public ConfigEntry<bool> ExperimentalSync { get; set; }
        public ConfigEntry<float> ResyncStrength { get; set; }
        public ConfigEntry<float> SliderSamplePoints { get; set; }
        public ConfigEntry<bool> RememberMyBoner { get; set; }
        public ConfigEntry<bool> LongTrombone { get; set; }
        public ConfigEntry<bool> TootRainbow { get; set; }
        public ConfigEntry<int> CharacterID { get; set; }
        public ConfigEntry<int> SoundID { get; set; }
        public ConfigEntry<int> VibeID { get; set; }
        public ConfigEntry<int> TromboneID { get; set; }
        public ConfigEntry<bool> ShowConfetti { get; set; }
    }
}