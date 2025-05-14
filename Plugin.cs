using MelonLoader;
using UnityEngine;
using System.Reflection;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using HarmonyLib;
using JoelG.ENA4;
using KatieSaveToolMod.Patches;

[assembly: MelonInfo(typeof(KatieSaveToolMod.KatieSaveToolMod), "Katie Save Tool", "1.0.4", "Katelyndev0211 and Zieraell")]

namespace KatieSaveToolMod
{
    public class KatieSaveToolMod : MelonMod
    {
        private const string modGUID = "Zieraell.KatieSaveTool";
        private const string modName = "Katie Save Tool";
        private const string modVersion = "1.0.4.0";
        private const string modAuthors = "Katelyndev0211 and Zieraell";

        public static MelonPreferences_Category configCategory;
        public static MelonPreferences_Entry<KeyCode> quickSaveKey;
        public static MelonPreferences_Entry<KeyCode> reloadSaveKey;
        public static MelonPreferences_Entry<KeyCode> resetSaveKey;
        public static MelonPreferences_Entry<KeyCode> resetSaveWithSeedKey;

        private readonly HarmonyLib.Harmony harmony = new HarmonyLib.Harmony(modGUID);

        internal static bool forceCustomSeed;
        internal static int customSeed;

        public override void OnInitializeMelon()
        {
            // Config setup
            configCategory = MelonPreferences.CreateCategory("KatieSaveTool", "Katie Save Tool Settings");

            quickSaveKey = configCategory.CreateEntry("QuickSaveKey", KeyCode.Alpha1, "Key used to save the game");
            reloadSaveKey = configCategory.CreateEntry("ReloadSaveKey", KeyCode.Alpha2, "Key used to reload the current save");
            resetSaveKey = configCategory.CreateEntry("HardResetKey", KeyCode.Alpha0, "Key used to hard reset the current save");
            resetSaveWithSeedKey = configCategory.CreateEntry("HardResetWithSeedKey", KeyCode.Alpha9, "Key used to hard reset the current save while keeping the same seed");

            configCategory.SaveToFile();

            MelonLogger.Msg($"{modName} loaded.");
            MelonLogger.Msg($"Using keys: Save={quickSaveKey.Value}, Reload={reloadSaveKey.Value}, Reset={resetSaveKey.Value}, ResetWithSeed={resetSaveWithSeedKey.Value}");
            MelonLogger.Msg($"Mod by {modAuthors}");

            harmony.PatchAll(typeof(SoftResetPatch));
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(quickSaveKey.Value))
            {
                SaveFile.WriteSave();
                MelonLogger.Msg("'Quick Save' key pressed");
            }

            if (Input.GetKeyDown(reloadSaveKey.Value))
            {
                SaveFile.ContinueSave();
                MelonLogger.Msg("'Reload Save' key pressed");
            }

            if (Input.GetKeyDown(resetSaveKey.Value))
            {
                SaveFile.ResetSave(MetaSaveFile.Current.SaveIndex);
                SaveFile.LoadSave(MetaSaveFile.Current.SaveIndex);
                SaveFile.ContinueSave();
                MelonLogger.Msg("'Reset Save' key pressed");
            }

            if (Input.GetKeyDown(resetSaveWithSeedKey.Value))
            {
                forceCustomSeed = true;
                customSeed = SaveFile.CurrentSave.SaveHash;
                SaveFile.ResetSave(MetaSaveFile.Current.SaveIndex);
                SaveFile.ContinueSave();
                forceCustomSeed = false;
                MelonLogger.Msg("'Reset Save with Seed' key pressed");
            }
        }
    }
}