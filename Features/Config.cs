using JoelG.ENA4;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;

namespace KatieSaveHelper
{
    public enum SeedGenerator
    {
        Random,
        PsuedoRandom,
        Static,
        Device
    }

    public enum MainMenuSkipType
    {
        Always,
        OnStartup,
        Never
    }

    public enum CustomEventType
    {
        OnHotkey,
        OnLoadSave,
        OnCreateSave,
        OnLoadMainMenu
    }

    public interface IKatieActionBase
    {
        string DisplayName { get; }
        string InternalName { get; }
        KeyCode Key { get; }
        Action Action { get; }
        void Run();
        void SetValuesFromConfig();
    }

    public interface IKatieSetting
    {
        string DisplayName { get; }
        string InternalName { get; }
        object GetValueAsObject();
        void SetValuesFromConfig(); 
    }

    public class KatieSetting<T> : IKatieSetting
    {
        public string DisplayName { get; private set; }
        public string InternalName { get; private set; }
        public T Value { get; private set; }
        public T DefaultValue { get; private set; }
        public KatieSettingConfig<T> Config {  get; private set; }

        public KatieSetting(string displayName, T defaultValue)
        {
            DisplayName = displayName;
            InternalName = DisplayName.Replace(" ", "");
            DefaultValue = defaultValue;
            Value = defaultValue;
            Config = new KatieSettingConfig<T>();
        }

        public KatieSetting(string displayName, string internalName, T defaultValue)
        {
            DisplayName = displayName;
            InternalName = internalName;
            DefaultValue = defaultValue;
            Value = defaultValue;
            Config = new KatieSettingConfig<T>();
        }

        public object GetValueAsObject() => Value;

        public void CreateValueConfigEntry(string description)
        {
            Config.Value = KatieSaveHelperMod.Instance.Config.Bind("Settings", InternalName, DefaultValue, description);
        }

        public void SetValuesFromConfig()
        {
            Value = Config.Value.Value;
        }

        public void SetValuesFromDefault()
        {
            Value = DefaultValue;
        }

    }

    public static class KatieSettingExtensions
    {
        public static string TryGetRealValue(this KatieSetting<string> setting)
        {
            if (string.IsNullOrEmpty(setting.Value))
            {
                KatieLogger.Warning($"No mod config entry found for setting '{setting.InternalName}', defaulting to '{setting.DefaultValue}'");
                return setting.DefaultValue;
            }
            return setting.Value;
        }

        public static string TryGetRealValue(this KatieSetting<string> setting, string defaultValue)
        {
            if (string.IsNullOrEmpty(setting.Value))
            {
                KatieLogger.Warning($"No mod config entry found for setting '{setting.InternalName}', defaulting to '{defaultValue}'");
                return defaultValue;
            }
            return setting.Value;
        }

        public static (bool success, Color color) TryGetColorFromValue(this KatieSetting<string> setting)
        {
            string hex = setting.Value;

            if (!hex.StartsWith("#"))
                hex = "#" + hex;

            if (ColorUtility.TryParseHtmlString(hex, out Color color))
                return (true, color);

            string hex2 = setting.DefaultValue;

            if (ColorUtility.TryParseHtmlString(hex2, out Color color2))
            {
                KatieLogger.Warning($"Failed to parse color from '{hex}' in setting '{setting.InternalName}', defaulting to '{hex2}'");
                return (true, color2);
            }

            KatieLogger.Error($"Failed to parse color from all values in setting '{setting.InternalName}'");
            return (false, Color.black);
        }

        public static (bool success, Color color) TryGetColorFromValue(this KatieSetting<string> setting, string defaultValue)
        {
            string hex = setting.Value;

            if (!hex.StartsWith("#"))
                hex = "#" + hex;

            if (ColorUtility.TryParseHtmlString(hex, out Color color))
                return (true, color);

            string hex2 = defaultValue;

            if (ColorUtility.TryParseHtmlString(hex2, out Color color2))
            {
                KatieLogger.Warning($"Failed to parse color from '{hex}' in setting '{setting.InternalName}', defaulting to '{hex2}'");
                return (true, color2);
            }

            KatieLogger.Error($"Failed to parse color from '{hex}' in setting '{setting.InternalName}' and '{hex2}' from the default argument");
            return (false, Color.black);
        }
    }

    public class KatieSettingConfig<T>
    {
        public ConfigEntry<T> Value = default;
    }

    public class KatieAction : IKatieActionBase
    {
        public string DisplayName { get; private set; }
        public string InternalName { get; private set; }
        public KeyCode Key { get; private set; }
        public KeyCode DefaultKey { get; private set; }
        public Action Action { get; private set; }
        public KatieActionConfig Config { get; private set; }

        public void Run() => Action?.Invoke();

        public void CreateKeyConfigEntry(string description)
        {
            Config.Key = KatieSaveHelperMod.Instance.Config.Bind("Hotkeys", $"{InternalName}_Key", DefaultKey, $"Key used to {description}");
        }

        public KatieAction(string displayName, Action action, KeyCode defaultKey)
        {
            DisplayName = displayName;
            InternalName = DisplayName.Replace(" ", "");
            Action = action;
            DefaultKey = defaultKey;
            Key = DefaultKey;
            Config = new KatieActionConfig();
        }

        public KatieAction(string displayName, string internalName, Action action, KeyCode defaultKey)
        {
            DisplayName = displayName;
            InternalName = internalName;
            Action = action;
            DefaultKey = defaultKey;
            Key = DefaultKey;
            Config = new KatieActionConfig();
        }

        public void SetValuesFromConfig()
        {
            Key = Config.Key.Value;
        }
        public void SetValuesFromDefault()
        {
            Key = DefaultKey;
        }
    }

    public class KatieActionConfig
    {
        public ConfigEntry<KeyCode> Key;
    }

    public class KatieTransitionAction : IKatieActionBase
    {
        public string DisplayName { get; private set; }
        public string InternalName { get; private set; }
        public KeyCode Key { get; private set; }
        public KeyCode DefaultKey { get; private set; }
        public Action Action { get; private set; }
        public Transition DefaultTransition {  get; private set; }
        public Transition Transition { get; private set; }
        public KatieTransitionActionConfig Config { get; private set; }

        public void Run() => Action?.Invoke();

        public KatieTransitionAction(string displayName, Action action, KeyCode defaultKey, Transition defaultTransition)
        {
            DisplayName = displayName;
            InternalName = DisplayName.Replace(" ", "");
            DefaultKey = defaultKey;
            Key = DefaultKey;
            DefaultTransition = defaultTransition;
            Transition = DefaultTransition.Copy();
            Config = new KatieTransitionActionConfig();
            Action = action;
        }

        public KatieTransitionAction(string displayName, string internalName, Action action, KeyCode defaultKey, Transition defaultTransition)
        {
            DisplayName = displayName;
            InternalName = internalName;
            DefaultKey = defaultKey;
            Key = DefaultKey;
            DefaultTransition = defaultTransition;
            Transition = DefaultTransition.Copy();
            Config = new KatieTransitionActionConfig();
            Action = action;
        }

        public void CreateKeyConfigEntry(string description)
        {
            Config.Key = KatieSaveHelperMod.Instance.Config.Bind("Hotkeys", $"{InternalName}_Key", DefaultKey, $"Key used to {description}");
        }

        public void CreateTransitionConfigEntry()
        {
            string name = InternalName;
            Config.Transition.Type = KatieSaveHelperMod.Instance.Config.Bind("Scene Transitions", $"{name}_TransitionType", DefaultTransition.Type, $"Transition type to use for the '{DisplayName}' action's transition");
            Config.Transition.Color = KatieSaveHelperMod.Instance.Config.Bind("Scene Transitions", $"{name}_TransitionColor", KatieUtil.GetHexFromColor(DefaultTransition.Color), $"Transition color to use for the '{DisplayName}' action's transition");
            Config.Transition.FadeInTime = KatieSaveHelperMod.Instance.Config.Bind("Scene Transitions", $"{name}_TransitionFadeInTime", DefaultTransition.FadeInTime, $"Transition fade-in time to use for the '{DisplayName}' action's transition");
            Config.Transition.FadeOutTime = KatieSaveHelperMod.Instance.Config.Bind("Scene Transitions", $"{name}_TransitionFadeOutTime", DefaultTransition.FadeOutTime, $"Transition fade-out time to use for the '{DisplayName}' action's transition");
        }

        public void CreateFullConfigEntry(string description)
        {
            CreateKeyConfigEntry(description);
            CreateTransitionConfigEntry();
        }

        public void SetValuesFromConfig()
        {
            Key = Config.Key.Value;
            Transition.Type = Config.Transition.Type.Value;
            Transition.Color = KatieUtil.GetColorFromHex(Config.Transition.Color.Value);
            Transition.FadeInTime = Config.Transition.FadeInTime.Value;
            Transition.FadeOutTime = Config.Transition.FadeOutTime.Value;
        }

        public void SetValuesFromDefault()
        {
            Key = DefaultKey;
            Transition = DefaultTransition.Copy();
        }
    }
    public class KatieTransitionActionConfig
    {
        public ConfigEntry<KeyCode> Key;
        public TransitionConfig Transition;

        public KatieTransitionActionConfig()
        {
            Transition = new TransitionConfig();
        }
    }

    public class Transition
    {
        public SceneChanger.TransitionType Type;
        public Color Color;
        public float FadeInTime;
        public float FadeOutTime;

        public string hexColor => KatieUtil.GetHexFromColor(Color);

        public Transition()
        {

        }

        public Transition(Transition transition)
        {
            Type = transition.Type;
            Color = transition.Color;
            FadeInTime = transition.FadeInTime;
            FadeOutTime = transition.FadeOutTime;
        }

        public Transition(SceneChanger.TransitionType type, Color color, float fadeInTime, float fadeOutTime)
        {
            Type = type;
            Color = color;
            FadeInTime = fadeInTime;
            FadeOutTime = fadeOutTime;
        }

        public Transition Copy()
        {
            return new Transition(this);
        }
    }
    public class TransitionConfig
    {
        public ConfigEntry<SceneChanger.TransitionType> Type;
        public ConfigEntry<string> Color;
        public ConfigEntry<float> FadeInTime;
        public ConfigEntry<float> FadeOutTime;
    }

    public static class KatieSaveHelperModConfig
    {
        internal static event Action OnConfigLoaded;

        internal static List<IKatieSetting> allSettings = new List<IKatieSetting>();
        internal static List<IKatieActionBase> allActions = new List<IKatieActionBase>();
        internal static IEnumerable<IKatieActionBase> allTransitionActions => allActions.Where(action => action is KatieTransitionAction);
        internal static List<IKatieActionBase> allActiveActions = new List<IKatieActionBase>();
        internal static IEnumerable<IKatieActionBase> allActiveTransitionActions => allActiveActions.Where(action => action is KatieTransitionAction);

        internal static KatieSetting<bool> autoSaveDisabledByDefault = new KatieSetting<bool>("Game Auto Saving Disabled By Default", false);
        internal static KatieSetting<SeedGenerator> saveSeedGeneratorType = new KatieSetting<SeedGenerator>("Save Seed Generator Type", SeedGenerator.Random);
        internal static KatieSetting<SeedGenerator> sessionSeedGeneratorType = new KatieSetting<SeedGenerator>("Session Seed Generator Type", SeedGenerator.Random);
        internal static KatieSetting<SeedGenerator> hardwareSeedGeneratorType = new KatieSetting<SeedGenerator>("Hardware Seed Generator Type", SeedGenerator.Device);
        internal static KatieSetting<CustomEventType> regenerateSessionSeed = new KatieSetting<CustomEventType>("Regenerate Session Seed", CustomEventType.OnHotkey);
        internal static KatieSetting<CustomEventType> regenerateHardwareSeed = new KatieSetting<CustomEventType>("Regenerate Hardware Seed", CustomEventType.OnHotkey);
        internal static KatieSetting<CustomEventType> resetBlinkRandomizer = new KatieSetting<CustomEventType>("Reset Game Blink Randomizer", CustomEventType.OnHotkey);
        internal static KatieSetting<int> staticSaveSeed = new KatieSetting<int>("Static Save Seed", 0);
        internal static KatieSetting<int> staticSessionSeed = new KatieSetting<int>("Static Session Seed", 0);
        internal static KatieSetting<int> staticHardwareSeed = new KatieSetting<int>("Static Hardware Seed", 0);
        internal static KatieSetting<MainMenuSkipType> skipMainMenuIntro = new KatieSetting<MainMenuSkipType>("Skip Main Menu Intro", MainMenuSkipType.Never);
        internal static KatieSetting<bool> disableReturnToMainMenuPopup = new KatieSetting<bool>("Disable Return To Main Menu Popup", false);
        internal static KatieSetting<bool> disableCreateSavePopup = new KatieSetting<bool>("Disable Create Save Popup", false);
        internal static KatieSetting<bool> disableResetSavePopup = new KatieSetting<bool>("Disable Reset Save Popup", false);
        internal static KatieSetting<bool> disableSaveFileLockAfterCompletion = new KatieSetting<bool>("Disable Save File Lock After Completion", false);
        internal static KatieSetting<bool> disableSaveFileEncryption = new KatieSetting<bool>("Disable Save File Encryption", false);
        internal static KatieSetting<bool> disableRemoteSaveSync = new KatieSetting<bool>("Disable Steam Remote Save Sync", false);

        internal static KatieSetting<bool> psuedoRandomNaturalSeedsOnly = new KatieSetting<bool>("Psuedo Randomizer Generate Natural Seeds Only", "PsuedoRandomizer_General_GenerateNaturalSeedsOnly" , true);
        internal static KatieSetting<int> psuedoRandomMaxAttempts = new KatieSetting<int>("Psuedo Randomizer Search Attempt Limit", "PsuedoRandomizer_General_SearchAttemptLimit", 1000000);
        internal static KatieSetting<FrankDoor> psuedoRandomTargetFrankDoor = new KatieSetting<FrankDoor>("Psuedo Randomizer Target Frank Door", "PsuedoRandomizer_SaveMode_FrankDoor_Target", FrankDoor.Any);
        internal static KatieSetting<TaxiHead> psuedoRandomTargetTaxiHead = new KatieSetting<TaxiHead>("Psuedo Randomizer Target Taxi Head", "PsuedoRandomizer_SaveMode_TaxiHead_Target", TaxiHead.Socio);
        internal static KatieSetting<string> psuedoRandomTargetPurgeRoomGoals = new KatieSetting<string>("Psuedo Randomizer Target Purge Goals", "PsuedoRandomizer_SaveMode_PurgeGoals_Target", "*RLRLRL");
        internal static KatieSetting<string> psuedoRandomTargetPurgeRoomObstacles = new KatieSetting<string>("Psuedo Randomizer Target Purge Obstacles", "PsuedoRandomizer_SaveMode_PurgeObstacles_Target", "**Any, !WanderingFish");
        internal static KatieSetting<string> psuedoRandomTargetBlinkAttempt = new KatieSetting<string>("Psuedo Randomizer Target First Blink Attempt", "PsuedoRandomizer_SessionMode_FirstBlinkAttempt_Target", "1");
        internal static KatieSetting<bool> psuedoRandomBlinkAssumeInCore = new KatieSetting<bool>("Psuedo Randomizer Assume Blink In Core", "PsuedoRandomizer_SessionMode_FirstBlinkAttempt_AssumeInCore", false);
        internal static KatieSetting<EnaTaxiMood> psuedoRandomEnaTaxiMood = new KatieSetting<EnaTaxiMood>("Psuedo Randomizer Target Ena Taxi Mood", "PsuedoRandomizer_HardwareMode_EnaTaxiMood_Target", EnaTaxiMood.Meanie);

        internal static KatieSetting<bool> showToasts = new KatieSetting<bool>("Show Toast Notifications", true);
        internal static KatieSetting<string> toastFontFileName = new KatieSetting<string>("Toast Font File Name", "Toast_FontFileName", "RuneScape-ENA");
        internal static KatieSetting<int> toastFontSize = new KatieSetting<int>("Toast Font Size", "Toast_FontSize", 36);
        internal static KatieSetting<string> toastFontColor = new KatieSetting<string>("Toast Font Color", "Toast_FontColor", "#FFFFFF");
        internal static KatieSetting<TextAlignmentOptions> toastAlignment = new KatieSetting<TextAlignmentOptions>("Toast Screen Position", "Toast_ScreenPosition", TextAlignmentOptions.TopRight);
        internal static KatieSetting<int> toastOutlineWidth = new KatieSetting<int>("Toast Outline Width", "Toast_OutlineWidth" , 5);
        internal static KatieSetting<string> toastOutlineColor = new KatieSetting<string>("Toast Outline Color", "Toast_OutlineColor", "#000000");
        internal static KatieSetting<int> toastOpacity = new KatieSetting<int>("Toast Opacity", "Toast_Opacity", 100);
        internal static KatieSetting<float> toastFadeInTime = new KatieSetting<float>("Toast Fade-In Time", "Toast_FadeInTime", 0.25f);
        internal static KatieSetting<float> toastFadeOutTime = new KatieSetting<float>("Toast Fade-Out Time", "Toast_FadeOutTime", 0.25f);
        internal static KatieSetting<float> toastHoldTime = new KatieSetting<float>("Toast Hold Time", "Toast_HoldTime", 1.50f);
        internal static KatieSetting<float> toastGapTime = new KatieSetting<float>("Toast Gap Time", "Toast_GapTime", 0.25f);

        internal static KatieSetting<bool> notifyOnFirstBlinkAttempts = new KatieSetting<bool>("Notify On First Blink Attempts", false);
        internal static KatieSetting<bool> logConfigOnReload = new KatieSetting<bool>("Log Config On Reload", true);
        internal static KatieSetting<bool> assetSubcriber = new KatieSetting<bool>("Subscribe To Asset Updates", false);

        internal static KatieAction reloadConfig = new KatieAction("Reload Config", KatieSaveHelperModActions.reloadConfig, KeyCode.Alpha4);
        internal static KatieAction quickSave = new KatieAction("Quick Save", KatieSaveHelperModActions.quickSave, KeyCode.Alpha1);
        internal static KatieAction logCurrentSeedInfo = new KatieAction("Log Current Seed Info", KatieSaveHelperModActions.logCurrentSeedInfo, KeyCode.None);
        internal static KatieAction toggleAutoSave = new KatieAction("Toggle Game Auto Saving", KatieSaveHelperModActions.toggleAutoSave, KeyCode.None);
        internal static KatieAction regenerateSessionSeedAction = new KatieAction("Regenerate Session Seed", KatieSaveHelperModActions.regenerateSessionSeed, KeyCode.None);
        internal static KatieAction regenerateHardwareSeedAction = new KatieAction("Regenerate Hardware Seed", KatieSaveHelperModActions.regenerateHardwareSeed, KeyCode.None);
        internal static KatieAction resetGameBlinkRandomizerAction = new KatieAction("Reset Game Blink Randomizer", KatieSaveHelperModActions.resetGameBlinkRandomizer, KeyCode.None);

        internal static readonly Transition defaultTransition = new Transition(SceneChanger.TransitionType.FadeToColor, Color.black, 0.5f, 0.5f);

        internal static KatieTransitionAction exitToSaveSelect = new KatieTransitionAction("Exit To Save Select", KatieSaveHelperModActions.exitToSaveSelect, KeyCode.None, defaultTransition.Copy());
        internal static KatieTransitionAction exitToSaveSelectAndEraseSave = new KatieTransitionAction("Exit To Save Select And Erase Save", KatieSaveHelperModActions.exitToSaveSelectAndEraseSave, KeyCode.None, defaultTransition.Copy());
        internal static KatieTransitionAction warpToNextScene = new KatieTransitionAction("Warp To Next Scene", KatieSaveHelperModActions.warpToNextScene, KeyCode.None, defaultTransition.Copy());
        internal static KatieTransitionAction warpToPrevScene = new KatieTransitionAction("Warp To Previous Scene", KatieSaveHelperModActions.warpToPrevScene, KeyCode.None, defaultTransition.Copy());

        internal static KatieTransitionAction reloadSaveWithFileSeed = new KatieTransitionAction("Reload Save With File Seed", KatieSaveHelperModActions.reloadSaveWithFileSeed, KeyCode.Alpha2, defaultTransition.Copy());
        internal static KatieTransitionAction reloadSaveWithCurrentSeed = new KatieTransitionAction("Reload Save With Current Seed", KatieSaveHelperModActions.reloadSaveWithCurrentSeed, KeyCode.None, defaultTransition.Copy());
        internal static KatieTransitionAction reloadSaveWithNewSeed = new KatieTransitionAction("Reload Save With New Seed", KatieSaveHelperModActions.reloadSaveWithNewSeed, KeyCode.None, defaultTransition.Copy());

        internal static KatieTransitionAction resetSaveWithFileSeed = new KatieTransitionAction("Reset Save With File Seed", KatieSaveHelperModActions.resetSaveWithFileSeed, KeyCode.None, defaultTransition.Copy());
        internal static KatieTransitionAction resetSaveWithCurrentSeed = new KatieTransitionAction("Reset Save With Current Seed", KatieSaveHelperModActions.resetSaveWithCurrentSeed, KeyCode.Alpha3, defaultTransition.Copy());
        internal static KatieTransitionAction resetSaveWithNewSeed = new KatieTransitionAction("Reset Save With New Seed", KatieSaveHelperModActions.resetSaveWithNewSeed, KeyCode.None, defaultTransition.Copy());

        public static void LoadSettingsList()
        {
            allSettings.Clear();

            var fields = typeof(KatieSaveHelperModConfig).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var field in fields)
            {
                if (typeof(IKatieSetting).IsAssignableFrom(field.FieldType))
                {
                    if (field.GetValue(null) is IKatieSetting setting)
                    {
                        allSettings.Add(setting);
                    }
                }
            }
        }

        public static void LoadActionsList()
        {
            allActions.Clear();

            var fields = typeof(KatieSaveHelperModConfig).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (var field in fields)
            {
                if (typeof(IKatieActionBase).IsAssignableFrom(field.FieldType))
                {
                    if (field.GetValue(null) is IKatieActionBase action)
                    {
                        allActions.Add(action);
                    }
                }
            }
        }

        public static void LoadActiveActionsList()
        {
            allActiveActions.Clear();

            allActiveActions.AddRange(allActions.Where(action => action.Key != KeyCode.None));
        }

        public static void LoadConfig()
        {
            KatieLogger.Info("Loading config...");

            LoadSettingsList();
            LoadActionsList();

            autoSaveDisabledByDefault.CreateValueConfigEntry("Whether the mod should disable the game's automatic saving feature");
            saveSeedGeneratorType.CreateValueConfigEntry("Which method the game should use to select a save seed when creating a new save");
            sessionSeedGeneratorType.CreateValueConfigEntry("Which method the game should use to select a session seed");
            hardwareSeedGeneratorType.CreateValueConfigEntry("Which method the game should use to select a hardware seed");
            regenerateSessionSeed.CreateValueConfigEntry("What will trigger the gane's active session seed being regenerated");
            regenerateHardwareSeed.CreateValueConfigEntry("what will trigger the game's active hardware seed being regenerated");
            resetBlinkRandomizer.CreateValueConfigEntry("What will trigger the game's blink randomizer being reset");

            staticSaveSeed.CreateValueConfigEntry("The save seed in which the mod will force every save when it is first created using the in-game Main Menu, if configured");
            staticSessionSeed.CreateValueConfigEntry("The session seed in which the mod will force the game to use on launch, if configured");
            staticHardwareSeed.CreateValueConfigEntry("The hardware seed in which the mod will force the game to use on launch, if configured");

            skipMainMenuIntro.CreateValueConfigEntry("Whether the mod should immediately skip the intro cutscene and title screen upon loading into the Main Menu");
            disableReturnToMainMenuPopup.CreateValueConfigEntry("Whether the mod should disable the confirmation window being displayed when attempting to return to the Main Menu from the Pause Menu");
            disableCreateSavePopup.CreateValueConfigEntry("Whether the mod should disable the confirmation window being displayed when creating a new save using the in-game Main Menu");
            disableResetSavePopup.CreateValueConfigEntry("Whether the mod should disable the confirmation window being displayed when resetting an existing save using the in-game Main Menu");
            disableSaveFileLockAfterCompletion.CreateValueConfigEntry("Whether the mod should disable save files becoming locked after being completed");
            disableSaveFileEncryption.CreateValueConfigEntry("Whether the mod should prevent the game from encrypting save files when they are updated");
            disableRemoteSaveSync.CreateValueConfigEntry("Whether the mod should prevent the game from overwriting existing save files with backups from Steam Remote Storage on launch");

            psuedoRandomTargetFrankDoor.CreateValueConfigEntry("Whether a single door or multiple doors in the Lost Village will be knockable");
            psuedoRandomTargetTaxiHead.CreateValueConfigEntry("Name of the Taxi Head that the mod's psuedo-randomizer will target when generating a new seed");
            psuedoRandomTargetPurgeRoomGoals.CreateValueConfigEntry("Order of the room goals during the Purge Event Maze that the mod's psuedo-randomizer will target when generating a new seed");
            psuedoRandomTargetPurgeRoomObstacles.CreateValueConfigEntry("Dog obstacles within each room during the Purge Event Maze that the mod's psuedo-randomizer will target when generating a new seed");
            psuedoRandomTargetBlinkAttempt.CreateValueConfigEntry("The range of attempt numbers in which ENA will blink for the first time");
            psuedoRandomBlinkAssumeInCore.CreateValueConfigEntry("Whether to assume the player is in the core when the randomizer is simulating blink attempts");
            psuedoRandomEnaTaxiMood.CreateValueConfigEntry("Which side of ENA will speak for the special dialogue during the first interaction with the Taxi Driver");
            psuedoRandomMaxAttempts.CreateValueConfigEntry("Maximum number of attempts the psuedo-randomizer will make to find a matching seed before quitting");
            psuedoRandomNaturalSeedsOnly.CreateValueConfigEntry("Whether the mod's psuedo-randomizer should exclusively generate seeds the game itself can naturally generate");

            showToasts.CreateValueConfigEntry("Whether the mod should display a toast notification on the screen when performing the mod's various hotkey actions");
            notifyOnFirstBlinkAttempts.CreateValueConfigEntry("Whether the mod should display a toast notification when the game internally attempts a blink and has not yet triggered the randomizer's first blink");
            logConfigOnReload.CreateValueConfigEntry("Whether the mod should print the newly loaded config to the modloader's console log after reloading it");
            assetSubcriber.CreateValueConfigEntry("Whether the mod should attempt to download new assets from it's GitHub repo on launch, when they are available");

            quickSave.CreateKeyConfigEntry("write the data from the current save into it's respective save file");
            reloadConfig.CreateKeyConfigEntry("reload all mod settings from this config file");
            logCurrentSeedInfo.CreateKeyConfigEntry("log the current seed values, and the in-game events they trigger, to the modloader console log");
            toggleAutoSave.CreateKeyConfigEntry("toggle the game's auto saving feature on and off");
            regenerateSessionSeedAction.CreateKeyConfigEntry("regenerate the game's active session seed using the generator specified in the 'Session Seed Generator Type' setting");
            regenerateHardwareSeedAction.CreateKeyConfigEntry("regenerate the game's active hardware seed using the generator specified in the 'Hardware Seed Generator Type' setting");
            resetGameBlinkRandomizerAction.CreateKeyConfigEntry("reset the game's Blink Randomizer");

            exitToSaveSelect.CreateKeyConfigEntry("immediately exit the current save to the Save Select Menu");
            exitToSaveSelectAndEraseSave.CreateKeyConfigEntry("immediately erase the current save and exit to the Save Select Menu");
            warpToNextScene.CreateKeyConfigEntry("immediately warp to the next scene from the game's internal scene list");
            warpToPrevScene.CreateKeyConfigEntry("immediately warp to the previous scene from the game's internal scene list");
            reloadSaveWithFileSeed.CreateKeyConfigEntry("reload the current save normally, using the seed from it's respective save file");
            reloadSaveWithCurrentSeed.CreateKeyConfigEntry("reload the current save with the currently loaded seed");
            reloadSaveWithNewSeed.CreateKeyConfigEntry("reload the current save with a new seed generated using the method specified in the 'Save Seed Generator Type' setting");
            resetSaveWithFileSeed.CreateKeyConfigEntry("immediately erase the current save, create a new empty one in the same slot that has the same seed as the deleted save file, then load it");
            resetSaveWithCurrentSeed.CreateKeyConfigEntry("immediately erase the current save, create a new empty one in the same slot that has the currently loaded seed, then load it");
            resetSaveWithNewSeed.CreateKeyConfigEntry("immediately erase the current save, create a new empty one in the same slot that has a new generated seed using the method specified in the 'Save Seed Generator Type' setting, then load it");

            exitToSaveSelect.CreateTransitionConfigEntry();
            exitToSaveSelectAndEraseSave.CreateTransitionConfigEntry();
            warpToNextScene.CreateTransitionConfigEntry();
            warpToPrevScene.CreateTransitionConfigEntry();
            reloadSaveWithFileSeed.CreateTransitionConfigEntry();
            reloadSaveWithCurrentSeed.CreateTransitionConfigEntry();
            reloadSaveWithNewSeed.CreateTransitionConfigEntry();
            resetSaveWithFileSeed.CreateTransitionConfigEntry();
            resetSaveWithCurrentSeed.CreateTransitionConfigEntry();
            resetSaveWithNewSeed.CreateTransitionConfigEntry();

            toastFontFileName.CreateValueConfigEntry("Name of the font '.ttf' file the toast should load it's displayed font from");
            toastFontSize.CreateValueConfigEntry("Font size the toast should use");
            toastFontColor.CreateValueConfigEntry("Font color the toast should use");
            toastAlignment.CreateValueConfigEntry("The position on the screen the toast should popup");
            toastOutlineWidth.CreateValueConfigEntry("Width of the text outline the toast should use");
            toastOutlineColor.CreateValueConfigEntry("Color of the text outline the toast should use");
            toastOpacity.CreateValueConfigEntry("The opacity of the text the toast should use");
            toastFadeInTime.CreateValueConfigEntry("How long the toast should take to fade in");
            toastFadeOutTime.CreateValueConfigEntry("How long the toast should take to fade out");
            toastHoldTime.CreateValueConfigEntry("How long the toast should remain on the screen before fading out");
            toastGapTime.CreateValueConfigEntry("How long the interval should be between displaying queued toasts");

            LoadOptionsFromConfig();
        }
        public static void LoadOptionsFromConfig()
        {
            foreach (IKatieSetting setting in allSettings)
            {
                setting.SetValuesFromConfig();
            }

            KatiePsuedoRandomizer.FillTargetEventLists();

            foreach (IKatieActionBase action in allActions)
            {
                action.SetValuesFromConfig();
            }

            LoadActiveActionsList();
            TryPrintConfig();

            OnConfigLoaded.Invoke();
        }

        public static void ReloadConfig()
        {
            KatieSaveHelperMod.Instance.Config.Reload();
            LoadOptionsFromConfig();
            ToastSettings.UpdateFromConfig();
        }

        public static void SaveConfig()
        {
            KatieSaveHelperMod.Instance.Config.Save();
        }

        public static void PrintConfig()
        {
            string configLog = $"Using mod config: \n" +
                $"\tSettings: \n";

            foreach (IKatieSetting setting in allSettings)
            {
                configLog += $"\t\t{setting.InternalName}: {setting.GetValueAsObject()}\n";
            }

            configLog += $"\tKeys: \n";

            foreach (IKatieActionBase action in allActiveActions)
            {
                configLog += $"\t\t{action.InternalName}: {action.Key}\n";
            }

            configLog += $"\tTransitions: \n";

            foreach (KatieTransitionAction action in allActiveTransitionActions)
            {
                configLog += $"\t\t{action.InternalName}:\n" +
                             $"\t\t\tType: {action.Transition.Type}\n";
                if (action.Transition.Type == SceneChanger.TransitionType.FadeToColor)
                    configLog += $"\t\t\tColor: {action.Transition.hexColor}\n" +
                                $"\t\t\tFade-In: {action.Transition.FadeInTime}\n" +
                                $"\t\t\tFade-Out: {action.Transition.FadeOutTime}\n";
            }

            KatieLogger.Info(configLog);
        }

        public static void TryPrintConfig()
        {
            if (logConfigOnReload.Value)
                PrintConfig();
        }
    }
}
