using JoelG.ENA4;
using KatieSaveHelper.Patches;
using UnityEngine.SceneManagement;
using LMirman.Utilities;
using System;
using System.Collections;
using MelonLoader;
using System.IO;
using KatieSaveHelper.Features.API;
using KatieSaveHelper.Features.Util;

namespace KatieSaveHelper
{
    public static class ModActions
    {
        internal static StagedValue<MainMenuPanelType> customMainMenuPanelOnLoad = new StagedValue<MainMenuPanelType>();

        internal static bool autoSaveDisabled = false;
        internal static bool allowNextSaveAttempt = false;

        private static string ActiveSceneName
        {
            get
            {
                return SceneManager.GetActiveScene().name;
            }
        }

        private static readonly OnSceneLoadPatch oslPatcher = new OnSceneLoadPatch(CheckKatieRoutines, patchOnStartup: true);
        private static void CheckKatieRoutines(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "Menu")
            {
                StaticCoroutine.RequestCancelAll(sc => sc.Identifier.StartsWith("KSH.Action.SaveReloader"));
            }
            else
            {
                StaticCoroutine.RequestCancelAll(sc => sc.Identifier == MainMenuPanelGroup_Patch.menuEventTriggerRoutineIdentifier || sc.Identifier.StartsWith("KSH.Action.RegenerateSeed"));
            }
        }

        public static IEnumerator reloadConfig(StaticCoroutine scWrapper)
        {
            if (StaticCoroutine.AnyGroupDupesActive(scWrapper))
            {
                ToastBehaviours.Notice(
                    new ToastInstance(
                        "Config failed to reload, please try again shortly",
                        "KSH.ReloadConfig.Fail.Throttle",
                        holdTime: 3f
                    ),
                    sendToLog: false
                );
                yield break;
            }

            yield return StaticCoroutine.WaitForCancelDupes(scWrapper, scWrapper.CancelToken);
            if (scWrapper.CancelToken.IsCancellationRequested) yield break;

            bool success = false;

            yield return KatieConfig.configFile.FileActionRoutine(
                FileMode.OpenOrCreate,
                FileAccess.Read,
                fileAction: MelonPreferences.Load,
                token: scWrapper.CancelToken,
                onSuccess: () => success = true,
                onFail: () => success = false
            );

            if (scWrapper.CancelToken.IsCancellationRequested) yield break;

            if (success)
            {
                KatieConfig.LoadOptionsFromConfig();
                ToastSettings.UpdateFromConfig();
                ToastBehaviours.QuickAction("Config reloaded", "KSH.ReloadConfig.Success", sendToLog: false);
            }
            else
            {
                ToastBehaviours.Notice(
                    new ToastInstance(
                        "Config failed to reload, check logs for details",
                        "KSH.ReloadConfig.Fail.Other",
                        holdTime: 5f
                    ),
                    sendToLog: false
                );
                KatieLogger.Error("Failed to reload config");
            }

            StaticCoroutine.RequestCancelAll(sc => sc.Identifier.StartsWith("KSH.Action.SaveReloader") || sc.Identifier.StartsWith("KSH.Action.RegenerateSeed"));

            if (StaticCoroutine.AnyActive(sc => sc.Identifier == SaveRandomizerHashes_Patch.seedInjectRoutineIdentifier && !sc.CancelToken.IsCancellationRequested))
            {
                SaveRandomizerHashes_Patch.StartNewSeedInjector();
            }
            else if (StaticCoroutine.AnyActive(sc => sc.Identifier == MainMenuPanelGroup_Patch.menuEventTriggerRoutineIdentifier && !sc.CancelToken.IsCancellationRequested))
            {
                MainMenuPanelGroup_Patch.StartNewMenuEventTrigger();
            }
            else
            {
                MainMenu_Patch.TryCancelSaveTransition();
            }
        }
        public static void quickSave()
        {
            string currentSceneName = ActiveSceneName;

            if (currentSceneName == "Menu")
            {
                ToastBehaviours.Notice("Cannot save in Main Menu", "KSH.QuickSave.InMenu");
                return;
            }

            allowNextSaveAttempt = true;
            string stateSceneName = SaveFile.CurrentSave.GameState.GetDestinationScene();
            if (currentSceneName == stateSceneName)
            {
                // Save at the scene and entrance flag stored in the internal game state
                SaveFile.WriteSave();
            }
            else
            {
                // Save at the currently active scene with the default entrance flag
                SaveFile.WriteSaveWithEntrance();
            }

            ToastBehaviours.QuickAction("Game saved", "KSH.QuickSave.Success");
        }

        public static void logCurrentSeedInfo()
        {
            int currentSaveIndex = MetaSaveFile.Current.SaveIndex;
            int currentSaveFileHash = 0;
            bool fileSuccess;
            try
            {
                var gameFile = KatieUtil.ReadGameFile(currentSaveIndex);
                currentSaveFileHash = gameFile.Data.SaveHash;
                fileSuccess = true;
            }
            catch (Exception)
            {
                fileSuccess = false;
            }

            int playSessionHash = SaveRandomizerHashes.GetHashByType(SaveRandomizerHashes.HashType.PlaySession);
            int hardwareHash = SaveRandomizerHashes.GetHashByType(SaveRandomizerHashes.HashType.Hardware);
            int saveHash = SaveRandomizerHashes.GetHashByType(SaveRandomizerHashes.HashType.SaveFile);

            var purgeTuple = KatiePseudoRandomizer.SaveMode.EvaluatePurgeSpecial(saveHash);
            var blinkTuple = KatiePseudoRandomizer.SessionMode.EvaluateFirstBlink(playSessionHash);

            KatieLogger.Info($"Current Seeds:" +
                $"\n\tActive Save Seed: {saveHash}" +
                $"\n\tSlot {currentSaveIndex + 1} File Save Seed: {(fileSuccess ? currentSaveFileHash.ToString() : "File Not Found.")}" +
                $"\n\tActive Session Seed: {playSessionHash}" +
                $"\n\tActive Hardware Seed: {hardwareHash}");

            KatieLogger.Info("Active Events: " +
                $"\n\tSave Events: " +
                $"\n\t\tFrank Door: {KatiePseudoRandomizer.SaveMode.frankDoor.Evaluate(saveHash)?.Name}" +
                $"\n\t\tTaxi Driver Head: {KatiePseudoRandomizer.SaveMode.taxiHeads.Evaluate(saveHash)?.Name}" +
                $"\n\t\tPurge Goals: {string.Join(", ", purgeTuple.purgeGoals)}" +
                $"\n\t\tPurge Obstacles: {string.Join(", ", purgeTuple.purgeObstacles)}" +
                $"\n\tSession Events: " +
                $"\n\t\tFirst Possible Normal Blink Attempt: {blinkTuple.firstNormalBlinkAttempt}" +
                $"\n\t\tFirst Possible Core Blink Attempt: {blinkTuple.firstCoreBlinkAttempt}" +
                $"\n\tHardware Events: " +
                $"\n\t\tENA Taxi Mood: {KatiePseudoRandomizer.HardwareMode.EvaluateEnaTaxiMood(hardwareHash)}"
                );

            ToastBehaviours.QuickAction("Current seed info logged", "KSH.LogCurrentSeedInfo");
        }

        public static void toggleAutoSave()
        {
            autoSaveDisabled = !autoSaveDisabled;

            string status = autoSaveDisabled ? "disabled" : "enabled";

            ToastBehaviours.Toggle($"Auto saving is now {status}", "KSH.ToggleAutoSave");
        }

        public static void exitToSaveSelect()
        {
            if (ActiveSceneName == "Menu")
            {
                ToastBehaviours.Notice("Already in the Main Menu", "KSH.ExitToSaveSelect.InMenu");
                return;
            }

            customMainMenuPanelOnLoad.StageValue(MainMenuPanelType.FileSelect);

            KatieUtil.ChangeScene("Menu", transition: KatieConfig.Settings.baseTransition, stopAudio: true, stopCutscenes: true);
        }

        public static void exitToSaveSelectAndEraseSave()
        {
            if (ActiveSceneName == "Menu")
            {
                ToastBehaviours.Notice("Already in the Main Menu", "KSH.ExitToSaveSelect.InMenu");
                return;
            }

            int currentSaveIndex = MetaSaveFile.Current.SaveIndex;

            SaveFile.DeleteSave(currentSaveIndex);

            customMainMenuPanelOnLoad.StageValue(MainMenuPanelType.FileSelect);

            KatieUtil.ChangeScene("Menu", transition: KatieConfig.Settings.baseTransition, stopAudio: true, stopCutscenes: true);

            ToastController.TryQueueAndLogToast($"Save {currentSaveIndex} erased", "KSH.ExitToSaveSelect.SaveErased");
        }

        public static void warpToNextScene()
        {
            if (ActiveSceneName == "Menu")
            {
                ToastBehaviours.Notice("Cannot warp in Main Menu", "KSH.Warp.InMenu");
                return;
            }

            string targetScene = KatieSceneWarp.WarpNextScene();
            var sceneInfo = KatieSceneWarp.RelativeSceneInfo;
            if (targetScene != null)
                ToastBehaviours.Toggle($"Warping to next Scene '{sceneInfo.name}'", "KSH.Warp");
            else
                ToastBehaviours.Toggle($"No next Scene found", "KSH.Warp");
        }

        public static void warpToPrevScene()
        {
            if (ActiveSceneName == "Menu")
            {
                ToastBehaviours.Notice("Cannot warp in Main Menu", "KSH.Warp.InMenu");
                return;
            }

            string targetScene = KatieSceneWarp.WarpPrevScene();
            var sceneInfo = KatieSceneWarp.RelativeSceneInfo;
            if (targetScene != null)
                ToastBehaviours.Toggle($"Warping to previous Scene '{sceneInfo.name}'", "KSH.Warp");
            else
                ToastBehaviours.Toggle($"No previous Scene found", "KSH.Warp");
        }

        public static void warpToNextEntrance()
        {
            if (ActiveSceneName == "Menu")
            {
                ToastBehaviours.Notice("Cannot warp in Main Menu", "KSH.Warp.InMenu");
                return;
            }

            var warpTuple = KatieSceneWarp.WarpNextEntrance();
            var sceneInfo = KatieSceneWarp.RelativeSceneInfo;


            switch (warpTuple.returnCode)
            {
                case 0:
                    ToastBehaviours.Toggle($"Warped to next Entrance '{sceneInfo.entranceFlag}' in Scene '{sceneInfo.name}'", "KSH.Warp");
                    break;
                case 1:
                    ToastBehaviours.Toggle($"Entrances for scene '{sceneInfo.name}' are undiscovered", "KSH.Warp");
                    break;
                case 2:
                    ToastBehaviours.Toggle($"No next Entrance found", "KSH.Warp");
                    break;
            }
        }

        public static void warpToPrevEntrance()
        {
            if (ActiveSceneName == "Menu")
            {
                ToastBehaviours.Notice("Cannot warp in Main Menu", "KSH.Warp.InMenu");
                return;
            }

            var warpTuple = KatieSceneWarp.WarpPrevEntrance();
            var sceneInfo = KatieSceneWarp.RelativeSceneInfo;

            switch (warpTuple.returnCode)
            {
                case 0:
                    ToastBehaviours.Toggle($"Warped to previous Entrance '{sceneInfo.entranceFlag}' in Scene '{sceneInfo.name}'", "KSH.Warp");
                    break;
                case 1:
                    ToastBehaviours.Toggle($"Entrances for scene '{sceneInfo.name}' are undiscovered", "KSH.Warp");
                    break;
                case 2:
                    ToastBehaviours.Toggle($"No previous Entrance found", "KSH.Warp");
                    break;
            }
        }

        public static IEnumerator regenerateSessionSeed(StaticCoroutine scWrapper)
        {
            if (KatieConfig.Settings.regenerateSessionSeed.Value != CustomEventType.OnHotkey)
            {
                ToastBehaviours.Notice("Hotkey for regenerating Session seed is disabled", "KSH.RegenerateSessionSeed.Disabled");
                yield break;
            }

            if (ActiveSceneName != "Menu")
            {
                ToastBehaviours.Notice("Cannot regenerate Session seed outside Main Menu", "KSH.RegenerateSessionSeed.NotInMenu");
                yield break;
            }

            if (StaticCoroutine.AnyActive(sc => sc.Identifier == MainMenuPanelGroup_Patch.menuEventTriggerRoutineIdentifier || sc.Identifier == SaveRandomizerHashes_Patch.seedInjectRoutineIdentifier))
            {
                ToastBehaviours.Notice("Please wait, Non-Save seeds still generating", "KSH.SeedGenBusy.NonSave");
                yield break;
            }

            yield return StaticCoroutine.WaitForCancelDupes(scWrapper, scWrapper.CancelToken);
            if (scWrapper.CancelToken.IsCancellationRequested) yield break;

            var introHandle = ToastBehaviours.BeginActivity("Regenerating Session seed", "KSH.RegenerateSessionSeed.Start");

            var seedTask = KatieUtil.GenerateSessionSeed(scWrapper.CancelToken);
            yield return KatieUtil.WaitForTask(seedTask);
            if (scWrapper.CancelToken.IsCancellationRequested) yield break;

            if (seedTask.Result.returnCode != SeedGeneratorReturnCode.Success)
            {
                ToastController.TryQueueToast("Could not regenerate Session seed, no seed found", "KSH.RegenerateSessionSeed.SeedNotFound");
            }

            int newSeed = seedTask.Result.seed;

            KatieUtil.EditSessionHash(newSeed);

            ToastBehaviours.EndActivity(introHandle, $"Session seed changed to {newSeed}", "KSH.RegenerateSessionSeed.Finish");
        }

        public static IEnumerator regenerateHardwareSeed(StaticCoroutine scWrapper)
        {
            if (KatieConfig.Settings.regenerateHardwareSeed.Value != CustomEventType.OnHotkey)
            {
                ToastBehaviours.Notice("Hotkey for regenerating Hardware seed is disabled", "KSH.RegenerateHardwareSeed.Disabled");
                yield break;
            }

            if (ActiveSceneName != "Menu")
            {
                ToastBehaviours.Notice("Cannot regenerate Hardware seed outside Main Menu", "KSH.RegenerateHardwareSeed.NotInMenu");
                yield break;
            }

            if (StaticCoroutine.AnyActive(sc => sc.Identifier == MainMenuPanelGroup_Patch.menuEventTriggerRoutineIdentifier || sc.Identifier == SaveRandomizerHashes_Patch.seedInjectRoutineIdentifier))
            {
                ToastBehaviours.Notice("Please wait, Non-Save seeds still generating", "KSH.SeedGenBusy.NonSave");
                yield break;
            }

            yield return StaticCoroutine.WaitForCancelDupes(scWrapper, scWrapper.CancelToken);
            if (scWrapper.CancelToken.IsCancellationRequested) yield break;

            var introHandle = ToastBehaviours.BeginActivity("Regenerating Hardware seed", "KSH.RegenerateHardwareSeed.Start");

            var seedTask = KatieUtil.GenerateHardwareSeed(scWrapper.CancelToken);
            yield return KatieUtil.WaitForTask(seedTask);

            if (scWrapper.CancelToken.IsCancellationRequested) yield break;
            if (seedTask.Result.returnCode != SeedGeneratorReturnCode.Success)
            {
                ToastController.TryQueueToast("Could not regenerate Hardware seed, no seed found");
            }

            int newSeed = seedTask.Result.seed;

            KatieUtil.EditHardwareHash(newSeed);

            ToastBehaviours.EndActivity(introHandle, $"Hardware seed changed to {newSeed}", "KSH.RegenerateHardwareSeed.Finish");
        }

        public static void resetGameBlinkRandomizer()
        {
            if (KatieConfig.Settings.resetBlinkRandomizer.Value != CustomEventType.OnHotkey)
            {
                ToastBehaviours.Notice("Hotkey for resetting Blink Randomizer is disabled", "KSH.ResetGameBlinkRandomizer.Disabled");
                return;
            }

            if (ActiveSceneName != "Menu")
            {
                ToastBehaviours.Notice("Cannot reset Blink Randomizer outside Main Menu", "KSH.ResetGameBlinkRandomizer.NotInMenu");
                return;
            }

            PlayerRandomBlink_Patch.ResetBlinkChanceGenerator();

            ToastBehaviours.QuickAction("Blink Randomizer reset", "KSH.ResetGameBlinkRandomizer.Success");
        }

        public static IEnumerator reloadSaveWithFileSeed(StaticCoroutine scWrapper)
        {
            if (ActiveSceneName == "Menu")
            {
                ToastBehaviours.Notice("Cannot reload saves in Main Menu", "KSH.SaveReloaderAction.Reload.InMenu");
                yield break;
            }

            yield return StaticCoroutine.WaitForCancelAll(sc => sc.Identifier.StartsWith("KSH.Action.SaveReloader") && sc != scWrapper);
            if (scWrapper.CancelToken.IsCancellationRequested) yield break;

            var introHandle = ToastBehaviours.BeginActivity("Reloading save with File seed", "KSH.SaveReloaderAction.Reload.WithFile.Start");

            var continueTask = ContinueSave_Patch.ContinueSaveAsync(
                token: scWrapper.CancelToken,
                transition: KatieConfig.Settings.baseTransition,
                origin: KatieSceneChanger.Origin.Manual
                );

            yield return KatieUtil.WaitForTask(continueTask);
            if (scWrapper.CancelToken.IsCancellationRequested) yield break;

            string activityEndMessage;

            if (continueTask.Result.returnCode > SeedGeneratorReturnCode.TaskCancelled)
            {
                activityEndMessage = "Could not reload, no seed found";
            }
            else
            {
                activityEndMessage = $"Loaded Save {continueTask.Result.index} with seed {continueTask.Result.saveHash}";
            }

            ToastBehaviours.EndActivity(introHandle, $"Loaded Save {continueTask.Result.index} with seed {continueTask.Result.saveHash}", "KSH.SaveReloaderAction.Reload.WithFile.Finish", 
                h => h.instance.groupName.StartsWith("KSH.SaveReloaderAction") && (h.instance.groupName.EndsWith("Start") || h.instance.groupName.EndsWith("Finish")));
        }

        public static IEnumerator reloadSaveWithCurrentSeed(StaticCoroutine scWrapper)
        {
            if (ActiveSceneName == "Menu")
            {
                ToastBehaviours.Notice("Cannot reload saves in Main Menu", "KSH.SaveReloaderAction.Reload.InMenu");
                yield break;
            }

            yield return StaticCoroutine.WaitForCancelAll(sc => sc.Identifier.StartsWith("KSH.Action.SaveReloader") && sc != scWrapper);
            if (scWrapper.CancelToken.IsCancellationRequested) yield break;

            var introHandle = ToastBehaviours.BeginActivity("Reloading save with Current seed", "KSH.SaveReloaderAction.Reload.WithCurrent.Start");

            var continueTask = ContinueSave_Patch.ContinueSaveAsync(
                token: scWrapper.CancelToken,
                seed: SaveFile.CurrentSave.SaveHash,
                transition: KatieConfig.Settings.baseTransition,
                origin: KatieSceneChanger.Origin.Manual
                );

            yield return KatieUtil.WaitForTask(continueTask);
            if (scWrapper.CancelToken.IsCancellationRequested) yield break;

            string activityEndMessage;

            if (continueTask.Result.returnCode > SeedGeneratorReturnCode.TaskCancelled)
            {
                activityEndMessage = "Could not reload, no seed found";
            }
            else
            {
                activityEndMessage = $"Loaded Save {continueTask.Result.index} with seed {continueTask.Result.saveHash}";
            }

            ToastBehaviours.EndActivity(introHandle, activityEndMessage, "KSH.SaveReloaderAction.Reload.WithCurrent.Finish",
                h => h.instance.groupName.StartsWith("KSH.SaveReloaderAction") && (h.instance.groupName.EndsWith("Start") || h.instance.groupName.EndsWith("Finish")));
        }

        public static IEnumerator reloadSaveWithNewSeed(StaticCoroutine scWrapper)
        {
            if (ActiveSceneName == "Menu")
            {
                ToastBehaviours.Notice("Cannot reload saves in Main Menu", "KSH.SaveReloaderAction.Reload.InMenu");
                yield break;
            }

            yield return StaticCoroutine.WaitForCancelAll(sc => sc.Identifier.StartsWith("KSH.Action.SaveReloader") && sc != scWrapper);
            if (scWrapper.CancelToken.IsCancellationRequested) yield break;

            var introHandle = ToastBehaviours.BeginActivity("Reloading save with New seed", "KSH.SaveReloaderAction.Reload.WithNew.Start");

            var continueTask = ContinueSave_Patch.ContinueSaveAsync(
                token: scWrapper.CancelToken,
                seedGenerator: KatieUtil.GenerateSaveSeed,
                transition: KatieConfig.Settings.baseTransition,
                origin: KatieSceneChanger.Origin.Manual
                );

            yield return KatieUtil.WaitForTask(continueTask);
            if (scWrapper.CancelToken.IsCancellationRequested) yield break;

            string activityEndMessage;

            if (continueTask.Result.returnCode > SeedGeneratorReturnCode.TaskCancelled)
            {
                activityEndMessage = "Could not reload, no seed found";
            }
            else
            {
                activityEndMessage = $"Loaded Save {continueTask.Result.index} with seed {continueTask.Result.saveHash}";
            }

            ToastBehaviours.EndActivity(introHandle, activityEndMessage, "KSH.SaveReloaderAction.Reload.WithNew.Finish",
                h => h.instance.groupName.StartsWith("KSH.SaveReloaderAction") && (h.instance.groupName.EndsWith("Start") || h.instance.groupName.EndsWith("Finish")));
        }

        public static IEnumerator resetSaveWithFileSeed(StaticCoroutine scWrapper)
        {
            if (ActiveSceneName == "Menu")
            {
                ToastBehaviours.Notice("Cannot reset saves in Main Menu", "KSH.SaveReloaderAction.Reset.InMenu");
                yield break;
            }

            yield return StaticCoroutine.WaitForCancelAll(sc => sc.Identifier.StartsWith("KSH.Action.SaveReloader") && sc != scWrapper);
            if (scWrapper.CancelToken.IsCancellationRequested) yield break;

            var introHandle = ToastBehaviours.BeginActivity("Resetting save with File seed", "KSH.SaveReloaderAction.Reset.WithFile.Start");

            GameFile<SaveFileData> gameFile = KatieUtil.ReadGameFile(MetaSaveFile.Current.SaveIndex);

            var continueTask = ContinueSave_Patch.ContinueSaveAsync(
                token: scWrapper.CancelToken,
                seed: gameFile.Data.SaveHash,
                triggerReset: true,
                transition: KatieConfig.Settings.baseTransition,
                origin: KatieSceneChanger.Origin.Manual
                );

            yield return KatieUtil.WaitForTask(continueTask);
            if (scWrapper.CancelToken.IsCancellationRequested) yield break;

            string activityEndMessage;

            if (continueTask.Result.returnCode > SeedGeneratorReturnCode.TaskCancelled)
            {
                activityEndMessage = "Could not reset, no seed found";
            }
            else
            {
                activityEndMessage = $"Reset Save {continueTask.Result.index} with seed {continueTask.Result.saveHash}";
            }

            ToastBehaviours.EndActivity(introHandle, activityEndMessage, "KSH.SaveReloaderAction.Reset.WithFile.Finish",
                h => h.instance.groupName.StartsWith("KSH.SaveReloaderAction") && (h.instance.groupName.EndsWith("Start") || h.instance.groupName.EndsWith("Finish")));
        }
        public static IEnumerator resetSaveWithCurrentSeed(StaticCoroutine scWrapper)
        {
            if (ActiveSceneName == "Menu")
            {
                ToastBehaviours.Notice("Cannot reset saves in Main Menu", "KSH.SaveReloaderAction.Reset.InMenu");
                yield break;
            }

            yield return StaticCoroutine.WaitForCancelAll(sc => sc.Identifier.StartsWith("KSH.Action.SaveReloader") && sc != scWrapper);
            if (scWrapper.CancelToken.IsCancellationRequested) yield break;

            var introHandle = ToastBehaviours.BeginActivity("Resetting save with Current seed", "KSH.SaveReloaderAction.Reset.WithCurrent.Start");

            var continueTask = ContinueSave_Patch.ContinueSaveAsync(
                token: scWrapper.CancelToken,
                seed: SaveFile.CurrentSave.SaveHash,
                triggerReset: true,
                transition: KatieConfig.Settings.baseTransition,
                origin: KatieSceneChanger.Origin.Manual
                );

            yield return KatieUtil.WaitForTask(continueTask);
            if (scWrapper.CancelToken.IsCancellationRequested) yield break;


            string activityEndMessage;

            if (continueTask.Result.returnCode > SeedGeneratorReturnCode.TaskCancelled)
            {
                activityEndMessage = "Could not reset, no seed found";
            }
            else
            {
                activityEndMessage = $"Reset Save {continueTask.Result.index} with seed {continueTask.Result.saveHash}";
            }


            ToastBehaviours.EndActivity(introHandle, activityEndMessage, "KSH.SaveReloaderAction.Reset.WithCurrent.Finish",
                h => h.instance.groupName.StartsWith("KSH.SaveReloaderAction") && (h.instance.groupName.EndsWith("Start") || h.instance.groupName.EndsWith("Finish")));
        }

        public static IEnumerator resetSaveWithNewSeed(StaticCoroutine scWrapper)
        {
            if (ActiveSceneName == "Menu")
            {
                ToastBehaviours.Notice("Cannot reset saves in Main Menu", "KSH.SaveReloaderAction.Reset.InMenu");
                yield break;
            }

            yield return StaticCoroutine.WaitForCancelAll(sc => sc.Identifier.StartsWith("KSH.Action.SaveReloader") && sc != scWrapper);
            if (scWrapper.CancelToken.IsCancellationRequested) yield break;

            var introHandle = ToastBehaviours.BeginActivity("Resetting save with New seed", "KSH.SaveReloaderAction.Reset.WithNew.Start");

            var continueTask = ContinueSave_Patch.ContinueSaveAsync(
                token: scWrapper.CancelToken,
                triggerReset: true,
                transition: KatieConfig.Settings.baseTransition,
                origin: KatieSceneChanger.Origin.Manual
                );

            yield return KatieUtil.WaitForTask(continueTask);
            if (scWrapper.CancelToken.IsCancellationRequested) yield break;

            string activityEndMessage;

            if (continueTask.Result.returnCode > SeedGeneratorReturnCode.TaskCancelled)
            {
                activityEndMessage = "Could not reset, no seed found";
            }
            else
            {
                activityEndMessage = $"Reset Save {continueTask.Result.index} with seed {continueTask.Result.saveHash}";
            }

            ToastBehaviours.EndActivity(introHandle, activityEndMessage, "KSH.SaveReloaderAction.Reset.WithNew.Finish",
                h => h.instance.groupName.StartsWith("KSH.SaveReloaderAction") && (h.instance.groupName.EndsWith("Start") || h.instance.groupName.EndsWith("Finish")));
        }

    }

}
