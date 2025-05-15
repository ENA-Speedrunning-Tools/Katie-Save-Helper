using MelonLoader;
using UnityEngine;
using JoelG.ENA4;
using KatieSaveHelper.Patches;

[assembly: MelonInfo(typeof(KatieSaveHelper.KatieSaveHelperMod), "Katie Save Helper", "1.0.5", "Katelyndev0211 and Zieraell")]

namespace KatieSaveHelper
{
    public class KatieSaveHelperMod : MelonMod
    {
        private const string modGUID = "Zieraell.KatieSaveHelper";
        private const string modName = "Katie Save Helper";
        private const string modVersion = "1.0.5.0";
        private const string modAuthors = "Katelyndev0211 and Zieraell";

        internal static MelonPreferences_Category configCategory;
        internal static MelonPreferences_Entry<KeyCode> quickSaveKeyConfig;
        internal static MelonPreferences_Entry<KeyCode> reloadSaveKeyConfig;
        internal static MelonPreferences_Entry<KeyCode> resetSaveKeyConfig;
        internal static MelonPreferences_Entry<KeyCode> resetSaveWithSeedKeyConfig;
        internal static MelonPreferences_Entry<KeyCode> reloadConfigKeyConfig;

        internal static KeyCode quickSaveKey;
        internal static KeyCode reloadSaveKey;
        internal static KeyCode resetSaveKey;
        internal static KeyCode resetSaveWithSeedKey;
        internal static KeyCode reloadConfigKey;

        private readonly HarmonyLib.Harmony harmony = new HarmonyLib.Harmony(modGUID);

        internal static bool forceCustomSeed;
        internal static int customSeed;

        void LoadConfig()
        {
            MelonLogger.Msg("Loading config...");

            configCategory = MelonPreferences.CreateCategory("KatieSaveHelper", "Katie Save Helper Settings");

            quickSaveKeyConfig = configCategory.CreateEntry("QuickSaveKey", KeyCode.Alpha1, "Key used to save the game");
            reloadSaveKeyConfig = configCategory.CreateEntry("ReloadSaveKey", KeyCode.Alpha2, "Key used to reload the current save");
            resetSaveKeyConfig = configCategory.CreateEntry("HardResetKey", KeyCode.Alpha0, "Key used to hard reset the current save");
            resetSaveWithSeedKeyConfig = configCategory.CreateEntry("HardResetWithSeedKey", KeyCode.Alpha9, "Key used to hard reset the current save while keeping the same seed");
            reloadConfigKeyConfig = configCategory.CreateEntry("ReloadConfigKey", KeyCode.Alpha8, "Key used to hard reset the current save while keeping the same seed");

            configCategory.SaveToFile();

            LoadKeysFromConfig();
        }
        void LoadKeysFromConfig()
        {
            quickSaveKey = quickSaveKeyConfig.Value;
            reloadSaveKey = reloadSaveKeyConfig.Value;
            resetSaveKey = resetSaveKeyConfig.Value;
            resetSaveWithSeedKey = resetSaveWithSeedKeyConfig.Value;
            reloadConfigKey = reloadConfigKeyConfig.Value;

            MelonLogger.Msg(
                $"Using keys: \n" +
                $"\tSave={quickSaveKey}\n" +
                $"\tReload={reloadSaveKey}\n" +
                $"\tReset={resetSaveKey}\n" +
                $"\tResetWithSeed={resetSaveWithSeedKey}\n" +
                $"\tReloadConfig={reloadConfigKey}");
        }

        public override void OnInitializeMelon()
        {
            LoadConfig();

            MelonLogger.Msg($"{modName} loaded.");
            MelonLogger.Msg($"Mod by {modAuthors}");

            harmony.PatchAll(typeof(SoftResetPatch));
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(quickSaveKey))
            {
                MelonLogger.Msg("'Quick Save' key pressed");
                SaveFile.WriteSave();
            }

            if (Input.GetKeyDown(reloadSaveKey))
            {
                MelonLogger.Msg("'Reload Save' key pressed");
                SaveFile.ContinueSave();
            }

            if (Input.GetKeyDown(resetSaveKey))
            {
                MelonLogger.Msg("'Reset Save' key pressed");
                SaveFile.ResetSave(MetaSaveFile.Current.SaveIndex);
                SaveFile.ContinueSave();
            }

            if (Input.GetKeyDown(resetSaveWithSeedKey))
            {
                MelonLogger.Msg("'Reset Save with Seed' key pressed");
                forceCustomSeed = true;
                customSeed = SaveFile.CurrentSave.SaveHash;
                SaveFile.ResetSave(MetaSaveFile.Current.SaveIndex);
                SaveFile.ContinueSave();
                forceCustomSeed = false;
            }

            if (Input.GetKeyDown(reloadConfigKey))
            {
                MelonLogger.Msg("'Reload Config' key pressed");
                MelonPreferences.Load();
                LoadKeysFromConfig();
            }
        }
    }
}