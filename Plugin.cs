using MelonLoader;
using UnityEngine;
using JoelG.ENA4;
using UnityEngine.SceneManagement;

[assembly: MelonInfo(typeof(KatieSaveHelper.KatieSaveHelperMod), "Katie Save Helper", "1.0.6", "Katelyndev0211 and Zieraell")]

namespace KatieSaveHelper
{
    public class KatieSaveHelperMod : MelonMod
    {
        private const string modGUID = "Zieraell.KatieSaveHelper";
        private const string modName = "Katie Save Helper";
        private const string modVersion = "1.0.6.0";
        private const string modAuthors = "Katelyndev0211 and Zieraell";

        internal static SceneChanger.TransitionType customTransitionType = SceneChanger.TransitionType.FadeToColor;
        internal static bool forceCustomSeed = false;
        internal static bool forceCustomTransition = false;
        internal static int customSeed;
        internal static Color customTransitionColor = Color.black;
        internal static float customTransitionFadeInTime = 0;
        internal static float customTransitionFadeOutTime = 0;
        internal static bool allowSave = false;

        private readonly HarmonyLib.Harmony harmony = new HarmonyLib.Harmony(modGUID);

        public override void OnInitializeMelon()
        {
            KatieSaveHelperModConfig.LoadConfig();

            MelonLogger.Msg($"{modName} loaded.");
            MelonLogger.Msg($"Mod by {modAuthors}");

            harmony.PatchAll();
        }

        public override void OnUpdate()
        {
            if (!Input.anyKeyDown)
            {
                return;
            }

            if (Input.GetKeyDown(KatieSaveHelperModConfig.quickSave_Key))
            {
                MelonLogger.Msg("'Quick Save' key pressed");
                string currentSceneName = SceneManager.GetActiveScene().name;

                if (currentSceneName == "Menu")
                {
                    MelonLogger.Msg("Cannot save in Main Menu");
                    return;
                }

                allowSave = true;
                if (currentSceneName == SaveFile.CurrentSave.GameState.GetDestinationScene())
                {
                    SaveFile.WriteSaveWithEntrance(SaveFile.CurrentSave.GameState.SavedSceneEntrance);
                    MelonLogger.Msg("Save complete");
                }
                else
                {
                    SaveFile.WriteSaveWithEntrance();
                    MelonLogger.Msg("Save complete (Failsafe)");
                }
                allowSave = false;
                return;
            }

            if (Input.GetKeyDown(KatieSaveHelperModConfig.reloadSave_Key))
            {
                MelonLogger.Msg("'Reload Save' key pressed");

                forceCustomTransition = true;
                customTransitionType = KatieSaveHelperModConfig.reloadSave_TransitionType;
                customTransitionColor = KatieSaveHelperModConfig.reloadSave_TransitionColor;
                customTransitionFadeInTime = KatieSaveHelperModConfig.reloadSave_TransitionFadeInTime;
                customTransitionFadeOutTime = KatieSaveHelperModConfig.reloadSave_TransitionFadeOutTime;

                SaveFile.ContinueSave();
                forceCustomTransition = false;
                return;
            }

            if (Input.GetKeyDown(KatieSaveHelperModConfig.resetSave_Key))
            {
                MelonLogger.Msg("'Reset Save' key pressed");

                forceCustomTransition = true;
                customTransitionType = KatieSaveHelperModConfig.resetSave_TransitionType;
                customTransitionColor = KatieSaveHelperModConfig.resetSave_TransitionColor;
                customTransitionFadeInTime = KatieSaveHelperModConfig.resetSave_TransitionFadeInTime;
                customTransitionFadeOutTime = KatieSaveHelperModConfig.resetSave_TransitionFadeOutTime;

                SaveFile.ResetSave(MetaSaveFile.Current.SaveIndex);
                SaveFile.ContinueSave();
                forceCustomTransition = false;
                return;
            }

            if (Input.GetKeyDown(KatieSaveHelperModConfig.resetSaveWithSeed_Key))
            {
                MelonLogger.Msg("'Reset Save with Seed' key pressed");

                forceCustomTransition = true;
                customTransitionType = KatieSaveHelperModConfig.resetSaveWithSeed_TransitionType;
                customTransitionColor = KatieSaveHelperModConfig.resetSaveWithSeed_TransitionColor;
                customTransitionFadeInTime = KatieSaveHelperModConfig.resetSaveWithSeed_TransitionFadeInTime;
                customTransitionFadeOutTime = KatieSaveHelperModConfig.resetSaveWithSeed_TransitionFadeOutTime;

                forceCustomSeed = true;
                customSeed = SaveFile.CurrentSave.SaveHash;
                SaveFile.ResetSave(MetaSaveFile.Current.SaveIndex);
                SaveFile.ContinueSave();
                forceCustomSeed = false;
                forceCustomTransition = false;
                return;
            }

            if (Input.GetKeyDown(KatieSaveHelperModConfig.reloadConfig_Key))
            {
                MelonLogger.Msg("'Reload Config' key pressed");
                MelonPreferences.Load();
                KatieSaveHelperModConfig.LoadOptionsFromConfig();
                return;
            }
        }
    }
}