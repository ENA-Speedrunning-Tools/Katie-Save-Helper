using JoelG.ENA4;
using HarmonyLib;
using UnityEngine;

namespace KatieSaveHelper.Patches
{
    // Slightly reduce the 'sorting order' of a fade transition's canvas component to allow other UI elements to overlay on top of it

    [HarmonyPatch(typeof(SceneChanger), "InitializeTransition")]
    public static class InitializeTransition_Patch
    {
        private static void Postfix(GameObject transitionObject)
        {
            if (transitionObject == null) return;
            foreach (var canvas in transitionObject.GetComponentsInChildren<Canvas>(true))
                if (canvas.sortingOrder == 32767)
                    canvas.sortingOrder = 32600;
        }
    }
}
