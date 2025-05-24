using JoelG.ENA4;
using MelonLoader;
using HarmonyLib;
using System;
using System.Reflection;

namespace KatieSaveHelper.Patches
{

    // After a save is loaded, replace the loaded save seed with a custom one, if performed by the mod

    [HarmonyPatch(typeof(SaveFile), nameof(SaveFile.LoadSave))]
    public class LoadSavePatch
    {
        private static readonly FieldInfo currentField = typeof(SaveFile).GetField("current", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly FieldInfo eventField = typeof(SaveFile).GetField("CurrentFileLoaded", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly FieldInfo saveHashField = typeof(SaveFileData).GetField("saveHash", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly PropertyInfo dataProp = typeof(RemoteGameFile<SaveFileData>).GetProperty("Data", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly MethodInfo validateMethod = typeof(SaveFileData).GetMethod("Validate", BindingFlags.Public | BindingFlags.Instance);
        private static readonly MethodInfo readFileMethod = typeof(RemoteGameFile<SaveFileData>).GetMethod("ReadFile", BindingFlags.Instance | BindingFlags.Public);
        private static readonly MethodInfo clampIndexMethod = AccessTools.Method(typeof(SaveFile), "ClampIndex");
        private static readonly MethodInfo getGameFileMethod = AccessTools.Method(typeof(SaveFile), "GetGameFile");
        private static readonly MethodInfo resetSaveMethod = AccessTools.Method(typeof(SaveFile), "ResetSave");
        private static readonly MethodInfo setSaveIndexMethod = AccessTools.Method(typeof(MetaSaveFileData), "SetSaveIndex");
        static bool Prefix(int index)
        {
            try
            {

                int clampedIndex = (int)clampIndexMethod.Invoke(null, new object[] { index });

                RemoteGameFile<SaveFileData> gameFile = (RemoteGameFile<SaveFileData>)getGameFileMethod.Invoke(null, new object[] { clampedIndex });

                bool readSuccess = (bool)readFileMethod.Invoke(gameFile, null);

                if (readSuccess && dataProp != null)
                {
                    var dataValue = dataProp.GetValue(gameFile);
                    if (dataValue != null)
                    {
                        if (KatieSaveHelperMod.forceCustomSeedOnLoad)
                        {
                            saveHashField.SetValue(dataValue, KatieSaveHelperMod.customSeed);
                            MelonLogger.Msg($"Loaded Save {index} with seed {KatieSaveHelperMod.customSeed}");
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
                    MelonLogger.Msg("CurrentSaveLoaded event not invoked");
                }

                return false;
            }
            catch (Exception e)
            {
                MelonLogger.Error(e);
                return true;
            }
        }
    }
}
