using JoelG.ENA4;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine.SceneManagement;
using HarmonyLib;
using Newtonsoft.Json;
using KatieSaveHelper.Patches;

namespace KatieSaveHelper.Features.Util
{
    public static class KatieSceneWarp
    {
        private static readonly FieldInfo sceneChangerField = AccessTools.Field(typeof(SceneTransition), "sceneChanger");
        private static readonly FieldInfo useDestinationIndexField = AccessTools.Field(typeof(SceneChanger), "useDestinationIndex");
        private static readonly FieldInfo destinationIndexField = AccessTools.Field(typeof(SceneChanger), "destinationIndex");
        private static readonly FieldInfo destinationNameField = AccessTools.Field(typeof(SceneChanger), "destinationName");
        private static readonly FieldInfo entranceFlagField = AccessTools.Field(typeof(SceneChanger), "entranceFlag");

        private static Dictionary<string, List<string>> sceneEntranceCache = new Dictionary<string, List<string>>();

        private static readonly OnSceneLoadPatch oslPatcher = new OnSceneLoadPatch(FindEntrances, patchOnStartup: true);

        private static FileHandler cacheFile = new FileHandler(Path.Combine(KatieUtil.appDataDir, "SceneEntrance_Cache.json"), "KSH.Event.SceneEntranceCache");

        private static HashSet<string> filteredScenesList = new HashSet<string>
        {
            "Boot",
            "Menu"
        };

        public static List<string> SceneNamesByBuildIndex
        {
            get
            {
                int count = SceneManager.sceneCountInBuildSettings;
                List<string> list = new List<string>(count);

                for (int i = 0; i < count; i++)
                    list.Add(GetSceneNameByBuildIndex(i));

                return list;
            }
        }

        public static List<string> CurrentlyLoadedEntranceFlags
        {
            get
            {
                var entrances = UnityEngine.Object.FindObjectsOfType<SceneEntrance>();
                List<string> flags = new List<string>();

                foreach (var entrance in entrances)
                {
                    if (entrance == null)
                        continue;

                    flags.Add(entrance.EntranceFlag);
                }

                flags.Sort(StringComparer.OrdinalIgnoreCase);
                return flags;
            }
        }

        public static (string name, string entranceFlag) RelativeSceneInfo
        {
            get
            {
                if (SceneChanger.IsBusy)
                {
                    try
                    {
                        var sceneTransition = SceneChanger.ActiveTransitions[0];
                        var sceneChanger = (SceneChanger)sceneChangerField.GetValue(sceneTransition);
                        string entranceFlag = (string)entranceFlagField.GetValue(sceneChanger);
                        bool useDestinationIndex = (bool)useDestinationIndexField.GetValue(sceneChanger);
                        if (useDestinationIndex)
                        {
                            int destinationIndex = (int)destinationIndexField.GetValue(sceneChanger);
                            string destinationName = SceneUtility.GetScenePathByBuildIndex(destinationIndex);
                            return (destinationName, entranceFlag);
                        }
                        else
                        {
                            string destinationName = (string)destinationNameField.GetValue(sceneChanger);
                            return (destinationName, entranceFlag);
                        }
                    }
                    catch (Exception ex)
                    {
                        KatieLogger.Error($"Error when attempting to obtain relative Scene info : {ex}");
                        KatieLogger.Error("Falling back to currently active scene");
                    }
                }
                return (SceneManager.GetActiveScene().name, SceneChanger.ActiveEntranceFlag);
            }
        }

        private static string GetSceneNameByBuildIndex(int index)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(index);
            return Path.GetFileNameWithoutExtension(path);
        }

        public static void LoadEntranceCache()
        {
            if (!File.Exists(cacheFile.filePath))
            {
                KatieLogger.Warning("No scene entrance cache found");
                return;
            }

            cacheFile.RunWithFile(
                "KSH.Event.SceneEntranceCache.Load",
                FileMode.Open,
                FileAccess.Read,
                fileAction: () =>
                {
                    if (!File.Exists(cacheFile.filePath))
                    {
                        if (KatieConfig.Settings.debugMode.Value) KatieLogger.Warning("No scene entrance cache found");
                        return;
                    }

                    string json = File.ReadAllText(cacheFile.filePath);
                    var cache = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
                    if (cache == null)
                    {
                        KatieLogger.Error("Failed to de-serialize cached scene entrance data");
                        return;
                    }

                    sceneEntranceCache = cache;
                    if (KatieConfig.Settings.debugMode.Value) KatieLogger.Info("Successfully loaded cached scene entrance data");
                },
                onFail : () => KatieLogger.Error($"Failed to load cached scene entrance data"),
                async: true
             );
        }

        public static void WriteEntranceCache()
        {
            cacheFile.RunWithFile(
                "KSH.Event.SceneEntranceCache.Write",
                FileMode.OpenOrCreate,
                FileAccess.Write,
                fileAction: () =>
                {
                    string json = JsonConvert.SerializeObject(sceneEntranceCache, Formatting.Indented);
                    File.WriteAllText(cacheFile.filePath, json);
                    if (KatieConfig.Settings.debugMode.Value) KatieLogger.Info("Successfully updated cached scene entrance data");
                },
                onFail: () => KatieLogger.Error($"Failed to update cached scene entrance data")
            );
        }

        private static void FindEntrances(Scene scene, LoadSceneMode mode)
        {
            if (filteredScenesList.Contains(scene.name)) return;
            sceneEntranceCache[scene.name] = CurrentlyLoadedEntranceFlags;
        }

        public static string WarpNextScene()
        {
            int total = SceneNamesByBuildIndex.Count;
            int current = SceneUtility.GetBuildIndexByScenePath(RelativeSceneInfo.name);

            for (int i = 1; i <= total; i++)
            {
                int index = (current + i) % total;
                string sceneName = SceneNamesByBuildIndex[index];

                if (!filteredScenesList.Contains(sceneName))
                {
                    KatieUtil.ChangeScene(
                        sceneName,
                        transition: KatieConfig.Settings.baseTransition,
                        notifyType: SceneChanger.NotifyType.Notify,
                        origin: KatieSceneChanger.Origin.Manual,
                        stopAudio: true,
                        stopCutscenes: true
                    );
                    return sceneName;
                }
            }

            KatieLogger.Warning("No valid next scene found");
            return null;
        }

        public static string WarpPrevScene()
        {
            int total = SceneNamesByBuildIndex.Count;
            int current = SceneUtility.GetBuildIndexByScenePath(RelativeSceneInfo.name);

            for (int i = 1; i <= total; i++)
            {
                int index = (current - i + total) % total;
                string sceneName = SceneNamesByBuildIndex[index];

                if (!filteredScenesList.Contains(sceneName))
                {
                    KatieUtil.ChangeScene(
                        sceneName,
                        transition: KatieConfig.Settings.baseTransition,
                        notifyType: SceneChanger.NotifyType.Notify,
                        origin: KatieSceneChanger.Origin.Manual,
                        stopAudio: true,
                        stopCutscenes: true
                    );
                    return sceneName;
                }
            }

            KatieLogger.Warning("No valid previous scene found");
            return null;
        }

        public static (int returnCode, string targetName) WarpNextEntrance()
        {
            var sceneInfo = RelativeSceneInfo;
            if (!sceneEntranceCache.ContainsKey(sceneInfo.name))
            {
                KatieLogger.Warning($"Entrance flags for scene '{sceneInfo.name}' are unknown, the scene must be loaded first to discover them");
                return (1, null);
            }

            var flags = sceneEntranceCache[sceneInfo.name];
            if (flags.Count == 0) return (2, null);

            int index = flags.IndexOf(sceneInfo.entranceFlag);
            int nextIndex = index < 0 ? 0 : (index + 1) % flags.Count;

            string target = flags[nextIndex];

            KatieUtil.ChangeScene(
                sceneInfo.name,
                target,
                transition: KatieConfig.Settings.baseTransition,
                notifyType: SceneChanger.NotifyType.Notify,
                origin: KatieSceneChanger.Origin.Manual,
                stopAudio: true,
                stopCutscenes: true
            );

            return (0, target);
        }

        public static (int returnCode, string targetName) WarpPrevEntrance()
        {
            var sceneInfo = RelativeSceneInfo;
            if (!sceneEntranceCache.ContainsKey(sceneInfo.name))
            {
                KatieLogger.Warning($"Entrance flags for scene '{sceneInfo.name}' are unknown, the scene must be loaded first to discover them");
                return (1, null);
            }

            var flags = sceneEntranceCache[sceneInfo.name];
            if (flags.Count == 0) return (2, null);

            int index = flags.IndexOf(sceneInfo.entranceFlag);
            int prevIndex = index < 0 ? 0 : (index - 1 + flags.Count) % flags.Count;

            string target = flags[prevIndex];

            KatieUtil.ChangeScene(
                sceneInfo.name,
                target,
                transition: KatieConfig.Settings.baseTransition,
                notifyType: SceneChanger.NotifyType.Notify,
                origin: KatieSceneChanger.Origin.Manual,
                stopAudio: true,
                stopCutscenes: true
            );

            return (0, target);
        }
    }
}
