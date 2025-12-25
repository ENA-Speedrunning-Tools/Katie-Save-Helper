using JoelG.ENA4.UI;
using HarmonyLib;

namespace KatieSaveHelper.Patches
{
    public enum MainMenuPanelType : int
    {
        Intro = 0,
        Title = 1,
        FileSelect = 2,
        Main = 3,
        Options = 4,
        Extras = 5,
        Supporter = 6,
        Credits = 7,
        Null = 8
    }

    // When the Main Menu is loaded, skip the intro cutscene and title screen if configured by the mod, or load a specific screen if requested by a mod action

    [HarmonyPatch(typeof(MainMenuPanelGroup), "Start")]
    public static class MainMenuPanelGroup_Patch
    {
        public static bool menuLoadedOnce { get; private set; } = false;

        public static readonly string menuEventTriggerRoutineIdentifier = "KSH.Event.TriggerCustomEvent.OnMainMenuLoad";

        public static bool Prefix(MainMenuPanelGroup __instance)
        {
            __instance.ReloadInitialItems();
            __instance.DisableAllItems();

            MainMenuPanelType panelType;

            if (KatieActions.customMainMenuPanelOnLoad.IsReady)
            {
                panelType = KatieActions.customMainMenuPanelOnLoad.TakeValue();
            }
            else
            {
                switch (KatieConfig.Settings.skipMainMenuIntro.Value)
                {
                    case MainMenuSkipType.OnStartup:
                        panelType = menuLoadedOnce ? MainMenuPanelType.Intro : MainMenuPanelType.Main;
                        break;
                    case MainMenuSkipType.Always:
                        panelType = MainMenuPanelType.Main;
                        break;
                    default:
                        panelType = MainMenuPanelType.Intro;
                        break;
                }
            }

            __instance.SetPanelImmediately((int)panelType);

            if (menuLoadedOnce)
                StartNewMenuEventTrigger();
            else
                menuLoadedOnce = true;

            return false;
        }

        public static void StartNewMenuEventTrigger()
        {
            StaticCoroutine.Start(sc => KatieUtil.TriggerCustomEventRoutine(CustomEventType.OnLoadMainMenu, sc), menuEventTriggerRoutineIdentifier);
        }
    }
}
