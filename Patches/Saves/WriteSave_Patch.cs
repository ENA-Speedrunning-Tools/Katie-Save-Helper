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
        public static bool Prefix()
        {
            if (ModActions.autoSaveDisabled && !ModActions.allowNextSaveAttempt)
                return false;

            ModActions.allowNextSaveAttempt = false;

            RemoteGameFile<SaveFileData> current = (RemoteGameFile<SaveFileData>)currentField.GetValue(null);

            current.Data.Metadata.SetSavedTimeToNow();

            if (KatieConfig.Settings.disableSaveFileEncryption.Value)
            {
                GameFile_Patch.WriteFileAsJsonDat(current);
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