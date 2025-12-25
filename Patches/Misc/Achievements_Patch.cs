using HarmonyLib;
using JoelG.ENA4;
using Steamworks;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace KatieSaveHelper.Patches
{
    public static class Achievements_Patch
    {
        private static readonly FieldInfo LookupField = AccessTools.Field(typeof(AchievementsLookup), "Lookup");
        private static readonly FieldInfo DefaultDataField = AccessTools.Field(typeof(AchievementsLookup), "defaultData");

        private static List<string> simulatedAchievements = new List<string>();

        private static readonly OnSceneLoadPatch oslPatcher = new OnSceneLoadPatch(ApplyPatch, patchOnStartup: true);

        // Very late harmony patch so stuff doesn't break
        private static void ApplyPatch(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Boot")
            {
                KatieMain.Instance.harmony.Patch(
                    original: AccessTools.Method(typeof(Achievements), nameof(Achievements.UnlockAchievement)),
                    postfix: new HarmonyMethod(typeof(Achievements_Patch), nameof(Achievements_Patch.UnlockAchievement_Postfix))
                );
                oslPatcher.TryUnpatch();
            }
        }

        public static void UnlockAchievement_Postfix(string internalKey)
        {
            var Lookup = (Dictionary<string, AchievementData>)LookupField.GetValue(null);
            var defaultData = (AchievementData)DefaultDataField.GetValue(null);

            if (Lookup == null || defaultData == null)
            {
                KatieLogger.Warning("AchievementsLookup not yet initialized");
                return;
            }

            var achData = AchievementsLookup.GetAchievementDataOrDefault(internalKey);
            bool keyIsValid = achData != defaultData;

            if (keyIsValid && !simulatedAchievements.Contains(internalKey))
            {
                simulatedAchievements.Add(internalKey);

                if (KatieConfig.Settings.notifyOnSimulatedAchievements.Value)
                {
                    string achDisplayName = SteamUserStats.GetAchievementDisplayAttribute(internalKey, "name");
                    if (string.IsNullOrEmpty(achDisplayName))
                        achDisplayName = internalKey;
                    ToastController.TryQueueAndLogToast(new ToastInstance($"Triggered Achievement '{achDisplayName}' ({simulatedAchievements.Count}/{Lookup.Count})", holdTime: Mathf.Max(0, KatieConfig.Settings.simulatedAchievementToastHoldTime.Value)));
                }
            }
            else if (!keyIsValid)
            {
                KatieLogger.Warning($"An unknown achievement key was triggered : '{internalKey}'");
            }
        }

        public static void ResetSimulatedAchievements()
        {
            simulatedAchievements.Clear();
            KatieLogger.Info("Simulated Achievements reset");
        }
    }
}
