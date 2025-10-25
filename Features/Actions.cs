using JoelG.ENA4;
using KatieSaveHelper.Patches;
using UnityEngine.SceneManagement;
using LMirman.Utilities;
using System;
using JoelG.ENA4.UI;
using UnityEngine;
using JoelG.ENA4.Audio;

namespace KatieSaveHelper
{
    public static class KatieSaveHelperModActions
    {

        internal static StagedValue<int> customSeedOnReset = new StagedValue<int>();
        internal static StagedValue<int> customSeedOnLoad = new StagedValue<int>();
        internal static StagedValue<Transition> customTransition = new StagedValue<Transition>();
        internal static StagedValue<MainMenuPanelType> customMainMenuPanelOnLoad = new StagedValue<MainMenuPanelType>();

        internal static bool autoSaveDisabled = false;
        internal static bool allowNextSaveAttempt = false;

        public static void reloadConfig()
        {
            KatieSaveHelperModConfig.ReloadConfig();

            ToastController.TryQueueToast("Config reloaded");
        }
        public static void quickSave()
        {
            string currentSceneName = SceneManager.GetActiveScene().name;

            if (currentSceneName == "Menu")
            {
                ToastController.TryQueueAndLogToast("Cannot save in Main Menu");
                return;
            }

            allowNextSaveAttempt = true;
            if (currentSceneName == SaveFile.CurrentSave.GameState.GetDestinationScene())
            {
                SaveFile.WriteSaveWithEntrance(SaveFile.CurrentSave.GameState.SavedSceneEntrance);
            }
            else
            {
                SaveFile.WriteSaveWithEntrance();
            }
            ToastController.TryQueueAndLogToast("Game saved");
        }

        public static void logCurrentSeedInfo()
        {
            int currentSaveIndex = MetaSaveFile.Current.SaveIndex;
            int currentSaveFileHash;
            bool fileSuccess;
            try
            {
                var ganeFile = KatieUtil.ReadGameFile(currentSaveIndex);
                currentSaveFileHash = ganeFile.Data.SaveHash;
                fileSuccess = true;
            }
            catch (Exception ex)
            {
                currentSaveFileHash = 0;
                fileSuccess = false;
            }

            int playSessionHash = SaveRandomizerHashes.GetHashByType(SaveRandomizerHashes.HashType.PlaySession);
            int hardwareHash = SaveRandomizerHashes.GetHashByType(SaveRandomizerHashes.HashType.Hardware);
            int saveHash = SaveRandomizerHashes.GetHashByType(SaveRandomizerHashes.HashType.SaveFile);

            var purgeTuple = KatiePsuedoRandomizer.SaveMode.EvaluatePurgeSpecial(saveHash);
            var blinkTuple = KatiePsuedoRandomizer.SessionMode.EvaluateFirstBlink(playSessionHash);

            KatieLogger.Info($"Current Seeds:" +
                $"\n\tActive Save Seed: {saveHash}" +
                $"\n\tSlot {currentSaveIndex + 1} File Save Seed: {(fileSuccess ? currentSaveFileHash.ToString() : "File Not Found.")}" +
                $"\n\tActive Session Seed: {playSessionHash}" +
                $"\n\tActive Hardware Seed: {hardwareHash}");

            KatieLogger.Info("Active Events: " +
                $"\n\tSave Events: " +
                $"\n\t\tFrank Door: {KatiePsuedoRandomizer.SaveMode.frankDoor.Evaluate(saveHash)?.Name}" +
                $"\n\t\tTaxi Driver Head: {KatiePsuedoRandomizer.SaveMode.taxiHeads.Evaluate(saveHash)?.Name}" +
                $"\n\t\tPurge Goals: {string.Join(", ", purgeTuple.purgeGoals)}" +
                $"\n\t\tPurge Obstacles: {string.Join(", ", purgeTuple.purgeObstacles)}" +
                $"\n\tSession Events: " +
                $"\n\t\tFirst Possible Normal Blink Attempt: {blinkTuple.firstNormalBlinkAttempt}" +
                $"\n\t\tFirst Possible Core Blink Attempt: {blinkTuple.firstCoreBlinkAttempt}" +
                $"\n\tHardware Events: " +
                $"\n\t\tENA Taxi Mood: {KatiePsuedoRandomizer.HardwareMode.EvaluateEnaTaxiMood(hardwareHash)}"
                );

            ToastController.TryQueueToast("Current seed info logged");
        }

        public static void toggleAutoSave()
        {
            autoSaveDisabled = !autoSaveDisabled;

            string status = autoSaveDisabled ? "disabled" : "enabled";

            ToastController.TryQueueAndLogToast($"Auto saving {status}");
        }

        public static void exitToSaveSelect()
        {
            if (SceneManager.GetActiveScene().name == "Menu")
            {
                ToastController.TryQueueAndLogToast("Already in the Main Menu");
                return;
            }

            customMainMenuPanelOnLoad.StageValue(MainMenuPanelType.FileSelect);

            KatieUtil.ChangeScene("Menu", KatieSaveHelperModConfig.exitToSaveSelect.Transition, stopAudio: true, stopCutscenes: true);
        }

        public static void exitToSaveSelectAndEraseSave()
        {
            if (SceneManager.GetActiveScene().name == "Menu")
            {
                ToastController.TryQueueAndLogToast("Already in the Main Menu");
                return;
            }

            SaveFile.DeleteSave(MetaSaveFile.Current.SaveIndex);

            customMainMenuPanelOnLoad.StageValue(MainMenuPanelType.FileSelect);

            KatieUtil.ChangeScene("Menu", KatieSaveHelperModConfig.exitToSaveSelectAndEraseSave.Transition, stopAudio: true, stopCutscenes: true);
        }

        public static void warpToNextScene()
        {
            if (SceneManager.GetActiveScene().name == "Menu")
            {
                ToastController.TryQueueAndLogToast("Cannot warp in Main Menu");
                return;
            }

            KatieSceneWarp.WarpNext();
            ToastController.TryQueueAndLogToast("Warped to next Scene");
        }

        public static void warpToPrevScene()
        {
            if (SceneManager.GetActiveScene().name == "Menu")
            {
                ToastController.TryQueueAndLogToast("Cannot warp in Main Menu");
                return;
            }

            KatieSceneWarp.WarpPrev();
            ToastController.TryQueueAndLogToast("Warped to previous Scene");
        }

        public static void regenerateSessionSeed()
        {
            if (KatieSaveHelperModConfig.regenerateSessionSeed.Value != CustomEventType.OnHotkey)
            {
                ToastController.TryQueueAndLogToast("Hotkey for regenerating Session seed is disabled");
                return;
            }

            if (SceneManager.GetActiveScene().name != "Menu")
            {
                ToastController.TryQueueAndLogToast("Cannot regenerate Session seed outside Main Menu");
                return;
            }

            var seedTuple = KatieUtil.GenerateSessionSeed();
            if (!seedTuple.success)
            {
                ToastController.TryQueueToast("Could not regenerate Sessions seed, no seed found");
            }

            KatieUtil.EditSessionHash(seedTuple.seed, showToast: true);
        }

        public static void regenerateHardwareSeed()
        {
            if (KatieSaveHelperModConfig.regenerateHardwareSeed.Value != CustomEventType.OnHotkey)
            {
                ToastController.TryQueueAndLogToast("Hotkey for regenerating Hardware seed is disabled");
                return;
            }

            if (SceneManager.GetActiveScene().name != "Menu")
            {
                ToastController.TryQueueAndLogToast("Cannot regenerate Hardware seed outside Main Menu");
                return;
            }

            var seedTuple = KatieUtil.GenerateHardwareSeed();
            if (!seedTuple.success)
            {
                ToastController.TryQueueToast("Could not regenerate Sessions seed, no seed found");
            }

            KatieUtil.EditHardwareHash(seedTuple.seed, showToast: true);
        }

        public static void resetGameBlinkRandomizer()
        {
            if (KatieSaveHelperModConfig.resetBlinkRandomizer.Value != CustomEventType.OnHotkey)
            {
                ToastController.TryQueueAndLogToast("Hotkey for resetting Blink Randomizer is disabled");
                return;
            }

            if (SceneManager.GetActiveScene().name != "Menu")
            {
                ToastController.TryQueueAndLogToast("Cannot reset Blink Randomizer outside Main Menu");
                return;
            }

            PlayerRandomBlink_Patch.ResetBlinkChanceGenerator();

            ToastController.TryQueueToast("Blink Randomizer reset");
        }

        public static void resetSimulatedAchievements()
        {
            if (KatieSaveHelperModConfig.resetSimulatedAchievements.Value != CustomEventType.OnHotkey)
            {
                ToastController.TryQueueAndLogToast("Hotkey for resetting Simulated Achievements is disabled");
                return;
            }

            if (SceneManager.GetActiveScene().name != "Menu")
            {
                ToastController.TryQueueAndLogToast("Cannot reset Simulated Achievements outside Main Menu");
                return;
            }

            Achievements_Patch.ResetSimulatedAchievements(showToast: true);
        }

        public static void reloadSaveWithFileSeed()
        {
            if (SceneManager.GetActiveScene().name == "Menu")
            {
                ToastController.TryQueueAndLogToast("Cannot reload saves in Main Menu");
                return;
            }

            ToastController.TryQueueToast("Reloading save with File seed");

            KatieUtil.StopAllCutscenes();
            AudioPlayback.StopAllAudio();

            customTransition.StageValue(KatieSaveHelperModConfig.reloadSaveWithFileSeed.Transition.Copy());

            SaveFile.ContinueSave();
        }

        public static void reloadSaveWithCurrentSeed()
        {
            if (SceneManager.GetActiveScene().name == "Menu")
            {
                ToastController.TryQueueAndLogToast("Cannot reload saves in Main Menu");
                return;
            }

            ToastController.TryQueueToast("Reloading Save using Current seed");

            KatieUtil.StopAllCutscenes();
            AudioPlayback.StopAllAudio();

            customTransition.StageValue(KatieSaveHelperModConfig.reloadSaveWithCurrentSeed.Transition.Copy());

            customSeedOnLoad.StageValue(SaveFile.CurrentSave.SaveHash);

            SaveFile.ContinueSave();
        }

        public static void reloadSaveWithNewSeed()
        {
            if (SceneManager.GetActiveScene().name == "Menu")
            {
                ToastController.TryQueueAndLogToast("Cannot reload saves in Main Menu");
                return;
            }

            ToastController.TryQueueToast("Reloading Save using New seed");

            customTransition.StageValue(KatieSaveHelperModConfig.reloadSaveWithNewSeed.Transition.Copy());

            var seedTuple = KatieUtil.GenerateSaveSeed();
            if (!seedTuple.success)
            {
                ToastController.TryQueueToast("Could not reload, no seed found");
                return;
            }

            KatieUtil.StopAllCutscenes();
            AudioPlayback.StopAllAudio();

            customSeedOnLoad.StageValue(seedTuple.seed);

            SaveFile.ContinueSave();
        }

        public static void resetSaveWithFileSeed()
        {
            if (SceneManager.GetActiveScene().name == "Menu")
            {
                ToastController.TryQueueAndLogToast("Cannot reset saves in Main Menu");
                return;
            }

            ToastController.TryQueueToast("Resetting Save using File seed");

            GameFile<SaveFileData> gameFile = KatieUtil.ReadGameFile(MetaSaveFile.Current.SaveIndex);

            KatieUtil.StopAllCutscenes();
            AudioPlayback.StopAllAudio();

            customTransition.StageValue(KatieSaveHelperModConfig.resetSaveWithFileSeed.Transition.Copy());

            customSeedOnReset.StageValue(gameFile.Data.SaveHash);

            SaveFile.ResetSave(MetaSaveFile.Current.SaveIndex);
            SaveFile.ContinueSave();
        }
        public static void resetSaveWithCurrentSeed()
        {
            if (SceneManager.GetActiveScene().name == "Menu")
            {
                ToastController.TryQueueAndLogToast("Cannot reset saves in Main Menu");
                return;
            }

            ToastController.TryQueueToast("Resetting Save using Current seed");

            KatieUtil.StopAllCutscenes();
            AudioPlayback.StopAllAudio();

            customTransition.StageValue(KatieSaveHelperModConfig.resetSaveWithCurrentSeed.Transition.Copy());

            customSeedOnReset.StageValue(SaveFile.CurrentSave.SaveHash);

            SaveFile.ResetSave(MetaSaveFile.Current.SaveIndex);
            SaveFile.ContinueSave();
        }

        public static void resetSaveWithNewSeed()
        {
            if (SceneManager.GetActiveScene().name == "Menu")
            {
                ToastController.TryQueueAndLogToast("Cannot reset saves in Main Menu");
                return;
            }

            ToastController.TryQueueToast("Resetting Save using New seed");

            var seedTuple = KatieUtil.GenerateSaveSeed();
            if (!seedTuple.success)
            {
                ToastController.TryQueueToast("Could not reset, no seed found");
                return;
            }

            KatieUtil.StopAllCutscenes();
            AudioPlayback.StopAllAudio();

            customTransition.StageValue(KatieSaveHelperModConfig.resetSaveWithNewSeed.Transition.Copy());

            customSeedOnReset.StageValue(seedTuple.seed);

            SaveFile.ResetSave(MetaSaveFile.Current.SaveIndex);
            SaveFile.ContinueSave();
        }

    }

}
