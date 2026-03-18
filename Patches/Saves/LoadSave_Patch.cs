using JoelG.ENA4;
using HarmonyLib;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;

namespace KatieSaveHelper.Patches
{

    // After a save is loaded, replace the loaded save seed with a custom one, if performed by the mod
    public static class LoadSave_Patch
    {
        private static readonly FieldInfo currentField = AccessTools.Field(typeof(SaveFile), "current");
        private static readonly FieldInfo eventField = AccessTools.Field(typeof(SaveFile), "CurrentFileLoaded");
        private static readonly FieldInfo saveHashField = AccessTools.Field(typeof(SaveFileData), "saveHash");
        private static readonly PropertyInfo dataProp = AccessTools.Property(typeof(RemoteGameFile<SaveFileData>), "Data");
        private static readonly MethodInfo validateMethod = AccessTools.Method(typeof(SaveFileData), "Validate");
        private static readonly MethodInfo readFileMethod = AccessTools.Method(typeof(RemoteGameFile<SaveFileData>), "ReadFile");
        private static readonly MethodInfo clampIndexMethod = AccessTools.Method(typeof(SaveFile), "ClampIndex");
        private static readonly MethodInfo getGameFileMethod = AccessTools.Method(typeof(SaveFile), "GetGameFile");
        private static readonly MethodInfo resetSaveMethod = AccessTools.Method(typeof(SaveFile), "ResetSave");
        private static readonly MethodInfo setSaveIndexMethod = AccessTools.Method(typeof(MetaSaveFileData), "SetSaveIndex");

        private static Action _cachedEventDelegate = null;
        private static Action CurrentFileLoadedEvent
        {
            get
            {
                if (_cachedEventDelegate != null)
                    return _cachedEventDelegate;

                var eventDelegate = eventField.GetValue(null) as Action;
                if (eventDelegate != null)
                {
                    _cachedEventDelegate = eventDelegate;
                    return eventDelegate;
                }
                else
                {
                    KatieLogger.Error("Could not obtain CurrentSaveLoaded event");
                    return null;
                }
            }
        }

        public static void ApplyGameData(RemoteGameFile<SaveFileData> gameFile)
        {
            currentField.SetValue(null, gameFile);

            //setSaveIndexMethod.Invoke(MetaSaveFile.Current, new object[] { clampedIndex });

            CurrentFileLoadedEvent?.Invoke();
        }

        public static async Task<(SeedGeneratorReturnCode returnCode, int saveHash)> LoadSaveAsync(int index, CancellationToken token = default, bool triggerCustomEvent = true, bool triggerReset = false)
        {
            try
            {
                Task<KatieUtil.CustomEventGenerationResult> tceTask = null;
                if (MainMenuPanelGroup_Patch.menuLoadedOnce && triggerCustomEvent)
                    tceTask = KatieUtil.GenerateCustomEventSeedsAsync(CustomEventType.OnLoadSave, token);

                int clampedIndex = (int)clampIndexMethod.Invoke(null, new object[] { index });

                RemoteGameFile<SaveFileData> gameFile = (RemoteGameFile<SaveFileData>)getGameFileMethod.Invoke(null, new object[] { clampedIndex });

                bool readSuccess = (bool)readFileMethod.Invoke(gameFile, null);

                int saveHash = 0;

                if (readSuccess && dataProp != null && !triggerReset)
                {
                    var dataValue = dataProp.GetValue(gameFile);
                    if (dataValue != null)
                    {
                        saveHash = (int)saveHashField.GetValue(dataValue);

                        validateMethod.Invoke(dataValue, null);
                    }
                }
                else
                {
                    var resetSaveTuple = await ResetSave_Patch.ResetSaveAsync(clampedIndex, token);

                    if (resetSaveTuple.returnCode == SeedGeneratorReturnCode.Success)
                        saveHash = resetSaveTuple.saveHash;
                    else
                        return (resetSaveTuple.returnCode, 0);
                }

                if (tceTask != null)
                    await tceTask;

                if (token.IsCancellationRequested) return (SeedGeneratorReturnCode.TaskCancelled, 0);

                if (tceTask != null)
                {
                    if (tceTask.Result.HighestReturnCode == SeedGeneratorReturnCode.Success)
                        KatieUtil.ApplyCustomEventResults(tceTask.Result);
                    else
                        return (tceTask.Result.HighestReturnCode, 0);
                }

                currentField.SetValue(null, gameFile);

                setSaveIndexMethod.Invoke(MetaSaveFile.Current, new object[] { clampedIndex });

                CurrentFileLoadedEvent?.Invoke();

                return (SeedGeneratorReturnCode.Success, saveHash);
            }
            catch (Exception ex)
            {
                var error = ex.InnerException ?? ex;
                KatieLogger.Error(error.ToString());
                return (SeedGeneratorReturnCode.OtherError, 0);
            }
        }

        public static async Task<(SeedGeneratorReturnCode returnCode, int saveHash)> LoadSaveAsync(int index, int seed, CancellationToken token = default, bool triggerCustomEvent = true, bool triggerReset = false)
        {
            try
            {
                Task<KatieUtil.CustomEventGenerationResult> tceTask = null;
                if (MainMenuPanelGroup_Patch.menuLoadedOnce && triggerCustomEvent)
                    tceTask = KatieUtil.GenerateCustomEventSeedsAsync(CustomEventType.OnLoadSave, token);

                int clampedIndex = (int)clampIndexMethod.Invoke(null, new object[] { index });

                RemoteGameFile<SaveFileData> gameFile = (RemoteGameFile<SaveFileData>)getGameFileMethod.Invoke(null, new object[] { clampedIndex });

                //bool readSuccess = (bool)readFileMethod.Invoke(gameFile, null);
                bool readSuccess = gameFile.ReadFile();

                int saveHash = 0;

                if (readSuccess && dataProp != null && !triggerReset)
                {
                    var dataValue = dataProp.GetValue(gameFile);
                    if (dataValue != null)
                    {
                        saveHashField.SetValue(dataValue, seed);

                        saveHash = (int)saveHashField.GetValue(dataValue);

                        validateMethod.Invoke(dataValue, null);
                    }
                }
                else
                {
                    var resetSaveTuple = await ResetSave_Patch.ResetSaveAsync(clampedIndex, seed: seed, token);

                    if (resetSaveTuple.returnCode == SeedGeneratorReturnCode.Success)
                        saveHash = resetSaveTuple.saveHash;
                    else
                        return (resetSaveTuple.returnCode, 0);
                }

                if (tceTask != null)
                    await tceTask;

                if (token.IsCancellationRequested) return (SeedGeneratorReturnCode.TaskCancelled, 0);

                if (tceTask != null)
                {
                    if (tceTask.Result.HighestReturnCode == SeedGeneratorReturnCode.Success)
                        KatieUtil.ApplyCustomEventResults(tceTask.Result);
                    else
                        return (tceTask.Result.HighestReturnCode, 0);
                }

                currentField.SetValue(null, gameFile);

                setSaveIndexMethod.Invoke(MetaSaveFile.Current, new object[] { clampedIndex });

                CurrentFileLoadedEvent?.Invoke();

                return (SeedGeneratorReturnCode.Success, saveHash);
            }
            catch (Exception e)
            {
                KatieLogger.Error(e.ToString());
                return (SeedGeneratorReturnCode.OtherError, 0);
            }
        }

        public static async Task<(SeedGeneratorReturnCode returnCode, int saveHash)> LoadSaveAsync(int index, Func<CancellationToken, Task<(SeedGeneratorReturnCode returnCode, int seed)>> seedGenerator, CancellationToken token = default, bool triggerCustomEvent = true, bool triggerReset = false)
        {
            try
            {
                Task<KatieUtil.CustomEventGenerationResult> tceTask = null;
                if (MainMenuPanelGroup_Patch.menuLoadedOnce && triggerCustomEvent)
                    tceTask = KatieUtil.GenerateCustomEventSeedsAsync(CustomEventType.OnLoadSave, token);

                int clampedIndex = (int)clampIndexMethod.Invoke(null, new object[] { index });

                RemoteGameFile<SaveFileData> gameFile = (RemoteGameFile<SaveFileData>)getGameFileMethod.Invoke(null, new object[] { clampedIndex });

                bool readSuccess = (bool)readFileMethod.Invoke(gameFile, null);

                int saveHash = 0;

                if (readSuccess && dataProp != null && !triggerReset)
                {
                    var dataValue = dataProp.GetValue(gameFile);
                    if (dataValue != null)
                    {
                        if (seedGenerator != null)
                        {
                            var seedTuple = await seedGenerator(token);
                            if (seedTuple.returnCode == SeedGeneratorReturnCode.Success)
                                saveHashField.SetValue(dataValue, seedTuple.seed);
                            else
                                return (seedTuple.returnCode, 0);
                        }

                        saveHash = (int)saveHashField.GetValue(dataValue);

                        validateMethod.Invoke(dataValue, null);
                    }
                }
                else
                {
                    var resetSaveTuple = await ResetSave_Patch.ResetSaveAsync(clampedIndex, seedGenerator: seedGenerator, token);

                    if (resetSaveTuple.returnCode == SeedGeneratorReturnCode.Success)
                        saveHash = resetSaveTuple.saveHash;
                    else
                        return (resetSaveTuple.returnCode, 0);
                }

                if (tceTask != null)
                    await tceTask;

                if (token.IsCancellationRequested) return (SeedGeneratorReturnCode.TaskCancelled, 0);

                if (tceTask != null)
                {
                    if (tceTask.Result.HighestReturnCode == SeedGeneratorReturnCode.Success)
                        KatieUtil.ApplyCustomEventResults(tceTask.Result);
                    else
                        return (tceTask.Result.HighestReturnCode, 0);
                }

                currentField.SetValue(null, gameFile);

                setSaveIndexMethod.Invoke(MetaSaveFile.Current, new object[] { clampedIndex });

                CurrentFileLoadedEvent?.Invoke();

                return (SeedGeneratorReturnCode.Success, saveHash);
            }
            catch (Exception e)
            {
                KatieLogger.Error(e.ToString());
                return (SeedGeneratorReturnCode.OtherError, 0);
            }
        }
    }
}