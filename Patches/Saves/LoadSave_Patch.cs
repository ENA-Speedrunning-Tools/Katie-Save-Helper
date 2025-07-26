using JoelG.ENA4;
using HarmonyLib;
using System;
using System.Reflection;

namespace KatieSaveHelper.Patches
{

    // After a save is loaded, replace the loaded save seed with a custom one, if performed by the mod

    [HarmonyPatch(typeof(SaveFile), nameof(SaveFile.LoadSave))]
    public class LoadSave_Patch
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
        static bool Prefix(int index)
        {
            try
            {
                if (MainMenuPanelGroup_Patch.menuLoadedOnce)
                    KatieUtil.TriggerCustomEvent(CustomEventType.OnLoadSave);

                int clampedIndex = (int)clampIndexMethod.Invoke(null, new object[] { index });

                RemoteGameFile<SaveFileData> gameFile = (RemoteGameFile<SaveFileData>)getGameFileMethod.Invoke(null, new object[] { clampedIndex });

                bool readSuccess = (bool)readFileMethod.Invoke(gameFile, null);

                if (readSuccess && dataProp != null)
                {
                    var dataValue = dataProp.GetValue(gameFile);
                    if (dataValue != null)
                    {
                        if (KatieSaveHelperModActions.customSeedOnLoad.IsReady)
                        {
                            int newSeed = KatieSaveHelperModActions.customSeedOnLoad.TakeValue();
                            saveHashField.SetValue(dataValue, newSeed);
                            ToastController.TryQueueAndLogToast($"Loaded Save {index} with seed {newSeed}");
                        }

                        validateMethod.Invoke(dataValue, null);
                    }
                }
                else
                {
                    resetSaveMethod.Invoke(null, new object[] { clampedIndex });
                }

                currentField.SetValue(null, gameFile);

                setSaveIndexMethod.Invoke(MetaSaveFile.Current, new object[] { clampedIndex });

                var eventDelegate = eventField.GetValue(null) as Action;
                if (eventDelegate != null)
                {
                    eventDelegate.Invoke();
                }
                else
                {
                    KatieLogger.Error("CurrentSaveLoaded event not invoked");
                }

                return false;
            }
            catch (Exception e)
            {
                KatieLogger.Error(e.ToString());
                return true;
            }
        }
    }
}
