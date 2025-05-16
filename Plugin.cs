using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using JoelG.ENA4;
using UnityEngine.SceneManagement;

namespace KatieSaveHelper
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class KatieSaveHelperMod : BaseUnityPlugin
    {
        private const string modGUID = "Zieraell.KatieSaveHelper";
        private const string modName = "Katie Save Helper";
        private const string modVersion = "1.0.6.0";
        private const string modAuthors = "Katelyndev0211 and Zieraell";

        private readonly Harmony harmony = new Harmony(modGUID);
        internal static KatieSaveHelperMod Instance;

        internal static ManualLogSource mls;

        internal static SceneChanger.TransitionType customTransitionType = SceneChanger.TransitionType.FadeToColor;
        internal static bool forceCustomSeed = false;
        internal static bool forceCustomTransition = false;
        internal static int customSeed;
        internal static Color customTransitionColor = Color.black;
        internal static float customTransitionFadeInTime = 0;
        internal static float customTransitionFadeOutTime = 0;
        internal static bool allowSave = false;

        void Awake()
        {
            if (Instance == null)
                Instance = this;

            mls = Logger;

            KatieSaveHelperModConfig.LoadConfig();

            harmony.PatchAll();

            mls.LogInfo($"{modName} loaded.");
            mls.LogInfo($"Mod by {modAuthors}");
        }

        void Update()
        {
            if (!Input.anyKeyDown)
            {
                return;
            }

            if (Input.GetKeyDown(KatieSaveHelperModConfig.quickSave_Key))
            {
                mls.LogInfo("'Quick Save' key pressed");
                string currentSceneName = SceneManager.GetActiveScene().name;

                if (currentSceneName == "Menu")
                {
                    mls.LogInfo("Cannot save in Main Menu");
                    return;
                }

                allowSave = true;
                if (currentSceneName == SaveFile.CurrentSave.GameState.GetDestinationScene())
                {
                    SaveFile.WriteSaveWithEntrance(SaveFile.CurrentSave.GameState.SavedSceneEntrance);
                    mls.LogInfo("Save complete");
                }
                else
                {
                    SaveFile.WriteSaveWithEntrance();
                    mls.LogInfo("Save complete (Failsafe)");
                }
                allowSave = false;
                return;
            }

            if (Input.GetKeyDown(KatieSaveHelperModConfig.reloadSave_Key))
            {
                mls.LogInfo("'Reload Save' key pressed");

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
                mls.LogInfo("'Reset Save' key pressed");

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
                mls.LogInfo("'Reset Save with Seed' key pressed");

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
                mls.LogInfo("'Reload Config' key pressed");
                Config.Reload();
                KatieSaveHelperModConfig.LoadOptionsFromConfig();
                return;
            }
        }
    }
}