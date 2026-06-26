using JoelG.ENA4;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using System.Reflection;
using System.IO;
using BepInEx;
using KatieSaveHelper.Features.Util;
using KatieSaveHelper.Features.API;

namespace KatieSaveHelper
{
    #region Setting Enums

    public enum SeedGenerator
    {
        Random,
        PseudoRandom,
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

    #endregion

    public static class KatieConfig
    {
        internal static event Action OnConfigLoaded;

        public static FileHandler configFile { get; private set; } = new FileHandler(
            Path.Combine(Paths.ConfigPath, $"{KatieMain.modGUID}.cfg"),
            "KSH.Event.Config"
            );

        internal static List<IModSetting> allSettings
        {
            get
            {
                var settingList = new List<IModSetting>();
                var fields = typeof(Settings).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                foreach (var field in fields)
                {
                    if (typeof(IModSetting).IsAssignableFrom(field.FieldType))
                        if (field.GetValue(null) is IModSetting setting)
                            settingList.Add(setting);
                }
                return settingList;
            }
        }
        internal static List<IModActionBase> allActions
        {
            get
            {
                var actionList = new List<IModActionBase>();
                var fields = typeof(Settings).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                foreach (var field in fields)
                {
                    if (typeof(IModActionBase).IsAssignableFrom(field.FieldType))
                        if (field.GetValue(null) is IModActionBase action)
                            actionList.Add(action);
                }
                return actionList;
            }
        }

        internal static IEnumerable<IModActionBase> allActiveActions => allActions.Where(action => action.Key != KeyCode.None);

        internal static class Settings
        {
            internal static Transition baseTransition
            {
                get
                {
                    var colorTuple = baseTransitionColor.TryGetColorFromValue();
                    var color = colorTuple.success ? colorTuple.color : Color.black;

                    float fadeInTime = Mathf.Max(0f, baseTransitionFadeInTime.Value);
                    float fadeOutTime = Mathf.Max(0f, baseTransitionFadeOutTime.Value);

                    return new Transition(
                        baseTransitionType.Value,
                        color,
                        fadeInTime,
                        fadeOutTime
                        );
                }
            }

            internal static ModSetting<bool> autoSaveDisabledByDefault = new ModSetting<bool>("Game Auto Saving Disabled By Default", false);
            internal static ModSetting<SeedGenerator> saveSeedGeneratorType = new ModSetting<SeedGenerator>("Save Seed Generator Type", "SeedType_Save_Generator_Type", SeedGenerator.Random);
            internal static ModSetting<SeedGenerator> sessionSeedGeneratorType = new ModSetting<SeedGenerator>("Session Seed Generator Type", "SeedType_Session_Generator_Type", SeedGenerator.Random);
            internal static ModSetting<SeedGenerator> hardwareSeedGeneratorType = new ModSetting<SeedGenerator>("Hardware Seed Generator Type", "SeedType_Hardware_Generator_Type", SeedGenerator.Device);
            internal static ModSetting<CustomEventType> regenerateSessionSeed = new ModSetting<CustomEventType>("Regenerate Session Seed Event Trigger", "SeedType_Session_RegenerateSeed_EventTrigger", CustomEventType.OnHotkey);
            internal static ModSetting<CustomEventType> regenerateHardwareSeed = new ModSetting<CustomEventType>("Regenerate Hardware Seed Event Trigger", "SeedType_Hardware_RegenerateSeed_EventTrigger", CustomEventType.OnHotkey);
            internal static ModSetting<CustomEventType> resetBlinkRandomizer = new ModSetting<CustomEventType>("Reset Game Blink Randomizer Event Trigger", "ResetGameBlinkRandomizer_EventTrigger", CustomEventType.OnHotkey);
            internal static ModSetting<int> staticSaveSeed = new ModSetting<int>("Static Save Seed Value", "SeedType_Save_Generator_StaticSeedValue", 0);
            internal static ModSetting<int> staticSessionSeed = new ModSetting<int>("Static Session Seed Value", "SeedType_Session_Generator_StaticSeedValue", 0);
            internal static ModSetting<int> staticHardwareSeed = new ModSetting<int>("Static Hardware Seed Value", "SeedType_Hardware_Generator_StaticSeedValue", 0);
            internal static ModSetting<MainMenuSkipType> skipMainMenuIntro = new ModSetting<MainMenuSkipType>("Skip Main Menu Intro", MainMenuSkipType.Never);
            internal static ModSetting<bool> disableReturnToMainMenuPopup = new ModSetting<bool>("Disable Return To Main Menu Popup", false);
            internal static ModSetting<bool> disableCreateSavePopup = new ModSetting<bool>("Disable Create Save Popup", false);
            internal static ModSetting<bool> disableResetSavePopup = new ModSetting<bool>("Disable Reset Save Popup", false);
            internal static ModSetting<bool> disableSaveSelectDelay = new ModSetting<bool>("Disable Save Select Delay", false);
            internal static ModSetting<bool> disableSaveFileLockAfterCompletion = new ModSetting<bool>("Disable Save File Lock After Completion", false);
            internal static ModSetting<bool> disableSaveFileEncryption = new ModSetting<bool>("Disable Save File Encryption", "DisableFileEncryption_GameSave", false);
            internal static ModSetting<bool> disableMetaSaveFileEncryption = new ModSetting<bool>("Disable Meta Save File Encryption", "DisableFileEncryption_MetaSave", false);
            internal static ModSetting<bool> disableRemoteSaveSync = new ModSetting<bool>("Disable Steam Remote Save Sync", false);

            internal static ModSetting<bool> pseudoRandomNaturalSeedsOnly = new ModSetting<bool>("Pseudo Randomizer Generate Natural Seeds Only", "PseudoRandomizer_General_GenerateNaturalSeedsOnly", true);
            internal static ModSetting<long> pseudoRandomMaxAttempts = new ModSetting<long>("Pseudo Randomizer Search Attempt Limit", "PseudoRandomizer_General_SearchAttemptLimit", 1000000);
            internal static ModSetting<FrankDoor> pseudoRandomTargetFrankDoor = new ModSetting<FrankDoor>("Pseudo Randomizer Target Frank Door", "PseudoRandomizer_SaveMode_FrankDoor_Target", FrankDoor.Any);
            internal static ModSetting<TaxiHead> pseudoRandomTargetTaxiHead = new ModSetting<TaxiHead>("Pseudo Randomizer Target Taxi Head", "PseudoRandomizer_SaveMode_TaxiHead_Target", TaxiHead.Socio);
            internal static ModSetting<string> pseudoRandomTargetPurgeRoomGoals = new ModSetting<string>("Pseudo Randomizer Target Purge Goals", "PseudoRandomizer_SaveMode_PurgeGoals_Target", "*RLRLRL");
            internal static ModSetting<string> pseudoRandomTargetPurgeRoomObstacles = new ModSetting<string>("Pseudo Randomizer Target Purge Obstacles", "PseudoRandomizer_SaveMode_PurgeObstacles_Target", "**Any, !WanderingFish");
            internal static ModSetting<string> pseudoRandomTargetBlinkAttempt = new ModSetting<string>("Pseudo Randomizer Target First Blink Attempt", "PseudoRandomizer_SessionMode_FirstBlinkAttempt_Target", "1");
            internal static ModSetting<bool> pseudoRandomBlinkAssumeInCore = new ModSetting<bool>("Pseudo Randomizer Assume Blink In Core", "PseudoRandomizer_SessionMode_FirstBlinkAttempt_AssumeInCore", false);
            internal static ModSetting<EnaTaxiMood> pseudoRandomEnaTaxiMood = new ModSetting<EnaTaxiMood>("Pseudo Randomizer Target Ena Taxi Mood", "PseudoRandomizer_HardwareMode_EnaTaxiMood_Target", EnaTaxiMood.Meanie);

            internal static ModSetting<bool> showToasts = new ModSetting<bool>("Show Toast Notifications", true);
            internal static ModSetting<string> toastFontFileName = new ModSetting<string>("Toast Font File Name", "Toast_FontFileName", "RuneScape-ENA");
            internal static ModSetting<int> toastFontSize = new ModSetting<int>("Toast Font Size", "Toast_FontSize", 36);
            internal static ModSetting<string> toastFontColor = new ModSetting<string>("Toast Font Color", "Toast_FontColor", "#FFFFFF");
            internal static ModSetting<TextAlignmentOptions> toastAlignment = new ModSetting<TextAlignmentOptions>("Toast Screen Position", "Toast_ScreenPosition", TextAlignmentOptions.TopRight);
            internal static ModSetting<int> toastOutlineWidth = new ModSetting<int>("Toast Outline Width", "Toast_OutlineWidth", 5);
            internal static ModSetting<string> toastOutlineColor = new ModSetting<string>("Toast Outline Color", "Toast_OutlineColor", "#000000");
            internal static ModSetting<int> toastOpacity = new ModSetting<int>("Toast Opacity", "Toast_Opacity", 100);
            internal static ModSetting<float> toastFadeInTime = new ModSetting<float>("Toast Fade-In Time", "Toast_FadeInTime", 0.25f);
            internal static ModSetting<float> toastFadeOutTime = new ModSetting<float>("Toast Fade-Out Time", "Toast_FadeOutTime", 0.25f);
            internal static ModSetting<float> toastHoldTime = new ModSetting<float>("Toast Hold Time", "Toast_HoldTime", 1.50f);
            internal static ModSetting<float> toastGapTime = new ModSetting<float>("Toast Gap Time", "Toast_GapTime", 0.25f);

            internal static ModSetting<SceneChanger.TransitionType> baseTransitionType = new ModSetting<SceneChanger.TransitionType>("Hotkey Scene Transition Type", "HotkeySceneTransition_Type", SceneChanger.TransitionType.FadeToColor);
            internal static ModSetting<string> baseTransitionColor = new ModSetting<string>("Hotkey Scene Transition Color", "HotkeySceneTransition_Color", "#000000");
            internal static ModSetting<float> baseTransitionFadeInTime = new ModSetting<float>("Hotkey Scene Transition Fade In Time", "HotkeySceneTransition_FadeInTime", 0.5f);
            internal static ModSetting<float> baseTransitionFadeOutTime = new ModSetting<float>("Hotkey Scene Transition Fade Out Time", "HotkeySceneTransition_FadeOutTime", 0.5f);
            internal static ModSetting<bool> notifyOnFirstBlinkAttempts = new ModSetting<bool>("Notify On First Blink Attempts", false);
            internal static ModSetting<bool> notifyOnSimulatedAchievements = new ModSetting<bool>("Notify On Simulated Achievements", false);
            internal static ModSetting<float> simulatedAchievementToastHoldTime = new ModSetting<float>("Simulated Achievement Toast Hold Time", "SimulatedAchievementToast_HoldTime", 5f);
            internal static ModSetting<bool> logConfigOnReload = new ModSetting<bool>("Log Config On Reload", true);
            internal static ModSetting<bool> assetSubcriber = new ModSetting<bool>("Subscribe To Asset Updates", false);
            internal static ModSetting<bool> debugMode = new ModSetting<bool>("Debug Mode", false);

            internal static ModAction reloadConfig = new ModAction("Reload Config", ModActions.reloadConfig, KeyCode.F4, customCoroutineGroup: configFile.groupIdentifier);
            internal static ModAction quickSave = new ModAction("Quick Save", ModActions.quickSave, KeyCode.F1);
            internal static ModAction logCurrentSeedInfo = new ModAction("Log Current Seed Info", ModActions.logCurrentSeedInfo, KeyCode.None);
            internal static ModAction toggleAutoSave = new ModAction("Toggle Game Auto Saving", ModActions.toggleAutoSave, KeyCode.None);
            internal static ModAction regenerateSessionSeedAction = new ModAction("Regenerate Session Seed", ModActions.regenerateSessionSeed, KeyCode.None, "RegenerateSeed.Session");
            internal static ModAction regenerateHardwareSeedAction = new ModAction("Regenerate Hardware Seed", ModActions.regenerateHardwareSeed, KeyCode.None, "RegenerateSeed.Hardware");
            internal static ModAction resetGameBlinkRandomizerAction = new ModAction("Reset Game Blink Randomizer", ModActions.resetGameBlinkRandomizer, KeyCode.None);

            internal static readonly Transition defaultTransition = new Transition(SceneChanger.TransitionType.FadeToColor, Color.black, 0.5f, 0.5f);

            internal static ModAction exitToSaveSelect = new ModAction("Exit To Save Select", ModActions.exitToSaveSelect, KeyCode.None);
            internal static ModAction exitToSaveSelectAndEraseSave = new ModAction("Exit To Save Select And Erase Save", ModActions.exitToSaveSelectAndEraseSave, KeyCode.None);
            internal static ModAction warpToNextScene = new ModAction("Warp To Next Scene", ModActions.warpToNextScene, KeyCode.None);
            internal static ModAction warpToPrevScene = new ModAction("Warp To Previous Scene", ModActions.warpToPrevScene, KeyCode.None);
            internal static ModAction warpToNextEntrance = new ModAction("Warp To Next Entrance", ModActions.warpToNextEntrance, KeyCode.None);
            internal static ModAction warpToPrevEntrance = new ModAction("Warp To Previous Entrance", ModActions.warpToPrevEntrance, KeyCode.None);

            internal static ModAction reloadSaveWithFileSeed = new ModAction("Reload Save With File Seed", ModActions.reloadSaveWithFileSeed, KeyCode.F2, "SaveReloader.Reload.WithFile");
            internal static ModAction reloadSaveWithCurrentSeed = new ModAction("Reload Save With Current Seed", ModActions.reloadSaveWithCurrentSeed, KeyCode.None, "SaveReloader.Reload.WithCurrent");
            internal static ModAction reloadSaveWithNewSeed = new ModAction("Reload Save With New Seed", ModActions.reloadSaveWithNewSeed, KeyCode.None, "SaveReloader.Reload.WithNew");

            internal static ModAction resetSaveWithFileSeed = new ModAction("Reset Save With File Seed", ModActions.resetSaveWithFileSeed, KeyCode.None, "SaveReloader.Reset.WithFile");
            internal static ModAction resetSaveWithCurrentSeed = new ModAction("Reset Save With Current Seed", ModActions.resetSaveWithCurrentSeed, KeyCode.F3, "SaveReloader.Reset.WithCurrent");
            internal static ModAction resetSaveWithNewSeed = new ModAction("Reset Save With New Seed", ModActions.resetSaveWithNewSeed, KeyCode.None, "SaveReloader.Reset.WithNew");

            internal static void CreateConfigEntries()
            {
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
                disableSaveSelectDelay.CreateValueConfigEntry("Whether the mod should disable the small delay before the scene transition after selecting a save in the Main Menu");
                disableSaveFileLockAfterCompletion.CreateValueConfigEntry("Whether the mod should disable save files becoming locked after being completed");
                disableSaveFileEncryption.CreateValueConfigEntry("Whether the mod should prevent the game from encrypting save files when they are updated");
                disableMetaSaveFileEncryption.CreateValueConfigEntry("Whether the mod should prevent the game from encrypting the meta save file when it is updated");
                disableRemoteSaveSync.CreateValueConfigEntry("Whether the mod should prevent the game from overwriting existing save files with backups from Steam Remote Storage on launch");

                pseudoRandomTargetFrankDoor.CreateValueConfigEntry("Whether a single door or multiple doors in the Lost Village will be knockable");
                pseudoRandomTargetTaxiHead.CreateValueConfigEntry("Name of the Taxi Head that the mod's pseudo-randomizer will target when generating a new seed");
                pseudoRandomTargetPurgeRoomGoals.CreateValueConfigEntry("Order of the room goals during the Purge Event Maze that the mod's pseudo-randomizer will target when generating a new seed");
                pseudoRandomTargetPurgeRoomObstacles.CreateValueConfigEntry("Dog obstacles within each room during the Purge Event Maze that the mod's pseudo-randomizer will target when generating a new seed");
                pseudoRandomTargetBlinkAttempt.CreateValueConfigEntry("The range of attempt numbers in which ENA will blink for the first time");
                pseudoRandomBlinkAssumeInCore.CreateValueConfigEntry("Whether to assume the player is in the core when the randomizer is simulating blink attempts");
                pseudoRandomEnaTaxiMood.CreateValueConfigEntry("Which side of ENA will speak for the special dialogue during the first interaction with the Taxi Driver");
                pseudoRandomMaxAttempts.CreateValueConfigEntry("Maximum number of attempts the pseudo-randomizer will make to find a matching seed before quitting");
                pseudoRandomNaturalSeedsOnly.CreateValueConfigEntry("Whether the mod's pseudo-randomizer should exclusively generate seeds the game itself can naturally generate");

                baseTransitionType.CreateValueConfigEntry("The transition type that hotkey actions should use when they trigger a scene transition");
                baseTransitionColor.CreateValueConfigEntry("The color that transitions will fade into and out of when a hotkey action triggers a scene transition");
                baseTransitionFadeInTime.CreateValueConfigEntry("The amount of time (in seconds) the transition should fade in for when a hotkey action triggers a scene transition");
                baseTransitionFadeOutTime.CreateValueConfigEntry("The amount of time (in seconds) the transition should fade out for when a hotkey action triggers a scene transition");
                showToasts.CreateValueConfigEntry("Whether the mod should display a toast notification on the screen when performing the mod's various hotkey actions");
                notifyOnFirstBlinkAttempts.CreateValueConfigEntry("Whether the mod should display a toast notification when the game internally attempts a blink and has not yet triggered the randomizer's first blink");
                notifyOnSimulatedAchievements.CreateValueConfigEntry("Whether the mod should display a toast notification when the game internally triggers an achievement that isn't already in the mod's Simulated Achievements list");
                simulatedAchievementToastHoldTime.CreateValueConfigEntry("How long Simulated Achievement toasts should remain on the screen before fading out");
                logConfigOnReload.CreateValueConfigEntry("Whether the mod should print the newly loaded config to the modloader's console log after reloading it");
                assetSubcriber.CreateValueConfigEntry("Whether the mod should attempt to download new assets from it's GitHub repo on launch, when they are available");
                debugMode.CreateValueConfigEntry("Whether the mod should print additional debug messages to the modloader's console log");

                quickSave.CreateKeyConfigEntry("write the data from the current save into it's respective save file");
                reloadConfig.CreateKeyConfigEntry("reload all mod settings from this config file");
                logCurrentSeedInfo.CreateKeyConfigEntry("log the current seed values, and the in-game events they trigger, to the modloader console log");
                toggleAutoSave.CreateKeyConfigEntry("toggle the game's auto saving feature on and off");
                regenerateSessionSeedAction.CreateKeyConfigEntry("regenerate the game's active session seed using the generator specified in the 'Session Seed Generator Type' setting");
                regenerateHardwareSeedAction.CreateKeyConfigEntry("regenerate the game's active hardware seed using the generator specified in the 'Hardware Seed Generator Type' setting");
                resetGameBlinkRandomizerAction.CreateKeyConfigEntry("reset the game's Blink Randomizer");

                exitToSaveSelect.CreateKeyConfigEntry("Key used to immediately exit the current save to the Save Select Menu");
                exitToSaveSelectAndEraseSave.CreateKeyConfigEntry("Key used to immediately erase the current save and exit to the Save Select Menu");
                warpToNextScene.CreateKeyConfigEntry("Key used to immediately warp to the next scene from the game's internal scene list");
                warpToPrevScene.CreateKeyConfigEntry("Key used to immediately warp to the previous scene from the game's internal scene list");
                warpToNextEntrance.CreateKeyConfigEntry("Key used to immediately warp to the next scene entrance within the currently loaded scene");
                warpToPrevEntrance.CreateKeyConfigEntry("Key used to immediately warp to the previous scene entrance within the currently loaded scene");
                reloadSaveWithFileSeed.CreateKeyConfigEntry("Key used to reload the current save normally, using the seed from it's respective save file");
                reloadSaveWithCurrentSeed.CreateKeyConfigEntry("Key used to reload the current save with the currently loaded seed");
                reloadSaveWithNewSeed.CreateKeyConfigEntry("Key used to reload the current save with a new seed generated using the method specified in the 'Save Seed Generator Type' setting");
                resetSaveWithFileSeed.CreateKeyConfigEntry("Key used to immediately erase the current save, create a new empty one in the same slot that has the same seed as the deleted save file, then load it");
                resetSaveWithCurrentSeed.CreateKeyConfigEntry("Key used to immediately erase the current save, create a new empty one in the same slot that has the currently loaded seed, then load it");
                resetSaveWithNewSeed.CreateKeyConfigEntry("Key used to immediately erase the current save, create a new empty one in the same slot that has a new generated seed using the method specified in the 'Save Seed Generator Type' setting, then load it");

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
            }
        }

        public static void LoadConfig()
        {
            KatieLogger.Info("Loading config...");

            Settings.CreateConfigEntries();

            LoadOptionsFromConfig();

            // Set default toggle settings
            ModActions.autoSaveDisabled = Settings.autoSaveDisabledByDefault.Value;
        }
        public static void LoadOptionsFromConfig()
        {
            foreach (IModSetting setting in allSettings)
            {
                setting.SetValuesFromConfig();
            }

            KatiePseudoRandomizer.ResetTargetEventLists();

            foreach (IModActionBase action in allActions)
            {
                action.SetValuesFromConfig();
            }

            TryPrintConfig();

            OnConfigLoaded.Invoke();
        }

        public static void DisableDefaults()
        {
            foreach(IModActionBase action in allActions)
                action.DisableDefaultKey();
        }

        public static void ReloadConfig(bool async = false) =>
            configFile.RunWithFile(
                "KSH.Event.Config.Reload",
                FileMode.OpenOrCreate,
                FileAccess.Read,
                fileAction: KatieMain.Instance.Config.Reload,
                async: async,
                onSuccess: () =>
                {
                    LoadOptionsFromConfig();
                    ToastSettings.UpdateFromConfig();
                },
                onFail: () => KatieLogger.Error("Failed to load config")
            );

        public static void SaveConfig(bool async = false) =>
            configFile.RunWithFile(
                "KSH.Event.Config.Save",
                FileMode.OpenOrCreate,
                FileAccess.Write,
                fileAction: KatieMain.Instance.Config.Save,
                async: async,
                onFail: () => KatieLogger.Error("Failed to save config")
            );

        public static void PrintConfig()
        {
            string configLog = $"Using mod config: \n" +
                $"\tSettings: \n";

            foreach (IModSetting setting in allSettings)
            {
                configLog += $"\t\t{setting.InternalName}: {setting.GetValueAsObject()}\n";
            }

            configLog += $"\tKeys: \n";

            if (allActiveActions.Any())
            {
                foreach (IModActionBase action in allActiveActions)
                {
                    configLog += $"\t\t{action.InternalName}: {action.Key}\n";
                }
            }
            else
            {
                configLog += "\t\tNone set";
            }

            KatieLogger.Info(configLog);
        }

        public static void TryPrintConfig()
        {
            if (Settings.logConfigOnReload.Value)
                PrintConfig();
        }
    }
}
