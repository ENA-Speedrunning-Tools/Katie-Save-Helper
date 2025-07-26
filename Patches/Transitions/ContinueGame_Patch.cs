using JoelG.ENA4;
using System.Reflection;
using System;
using UnityEngine;
using HarmonyLib;

namespace KatieSaveHelper.Patches
{

    // If performed by the mod, create a custom scene changer object for continuing the save

    [HarmonyPatch(typeof(SaveFileData), "ContinueGame")]
    public class ContinueGame_Patch
    {
        private static readonly FieldInfo gameStateField = AccessTools.Field(typeof(SaveFileData), "gameState");
        private static readonly FieldInfo sessionTraversalHistoryField = AccessTools.Field(typeof(SaveFileData), "sessionTraversalHistory");
        private static readonly FieldInfo transitionField = AccessTools.Field(typeof(SceneChanger), "transition");
        private static readonly FieldInfo milestoneProgressField = AccessTools.Field(typeof(SaveDataPlayerGameState), "milestoneProgress");

        public static bool Prefix(SaveFileData __instance, bool resetState = true, float fadeInTime = 1f, float fadeOutTime = 1f)
        {
            SaveDataPlayerGameState gameState = (SaveDataPlayerGameState)gameStateField.GetValue(__instance);
            SaveDataNodeTraversalHistory sessionTraversalHistory = (SaveDataNodeTraversalHistory)sessionTraversalHistoryField.GetValue(__instance);
            SceneChanger sceneChanger;

            if (gameState.HasCompletedGame)
            {
                if (KatieSaveHelperModConfig.disableSaveFileLockAfterCompletion.Value)
                {
                    // Reset milestones
                    milestoneProgressField.SetValue(SaveFile.CurrentSave.GameState, 0);
                    SaveFile.WriteSave();
                }
                else
                {
                    new SceneChanger("Menu", Color.black, fadeInTime, fadeOutTime).GoToDestination();
                    return false;
                }
            }

            if (resetState)
            {
                sessionTraversalHistory.ResetAllNodes();
            }

            if (!KatieSaveHelperModActions.customTransition.IsReady)
            {
                sceneChanger = new SceneChanger("Outworld", Color.black, fadeInTime, fadeOutTime);
            }
            else
            {
                Transition newTransition = KatieSaveHelperModActions.customTransition.TakeValue();
                sceneChanger = new SceneChanger("Outworld", newTransition.Color, newTransition.FadeInTime, newTransition.FadeOutTime);
                transitionField.SetValue(sceneChanger, newTransition.Type);
            }

            if (gameState.HasSavedSceneEntry)
            {
                string text = gameState.GetDestinationScene();
                if (text.Equals("menu", StringComparison.OrdinalIgnoreCase))
                {
                    text = "Hub";
                }

                sceneChanger.SetDestination(text, gameState.SavedSceneEntrance ?? string.Empty);
            }

            sceneChanger.GoToDestination();
            return false;
        }
    }
}