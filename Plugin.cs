using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace KatieSaveHelper
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class KatieMain : BaseUnityPlugin
    {
        internal const string modGUID = "Zieraell.KatieSaveHelper";
        internal const string modName = "Katie Save Helper";
        internal const string modVersion = "1.9.0.0";
        internal const string modAuthors = "Katelyndev0211 and Zieraell";

        internal readonly Harmony harmony = new Harmony(modGUID);

        internal static KatieMain Instance;

        private static List<IKatieActionBase> cachedActiveActions = new List<IKatieActionBase>();

        public static ManualLogSource mls;

        private bool configUpdateToken = false;

        void Awake()
        {
            // Set up logger
            if (Instance == null)
                Instance = this;
            mls = Logger;

            // Subscribe to config loads
            KatieConfig.OnConfigLoaded += NotifyOnUpdate;

            // Subscribe to the application closing
            Application.quitting += KatieSceneWarp.WriteEntranceCache;

            // Apply harmony patches
            harmony.PatchAll();

            KatieConfig.LoadConfig();

            // Try to load the Scene Entrance cache
            KatieSceneWarp.LoadEntranceCache();

            // Set up scene load events
            OnSceneLoadPatch.ApplyStartupPatches();

            // Sync assets folder with repo
            KatieAssetHandler.OnStartup();

            mls.LogInfo($"{modName} loaded.");
            mls.LogInfo($"Mod by {modAuthors}");
        }

        void Update()
        {
            if (configUpdateToken)
            {
                UpdateCachedActions();
                configUpdateToken = false;
            }

            if (!Input.anyKeyDown)
                return;

            foreach (IKatieActionBase action in cachedActiveActions)
            {
                if (Input.GetKeyDown(action.Key))
                {
                    KatieLogger.Info($"'{action.DisplayName}' key pressed");
                    action.Run();
                    return;
                }
            }
        }

        private static void UpdateCachedActions()
        {
            cachedActiveActions = KatieConfig.allActiveActions.ToList();
        }

        private void NotifyOnUpdate()
        {
            configUpdateToken = true;
        }
    }

    internal static class KatieLogger
    {
        public static readonly Action<string> Info = (message) => KatieMain.mls.LogInfo(message);
        public static readonly Action<string> Warning = (message) => KatieMain.mls.LogWarning(message);
        public static readonly Action<string> Error = (message) => KatieMain.mls.LogError(message);
    }
}