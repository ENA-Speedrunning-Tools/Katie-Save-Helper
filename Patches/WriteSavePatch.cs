using HarmonyLib;
using JoelG.ENA4;

namespace KatieSaveHelper.Patches
{

    // Disable save writes not performed by the mod if the 'autosave disabled' option is turned on

    [HarmonyPatch(typeof(SaveFile), nameof(SaveFile.WriteSave))]
    public class WriteSavePatch
    {
        static bool Prefix()
        {
            if (KatieSaveHelperModConfig.autoSaveDisabled.Value && !KatieSaveHelperMod.allowSave)
            {
                return false;
            }
            return true;
        }
    }

}