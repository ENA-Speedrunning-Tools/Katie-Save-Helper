using HarmonyLib;
using JoelG.ENA4;
using System.Reflection;
using UnityEngine.SceneManagement;

namespace KatieSaveHelper.Patches
{
    
    [HarmonyPatch(typeof(SaveRandomizerHashes))]
    public static class SaveRandomizerHashes_Patch
    {
        private static readonly FieldInfo hardwareHashField = AccessTools.Field(typeof(SaveRandomizerHashes), "HardwareHash");
        private static readonly FieldInfo sessionHashField = AccessTools.Field(typeof(SaveRandomizerHashes), "PlaySessionHash");
        private static readonly PropertyInfo saveFileHashProp = AccessTools.Property(typeof(SaveRandomizerHashes), "SaveFileHash");
        private static readonly PropertyInfo sceneHashProp = AccessTools.Property(typeof(SaveRandomizerHashes), "SceneHash");
        private static readonly OnSceneLoadPatch oslPatcher = new OnSceneLoadPatch(InjectCustomSeeds, patchOnStartup:true);

        // Bypass the unchangeable read-only field values when trying to read the current Session or Hardware hash

        [HarmonyPatch(nameof(SaveRandomizerHashes.GetHashByType))]
        [HarmonyPrefix]
        public static bool GetHashByType_Prefix(SaveRandomizerHashes.HashType hashType, ref int __result)
        {
            switch (hashType)
            {
                case SaveRandomizerHashes.HashType.Hardware:
                    __result = (int)hardwareHashField.GetValue(null);
                    return false;
                case SaveRandomizerHashes.HashType.PlaySession:
                    __result = (int)sessionHashField.GetValue(null);
                    return false;
                case SaveRandomizerHashes.HashType.Scene:
                    __result = (int)sceneHashProp.GetValue(null);
                    return false;
                case SaveRandomizerHashes.HashType.SaveFile:
                    __result = (int)saveFileHashProp.GetValue(null);
                    return false;
                default:
                    return true;
            }
        }

        // Inject a custom generated Session and Hardware seed on Main Menu load if configured by the mod

        public static void InjectCustomSeeds(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Menu") return;

            if (KatieSaveHelperModConfig.sessionSeedGeneratorType.Value != SeedGenerator.Random)
            {
                int newSeed = KatieUtil.GenerateSessionSeed().seed;
                KatieUtil.EditSessionHash(newSeed);
                PlayerRandomBlink_Patch.ResetBlinkChanceGenerator();
            }

            if (KatieSaveHelperModConfig.hardwareSeedGeneratorType.Value != SeedGenerator.Device)
            {
                int newSeed = KatieUtil.GenerateHardwareSeed().seed;
                KatieUtil.EditHardwareHash(newSeed);
            }

            oslPatcher.TryUnpatch();
        }
    }
}
