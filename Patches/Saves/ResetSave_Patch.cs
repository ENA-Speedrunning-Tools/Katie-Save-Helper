using JoelG.ENA4;
using System.Reflection;
using HarmonyLib;
using System.Threading;
using System.Threading.Tasks;
using System;
using LMirman.Utilities;
using KatieSaveHelper.Features.Util;

namespace KatieSaveHelper.Patches
{
    // Insert custom pseudo-random seed into new save if the 'use pseudo random seed on new saves' option is enabled

    [HarmonyPatch(typeof(SaveFile), nameof(SaveFile.ResetSave))]
    public static class ResetSave_Patch
    {
        private static readonly FieldInfo saveHashField = AccessTools.Field(typeof(SaveFileData), "saveHash");
        private static readonly MethodInfo getGameFileMethod = AccessTools.Method(typeof(SaveFile), "GetGameFile");

        private static void ResetGameFile(RemoteGameFile<SaveFileData> gameFile, int? setSeed = null)
        {
            gameFile.Data = new SaveFileData();

            if (setSeed != null)
                saveHashField.SetValue(gameFile.Data, setSeed.Value);

            gameFile.ValidData = true;

            if (KatieConfig.Settings.disableSaveFileEncryption.Value)
            {
                GameFile_Patch.WriteFileAsJsonDat(gameFile);
            }
            else
            {
                gameFile.WriteFile(GameFile<SaveFileData>.FileType.Encrypted);
            }
        }

        public static bool Prefix(int index)
        {
            RemoteGameFile<SaveFileData> gameFile = (RemoteGameFile<SaveFileData>)getGameFileMethod.Invoke(null, new object[] { index });

            ResetGameFile(gameFile);

            return false;
        }

        public static async Task<(SeedGeneratorReturnCode returnCode, int saveHash)> ResetSaveAsync(int index, CancellationToken token = default, bool triggerCustomEvent = true)
        {
            var tceCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            Task<KatieUtil.CustomEventGenerationResult> tceTask = null;
            if (triggerCustomEvent)
                tceTask = KatieUtil.GenerateCustomEventSeedsAsync(CustomEventType.OnCreateSave, tceCts.Token);

            RemoteGameFile<SaveFileData> gameFile = (RemoteGameFile<SaveFileData>)getGameFileMethod.Invoke(null, new object[] { index });

            try
            {
                var seedTuple = await KatieUtil.GenerateSaveSeed(token);
                int newSeed = seedTuple.seed;

                if (seedTuple.returnCode != SeedGeneratorReturnCode.Success && seedTuple.returnCode != SeedGeneratorReturnCode.TaskCancelled)
                    tceCts.Cancel();

                if (tceTask != null)
                    await tceTask;

                if (token.IsCancellationRequested) return (SeedGeneratorReturnCode.TaskCancelled, 0);
                if (tceCts.IsCancellationRequested) return (seedTuple.returnCode, 0);

                if (tceTask != null)
                {
                    if (tceTask.Result.HighestReturnCode == SeedGeneratorReturnCode.Success)
                        KatieUtil.ApplyCustomEventResults(tceTask.Result);
                    else
                        return (tceTask.Result.HighestReturnCode, 0);
                }

                ResetGameFile(gameFile, newSeed);

                return (SeedGeneratorReturnCode.Success, newSeed);
            }
            catch (TaskCanceledException)
            {
                return (SeedGeneratorReturnCode.TaskCancelled, 0);
            }
        }

        public static async Task<(SeedGeneratorReturnCode returnCode, int saveHash)> ResetSaveAsync(int index, int? seed = null, CancellationToken token = default, bool triggerCustomEvent = true)
        {
            var tceCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            Task<KatieUtil.CustomEventGenerationResult> tceTask = null;
            if (triggerCustomEvent)
                tceTask = KatieUtil.GenerateCustomEventSeedsAsync(CustomEventType.OnCreateSave, tceCts.Token);

            RemoteGameFile<SaveFileData> gameFile = (RemoteGameFile<SaveFileData>)getGameFileMethod.Invoke(null, new object[] { index });

            try
            {
                (SeedGeneratorReturnCode returnCode, int seed)? seedTuple = null;
                int newSeed;

                if (seed != null)
                {
                    newSeed = seed.Value;
                }
                else
                {
                    seedTuple = await KatieUtil.GenerateSaveSeed(token);
                    newSeed = seedTuple.Value.seed;
                }

                if (seedTuple != null && seedTuple.Value.returnCode != SeedGeneratorReturnCode.Success && seedTuple.Value.returnCode != SeedGeneratorReturnCode.TaskCancelled)
                    tceCts.Cancel();

                if (tceTask != null)
                    await tceTask;

                if (token.IsCancellationRequested) return (SeedGeneratorReturnCode.TaskCancelled, 0);
                if (tceCts.IsCancellationRequested) return (seedTuple.Value.returnCode, 0);

                if (tceTask != null)
                {
                    if (tceTask.Result.HighestReturnCode == SeedGeneratorReturnCode.Success)
                        KatieUtil.ApplyCustomEventResults(tceTask.Result);
                    else
                        return (tceTask.Result.HighestReturnCode, 0);
                }

                ResetGameFile(gameFile, newSeed);

                return (SeedGeneratorReturnCode.Success, newSeed);
            }
            catch (TaskCanceledException)
            {
                return (SeedGeneratorReturnCode.TaskCancelled, 0);
            }
        }

        public static async Task<(SeedGeneratorReturnCode returnCode, int saveHash)> ResetSaveAsync(int index, Func<CancellationToken, Task<(SeedGeneratorReturnCode returnCode, int seed)>> seedGenerator = null, CancellationToken token = default, bool triggerCustomEvent = true)
        {
            var tceCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            Task<KatieUtil.CustomEventGenerationResult> tceTask = null;
            if (triggerCustomEvent)
                tceTask = KatieUtil.GenerateCustomEventSeedsAsync(CustomEventType.OnCreateSave, tceCts.Token);

            RemoteGameFile<SaveFileData> gameFile = (RemoteGameFile<SaveFileData>)getGameFileMethod.Invoke(null, new object[] { index });

            try
            {
                (SeedGeneratorReturnCode returnCode, int seed)? seedTuple = null;
                int newSeed;

                if (seedGenerator != null)
                {
                    seedTuple = await seedGenerator(token);
                    newSeed = seedTuple.Value.seed;
                }
                else
                {
                    seedTuple = await KatieUtil.GenerateSaveSeed(token);
                    newSeed = seedTuple.Value.seed;
                }

                if (seedTuple != null && seedTuple.Value.returnCode != SeedGeneratorReturnCode.Success && seedTuple.Value.returnCode != SeedGeneratorReturnCode.TaskCancelled)
                    tceCts.Cancel();

                if (tceTask != null)
                    await tceTask;

                if (token.IsCancellationRequested) return (SeedGeneratorReturnCode.TaskCancelled, 0);
                if (tceCts.IsCancellationRequested) return (seedTuple.Value.returnCode, 0);

                if (tceTask != null)
                {
                    if (tceTask.Result.HighestReturnCode == SeedGeneratorReturnCode.Success)
                        KatieUtil.ApplyCustomEventResults(tceTask.Result);
                    else
                        return (tceTask.Result.HighestReturnCode, 0);
                }

                ResetGameFile(gameFile, newSeed);

                return (SeedGeneratorReturnCode.Success, newSeed);
            }
            catch (TaskCanceledException)
            {
                return (SeedGeneratorReturnCode.TaskCancelled, 0);
            }
        }

    }
}
