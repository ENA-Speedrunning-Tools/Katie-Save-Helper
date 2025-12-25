using JoelG.ENA4.Audio;
using JoelG.ENA4;
using KatieSaveHelper.Patches;
using LMirman.Utilities;
using Steamworks;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine;
using HarmonyLib;
using System.Security.Cryptography;
using System;
using System.IO;
using UnityEngine.Events;
using System.Collections.Generic;

namespace KatieSaveHelper
{
    public enum SeedType
    {
        Save,
        Session,
        Hardware
    }

    public enum SeedGeneratorReturnCode
    {
        Success = 0,
        TaskCancelled = 1,
        SeedNotFound = 2,
        OtherError = 3
    }

    public static class KatieUtil
    {
        public static readonly string dllDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static readonly string appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), KatieMain.modGUID);

        // File stuff

        public static bool CanWriteHere(string dir, bool cleanAfter = true)
        {
            bool dirExisted = Directory.Exists(dir);
            string testFile = Path.Combine(dir, Guid.NewGuid().ToString() + ".tmp");

            try
            {
                if (!dirExisted)
                    Directory.CreateDirectory(dir);

                using (File.Create(testFile, 1, FileOptions.DeleteOnClose)) { }

                return true;
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
            finally
            {
                if (cleanAfter && !dirExisted && Directory.Exists(dir))
                {
                    try { Directory.Delete(dir, recursive: false); } catch { }
                }
            }

            return false;
        }

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

        public enum UnityEventListenerType
        {
            Runtime,
            Persistent,
            Both
        }

        public static void RemoveListenerFromUnityEvent(UnityEvent unityEvent, object target, MethodInfo method, UnityEventListenerType callType)
        {
            UnityEventBase unityEventBase = unityEvent;

            if (callType == UnityEventListenerType.Runtime || callType == UnityEventListenerType.Both)
            {
                var callsField = typeof(UnityEventBase).GetField("m_Calls", BindingFlags.NonPublic | BindingFlags.Instance);
                var invokableCallList = callsField.GetValue(unityEventBase);

                var runtimeListField = invokableCallList.GetType().GetField("m_RuntimeCalls", BindingFlags.NonPublic | BindingFlags.Instance);
                var runtimeList = runtimeListField.GetValue(invokableCallList) as IList;

                for (int i = runtimeList.Count - 1; i >= 0; i--)
                {
                    var invokable = runtimeList[i];
                    var delegateField = invokable.GetType().GetField("Delegate", BindingFlags.NonPublic | BindingFlags.Instance);

                    if (delegateField != null)
                    {
                        var del = delegateField.GetValue(invokable) as Delegate;

                        if (del != null && del.Method == method && del.Target == target)
                        {
                            runtimeList.RemoveAt(i);
                        }
                    }
                }
            }

            if (callType == UnityEventListenerType.Persistent || callType == UnityEventListenerType.Both)
            {
                var persistentField = typeof(UnityEventBase).GetField("m_PersistentCalls", BindingFlags.NonPublic | BindingFlags.Instance);
                var persistentCallGroup = persistentField.GetValue(unityEventBase);

                var callsListField = persistentCallGroup.GetType().GetField("m_Calls", BindingFlags.NonPublic | BindingFlags.Instance);
                var persistentCalls = callsListField.GetValue(persistentCallGroup) as IList;

                for (int i = persistentCalls.Count - 1; i >= 0; i--)
                {
                    var call = persistentCalls[i];
                    var targetField = call.GetType().GetField("m_Target", BindingFlags.NonPublic | BindingFlags.Instance);
                    var methodNameField = call.GetType().GetField("m_MethodName", BindingFlags.NonPublic | BindingFlags.Instance);

                    var t = targetField.GetValue(call);
                    var methodName = (string)methodNameField.GetValue(call);

                    if (t == target && methodName == method.Name)
                    {
                        persistentCalls.RemoveAt(i);
                    }
                }
            }
        }

        // Coroutine stuff

        public static IEnumerator WaitForTask(Task task, Action<float> onCompleted = null)
        {
            float elapsedTime = 0f;

            while (!task.IsCompleted)
            {
                yield return null;
                elapsedTime += Time.deltaTime;
            }

            onCompleted?.Invoke(elapsedTime);

            if (task.IsFaulted)
                throw task.Exception;
        }

        public static IEnumerator WaitForFrames(int count)
        {
            for (int i = 0; i < count; i++)
                yield return null;
        }

        // Scene stuff

        private static readonly FieldInfo transitionField = AccessTools.Field(typeof(SceneChanger), "transition");
        public static void ChangeScene(string sceneName, string entranceFlag = null, Transition? transition = null, SceneChanger.NotifyType notifyType = SceneChanger.NotifyType.None, KatieSceneChanger.Origin origin = KatieSceneChanger.Origin.Natural, bool stopAudio = false, bool stopCutscenes = false)
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
                transition = new Transition(SceneChanger.TransitionType.FadeToColor, Color.black, 1f, 1f);

            SceneChanger sceneChanger = new KatieSceneChanger(sceneName, transition: transition.Value, notifyType, origin);
            if (entranceFlag != null)
                sceneChanger.SetDestination(sceneName, entranceFlag);
            sceneChanger.GoToDestination();
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

        public static void EditHardwareHash(int newHash)
        {
            EditFieldAttr(hardwareHashField, FieldAttributes.InitOnly, false);
            hardwareHashField.SetValue(null, newHash);
            EditFieldAttr(hardwareHashField, FieldAttributes.InitOnly, true);

            KatieLogger.Info($"Hardware seed changed to {newHash}");
        }

        public static void EditSessionHash(int newHash)
        {
            EditFieldAttr(sessionHashField, FieldAttributes.InitOnly, false);
            sessionHashField.SetValue(null, newHash);
            EditFieldAttr(sessionHashField, FieldAttributes.InitOnly, true);

            KatieLogger.Info($"Session seed changed to {newHash}");
        }

        public struct CustomEventGenerationResult
        {
            public CustomEventType eventType;
            public (SeedGeneratorReturnCode returnCode, int seed)? session;
            public (SeedGeneratorReturnCode returnCode, int seed)? hardware;

            public SeedGeneratorReturnCode HighestReturnCode
            {
                get
                {
                    int highestCode = (int)SeedGeneratorReturnCode.Success;

                    if (session.HasValue)
                        highestCode = Mathf.Max(highestCode, (int)session.Value.returnCode);

                    if (hardware.HasValue)
                        highestCode = Mathf.Max(highestCode, (int)hardware.Value.returnCode);

                    return (SeedGeneratorReturnCode)highestCode;
                }
            }

            public CustomEventGenerationResult(CustomEventType eventType)
            {
                this.eventType = eventType;
                session = null;
                hardware = null;
            }
        }

        public static async Task<CustomEventGenerationResult> GenerateCustomEventSeedsAsync(CustomEventType eventType, CancellationToken token = default)
        {
            var result = new CustomEventGenerationResult(eventType);
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token);

            Task<(SeedGeneratorReturnCode code, int seed)> sessionTask =
                KatieConfig.Settings.regenerateSessionSeed.Value == eventType
                ? GenerateSessionSeed(linkedCts.Token)
                : null;

            Task<(SeedGeneratorReturnCode code, int seed)> hardwareTask =
                KatieConfig.Settings.regenerateHardwareSeed.Value == eventType
                ? GenerateHardwareSeed(linkedCts.Token)
                : null;

            var tasks = new List<Task<(SeedGeneratorReturnCode, int)>>();
            if (sessionTask != null) tasks.Add(sessionTask);
            if (hardwareTask != null) tasks.Add(hardwareTask);

            if (tasks.Count == 0)
                return result;

            var first = await Task.WhenAny(tasks);
            var firstResult = await first;

            if (first == sessionTask)
                result.session = firstResult;
            else
                result.hardware = firstResult;

            // If first failed, cancel the other task
            if (firstResult.Item1 != SeedGeneratorReturnCode.Success)
                linkedCts.Cancel();

            // Await remaining tasks
            if (sessionTask != null && sessionTask != first)
            {
                try { result.session = await sessionTask; } catch { }
            }

            if (hardwareTask != null && hardwareTask != first)
            {
                try { result.hardware = await hardwareTask; } catch { }
            }

            return result;
        }

        public static void ApplyCustomEventResults(CustomEventGenerationResult result)
        {
            if (result.session.HasValue)
                EditSessionHash(result.session.Value.seed);

            if (result.hardware.HasValue)
                EditHardwareHash(result.hardware.Value.seed);

            if (KatieConfig.Settings.resetBlinkRandomizer.Value == result.eventType)
                PlayerRandomBlink_Patch.ResetBlinkChanceGenerator();

            if (KatieConfig.Settings.resetSimulatedAchievements.Value == result.eventType)
                Achievements_Patch.ResetSimulatedAchievements();
        }

        public static async Task TriggerCustomEventAsync(CustomEventType eventType, CancellationToken token = default)
        {
            Task<(SeedGeneratorReturnCode returnCode, int seed)> sessionTask = null;
            Task<(SeedGeneratorReturnCode returnCode, int seed)> hardwareTask = null;

            if (KatieConfig.Settings.regenerateSessionSeed.Value == eventType)
                sessionTask = GenerateSessionSeed(token);
            if (KatieConfig.Settings.regenerateHardwareSeed.Value == eventType)
                hardwareTask = GenerateHardwareSeed(token);

            if (sessionTask != null)
            {
                var sessionResult = await sessionTask;
                if (token.IsCancellationRequested) return;
                EditSessionHash(sessionResult.seed);
            }

            if (hardwareTask != null)
            {
                var hardwareResult = await hardwareTask;
                if (token.IsCancellationRequested) return;
                EditHardwareHash(hardwareResult.seed);
            }

            if (KatieConfig.Settings.resetBlinkRandomizer.Value == eventType)
                PlayerRandomBlink_Patch.ResetBlinkChanceGenerator();
            if (KatieConfig.Settings.resetSimulatedAchievements.Value == eventType)
                Achievements_Patch.ResetSimulatedAchievements();
        }

        public static IEnumerator TriggerCustomEventRoutine(CustomEventType eventType, StaticCoroutine scWrapper)
        {
            if (StaticCoroutine.AnyActive(sc => sc.Identifier == SaveRandomizerHashes_Patch.seedInjectRoutineIdentifier)) yield break;

            yield return StaticCoroutine.WaitForCancelDupes(scWrapper, scWrapper.CancelToken);
            if (scWrapper.CancelToken.IsCancellationRequested) yield break;

            Task<(SeedGeneratorReturnCode returnCode, int seed)> sessionTask = null;
            Task<(SeedGeneratorReturnCode returnCode, int seed)> hardwareTask = null;

            if (KatieConfig.Settings.regenerateSessionSeed.Value == eventType)
                sessionTask = GenerateSessionSeed(scWrapper.CancelToken);
            if (KatieConfig.Settings.regenerateHardwareSeed.Value == eventType)
                hardwareTask = GenerateHardwareSeed(scWrapper.CancelToken);

            if (sessionTask != null)
            {
                yield return WaitForTask(sessionTask);
                if (scWrapper.CancelToken.IsCancellationRequested) yield break;
                EditSessionHash(sessionTask.Result.seed);
            }

            if (hardwareTask != null)
            {
                yield return WaitForTask(hardwareTask);
                if (scWrapper.CancelToken.IsCancellationRequested) yield break;
                EditHardwareHash(hardwareTask.Result.seed);
            }

            if (KatieConfig.Settings.resetBlinkRandomizer.Value == eventType)
                PlayerRandomBlink_Patch.ResetBlinkChanceGenerator();
            if (KatieConfig.Settings.resetSimulatedAchievements.Value == eventType)
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

        public static int GetRandomInt32()
        {
            byte[] bytes = new byte[4];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        public static async Task<(SeedGeneratorReturnCode returnCode, int seed)> GenerateSeed(SeedType type, CancellationToken token = default)
        {
            KatieLogger.Info($"Generating new {type} seed...");

            KatiePsuedoRandomizerBase randomizer;
            KatieSetting<SeedGenerator> generatorSetting;
            KatieSetting<int> staticSeedSetting;

            switch (type)
            {
                default:
                    randomizer = KatiePsuedoRandomizer.SaveMode;
                    generatorSetting = KatieConfig.Settings.saveSeedGeneratorType;
                    staticSeedSetting = KatieConfig.Settings.staticSaveSeed;
                    break;
                case SeedType.Session:
                    randomizer = KatiePsuedoRandomizer.SessionMode;
                    generatorSetting = KatieConfig.Settings.sessionSeedGeneratorType;
                    staticSeedSetting = KatieConfig.Settings.staticSessionSeed;
                    break;
                case SeedType.Hardware:
                    randomizer = KatiePsuedoRandomizer.HardwareMode;
                    generatorSetting = KatieConfig.Settings.hardwareSeedGeneratorType;
                    staticSeedSetting = KatieConfig.Settings.staticHardwareSeed;
                    break;
            }

            switch (generatorSetting.Value)
            {
                case SeedGenerator.Static:
                    return (SeedGeneratorReturnCode.Success, staticSeedSetting.Value);
                case SeedGenerator.PsuedoRandom:
                    try
                    {
                        var task = randomizer.GeneratePsuedoRandomSeed(token);
                        await task.Await();
                        if (task.Result.success)
                            return (SeedGeneratorReturnCode.Success, task.Result.seed);
                        else
                            return (SeedGeneratorReturnCode.SeedNotFound, SaveRandomizer.GetAbsolutelyRandomValue());
                    }
                    catch (TaskCanceledException)
                    {
                        return (SeedGeneratorReturnCode.TaskCancelled, SaveRandomizer.GetAbsolutelyRandomValue());
                    }
                case SeedGenerator.Device:
                    return (SeedGeneratorReturnCode.Success, GetHardwareID().GetStableHashCode());
                default:
                    return (SeedGeneratorReturnCode.Success, SaveRandomizer.GetAbsolutelyRandomValue());
            }
        }

        public static async Task<(SeedGeneratorReturnCode returnCode, int seed)> GenerateSaveSeed(CancellationToken token = default)
        {
            KatieLogger.Info("Generating new Save seed...");
            switch (KatieConfig.Settings.saveSeedGeneratorType.Value)
            {
                case SeedGenerator.Static:
                    return (SeedGeneratorReturnCode.Success, KatieConfig.Settings.staticSaveSeed.Value);
                case SeedGenerator.PsuedoRandom:
                    try
                    {
                        var task = KatiePsuedoRandomizer.SaveMode.GeneratePsuedoRandomSeed(token);
                        await task.Await();
                        if (task.Result.success)
                            return (SeedGeneratorReturnCode.Success, task.Result.seed);
                        else
                            return (SeedGeneratorReturnCode.SeedNotFound, SaveRandomizer.GetAbsolutelyRandomValue());
                    }
                    catch (TaskCanceledException)
                    {
                        return (SeedGeneratorReturnCode.TaskCancelled, SaveRandomizer.GetAbsolutelyRandomValue());
                    }
                case SeedGenerator.Device:
                    return (SeedGeneratorReturnCode.Success, GetHardwareID().GetStableHashCode());
                default:
                    return (SeedGeneratorReturnCode.Success, SaveRandomizer.GetAbsolutelyRandomValue());
            }
        }

        public static async Task<(SeedGeneratorReturnCode returnCode, int seed)> GenerateSessionSeed(CancellationToken token = default)
        {
            KatieLogger.Info("Generating new Session seed...");
            switch (KatieConfig.Settings.sessionSeedGeneratorType.Value)
            {
                case SeedGenerator.Static:
                    return (SeedGeneratorReturnCode.Success, KatieConfig.Settings.staticSessionSeed.Value);
                case SeedGenerator.PsuedoRandom:
                    try
                    {
                        var task = KatiePsuedoRandomizer.SessionMode.GeneratePsuedoRandomSeed(token);
                        await task.Await();
                        if (task.Result.success)
                            return (SeedGeneratorReturnCode.Success, task.Result.seed);
                        else
                            return (SeedGeneratorReturnCode.SeedNotFound, SaveRandomizer.GetAbsolutelyRandomValue());
                    }
                    catch (TaskCanceledException)
                    {
                        return (SeedGeneratorReturnCode.TaskCancelled, SaveRandomizer.GetAbsolutelyRandomValue());
                    }
                case SeedGenerator.Device:
                    return (SeedGeneratorReturnCode.Success, GetHardwareID().GetStableHashCode());
                default:
                    return (SeedGeneratorReturnCode.Success, SaveRandomizer.GetAbsolutelyRandomValue());
            }
        }

        public static async Task<(SeedGeneratorReturnCode returnCode, int seed)> GenerateHardwareSeed(CancellationToken token = default)
        {
            KatieLogger.Info("Generating new Hardware seed...");
            switch (KatieConfig.Settings.hardwareSeedGeneratorType.Value)
            {
                case SeedGenerator.Static:
                    return (SeedGeneratorReturnCode.Success, KatieConfig.Settings.staticHardwareSeed.Value);
                case SeedGenerator.PsuedoRandom:
                    try
                    {
                        var task = KatiePsuedoRandomizer.HardwareMode.GeneratePsuedoRandomSeed(token);
                        await task.Await();
                        if (task.Result.success)
                            return (SeedGeneratorReturnCode.Success, task.Result.seed);
                        else
                            return (SeedGeneratorReturnCode.SeedNotFound, SaveRandomizer.GetAbsolutelyRandomValue());
                    }
                    catch (TaskCanceledException)
                    {
                        return (SeedGeneratorReturnCode.TaskCancelled, SaveRandomizer.GetAbsolutelyRandomValue());
                    }
                case SeedGenerator.Device:
                    return (SeedGeneratorReturnCode.Success, GetHardwareID().GetStableHashCode());
                default:
                    return (SeedGeneratorReturnCode.Success, SaveRandomizer.GetAbsolutelyRandomValue());
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
            bool debugMode = KatieConfig.Settings.debugMode.Value;
            if (debugMode) KatieLogger.Info($"Stopping cutscenes...");

            // cancel any active Playable Directors
            var directors = Resources.FindObjectsOfTypeAll<PlayableDirector>();
            foreach (var director in directors)
            {
                if (director == null || director.state != PlayState.Playing)
                    continue;
                director.Evaluate();
                director.Stop();
                if (debugMode) KatieLogger.Info($"Stopped playable director '{director.name}'");
            }

            // cancel any active Yarn Dialogue Runners
            foreach (var runner in GameObject.FindObjectsOfType<Yarn.Unity.DialogueRunner>())
            {
                if (runner.IsDialogueRunning)
                {
                    runner.Stop();
                    if (debugMode) KatieLogger.Info($"Stopped dialogue runner '{runner.name}'");
                }
            }

            // cancel any HUDDialogue coroutines (kills audio/text playback)
            foreach (var hudDialogue in GameObject.FindObjectsOfType<JoelG.ENA4.UI.HUD.Dialogue.HUDDialogue>())
            {
                hudDialogue.StopAllCoroutines();
                hudDialogue.gameObject.SetActive(false);
            }

            // cancel any Froggy Call animation coroutines
            StaticCoroutine.StopAll(sc => sc.Identifier.Contains("PreEffectAnticipation"));
        }

        // Game object stuff

        public static GameObject FindGameObjectByPath(string path)
        {
            string[] parts = path.Split('/');

            foreach (GameObject root in UnityEngine.SceneManagement.SceneManager
                .GetActiveScene().GetRootGameObjects())
            {
                if (root.name == parts[0])
                {
                    Transform t = root.transform;
                    for (int i = 1; i < parts.Length; i++)
                    {
                        t = t.Find(parts[i]);
                        if (t == null)
                            return null;
                    }
                    return t.gameObject;
                }
            }

            return null;
        }

        public static IEnumerator DelayedStateChange(GameObject target, Action<GameObject> action, int frameDelay)
        {
            for (int i = 0; i < frameDelay; i++)
                yield return null;

            if (target != null)
                action(target);
        }
    }
}
