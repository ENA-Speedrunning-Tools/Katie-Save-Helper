using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using JoelG.ENA4;
using KatieSaveToolMod.Patches;

namespace KatieSaveToolMod
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class KatieSaveToolMod : BaseUnityPlugin
    {
        private const string modGUID = "Zieraell.KatieSaveTool";
        private const string modName = "Katie Save Tool";
        private const string modVersion = "1.0.4.0";
        private const string modAuthors = "Katelyndev0211 and Zieraell";

        private readonly Harmony harmony = new Harmony(modGUID);
        private static KatieSaveToolMod Instance;

        internal static ManualLogSource mls;

        internal static bool forceCustomSeed;
        internal static int customSeed;

        // Config entries
        internal static ConfigEntry<KeyCode> quickSaveKey;
        internal static ConfigEntry<KeyCode> reloadSaveKey;
        internal static ConfigEntry<KeyCode> resetSaveKey;
        internal static ConfigEntry<KeyCode> resetSaveWithSeedKey;

        void Awake()
        {
            if (Instance == null)
                Instance = this;

            mls = Logger;

            // Create config entries with defaults and descriptions
            quickSaveKey = Config.Bind("Hotkeys", "QuickSaveKey", KeyCode.Alpha1, "Key used to save the game");
            reloadSaveKey = Config.Bind("Hotkeys", "ReloadSaveKey", KeyCode.Alpha2, "Key used to reload the current save");
            resetSaveKey = Config.Bind("Hotkeys", "HardResetKey", KeyCode.Alpha0, "Key used to hard reset the current save");
            resetSaveWithSeedKey = Config.Bind("Hotkeys", "HardResetWithSeedKey", KeyCode.Alpha9, "Key used to hard reset the current save while keeping the same seed");

            harmony.PatchAll(typeof(SoftResetPatch));

            mls.LogInfo($"{modName} loaded.");
            mls.LogInfo($"Using keys: Save={quickSaveKey.Value}, Reload={reloadSaveKey.Value}, Reset={resetSaveKey.Value}, ResetWithSeed={resetSaveWithSeedKey.Value}");
            mls.LogInfo($"Mod by {modAuthors}");
        }

        void Update()
        {
            if (Input.GetKeyDown(quickSaveKey.Value))
            {
                SaveFile.WriteSave();
                mls.LogInfo("'Quick Save' key pressed");
            }

            if (Input.GetKeyDown(reloadSaveKey.Value))
            {
                SaveFile.ContinueSave();
                mls.LogInfo("'Reload Save' key pressed");
            }

            if (Input.GetKeyDown(resetSaveKey.Value))
            {
                SaveFile.ResetSave(MetaSaveFile.Current.SaveIndex);
                SaveFile.ContinueSave();
                mls.LogInfo("'Reset Save' key pressed");
            }

            if (Input.GetKeyDown(resetSaveWithSeedKey.Value))
            {
                forceCustomSeed = true;
                customSeed = SaveFile.CurrentSave.SaveHash;
                SaveFile.ResetSave(MetaSaveFile.Current.SaveIndex);
                SaveFile.ContinueSave();
                forceCustomSeed = false;
                mls.LogInfo("'Reset Save with Seed' key pressed");
            }
        }
    }
}