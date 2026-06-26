using HarmonyLib;
using JoelG.ENA4;
using LMirman.VespaIO;
using System.Threading.Tasks;
using System.Threading;
using System;
using JoelG.ENA4.Audio;
using KatieSaveHelper.Features.Util;

namespace KatieSaveHelper.Patches
{
    // If configured by the mod, disable save locking after game completion

    [HarmonyPatch(typeof(SaveFile), nameof(SaveFile.ContinueSave))]
    public static class ContinueSave_Patch
    {
        public static bool Prefix()
        {
            SaveFile.LoadSave(MetaSaveFile.Current.SaveIndex);
            if (SaveFile.CurrentSave == null)
            {
                DevConsole.Log("Unable to continue save file, no file at save index.", LMirman.VespaIO.Console.LogStyling.Error);
                return false;
            }
            if (SaveFile.CurrentSave.GameState.HasCompletedGame && !KatieConfig.Settings.disableSaveFileLockAfterCompletion.Value)
            {
                DevConsole.Log("Unable to continue save file, the save file has completed all content so far.", LMirman.VespaIO.Console.LogStyling.Error);
                return false;
            }
            SaveFile.CurrentSave.ContinueGame(true, 1f, 1f);
            return false;
        }

        public static async Task<(bool success, SeedGeneratorReturnCode returnCode, int index, int saveHash)> ContinueSaveAsync(CancellationToken token = default, bool triggerReset = false, bool stopEvents = true, Transition? transition = null, KatieSceneChanger.Origin origin = KatieSceneChanger.Origin.Natural)
        {
            int currentSaveIndex = MetaSaveFile.Current.SaveIndex;
            var loadSaveTuple = await LoadSave_Patch.LoadSaveAsync(currentSaveIndex, token: token, triggerReset: triggerReset);
            if (token.IsCancellationRequested || loadSaveTuple.returnCode != SeedGeneratorReturnCode.Success) return (false, loadSaveTuple.returnCode, currentSaveIndex, 0);

            if (SaveFile.CurrentSave == null)
            {
                DevConsole.Log("Unable to continue save file, no file at save index.", LMirman.VespaIO.Console.LogStyling.Error);
                return (false, loadSaveTuple.returnCode, currentSaveIndex, 0);
            }

            if (SaveFile.CurrentSave.GameState.HasCompletedGame && !KatieConfig.Settings.disableSaveFileLockAfterCompletion.Value)
            {
                DevConsole.Log("Unable to continue save file, the save file has completed all content so far.", LMirman.VespaIO.Console.LogStyling.Error);
                return (false, loadSaveTuple.returnCode, currentSaveIndex, 0);
            }

            if (stopEvents)
            {
                KatieUtil.StopAllCutscenes();
                AudioPlayback.StopAllAudio();
            }

            SaveFile.CurrentSave.ContinueGame(true, transition, origin);
            return (true, loadSaveTuple.returnCode, currentSaveIndex, loadSaveTuple.saveHash);
        }

        public static async Task<(bool success, SeedGeneratorReturnCode returnCode, int index, int saveHash)> ContinueSaveAsync(int seed, CancellationToken token = default, bool triggerReset = false, bool stopEvents = true, Transition? transition = null, KatieSceneChanger.Origin origin = KatieSceneChanger.Origin.Natural)
        {
            int currentSaveIndex = MetaSaveFile.Current.SaveIndex;
            var loadSaveTuple = await LoadSave_Patch.LoadSaveAsync(currentSaveIndex, seed: seed, token: token, triggerReset: triggerReset);
            if (token.IsCancellationRequested || loadSaveTuple.returnCode != SeedGeneratorReturnCode.Success) return (false, loadSaveTuple.returnCode, currentSaveIndex, 0);

            if (SaveFile.CurrentSave == null)
            {
                DevConsole.Log("Unable to continue save file, no file at save index.", LMirman.VespaIO.Console.LogStyling.Error);
                return (false, loadSaveTuple.returnCode, currentSaveIndex, 0);
            }

            if (SaveFile.CurrentSave.GameState.HasCompletedGame && !KatieConfig.Settings.disableSaveFileLockAfterCompletion.Value)
            {
                DevConsole.Log("Unable to continue save file, the save file has completed all content so far.", LMirman.VespaIO.Console.LogStyling.Error);
                return (false, loadSaveTuple.returnCode, currentSaveIndex, 0);
            }

            if (stopEvents)
            {
                KatieUtil.StopAllCutscenes();
                AudioPlayback.StopAllAudio();
            }

            SaveFile.CurrentSave.ContinueGame(true, transition, origin);
            return (true, loadSaveTuple.returnCode, currentSaveIndex, loadSaveTuple.saveHash);
        }

        public static async Task<(bool success, SeedGeneratorReturnCode returnCode, int index, int saveHash)> ContinueSaveAsync(Func<CancellationToken, Task<(SeedGeneratorReturnCode returnCode, int seed)>> seedGenerator, CancellationToken token = default, bool triggerReset = false, bool stopEvents = true, Transition? transition = null, KatieSceneChanger.Origin origin = KatieSceneChanger.Origin.Natural)
        {
            int currentSaveIndex = MetaSaveFile.Current.SaveIndex;
            var loadSaveTuple = await LoadSave_Patch.LoadSaveAsync(currentSaveIndex, seedGenerator: seedGenerator, token: token, triggerReset: triggerReset);
            if (token.IsCancellationRequested || loadSaveTuple.returnCode != SeedGeneratorReturnCode.Success) return (false, loadSaveTuple.returnCode, currentSaveIndex, 0);

            if (SaveFile.CurrentSave == null)
            {
                DevConsole.Log("Unable to continue save file, no file at save index.", LMirman.VespaIO.Console.LogStyling.Error);
                return (false, loadSaveTuple.returnCode, currentSaveIndex, 0);
            }

            if (SaveFile.CurrentSave.GameState.HasCompletedGame && !KatieConfig.Settings.disableSaveFileLockAfterCompletion.Value)
            {
                DevConsole.Log("Unable to continue save file, the save file has completed all content so far.", LMirman.VespaIO.Console.LogStyling.Error);
                return (false, loadSaveTuple.returnCode, currentSaveIndex, 0);
            }

            if (stopEvents)
            {
                KatieUtil.StopAllCutscenes();
                AudioPlayback.StopAllAudio();
            }

            SaveFile.CurrentSave.ContinueGame(true, transition, origin);
            return (true, loadSaveTuple.returnCode, currentSaveIndex, loadSaveTuple.saveHash);
        }
    }
}
