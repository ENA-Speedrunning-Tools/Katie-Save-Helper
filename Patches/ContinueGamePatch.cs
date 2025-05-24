using JoelG.ENA4;
using System.Reflection;
using System;
using UnityEngine;
using HarmonyLib;

namespace KatieSaveHelper.Patches
{

    // If performed by the mod, create a custom scene changer object for continuing the save

    [HarmonyPatch(typeof(SaveFileData), "ContinueGame")]
    public class ContinueGamePatch
    {
        private static readonly FieldInfo gameStateField = AccessTools.Field(typeof(SaveFileData), "gameState");
        private static readonly FieldInfo sessionTraversalHistoryField = AccessTools.Field(typeof(SaveFileData), "sessionTraversalHistory");
        private static readonly FieldInfo transitionField = AccessTools.Field(typeof(SceneChanger), "transition");

        public static bool Prefix(SaveFileData __instance, bool resetState = true, float fadeInTime = 1f, float fadeOutTime = 1f)
        {
            SaveDataPlayerGameState gameState = (SaveDataPlayerGameState)gameStateField.GetValue(__instance);
            SaveDataNodeTraversalHistory sessionTraversalHistory = (SaveDataNodeTraversalHistory)sessionTraversalHistoryField.GetValue(__instance);
            SceneChanger sceneChanger;

            if (gameState.HasCompletedGame)
            {
                new SceneChanger("Menu", Color.black, fadeInTime, fadeOutTime).GoToDestination();
                return false;
            }

            if (resetState)
            {
                sessionTraversalHistory.ResetAllNodes();
            }

            if (!KatieSaveHelperMod.forceCustomTransition)
            {
                sceneChanger = new SceneChanger("Outworld", Color.black, fadeInTime, fadeOutTime);
            }
            else
            {
                sceneChanger = new SceneChanger("Outworld", KatieSaveHelperMod.customTransition.Color, KatieSaveHelperMod.customTransition.FadeInTime, KatieSaveHelperMod.customTransition.FadeOutTime);
                transitionField.SetValue(sceneChanger, KatieSaveHelperMod.customTransition.Type);
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