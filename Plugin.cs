using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using JoelG.ENA4;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

namespace KatieSaveHelper
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class KatieSaveHelperMod : BaseUnityPlugin
    {
        private const string modGUID = "Zieraell.KatieSaveHelper";
        private const string modName = "Katie Save Helper";
        private const string modVersion = "1.0.7.0";
        private const string modAuthors = "Katelyndev0211 and Zieraell";

        internal static KatieSaveHelperMod Instance;

        internal static bool forceCustomSeedOnReset = false;
        internal static bool forceCustomSeedOnLoad = false;
        internal static bool forceCustomTransition = false;
        internal static int customSeed;
        internal static Transition customTransition = new Transition(SceneChanger.TransitionType.FadeToColor, Color.black, 0, 0);
        internal static bool allowSave = false;

        private readonly Harmony harmony = new Harmony(modGUID);

        private static List<IKatieActionBase> cachedActiveActions = new List<IKatieActionBase>();

        internal static ManualLogSource mls;

        private const int cacheUpdateThreshold = 100;
        private int cacheUpdateCounter = 0;

        void Awake()
        {
            if (Instance == null)
                Instance = this;

            mls = Logger;

            KatieSaveHelperModConfig.LoadConfig();

            harmony.PatchAll();

            mls.LogInfo($"{modName} loaded.");
            mls.LogInfo($"Mod by {modAuthors}");
        }

        void Update()
        {
            if (++cacheUpdateCounter >= cacheUpdateThreshold)
            {
                UpdateCachedActions();
                cacheUpdateCounter = 0;
            }

            if (!Input.anyKeyDown)
            {
                return;
            }

            foreach (IKatieActionBase action in cachedActiveActions)
            {
                if (Input.GetKeyDown(action.Key))
                {
                    mls.LogInfo($"'{action.DisplayName}' key pressed");
                    action.Run();
                    return;
                }
            }
        }

        public static void setTransition(Transition transition)
        {
            customTransition = transition.Copy();
        }

        private static void UpdateCachedActions()
        {
            cachedActiveActions = KatieSaveHelperModConfig.allActiveActions.ToList();
        }
    }
}