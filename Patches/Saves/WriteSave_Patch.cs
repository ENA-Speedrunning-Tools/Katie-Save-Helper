using HarmonyLib;
using JoelG.ENA4;
using System;
using System.Reflection;
using LMirman.Utilities;

namespace KatieSaveHelper.Patches
{

    // Disable save writes not performed by the mod if the 'autosave disabled' option is turned on

    [HarmonyPatch(typeof(SaveFile), nameof(SaveFile.WriteSave))]
    public static class WriteSave_Patch
    {
        private static readonly FieldInfo currentField = AccessTools.Field(typeof(SaveFile), "current");
        private static readonly FieldInfo currentFileSavedField = AccessTools.Field(typeof(SaveFile), "CurrentFileSaved");
        static bool Prefix()
        {
            if (KatieSaveHelperModActions.autoSaveDisabled && !KatieSaveHelperModActions.allowNextSaveAttempt)
                return false;

            KatieSaveHelperModActions.allowNextSaveAttempt = false;

            RemoteGameFile<SaveFileData> current = (RemoteGameFile<SaveFileData>)currentField.GetValue(null);

            current.Data.Metadata.SetSavedTimeToNow();
            if (KatieSaveHelperModConfig.disableSaveFileEncryption.Value)
            {
                current.WriteFileAsJsonDat();
            }
            else
            {
                current.WriteFile(GameFile<SaveFileData>.FileType.Encrypted);
            }

            var currentFileSavedDelegate = currentFileSavedField.GetValue(null) as Action;
            currentFileSavedDelegate?.Invoke();

            return false;
        }
    }

}