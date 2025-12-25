using MelonLoader;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

[assembly: MelonInfo(typeof(KatieSaveHelper.KatieMain), "Katie Save Helper", "1.9.0", "Katelyndev0211 and Zieraell")]

namespace KatieSaveHelper
{
    public class KatieMain : MelonMod
    {
        internal const string modGUID = "Zieraell.KatieSaveHelper";
        internal const string modName = "Katie Save Helper";
        internal const string modVersion = "1.9.0.0";
        internal const string modAuthors = "Katelyndev0211 and Zieraell";

        internal static readonly HarmonyLib.Harmony harmony = new HarmonyLib.Harmony(modGUID);

        private static List<IKatieActionBase> cachedActiveActions = new List<IKatieActionBase>();

        private bool configUpdateToken = false;

        public override void OnInitializeMelon()
        {
            // Subscribe to config loads
            KatieConfig.OnConfigLoaded += NotifyOnUpdate;

            // Subscribe to the application closing
            Application.quitting += KatieSceneWarp.WriteEntranceCache;

            KatieConfig.LoadConfig();

            // Try to load the Scene Entrance cache
            KatieSceneWarp.LoadEntranceCache();

            // Set up scene load events
            OnSceneLoadPatch.ApplyStartupPatches();

            // Sync assets folder with repo
            KatieAssetHandler.OnStartup();
        }

        public override void OnUpdate()
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

        private static void UpdateCachedActions() =>
            cachedActiveActions = KatieConfig.allActiveActions.ToList();

        private void NotifyOnUpdate() =>
            configUpdateToken = true;

    }

    internal static class KatieLogger
    {
        public static readonly Action<string> Info = (message) => MelonLogger.Msg(message);
        public static readonly Action<string> Warning = (message) => MelonLogger.Warning(message);
        public static readonly Action<string> Error = (message) => MelonLogger.Error(message);
    }
}