using HarmonyLib;
using JoelG.ENA4.UI;
using JoelG.ENA4;
using System.Reflection;
using KatieSaveHelper.Features.Util;
using KatieSaveHelper.Features.API;

namespace KatieSaveHelper.Patches
{

    // If configured in the mod config, disable the 'Create New Save' and 'Reset Save' confirmation popups in the Main Menu, and allow completed saves to be selected

    [HarmonyPatch(typeof(MenuSaveSlot), nameof(MenuSaveSlot.SelectSave))]
    public static class MenuSaveSlot_Patch
    {
        private static readonly FieldInfo saveSlotUIField = AccessTools.Field(typeof(MenuSaveSlot), "saveSlotUI");
        private static readonly FieldInfo indexField = AccessTools.Field(typeof(MenuSaveSlot), "index");
        private static readonly MethodInfo requestResetSaveMethod = AccessTools.Method(typeof(MenuSaveSlot), "RequestResetSave");
        private static readonly MethodInfo resetSaveMethod = AccessTools.Method(typeof(MenuSaveSlot), "ResetSave");
        private static readonly MethodInfo executeSaveSelectionMethod = AccessTools.Method(typeof(MenuSaveSlot), "ExecuteSaveSelection");
        private static readonly MethodInfo requestCreateSaveMethod = AccessTools.Method(typeof(MenuSaveSlot), "RequestCreateSave");
        private static readonly MethodInfo notifyFinishedSaveMethod = AccessTools.Method(typeof(MenuSaveSlot), "NotifyFinishedSave");

        public static bool Prefix(MenuSaveSlot __instance)
        {
            MenuSaveSlotUI saveSlotUI = (MenuSaveSlotUI)saveSlotUIField.GetValue(__instance);
            int index = (int)indexField.GetValue(__instance);

            if (saveSlotUI.IsResetMode)
            {
                if (KatieConfig.Settings.disableResetSavePopup.Value)
                {
                    resetSaveMethod.Invoke(__instance, null);
                }
                else
                {
                    requestResetSaveMethod.Invoke(__instance, null);
                }
                return false;
            }

            if (StaticCoroutine.AnyActive(sc => sc.Identifier == MainMenuPanelGroup_Patch.menuEventTriggerRoutineIdentifier || sc.Identifier == SaveRandomizerHashes_Patch.seedInjectRoutineIdentifier || sc.Identifier.StartsWith("KSH.Action.RegenerateSeed")))
            {
                ToastBehaviours.Notice("Please wait, Non-Save seeds still generating", "KSH.SeedGenBusy.NonSave", sendToLog: false);
                return false;
            }

            SaveFileData saveFileData = SaveFile.PeekSave(index);
            if (saveFileData == null)
            {
                if (KatieConfig.Settings.disableCreateSavePopup.Value)
                {
                    executeSaveSelectionMethod.Invoke(__instance, null);
                }
                else
                {
                    requestCreateSaveMethod.Invoke(__instance, null);
                }
                return false;
            }
            if (saveFileData.GameState.HasCompletedGame && !KatieConfig.Settings.disableSaveFileLockAfterCompletion.Value)
            {
                notifyFinishedSaveMethod.Invoke(__instance, null);
                return false;
            }
            executeSaveSelectionMethod.Invoke(__instance, null);
            return false;
        }
    }
}
