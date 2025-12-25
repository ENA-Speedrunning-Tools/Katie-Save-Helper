using HarmonyLib;
using JoelG.ENA4;
using System.Reflection;
using UnityEngine;

namespace KatieSaveHelper.Patches
{
    // Ensure the static properties of SaveFile account for the 'current' field potentially being null

    [HarmonyPatch(typeof(SaveFile))]
    public static class SaveFile_Patch
    {
        private static readonly FieldInfo currentField = AccessTools.Field(typeof(SaveFile), "current");

        [HarmonyPatch("get_CurrentSave")]
        [HarmonyPrefix]
        public static bool get_CurrentSave_Prefix(ref SaveFileData __result)
        {
            if (SaveFile.HasData)
            {
                var current = (RemoteGameFile<SaveFileData>)currentField.GetValue(null);
                __result = current?.Data;
            }
            else
            {
                __result = null;
            }
            return false;
        }

        [HarmonyPatch("get_EngineTimeSinceLastSave")]
        [HarmonyPrefix]
        public static bool get_EngineTimeSinceLastSave_Prefix(ref float __result)
        {
            if (SaveFile.HasData)
            {
                var current = (RemoteGameFile<SaveFileData>)currentField.GetValue(null);
                if (current != null)
                {
                    __result = Time.time - current.LastSyncEngineTime;
                    return false;
                }
            }
            __result = 0f;
            return false;
        }

        [HarmonyPatch("get_LastSyncEngineRealtime")]
        [HarmonyPrefix]
        public static bool get_LastSyncEngineRealtime_Prefix(ref float __result)
        {
            if (SaveFile.HasData)
            {
                var current = (RemoteGameFile<SaveFileData>)currentField.GetValue(null);
                if (current != null)
                {
                    __result = current.LastSyncEngineRealtime;
                    return false;
                }
            }
            __result = 0f;
            return false;
        }
    }
    
    // Prevent the game from creating a save file in the last loaded save slot if the save in said slot does not exist, unless the cold open has been cleared

    [HarmonyPatch(typeof(SaveFile), "InitializeSave")]
    public static class InitializeSave_Patch
    {
        public static bool Prefix()
        {
            if (!MetaSaveFile.Current.HasClearedColdOpen)
                return true;
            return false;
        }
    }
}
