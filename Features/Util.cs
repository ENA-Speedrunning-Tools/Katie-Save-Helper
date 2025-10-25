using System.Reflection;
using UnityEngine;
using JoelG.ENA4;
using Steamworks;
using LMirman.Utilities;
using JoelG.ENA4.Audio;
using HarmonyLib;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.Events;
using System.Linq;
using KatieSaveHelper.Patches;
using UnityEngine.Playables;

namespace KatieSaveHelper
{
    public class OnSceneLoadPatch
    {
        public bool patchApplied { get; private set; } = false;
        public bool startupPatch { get; private set; }

        public UnityAction<Scene, LoadSceneMode> patch { get; private set; }

        public OnSceneLoadPatch(UnityAction<Scene, LoadSceneMode> patch, bool patchOnStartup = false)
        {
            this.patch = patch;
            this.startupPatch = patchOnStartup;
        }

        public bool TryPatch()
        {
            if (patchApplied) return false;
            SceneManager.sceneLoaded += patch;
            patchApplied = true;
            return true;
        }

        public bool TryUnpatch()
        {
            if (!patchApplied) return false;
            SceneManager.sceneLoaded -= patch;
            patchApplied = false;
            return true;
        }

        public static void ApplyStartupPatches()
        {
            var assembly = Assembly.GetExecutingAssembly();

            foreach (var type in assembly.GetTypes())
            {
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                                 .Where(f => f.FieldType == typeof(OnSceneLoadPatch));

                foreach (var field in fields)
                {
                    if (field.IsStatic)
                    {
                        var patcher = field.GetValue(null) as OnSceneLoadPatch;
                        TryInvokeStartupPatch(patcher, type.Name, field.Name);
                    }
                    else
                    {
                        object instance = null;
                        try
                        {
                            instance = Activator.CreateInstance(type);
                        }
                        catch
                        {
                            continue;
                        }

                        if (instance == null)
                            continue;

                        var patcher = field.GetValue(instance) as OnSceneLoadPatch;
                        TryInvokeStartupPatch(patcher, type.Name, field.Name);
                    }
                }
            }
        }

        private static void TryInvokeStartupPatch(OnSceneLoadPatch patcher, string className, string fieldName)
        {
            if (patcher == null || !patcher.startupPatch || patcher.patchApplied) return;

            try
            {
                patcher.TryPatch();
            }
            catch (Exception ex)
            {
                KatieLogger.Error($"Failed to apply scene load patch from {className}.{fieldName}: {ex.Message}");
            }
        }

    }

    public class StagedValue<T>
    {
        private bool ready;
        private T value;

        public bool IsReady => ready;

        public StagedValue()
        {
            this.ready = false;
            this.value = default(T);
        }

        public T TakeValue()
        {
            if (!this.ready)
                throw new System.Exception("Value is not marked as ready.");

            this.ready = false;
            T val = this.value;
            this.value = default(T);
            return val;
        }

        public T PeekValue()
        {
            return this.value;
        }

        public void StageValue(T value)
        {
            this.ready = true;
            this.value = value;
        }

        public void ResetStage()
        {
            this.ready = false;
            this.value = default(T);
        }

        public void ForceReady()
        {
            this.ready = true;
        }

        public void ForceNotReady()
        {
            this.ready = false;
        }
    }
    public static class KatieUtil
    {
        // Math stuff

        public static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }


        // Reflection stuff

        public static void EditFieldAttr(FieldInfo field, FieldAttributes attributes, bool add)
        {
            if (field == null) return;

            var attrField = typeof(FieldInfo).GetField("m_fieldAttributes", BindingFlags.Instance | BindingFlags.NonPublic);
            if (attrField != null)
            {
                var currentAttributes = (FieldAttributes)attrField.GetValue(field);

                if (add)
                {
                    currentAttributes |= attributes;
                }
                else
                {
                    currentAttributes &= ~attributes;
                }

                attrField.SetValue(field, currentAttributes);
            }
        }

        // Scene stuff

        private static readonly FieldInfo transitionField = AccessTools.Field(typeof(SceneChanger), "transition");
        public static void ChangeScene(string sceneName, Transition transition = null, SceneChanger.NotifyType notifyType = SceneChanger.NotifyType.None, bool stopAudio = false, bool stopCutscenes = false)
        {
            if (sceneName == "Menu" || stopAudio)
            {
                AudioPlayback.StopAllAudio();
            }
            if (stopCutscenes)
            {
                StopAllCutscenes();
            }
            if (transition == null)
            {
                new SceneChanger(sceneName, Color.black, 1f, 1f, notifyType).GoToDestination();
            }
            else
            {
                SceneChanger customSceneChanger = new SceneChanger(sceneName, transition.Color, transition.FadeInTime, transition.FadeOutTime, notifyType);
                transitionField.SetValue(customSceneChanger, transition.Type);
                customSceneChanger.GoToDestination();
            }
        }

        // Save file stuff

        public static GameFile<SaveFileData> ReadGameFile(int index)
        {
            int clampedIndex = ClampIndex(index);
            GameFile<SaveFileData> gameFile = new GameFile<SaveFileData>(GetSaveName(clampedIndex), Encryption.DefaultEncryptor, GameFile<SaveFileData>.FileType.Encrypted);
            gameFile.ReadFile();
            return gameFile;
        }

        public static string GetSaveName(int index)
        {
            if (Application.isPlaying && SteamManager.Initialized)
            {
                return string.Format("saves/steam/{0}/save_{1}", SteamUser.GetSteamID().m_SteamID, index);
            }
            return string.Format("saves/global/save_{0}", index);
        }

        public static int ClampIndex(int index)
        {
            if (index < 0)
            {
                return Mathf.Max(MetaSaveFile.Current.SaveIndex, 0);
            }
            return index;
        }

        // Seed stuff

        private static string GetHardwareID()
        {
            if (!(SystemInfo.deviceUniqueIdentifier != "n/a"))
            {
                return SystemInfo.deviceName;
            }
            return SystemInfo.deviceUniqueIdentifier;
        }

        private static readonly FieldInfo hardwareHashField = AccessTools.Field(typeof(SaveRandomizerHashes), "HardwareHash");
        private static readonly FieldInfo sessionHashField = AccessTools.Field(typeof(SaveRandomizerHashes), "PlaySessionHash");

        public static void EditHardwareHash(int newHash, bool showToast = false)
        {
            EditFieldAttr(hardwareHashField, FieldAttributes.InitOnly, false);
            hardwareHashField.SetValue(null, newHash);
            EditFieldAttr(hardwareHashField, FieldAttributes.InitOnly, true);

            string message = $"Hardware seed changed to {newHash}";

            if (showToast)
                ToastController.TryQueueAndLogToast(message);
            else
                KatieLogger.Info(message);
        }

        public static void EditSessionHash(int newHash, bool showToast = false)
        {
            EditFieldAttr(sessionHashField, FieldAttributes.InitOnly, false);
            sessionHashField.SetValue(null, newHash);
            EditFieldAttr(sessionHashField, FieldAttributes.InitOnly, true);

            string message = $"Session seed changed to {newHash}";

            if (showToast)
                ToastController.TryQueueAndLogToast(message);
            else
                KatieLogger.Info(message);
        }

        public static void TriggerCustomEvent(CustomEventType eventType)
        {
            if (KatieSaveHelperModConfig.regenerateSessionSeed.Value == eventType)
                EditSessionHash(GenerateSessionSeed().seed);
            if (KatieSaveHelperModConfig.regenerateHardwareSeed.Value == eventType)
                EditHardwareHash(GenerateHardwareSeed().seed);
            if (KatieSaveHelperModConfig.resetBlinkRandomizer.Value == eventType)
                PlayerRandomBlink_Patch.ResetBlinkChanceGenerator();
            if (KatieSaveHelperModConfig.resetSimulatedAchievements.Value == eventType)
                Achievements_Patch.ResetSimulatedAchievements();
        }

        // Random stuff

        public static bool GetChance(int baseHash, int idHash, float odds)
        {
            return new System.Random(baseHash + idHash).NextDouble() <= (double)odds;
        }

        public static double GetChanceRollValue(int baseHash, int idHash)
        {
            return new System.Random(baseHash + idHash).NextDouble();
        }

        public static (bool success, int seed) GenerateSaveSeed()
        {
            KatieLogger.Info("Generating new Save seed...");
            switch (KatieSaveHelperModConfig.saveSeedGeneratorType.Value)
            {
                case SeedGenerator.Static:
                    return (true, KatieSaveHelperModConfig.staticSaveSeed.Value);
                case SeedGenerator.PsuedoRandom:
                    var psuedoResult = KatiePsuedoRandomizer.SaveMode.GeneratePsuedoRandomSeed();
                    if (psuedoResult.success)
                        return (true, psuedoResult.seed);
                    else
                        return (false, SaveRandomizer.GetAbsolutelyRandomValue());
                case SeedGenerator.Device:
                    return (true, GetHardwareID().GetStableHashCode());
                default:
                    return (true, SaveRandomizer.GetAbsolutelyRandomValue());
            }
        }

        public static (bool success, int seed) GenerateSessionSeed()
        {
            KatieLogger.Info("Generating new Session seed...");
            switch (KatieSaveHelperModConfig.sessionSeedGeneratorType.Value)
            {
                case SeedGenerator.Static:
                    return (false, KatieSaveHelperModConfig.staticSessionSeed.Value);
                case SeedGenerator.PsuedoRandom:
                    var psuedoResult = KatiePsuedoRandomizer.SessionMode.GeneratePsuedoRandomSeed();
                    if (psuedoResult.success)
                        return (true, psuedoResult.seed);
                    else
                        return (false, SaveRandomizer.GetAbsolutelyRandomValue());
                case SeedGenerator.Device:
                    return (true, GetHardwareID().GetStableHashCode());
                default:
                    return (true, SaveRandomizer.GetAbsolutelyRandomValue());
            }
        }

        public static (bool success, int seed) GenerateHardwareSeed()
        {
            KatieLogger.Info("Generating new Hardware seed...");
            switch (KatieSaveHelperModConfig.hardwareSeedGeneratorType.Value)
            {
                case SeedGenerator.Static:
                    return (true, KatieSaveHelperModConfig.staticHardwareSeed.Value);
                case SeedGenerator.PsuedoRandom:
                    var psuedoResult = KatiePsuedoRandomizer.HardwareMode.GeneratePsuedoRandomSeed();
                    if (psuedoResult.success)
                        return (true, psuedoResult.seed);
                    else
                        return (false, SaveRandomizer.GetAbsolutelyRandomValue());
                case SeedGenerator.Device:
                    return (true, GetHardwareID().GetStableHashCode());
                default:
                    return (true, SaveRandomizer.GetAbsolutelyRandomValue());
            }
        }

        // Color stuff

        public static Color GetColorFromHex(string hex)
        {
            if (!hex.StartsWith("#"))
                hex = "#" + hex;

            if (ColorUtility.TryParseHtmlString(hex, out Color color))
                return color;

            KatieLogger.Warning($"Failed to parse color from '{hex}', defaulting to black");
            return Color.black;
        }

        public static string GetHexFromColor(Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(color);
        }

        // Cutscene stuff

        public static void StopAllCutscenes()
        {
            // cancel all active PlayableDirectors
            var directors = Resources.FindObjectsOfTypeAll<PlayableDirector>();
            foreach (var director in directors)
            {
                if (director == null || director.state != PlayState.Playing)
                    continue;
                director.Stop();
            }

            // cancel all Yarn DialogueRunners
            foreach (var runner in GameObject.FindObjectsOfType<Yarn.Unity.DialogueRunner>())
            {
                if (runner.IsDialogueRunning)
                    runner.Stop();
            }

            // cancel HUDDialogue coroutines (kills audio/text playback)
            foreach (var hudDialogue in GameObject.FindObjectsOfType<JoelG.ENA4.UI.HUD.Dialogue.HUDDialogue>())
            {
                hudDialogue.StopAllCoroutines();
                hudDialogue.gameObject.SetActive(false);
            }
        }

    }
}
