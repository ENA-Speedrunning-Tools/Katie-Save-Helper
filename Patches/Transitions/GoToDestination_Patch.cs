using HarmonyLib;
using JoelG.ENA4;

namespace KatieSaveHelper.Patches
{
    // Immediately complete the effects of any active scene transitions before making a scene change

    [HarmonyPatch(typeof(SceneChanger), nameof(SceneChanger.GoToDestination))]
    public static class GoToDestination_Patch
    {
        public static bool Prefix(SceneChanger __instance)
        {
            for (int i = SceneChanger.ActiveTransitions.Count - 1; i >= 0; i--)
            {
                var tr = SceneChanger.ActiveTransitions[i];
                if (tr == null) continue;
                tr.CompleteEffect();
            }
            return true;
        }
    }
}
