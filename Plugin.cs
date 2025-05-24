using MelonLoader;
using UnityEngine;
using JoelG.ENA4;
using System.Collections.Generic;
using System.Linq;

[assembly: MelonInfo(typeof(KatieSaveHelper.KatieSaveHelperMod), "Katie Save Helper", "1.0.7", "Katelyndev0211 and Zieraell")]

namespace KatieSaveHelper
{
    public class KatieSaveHelperMod : MelonMod
    {
        private const string modGUID = "Zieraell.KatieSaveHelper";
        private const string modName = "Katie Save Helper";
        private const string modVersion = "1.0.7.0";
        private const string modAuthors = "Katelyndev0211 and Zieraell";

        internal static bool forceCustomSeedOnReset = false;
        internal static bool forceCustomSeedOnLoad = false;
        internal static bool forceCustomTransition = false;
        internal static int customSeed;
        internal static Transition customTransition = new Transition(SceneChanger.TransitionType.FadeToColor, Color.black, 0, 0);
        internal static bool allowSave = false;

        private readonly HarmonyLib.Harmony harmony = new HarmonyLib.Harmony(modGUID);

        private static List<IKatieActionBase> cachedActiveActions = new List<IKatieActionBase>();

        private const int cacheUpdateThreshold = 100;
        private int cacheUpdateCounter = 0;

        public override void OnInitializeMelon()
        {
            KatieSaveHelperModConfig.LoadConfig();

            MelonLogger.Msg($"{modName} loaded.");
            MelonLogger.Msg($"Mod by {modAuthors}");
        }

        public override void OnUpdate()
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
                    MelonLogger.Msg($"'{action.DisplayName}' key pressed");
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