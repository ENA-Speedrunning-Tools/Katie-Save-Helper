using HarmonyLib;
using JoelG.ENA4;
using System.Collections;

namespace KatieSaveHelper.Patches
{

    // Force anything that makes use of the vanilla "Static Coroutine" system to use the mod's custom "Static Coroutine" system

    [HarmonyPatch(typeof(StaticCoroutineRunner), nameof(StaticCoroutineRunner.StartCoroutineOnInstance))]
    public static class StaticCoroutineRunner_Patch
    {
        public static bool Prefix(IEnumerator enumerator)
        {
            StaticCoroutine.Start(enumerator);
            return false;
        }
    }
}
