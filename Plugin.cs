using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using JoelG.ENA4;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System;

namespace KatieSaveHelper
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class KatieSaveHelperMod : BaseUnityPlugin
    {
        internal const string modGUID = "Zieraell.KatieSaveHelper";
        internal const string modName = "Katie Save Helper";
        internal const string modVersion = "1.0.8.2";
        internal const string modAuthors = "Katelyndev0211 and Zieraell";

        private readonly Harmony harmony = new Harmony(modGUID);

        internal static KatieSaveHelperMod Instance;

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
            KatieSaveHelperModConfig.OnConfigLoaded += NotifyOnUpdate;

            KatieSaveHelperModConfig.LoadConfig();

            // Apply harmony patches
            harmony.PatchAll();

            // Set up scene load events
            OnSceneLoadPatch.ApplyStartupPatches();

            // Set default toggle settings
            KatieSaveHelperModActions.autoSaveDisabled = KatieSaveHelperModConfig.autoSaveDisabledByDefault.Value;

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
            cachedActiveActions = KatieSaveHelperModConfig.allActiveActions.ToList();
        }

        private void NotifyOnUpdate()
        {
            configUpdateToken = true;
        }
    }

    internal static class KatieLogger
    {
        public static readonly Action<string> Info = (message) => KatieSaveHelperMod.mls.LogInfo(message);
        public static readonly Action<string> Warning = (message) => KatieSaveHelperMod.mls.LogWarning(message);
        public static readonly Action<string> Error = (message) => KatieSaveHelperMod.mls.LogError(message);
    }
}