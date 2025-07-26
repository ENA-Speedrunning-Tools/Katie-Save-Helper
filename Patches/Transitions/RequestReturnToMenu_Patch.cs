using HarmonyLib;
using JoelG.ENA4.UI;
using System.Reflection;

namespace KatieSaveHelper.Patches
{
    // If configured in the mod config, disable the 'Return to Main Menu' confirmation popup in the Pause Menu

    [HarmonyPatch(typeof(PauseGameOverlay), nameof(PauseGameOverlay.RequestReturnToMenu))]
    public static class RequestReturnToMenu_Patch
    {
        private static readonly MethodInfo changeSceneMethod = AccessTools.Method(typeof(PauseGameOverlay), "ChangeScene");
        public static bool Prefix(PauseGameOverlay __instance)
        {
            if (KatieSaveHelperModConfig.disableReturnToMainMenuPopup.Value)
            {
                changeSceneMethod.Invoke(__instance, new object[] { "Menu" });
                return false;
            }
            return true;
        }
    }
}
