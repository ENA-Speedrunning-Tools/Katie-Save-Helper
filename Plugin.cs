using MelonLoader;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

[assembly: MelonInfo(typeof(KatieSaveHelper.KatieSaveHelperMod), "Katie Save Helper", "1.0.8", "Katelyndev0211 and Zieraell")]

namespace KatieSaveHelper
{
    public class KatieSaveHelperMod : MelonMod
    {
        internal const string modGUID = "Zieraell.KatieSaveHelper";
        internal const string modName = "Katie Save Helper";
        internal const string modVersion = "1.0.8.1";
        internal const string modAuthors = "Katelyndev0211 and Zieraell";

        private readonly HarmonyLib.Harmony harmony = new HarmonyLib.Harmony(modGUID);

        private static List<IKatieActionBase> cachedActiveActions = new List<IKatieActionBase>();

        private bool configUpdateToken = false;

        public override void OnInitializeMelon()
        {
            // Subscribe to config loads
            KatieSaveHelperModConfig.OnConfigLoaded += NotifyOnUpdate;

            KatieSaveHelperModConfig.LoadConfig();

            // Set up scene load events
            OnSceneLoadPatch.ApplyStartupPatches();

            // Set default toggle settings
            KatieSaveHelperModActions.autoSaveDisabled = KatieSaveHelperModConfig.autoSaveDisabledByDefault.Value;

            // Sync assets folder with repo
            KatieAssetHandler.OnStartup();

            KatieLogger.Info($"{modName} loaded.");
            KatieLogger.Info($"Mod by {modAuthors}");
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
        public static readonly Action<string> Info = (message) => MelonLogger.Msg(message);
        public static readonly Action<string> Warning = (message) => MelonLogger.Warning(message);
        public static readonly Action<string> Error = (message) => MelonLogger.Error(message);
    }
}