using HarmonyLib;
using JoelG.ENA4;
using System.Reflection;

namespace KatieSaveHelper.Patches
{

    // Undo changes to the current save's hash during resets performed by the mod

    [HarmonyPatch(typeof(SaveFileData), nameof(SaveFileData.SoftReset))]
    public class SoftResetPatch
    {
        private static readonly FieldInfo saveHashField = typeof(SaveFileData).GetField("saveHash", BindingFlags.NonPublic | BindingFlags.Instance);
        static void Postfix(SaveFileData __instance)
        {
            if (KatieSaveHelperMod.forceCustomSeedOnReset)
            {
                saveHashField.SetValue(__instance, KatieSaveHelperMod.customSeed);
            }
        }
    }

}