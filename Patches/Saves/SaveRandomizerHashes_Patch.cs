using HarmonyLib;
using JoelG.ENA4;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using KatieSaveHelper.Features.Util;

namespace KatieSaveHelper.Patches
{
    [HarmonyPatch(typeof(SaveRandomizerHashes))]
    public static class SaveRandomizerHashes_Patch
    {
        private static readonly FieldInfo hardwareHashField = AccessTools.Field(typeof(SaveRandomizerHashes), "HardwareHash");
        private static readonly FieldInfo sessionHashField = AccessTools.Field(typeof(SaveRandomizerHashes), "PlaySessionHash");
        private static readonly PropertyInfo saveFileHashProp = AccessTools.Property(typeof(SaveRandomizerHashes), "SaveFileHash");
        private static readonly PropertyInfo sceneHashProp = AccessTools.Property(typeof(SaveRandomizerHashes), "SceneHash");
        private static readonly OnSceneLoadPatch oslPatcher = new OnSceneLoadPatch(WaitToInjectSeeds, patchOnStartup:true);

        public static readonly string seedInjectRoutineIdentifier = "KSH.Event.InjectCustomSeedsOnLaunch";

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

        public static void WaitToInjectSeeds(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Menu") return;

            StartNewSeedInjector();

            oslPatcher.TryUnpatch();
        }

        
        public static void StartNewSeedInjector()
        {
            StaticCoroutine.Start(sc => InjectCustomSeeds(sc), seedInjectRoutineIdentifier);
        }
        private static IEnumerator InjectCustomSeeds(StaticCoroutine scWrapper)
        {
            yield return StaticCoroutine.WaitForCancelAll(sc => (sc.Identifier == seedInjectRoutineIdentifier && sc != scWrapper) || sc.Identifier == MainMenuPanelGroup_Patch.menuEventTriggerRoutineIdentifier, scWrapper.CancelToken);
            if (scWrapper.CancelToken.IsCancellationRequested) yield break;

            Task<(SeedGeneratorReturnCode returnCode, int seed)> sessionTask = null;
            Task<(SeedGeneratorReturnCode returnCode, int seed)> hardwareTask = null;

            if (KatieConfig.Settings.sessionSeedGeneratorType.Value != SeedGenerator.Random)
                sessionTask = KatieUtil.GenerateSessionSeed(scWrapper.CancelToken);

            if (KatieConfig.Settings.hardwareSeedGeneratorType.Value != SeedGenerator.Device)
                hardwareTask = KatieUtil.GenerateHardwareSeed(scWrapper.CancelToken);

            if (sessionTask != null)
            {
                yield return KatieUtil.WaitForTask(sessionTask);
                if (scWrapper.CancelToken.IsCancellationRequested) yield break;
                KatieUtil.EditSessionHash(sessionTask.Result.seed);
                PlayerRandomBlink_Patch.ResetBlinkChanceGenerator();
            }

            if (hardwareTask != null)
            {
                yield return KatieUtil.WaitForTask(hardwareTask);
                if (scWrapper.CancelToken.IsCancellationRequested) yield break;
                KatieUtil.EditHardwareHash(hardwareTask.Result.seed);
            }
        }
    }
}
