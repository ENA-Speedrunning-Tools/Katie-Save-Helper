using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using JoelG.ENA4;
using KatieSaveHelper.Patches;

namespace KatieSaveHelper
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class KatieSaveHelperMod : BaseUnityPlugin
    {
        private const string modGUID = "Zieraell.KatieSaveHelper";
        private const string modName = "Katie Save Helper";
        private const string modVersion = "1.0.5.0";
        private const string modAuthors = "Katelyndev0211 and Zieraell";

        private readonly Harmony harmony = new Harmony(modGUID);
        private static KatieSaveHelperMod Instance;

        internal static ManualLogSource mls;

        internal static bool forceCustomSeed;
        internal static int customSeed;

        internal static ConfigEntry<KeyCode> quickSaveKeyConfig;
        internal static ConfigEntry<KeyCode> reloadSaveKeyConfig;
        internal static ConfigEntry<KeyCode> resetSaveKeyConfig;
        internal static ConfigEntry<KeyCode> resetSaveWithSeedKeyConfig;
        internal static ConfigEntry<KeyCode> reloadConfigKeyConfig;

        internal static KeyCode quickSaveKey;
        internal static KeyCode reloadSaveKey;
        internal static KeyCode resetSaveKey;
        internal static KeyCode resetSaveWithSeedKey;
        internal static KeyCode reloadConfigKey;


        void LoadConfig()
        {
            mls.LogInfo("Loading Config...");
            quickSaveKeyConfig = Config.Bind("Hotkeys", "QuickSaveKey", KeyCode.Alpha1, "Key used to save the game");
            reloadSaveKeyConfig = Config.Bind("Hotkeys", "ReloadSaveKey", KeyCode.Alpha2, "Key used to reload the current save");
            resetSaveKeyConfig = Config.Bind("Hotkeys", "HardResetKey", KeyCode.Alpha0, "Key used to hard reset the current save");
            resetSaveWithSeedKeyConfig = Config.Bind("Hotkeys", "HardResetWithSeedKey", KeyCode.Alpha9, "Key used to hard reset the current save while keeping the same seed");
            reloadConfigKeyConfig = Config.Bind("Hotkeys", "ReloadConfigKey", KeyCode.Alpha8, "Key used to reload the mod config");
            LoadKeysFromConfig();
        }

        void LoadKeysFromConfig()
        {
            quickSaveKey = quickSaveKeyConfig.Value;
            reloadSaveKey = reloadSaveKeyConfig.Value;
            resetSaveKey = resetSaveKeyConfig.Value;
            resetSaveWithSeedKey = resetSaveWithSeedKeyConfig.Value;
            reloadConfigKey = reloadConfigKeyConfig.Value;

            mls.LogInfo(
                $"Using keys: \n" +
                $"\tSave={quickSaveKey}\n" +
                $"\tReload={reloadSaveKey}\n" +
                $"\tReset={resetSaveKey}\n" +
                $"\tResetWithSeed={resetSaveWithSeedKey}\n" +
                $"\tReloadConfig={reloadConfigKey}");
        }

        void Awake()
        {
            if (Instance == null)
                Instance = this;

            mls = Logger;

            LoadConfig();

            harmony.PatchAll(typeof(SoftResetPatch));

            mls.LogInfo($"{modName} loaded.");
            mls.LogInfo($"Mod by {modAuthors}");
        }

        void Update()
        {
            if (Input.GetKeyDown(quickSaveKey))
            {
                mls.LogInfo("'Quick Save' key pressed");
                SaveFile.WriteSave();
            }

            if (Input.GetKeyDown(reloadSaveKey))
            {
                mls.LogInfo("'Reload Save' key pressed");
                SaveFile.ContinueSave();
            }

            if (Input.GetKeyDown(resetSaveKey))
            {
                mls.LogInfo("'Reset Save' key pressed");
                SaveFile.ResetSave(MetaSaveFile.Current.SaveIndex);
                SaveFile.ContinueSave();
            }

            if (Input.GetKeyDown(resetSaveWithSeedKey))
            {
                mls.LogInfo("'Reset Save with Seed' key pressed");
                forceCustomSeed = true;
                customSeed = SaveFile.CurrentSave.SaveHash;
                SaveFile.ResetSave(MetaSaveFile.Current.SaveIndex);
                SaveFile.ContinueSave();
                forceCustomSeed = false;
            }

            if (Input.GetKeyDown(reloadConfigKey))
            {
                mls.LogInfo("'Reload Config' key pressed");
                Config.Reload();
                LoadKeysFromConfig();
            }
        }
    }
}