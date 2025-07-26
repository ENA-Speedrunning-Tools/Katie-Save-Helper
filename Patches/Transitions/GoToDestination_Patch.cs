using HarmonyLib;
using JoelG.ENA4;
using static JoelG.ENA4.SceneChanger;

namespace KatieSaveHelper.Patches
{
    // Immediately complete the effects of any active scene transitions before making a scene change

    [HarmonyPatch(typeof(SceneChanger), nameof(SceneChanger.GoToDestination))]
    public static class GoToDestination_Patch
    {
        public static bool Prefix(SceneChanger __instance)
        {
            for (int i = ActiveTransitions.Count - 1; i >= 0; i--)
            {
                var tr = ActiveTransitions[i];
                if (tr == null) continue;
                tr.CompleteEffect();
            }
            return true;
        }
    }
}
