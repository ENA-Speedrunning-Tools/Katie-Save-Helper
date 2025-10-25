using HarmonyLib;
using JoelG.ENA4;
using LMirman.VespaIO;

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
                DevConsole.Log("Unable to continue save file, no file at save index.", Console.LogStyling.Error);
                return false;
            }
            if (SaveFile.CurrentSave.GameState.HasCompletedGame && !KatieSaveHelperModConfig.disableSaveFileLockAfterCompletion.Value)
            {
                DevConsole.Log("Unable to continue save file, the save file has completed all content so far.", Console.LogStyling.Error);
                return false;
            }
            SaveFile.CurrentSave.ContinueGame(true, 1f, 1f);
            return false;
        }
    }
}
