using HarmonyLib;
using JoelG.ENA4;
using JoelG.ENA4.UI;
using LMirman.Utilities.UI;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace KatieSaveHelper.Patches
{
    // If configured by the mod, use a custom transition coroutine when selecting a save that does not include the 2 second time delay

    [HarmonyPatch(typeof(MainMenu), nameof(MainMenu.TransitionThenContinueSave))]
    public static class MainMenu_Patch
    {
        private static readonly FieldInfo isSelectingSaveField = AccessTools.Field(typeof(MainMenu), "isSelectingSave");
        private static readonly FieldInfo panelGroupField = AccessTools.Field(typeof(MainMenu), "panelGroup");
        private static readonly MethodInfo ContinueGameMethod = AccessTools.Method(typeof(MainMenu), "ContinueGame");
        private static readonly Func<MainMenu, int, IEnumerator> originalTransitionThenPickSave = AccessTools.MethodDelegate<Func<MainMenu, int, IEnumerator>>(AccessTools.Method(typeof(MainMenu), "TransitionThenPickSave"));

        public static bool Prefix(MainMenu __instance, int index)
        {
            if (KatieSaveHelperModConfig.disableSaveSelectDelay.Value == true)
            {
                __instance.StartCoroutine(TransitionThenPickSave(__instance, index));
                return false;
            }
            return true;
        }

        private static IEnumerator TransitionThenPickSave(MainMenu __instance, int index)
        {
            var panelGroup = (MainMenuPanelGroup)panelGroupField.GetValue(__instance);

            if (panelGroup == null)
            {
                KatieLogger.Error("Could not run modified coroutine, falling back to original");
                __instance.StartCoroutine(originalTransitionThenPickSave(__instance, index));
                yield break;
            }

            try
            {
                isSelectingSaveField.SetValue(__instance, true);
                UIFunctions.AddFocus(__instance);
                panelGroup.TransitionToPanel("null");
                yield return null;
                float timer = Time.deltaTime;
                while (timer < 10f && panelGroup.TransitionTarget != null)
                {
                    yield return null;
                    timer += Time.deltaTime;
                }
                SaveFile.LoadSave(index);
                ContinueGameMethod.Invoke(__instance, null);
            }
            finally
            {
                UIFunctions.RemoveFocus(__instance);
                isSelectingSaveField.SetValue(__instance, false);
            }
            yield break;
        }
    }
}
