using JoelG.ENA4.UI;
using HarmonyLib;
using System.Threading;

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

        public static bool Prefix(MainMenuPanelGroup __instance)
        {
            __instance.ReloadInitialItems();
            __instance.DisableAllItems();

            int panelId;

            if (KatieSaveHelperModActions.customMainMenuPanelOnLoad.IsReady)
            {
                panelId = (int)KatieSaveHelperModActions.customMainMenuPanelOnLoad.TakeValue();
            }
            else
            {
                switch (KatieSaveHelperModConfig.skipMainMenuIntro.Value)
                {
                    case MainMenuSkipType.OnStartup:
                        panelId = (int)(menuLoadedOnce ? MainMenuPanelType.Intro : MainMenuPanelType.Main);
                        break;
                    case MainMenuSkipType.Always:
                        panelId = (int)MainMenuPanelType.Main;
                        break;
                    default:
                        panelId = (int)MainMenuPanelType.Intro;
                        break;
                }
            }

            __instance.SetPanelImmediately(panelId);

            if (menuLoadedOnce)
                KatieUtil.TriggerCustomEvent(CustomEventType.OnLoadMainMenu);

            menuLoadedOnce = true;

            return false;
        }
    }
}
