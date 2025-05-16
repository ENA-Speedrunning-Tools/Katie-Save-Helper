using JoelG.ENA4;
using MelonLoader;
using UnityEngine;

namespace KatieSaveHelper
{
    public static class KatieSaveHelperModConfig
    {
        // Entry Declarations

        internal static MelonPreferences_Category configCategory;

        internal static MelonPreferences_Entry<bool> autoSaveDisabled_Config;
        
        internal static MelonPreferences_Entry<KeyCode> quickSave_Key_Config;
        internal static MelonPreferences_Entry<KeyCode> reloadSave_Key_Config;
        internal static MelonPreferences_Entry<KeyCode> resetSave_Key_Config;
        internal static MelonPreferences_Entry<KeyCode> resetSaveWithSeed_Key_Config;
        internal static MelonPreferences_Entry<KeyCode> reloadConfig_Key_Config;

        internal static MelonPreferences_Entry<SceneChanger.TransitionType> reloadSave_TransitionType_Config;
        internal static MelonPreferences_Entry<SceneChanger.TransitionType> resetSave_TransitionType_Config;
        internal static MelonPreferences_Entry<SceneChanger.TransitionType> resetSaveWithSeed_TransitionType_Config;

        internal static MelonPreferences_Entry<string> reloadSave_TransitionColor_Config;
        internal static MelonPreferences_Entry<string> resetSave_TransitionColor_Config;
        internal static MelonPreferences_Entry<string> resetSaveWithSeed_TransitionColor_Config;

        internal static MelonPreferences_Entry<float> reloadSave_TransitionFadeInTime_Config;
        internal static MelonPreferences_Entry<float> resetSave_TransitionFadeInTime_Config;
        internal static MelonPreferences_Entry<float> resetSaveWithSeed_TransitionFadeInTime_Config;

        internal static MelonPreferences_Entry<float> reloadSave_TransitionFadeOutTime_Config;
        internal static MelonPreferences_Entry<float> resetSave_TransitionFadeOutTime_Config;
        internal static MelonPreferences_Entry<float> resetSaveWithSeed_TransitionFadeOutTime_Config;

        // Config Variables

        internal static bool autoSaveDisabled;

        internal static KeyCode quickSave_Key;
        internal static KeyCode reloadSave_Key;
        internal static KeyCode resetSave_Key;
        internal static KeyCode resetSaveWithSeed_Key;
        internal static KeyCode reloadConfig_Key;

        internal static SceneChanger.TransitionType reloadSave_TransitionType = SceneChanger.TransitionType.FadeToColor;
        internal static SceneChanger.TransitionType resetSave_TransitionType = SceneChanger.TransitionType.FadeToColor;
        internal static SceneChanger.TransitionType resetSaveWithSeed_TransitionType = SceneChanger.TransitionType.FadeToColor;

        internal static Color reloadSave_TransitionColor = Color.black;
        internal static Color resetSave_TransitionColor = Color.black;
        internal static Color resetSaveWithSeed_TransitionColor = Color.black;

        internal static float reloadSave_TransitionFadeInTime = 0;
        internal static float reloadSave_TransitionFadeOutTime = 0;

        internal static float resetSave_TransitionFadeInTime = 0;
        internal static float resetSave_TransitionFadeOutTime = 0;

        internal static float resetSaveWithSeed_TransitionFadeInTime = 0;
        internal static float resetSaveWithSeed_TransitionFadeOutTime = 0;

        public static void LoadConfig()
        {
            MelonLogger.Msg("Loading config...");

            configCategory = MelonPreferences.CreateCategory("KatieSaveHelper", "Katie Save Helper Settings");

            autoSaveDisabled_Config = configCategory.CreateEntry("DisableGameAutoSaving", false, "Whether the mod should disable the game's automatic saving feature");

            quickSave_Key_Config = configCategory.CreateEntry("QuickSave_Key", KeyCode.Alpha1, "Key used to save the game");
            reloadSave_Key_Config = configCategory.CreateEntry("ReloadSave_Key", KeyCode.Alpha2, "Key used to reload the current save");
            resetSave_Key_Config = configCategory.CreateEntry("ResetSave_Key", KeyCode.Alpha0, "Key used to hard reset the current save");
            resetSaveWithSeed_Key_Config = configCategory.CreateEntry("ResetSaveWithSeed_Key", KeyCode.Alpha9, "Key used to hard reset the current save while keeping the same seed");
            reloadConfig_Key_Config = configCategory.CreateEntry("ReloadConfig_Key", KeyCode.Alpha8, "Key used to hard reset the current save while keeping the same seed");

            reloadSave_TransitionType_Config = configCategory.CreateEntry("ReloadSave_TransitionType", SceneChanger.TransitionType.FadeToColor, "Transition type to use when reloading the save");
            reloadSave_TransitionColor_Config = configCategory.CreateEntry("ReloadSave_TransitionColor", GetHexFromColor(Color.black), "Transition color to use when reloading the save");
            reloadSave_TransitionFadeInTime_Config = configCategory.CreateEntry("ReloadSave_TransitionFadeInTime", 0.5f, "Transition fade-in time to use when reloading the save");
            reloadSave_TransitionFadeOutTime_Config = configCategory.CreateEntry("ReloadSave_TransitionFadeOutTime", 0.5f, "Transition fade-out time to use when reloading the save");

            resetSave_TransitionType_Config = configCategory.CreateEntry("ResetSave_TransitionType", SceneChanger.TransitionType.FadeToColor, "Transition type to use when resetting the save");
            resetSave_TransitionColor_Config = configCategory.CreateEntry("ResetSave_TransitionColor", GetHexFromColor(Color.black), "Transition color to use when resetting the save");
            resetSave_TransitionFadeInTime_Config = configCategory.CreateEntry("ResetSave_TransitionFadeInTime", 0.5f, "Transition fade-in time to use when resetting the save");
            resetSave_TransitionFadeOutTime_Config = configCategory.CreateEntry("ResetSave_TransitionFadeOutTime", 0.5f, "Transition fade-out time to use when resetting the save");

            resetSaveWithSeed_TransitionType_Config = configCategory.CreateEntry("ResetSaveWithSeed_TransitionType", SceneChanger.TransitionType.FadeToColor, "Transition type to use when resetting the save while keeping it's seed");
            resetSaveWithSeed_TransitionColor_Config = configCategory.CreateEntry("ResetSaveWithSeed_TransitionColor", GetHexFromColor(Color.black), "Transition color to use when resetting the save while keeping it's seed");
            resetSaveWithSeed_TransitionFadeInTime_Config = configCategory.CreateEntry("ResetSaveWithSeed_TransitionFadeInTime", 0.5f, "Transition fade-in time to use when resetting the save while keeping it's seed");
            resetSaveWithSeed_TransitionFadeOutTime_Config = configCategory.CreateEntry("ResetSaveWithSeed_TransitionFadeOutTime", 0.5f, "Transition fade-out time to use when resetting the save while keeping it's seed");

            configCategory.SaveToFile();

            LoadOptionsFromConfig();
        }
        public static void LoadOptionsFromConfig()
        {
            autoSaveDisabled = autoSaveDisabled_Config.Value;

            quickSave_Key = quickSave_Key_Config.Value;
            reloadSave_Key = reloadSave_Key_Config.Value;
            resetSave_Key = resetSave_Key_Config.Value;
            resetSaveWithSeed_Key = resetSaveWithSeed_Key_Config.Value;
            reloadConfig_Key = reloadConfig_Key_Config.Value;

            reloadSave_TransitionType = reloadSave_TransitionType_Config.Value;
            resetSave_TransitionType = resetSave_TransitionType_Config.Value;
            resetSaveWithSeed_TransitionType = resetSaveWithSeed_TransitionType_Config.Value;

            reloadSave_TransitionColor = GetColorFromHex(reloadSave_TransitionColor_Config.Value);
            resetSave_TransitionColor = GetColorFromHex(resetSave_TransitionColor_Config.Value);
            resetSaveWithSeed_TransitionColor = GetColorFromHex(resetSaveWithSeed_TransitionColor_Config.Value);

            reloadSave_TransitionFadeInTime = reloadSave_TransitionFadeInTime_Config.Value;
            resetSave_TransitionFadeInTime = resetSave_TransitionFadeInTime_Config.Value;
            resetSaveWithSeed_TransitionFadeInTime = resetSaveWithSeed_TransitionFadeInTime_Config.Value;

            reloadSave_TransitionFadeOutTime = reloadSave_TransitionFadeOutTime_Config.Value;
            resetSave_TransitionFadeOutTime = resetSave_TransitionFadeOutTime_Config.Value;
            resetSaveWithSeed_TransitionFadeOutTime = resetSaveWithSeed_TransitionFadeOutTime_Config.Value;

            MelonLogger.Msg(
                $"Using mod config: \n" +
                $"\tKeys: \n" +
                $"\t\tDisableAutoSaving: {autoSaveDisabled}\n" +
                $"\t\tSave: {quickSave_Key}\n" +
                $"\t\tReload: {reloadSave_Key}\n" +
                $"\t\tReset: {resetSave_Key}\n" +
                $"\t\tResetWithSeed: {resetSaveWithSeed_Key}\n" +
                $"\t\tReloadConfig: {reloadConfig_Key}\n" +
                $"\tTransitions: \n" +
                $"\t\tReload:\n" +
                $"\t\t\tType: {reloadSave_TransitionType}\n" +
                $"\t\t\tColor: {reloadSave_TransitionColor_Config.Value}\n" +
                $"\t\t\tFade-In: {reloadSave_TransitionFadeInTime}\n" +
                $"\t\t\tFade-Out: {reloadSave_TransitionFadeOutTime}\n" +
                $"\t\tReset:\n" +
                $"\t\t\tType: {resetSave_TransitionType}\n" +
                $"\t\t\tColor: {resetSave_TransitionColor_Config.Value}\n" +
                $"\t\t\tFade-In: {resetSave_TransitionFadeInTime}\n" +
                $"\t\t\tFade-Out: {resetSave_TransitionFadeOutTime}\n" +
                $"\t\tResetWithSeed:\n" +
                $"\t\t\tType: {resetSaveWithSeed_TransitionType}\n" +
                $"\t\t\tColor: {resetSaveWithSeed_TransitionColor_Config.Value}\n" +
                $"\t\t\tFade-In: {resetSaveWithSeed_TransitionFadeInTime}\n" +
                $"\t\t\tFade-Out: {resetSaveWithSeed_TransitionFadeOutTime}\n"
                );
        }

        public static Color GetColorFromHex(string hex)
        {
            if (!hex.StartsWith("#"))
                hex = "#" + hex;

            if (ColorUtility.TryParseHtmlString(hex, out Color color))
                return color;

            MelonLogger.Warning($"Failed to parse color from '{hex}', defaulting to black");
            return Color.black;
        }

        public static string GetHexFromColor(Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(color);
        }
    }
}
