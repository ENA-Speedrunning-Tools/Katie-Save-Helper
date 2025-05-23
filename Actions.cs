using JoelG.ENA4;
using KatieSaveHelper.PsuedoRandomizer;
using System.Reflection;
using UnityEngine.SceneManagement;

namespace KatieSaveHelper
{
    public static class KatieSaveHelperModActions
    {
        private static readonly FieldInfo currentSaveField = typeof(SaveFile).GetField("current", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly FieldInfo saveHashField = typeof(SaveFileData).GetField("saveHash", BindingFlags.NonPublic | BindingFlags.Instance);
        public static void quickSave()
        {
            string currentSceneName = SceneManager.GetActiveScene().name;

            if (currentSceneName == "Menu")
            {
                KatieSaveHelperMod.mls.LogInfo("Cannot save in Main Menu");
                return;
            }

            KatieSaveHelperMod.allowSave = true;
            if (currentSceneName == SaveFile.CurrentSave.GameState.GetDestinationScene())
            {
                SaveFile.WriteSaveWithEntrance(SaveFile.CurrentSave.GameState.SavedSceneEntrance);
                KatieSaveHelperMod.mls.LogInfo("Save complete");
            }
            else
            {
                SaveFile.WriteSaveWithEntrance();
                KatieSaveHelperMod.mls.LogInfo("Save complete (Failsafe)");
            }
            KatieSaveHelperMod.allowSave = false;
        }

        public static void reloadSaveWithFileSeed()
        {
            string currentSceneName = SceneManager.GetActiveScene().name;

            if (currentSceneName == "Menu")
            {
                KatieSaveHelperMod.mls.LogInfo("Cannot reload saves in Main Menu");
                return;
            }

            KatieSaveHelperMod.forceCustomTransition = true;
            KatieSaveHelperMod.setTransition(KatieSaveHelperModConfig.reloadSaveWithFileSeed.Transition);

            SaveFile.ContinueSave();
            KatieSaveHelperMod.forceCustomTransition = false;
        }

        public static void reloadSaveWithCurrentSeed()
        {
            string currentSceneName = SceneManager.GetActiveScene().name;

            if (currentSceneName == "Menu")
            {
                KatieSaveHelperMod.mls.LogInfo("Cannot reload saves in Main Menu");
                return;
            }

            KatieSaveHelperMod.forceCustomTransition = true;
            KatieSaveHelperMod.setTransition(KatieSaveHelperModConfig.reloadSaveWithCurrentSeed.Transition);

            KatieSaveHelperMod.forceCustomSeedOnLoad = true;
            KatieSaveHelperMod.customSeed = SaveFile.CurrentSave.SaveHash;
            SaveFile.ContinueSave();
            KatieSaveHelperMod.forceCustomSeedOnLoad = false;
            KatieSaveHelperMod.forceCustomTransition = false;
        }

        public static void reloadSaveWithRandomSeed()
        {
            string currentSceneName = SceneManager.GetActiveScene().name;

            if (currentSceneName == "Menu")
            {
                KatieSaveHelperMod.mls.LogInfo("Cannot reload saves in Main Menu");
                return;
            }

            KatieSaveHelperMod.forceCustomTransition = true;
            KatieSaveHelperMod.setTransition(KatieSaveHelperModConfig.reloadSaveWithRandomSeed.Transition);

            KatieSaveHelperMod.forceCustomSeedOnLoad = true;
            KatieSaveHelperMod.customSeed = SaveRandomizer.GetAbsolutelyRandomValue();
            SaveFile.ContinueSave();
            KatieSaveHelperMod.forceCustomSeedOnLoad = false;
            KatieSaveHelperMod.forceCustomTransition = false;
        }

        public static void reloadSaveWithPsuedoRandomSeed()
        {
            string currentSceneName = SceneManager.GetActiveScene().name;

            if (currentSceneName == "Menu")
            {
                KatieSaveHelperMod.mls.LogInfo("Cannot reload saves in Main Menu");
                return;
            }

            if (KatiePsuedoRandomizer.isBusy)
            {
                KatieSaveHelperMod.mls.LogInfo("Psuedo Randomizer is busy");
                return;
            }

            var psuedoResult = KatiePsuedoRandomizer.GeneratePsuedoRandomSeed();
            if (!psuedoResult.success)
            {
                return;
            }

            KatieSaveHelperMod.forceCustomTransition = true;
            KatieSaveHelperMod.setTransition(KatieSaveHelperModConfig.reloadSaveWithPsuedoRandomSeed.Transition);

            KatieSaveHelperMod.forceCustomSeedOnLoad = true;
            KatieSaveHelperMod.customSeed = psuedoResult.seed;
            SaveFile.ContinueSave();
            KatieSaveHelperMod.forceCustomSeedOnLoad = false;
            KatieSaveHelperMod.forceCustomTransition = false;
        }


        public static void resetSaveWithCurrentSeed()
        {
            string currentSceneName = SceneManager.GetActiveScene().name;

            if (currentSceneName == "Menu")
            {
                KatieSaveHelperMod.mls.LogInfo("Cannot reset saves in Main Menu");
                return;
            }

            KatieSaveHelperMod.forceCustomTransition = true;
            KatieSaveHelperMod.setTransition(KatieSaveHelperModConfig.resetSaveWithCurrentSeed.Transition);

            KatieSaveHelperMod.forceCustomSeedOnReset = true;
            KatieSaveHelperMod.customSeed = SaveFile.CurrentSave.SaveHash;
            SaveFile.ResetSave(MetaSaveFile.Current.SaveIndex);
            SaveFile.ContinueSave();
            KatieSaveHelperMod.forceCustomSeedOnReset = false;
            KatieSaveHelperMod.forceCustomTransition = false;
        }

        public static void resetSaveWithRandomSeed()
        {
            string currentSceneName = SceneManager.GetActiveScene().name;

            if (currentSceneName == "Menu")
            {
                KatieSaveHelperMod.mls.LogInfo("Cannot reset saves in Main Menu");
                return;
            }

            KatieSaveHelperMod.forceCustomTransition = true;
            KatieSaveHelperMod.setTransition(KatieSaveHelperModConfig.resetSaveWithRandomSeed.Transition);

            SaveFile.ResetSave(MetaSaveFile.Current.SaveIndex);
            SaveFile.ContinueSave();
            KatieSaveHelperMod.forceCustomTransition = false;
        }

        public static void resetSaveWithPsuedoRandomSeed()
        {
            string currentSceneName = SceneManager.GetActiveScene().name;

            if (currentSceneName == "Menu")
            {
                KatieSaveHelperMod.mls.LogInfo("Cannot reset saves in Main Menu");
                return;
            }

            var psuedoResult = KatiePsuedoRandomizer.GeneratePsuedoRandomSeed();
            if (!psuedoResult.success)
            {
                return;
            }

            KatieSaveHelperMod.forceCustomTransition = true;
            KatieSaveHelperMod.setTransition(KatieSaveHelperModConfig.resetSaveWithPsuedoRandomSeed.Transition);

            KatieSaveHelperMod.forceCustomSeedOnReset = true;
            KatieSaveHelperMod.customSeed = psuedoResult.seed;
            SaveFile.ResetSave(MetaSaveFile.Current.SaveIndex);
            SaveFile.ContinueSave();
            KatieSaveHelperMod.forceCustomSeedOnReset = false;
            KatieSaveHelperMod.forceCustomTransition = false;
        }

    }

}
