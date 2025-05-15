using HarmonyLib;
using JoelG.ENA4;
using System.Reflection;

namespace KatieSaveHelper.Patches
{
    [HarmonyPatch(typeof(SaveFileData), nameof(SaveFileData.SoftReset))]
    public class SoftResetPatch
    {
        static void Postfix(SaveFileData __instance)
        {
            if (KatieSaveHelperMod.forceCustomSeed)
            {
                var field = typeof(SaveFileData).GetField("saveHash", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(__instance, KatieSaveHelperMod.customSeed);
                }
            }
        }
    }
}