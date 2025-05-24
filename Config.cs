using JoelG.ENA4;
using KatieSaveHelper.PsuedoRandomizer;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KatieSaveHelper
{
    public interface IKatieActionBase
    {
        string DisplayName { get; }
        string InternalName { get; }
        KeyCode Key { get; }
        Action Action { get; }
        void Run();
        void SetValuesFromConfig();
    }

    public interface IKatieSetting
    {
        string DisplayName { get; }
        string InternalName { get; }
        object GetValueAsObject();
        void SetValuesFromConfig(); 
    }

    public class KatieSetting<T> : IKatieSetting
    {
        public string DisplayName { get; private set; }
        public string InternalName => DisplayName.Replace(" ", "");
        public T Value {  get; private set; }
        public KatieSettingConfig<T> Config {  get; private set; }

        public KatieSetting(string name)
        {
            this.DisplayName = name;
            this.Value = default;
            this.Config = new KatieSettingConfig<T>();
        }

        public object GetValueAsObject() => Value;

        public void CreateConfigEntry(MelonPreferences_Category category, T defaultValue, string description)
        {
            Config.Value = category.CreateEntry(InternalName, defaultValue, description);
        }

        public void SetValuesFromConfig()
        {
            Value = Config.Value.Value;
        }

    }

    public class KatieSettingConfig<T>
    {
        public MelonPreferences_Entry<T> Value = default;
    }

    public class KatieAction : IKatieActionBase
    {
        public string DisplayName { get; private set; }
        public string InternalName => DisplayName.Replace(" ", "");
        public KeyCode Key { get; private set; }
        public Action Action { get; private set; }
        public KatieActionConfig Config { get; private set; }

        public void Run() => Action?.Invoke();

        public void CreateKeyConfig(MelonPreferences_Category category, KeyCode defaultKey, string description)
        {
            Config.Key = category.CreateEntry($"{InternalName}_Key", defaultKey, $"Key used to {description}");
        }

        public KatieAction(string name, Action action)
        {
            DisplayName = name;
            Action = action;
            Key = KeyCode.None;
            Config = new KatieActionConfig();
        }

        public void SetValuesFromConfig()
        {
            Key = Config.Key.Value;
        }
    }

    public class KatieActionConfig
    {
        public MelonPreferences_Entry<KeyCode> Key;
    }

    public class KatieTransitionAction : IKatieActionBase
    {
        public string DisplayName { get; private set; }
        public string InternalName => DisplayName.Replace(" ", "");
        public KeyCode Key { get; private set; }
        public Action Action { get; private set; }
        public Transition Transition { get; private set; }
        public KatieTransitionActionConfig Config { get; private set; }

        public void Run() => Action?.Invoke();

        public KatieTransitionAction(string name, Action action)
        {
            DisplayName = name;
            Key = KeyCode.None;
            Transition = new Transition();
            Config = new KatieTransitionActionConfig();
            Action = action;
        }

        public void CreateKeyConfigEntry(MelonPreferences_Category category, KeyCode defaultKey, string description)
        {
            Config.Key = category.CreateEntry($"{InternalName}_Key", defaultKey, $"Key used to {description}");
        }

        public void CreateTransitionConfigEntry(MelonPreferences_Category category, SceneChanger.TransitionType defaultType, Color defaultColor, float defaultFadeInTime, float defaultFadeOutTime, string description)
        {
            string name = InternalName;
            Config.Transition.Type = category.CreateEntry($"{name}_TransitionType", defaultType, $"Transition type to use when {description}");
            Config.Transition.Color = category.CreateEntry($"{name}_TransitionColor", KatieSaveHelperModConfig.GetHexFromColor(defaultColor), $"Transition color to use when {description}");
            Config.Transition.FadeInTime = category.CreateEntry($"{name}_TransitionFadeInTime", defaultFadeInTime, $"Transition fade-in time to use when {description}");
            Config.Transition.FadeOutTime = category.CreateEntry($"{name}_TransitionFadeOutTime", defaultFadeOutTime, $"Transition fade-out time to use when {description}");
        }

        public void CreateTransitionConfigEntry(MelonPreferences_Category category, SceneChanger.TransitionType defaultType, string defaultColor, float defaultFadeInTime, float defaultFadeOutTime, string description)
        {
            string name = InternalName;
            Config.Transition.Type = category.CreateEntry($"{name}_TransitionType", defaultType, $"Transition type to use when {description}");
            Config.Transition.Color = category.CreateEntry($"{name}_TransitionColor", defaultColor, $"Transition color to use when {description}");
            Config.Transition.FadeInTime = category.CreateEntry($"{name}_TransitionFadeInTime", defaultFadeInTime, $"Transition fade-in time to use when {description}");
            Config.Transition.FadeOutTime = category.CreateEntry($"{name}_TransitionFadeOutTime", defaultFadeOutTime, $"Transition fade-out time to use when {description}");
        }

        public void SetValuesFromConfig()
        {
            Key = Config.Key.Value;
            Transition.Type = Config.Transition.Type.Value;
            Transition.Color = KatieSaveHelperModConfig.GetColorFromHex(Config.Transition.Color.Value);
            Transition.FadeInTime = Config.Transition.FadeInTime.Value;
            Transition.FadeOutTime = Config.Transition.FadeOutTime.Value;
        }
    }
    public class KatieTransitionActionConfig
    {
        public MelonPreferences_Entry<KeyCode> Key;
        public TransitionConfig Transition;

        public KatieTransitionActionConfig()
        {
            Transition = new TransitionConfig();
        }
    }

    public class Transition
    {
        public SceneChanger.TransitionType Type;
        public Color Color;
        public float FadeInTime;
        public float FadeOutTime;

        public string hexColor => KatieSaveHelperModConfig.GetHexFromColor(Color);

        public Transition()
        {

        }

        public Transition(Transition transition)
        {
            Type = transition.Type;
            Color = transition.Color;
            FadeInTime = transition.FadeInTime;
            FadeOutTime = transition.FadeOutTime;
        }

        public Transition(SceneChanger.TransitionType type, Color color, float fadeInTime, float fadeOutTime)
        {
            Type = type;
            Color = color;
            FadeInTime = fadeInTime;
            FadeOutTime = fadeOutTime;
        }

        public Transition Copy()
        {
            return new Transition(this);
        }
    }
    public class TransitionConfig
    {
        public MelonPreferences_Entry<SceneChanger.TransitionType> Type;
        public MelonPreferences_Entry<string> Color;
        public MelonPreferences_Entry<float> FadeInTime;
        public MelonPreferences_Entry<float> FadeOutTime;
    }

    public static class KatieSaveHelperModConfig
    {
        internal static List<IKatieSetting> allSettings = new List<IKatieSetting>();
        internal static List<IKatieActionBase> allActions = new List<IKatieActionBase>();
        internal static IEnumerable<IKatieActionBase> allTransitionActions => allActions.Where(action => action is KatieTransitionAction);
        internal static List<IKatieActionBase> allActiveActions = new List<IKatieActionBase>();
        internal static IEnumerable<IKatieActionBase> allActiveTransitionActions => allActiveActions.Where(action => action is KatieTransitionAction);

        internal static MelonPreferences_Category configCategory;

        internal static KatieSetting<bool> autoSaveDisabled = new KatieSetting<bool>("Disable Game Auto Saving");
        internal static KatieSetting<bool> createPsuedoRandomSaves = new KatieSetting<bool>("Psuedo Randomize New Saves");
        internal static KatieSetting<bool> psuedoRandomNaturalSeedsOnly = new KatieSetting<bool>("Psuedo Randomizer Generate Natural Seeds Only");
        internal static KatieSetting<TaxiHead> psuedoRandomTargetTaxiHead = new KatieSetting<TaxiHead>("Psuedo Randomizer Target Taxi Head");
        internal static KatieSetting<string> psuedoRandomTargetPurgeRoomGoals = new KatieSetting<string>("Psuedo Randomizer Target Purge Goals");
        internal static KatieSetting<string> psuedoRandomTargetPurgeRoomObstacles = new KatieSetting<string>("Psuedo Randomizer Target Purge Obstacles");
        internal static KatieSetting<int> psuedoRandomMaxAttempts = new KatieSetting<int>("Psuedo Randomizer Search Attempt Limit");

        internal static KatieAction reloadConfig = new KatieAction("Reload Config", ReloadConfig);
        internal static KatieAction quickSave = new KatieAction("Quick Save", KatieSaveHelperModActions.quickSave);

        internal static KatieTransitionAction reloadSaveWithFileSeed = new KatieTransitionAction("Reload Save With File Seed", KatieSaveHelperModActions.reloadSaveWithFileSeed);
        internal static KatieTransitionAction reloadSaveWithCurrentSeed = new KatieTransitionAction("Reload Save With Current Seed", KatieSaveHelperModActions.reloadSaveWithCurrentSeed);
        internal static KatieTransitionAction reloadSaveWithRandomSeed = new KatieTransitionAction("Reload Save With Random Seed", KatieSaveHelperModActions.reloadSaveWithRandomSeed);
        internal static KatieTransitionAction reloadSaveWithPsuedoRandomSeed = new KatieTransitionAction("Reload Save With Psuedo Random Seed", KatieSaveHelperModActions.reloadSaveWithPsuedoRandomSeed);
        internal static KatieTransitionAction resetSaveWithCurrentSeed = new KatieTransitionAction("Reset Save With Current Seed", KatieSaveHelperModActions.resetSaveWithCurrentSeed);
        internal static KatieTransitionAction resetSaveWithRandomSeed = new KatieTransitionAction("Reset Save With Random Seed", KatieSaveHelperModActions.resetSaveWithRandomSeed);
        internal static KatieTransitionAction resetSaveWithPsuedoRandomSeed = new KatieTransitionAction("Reset Save With Psuedo Random Seed", KatieSaveHelperModActions.resetSaveWithPsuedoRandomSeed);

        public static void LoadSettingsList()
        {
            allSettings.Clear();

            allSettings.Add(autoSaveDisabled);

            allSettings.Add(createPsuedoRandomSaves);
            allSettings.Add(psuedoRandomNaturalSeedsOnly);
            allSettings.Add(psuedoRandomTargetTaxiHead);
            allSettings.Add(psuedoRandomTargetPurgeRoomGoals);
            allSettings.Add(psuedoRandomTargetPurgeRoomObstacles);
            allSettings.Add(psuedoRandomMaxAttempts);
        }

        public static void LoadActionsList()
        {
            allActions.Clear();

            allActions.Add(reloadConfig);
            allActions.Add(quickSave);
            allActions.Add(reloadSaveWithFileSeed);
            allActions.Add(reloadSaveWithCurrentSeed);
            allActions.Add(reloadSaveWithRandomSeed);
            allActions.Add(reloadSaveWithPsuedoRandomSeed);
            allActions.Add(resetSaveWithCurrentSeed);
            allActions.Add(resetSaveWithRandomSeed);
            allActions.Add(resetSaveWithPsuedoRandomSeed);
        }

        public static void LoadActiveActionsList()
        {
            allActiveActions.Clear();

            allActiveActions.AddRange(allActions.Where(action => action.Key != KeyCode.None));
        }

        public static void LoadConfig()
        {
            MelonLogger.Msg("Loading config...");

            LoadSettingsList();
            LoadActionsList();

            configCategory = MelonPreferences.CreateCategory("KatieSaveHelper", "Katie Save Helper Settings");

            autoSaveDisabled.CreateConfigEntry(configCategory, false, "Whether the mod should disable the game's automatic saving feature");
            createPsuedoRandomSaves.CreateConfigEntry(configCategory, false, "Whether the mod should psuedo-randomize seeds for save files that are manually created using the in-game menu");
            psuedoRandomNaturalSeedsOnly.CreateConfigEntry(configCategory, true, "Whether the mod's psuedo-randomizer should exclusively generate seeds the game itself can naturally generate");
            psuedoRandomTargetTaxiHead.CreateConfigEntry(configCategory, TaxiHead.Socio, "Name of the Taxi Head that the mod's psuedo-randomizer will target when generating a new seed");
            psuedoRandomTargetPurgeRoomGoals.CreateConfigEntry(configCategory, "RLRLRL", "Order of the room goals during the Purge Event Maze that the mod's psuedo-randomizer will target when generating a new seed");
            psuedoRandomTargetPurgeRoomObstacles.CreateConfigEntry(configCategory, "*Any, !WanderingFish, !WanderingFish, !WanderingFish, !WanderingFish", "Dog obstacles within each room during the Purge Event Maze that the mod's psuedo-randomizer will target when generating a new seed");
            psuedoRandomMaxAttempts.CreateConfigEntry(configCategory, 1000000, "Maximum number of attempts the psuedo-randomizer will make to find a matching seed before quitting");

            quickSave.CreateKeyConfig(configCategory, KeyCode.Alpha1, "write the data from the current save into it's respective save file");
            reloadSaveWithFileSeed.CreateKeyConfigEntry(configCategory, KeyCode.Alpha2, "reload the current save normally, using the seed from it's respective save file");
            reloadSaveWithCurrentSeed.CreateKeyConfigEntry(configCategory, KeyCode.None, "reload the current save with the currently loaded seed");
            reloadSaveWithRandomSeed.CreateKeyConfigEntry(configCategory, KeyCode.None, "reload the current save with a randomized seed");
            reloadSaveWithPsuedoRandomSeed.CreateKeyConfigEntry(configCategory, KeyCode.None, "reload the current save with a psuedo-randomized seed");
            resetSaveWithCurrentSeed.CreateKeyConfigEntry(configCategory, KeyCode.Alpha3, "immediately erase the current save, create a new empty one in the same slot that has the currently loaded seed, then load it");
            resetSaveWithRandomSeed.CreateKeyConfigEntry(configCategory, KeyCode.None, "immediately erase the current save, create a new empty one in the same slot that has a randomized seed, then load it");
            resetSaveWithPsuedoRandomSeed.CreateKeyConfigEntry(configCategory, KeyCode.None, "immediately erase the current save, create a new empty one in the same slot that has a psuedo-randomized seed, then load it");
            reloadConfig.CreateKeyConfig(configCategory, KeyCode.Alpha4, "reload all mod settings from this config file");

            reloadSaveWithFileSeed.CreateTransitionConfigEntry(configCategory, SceneChanger.TransitionType.FadeToColor, Color.black, 0.5f, 0.5f, "reloading the current save normally");
            reloadSaveWithCurrentSeed.CreateTransitionConfigEntry(configCategory, SceneChanger.TransitionType.FadeToColor, Color.black, 0.5f, 0.5f, "reloading the current save with the currently loaded seed");
            reloadSaveWithRandomSeed.CreateTransitionConfigEntry(configCategory, SceneChanger.TransitionType.FadeToColor, Color.black, 0.5f, 0.5f, "reloading the current save with a new randomized seed");
            reloadSaveWithPsuedoRandomSeed.CreateTransitionConfigEntry(configCategory, SceneChanger.TransitionType.FadeToColor, Color.black, 0.5f, 0.5f, "reloading the current save with a new psuedo-randomized seed");
            resetSaveWithCurrentSeed.CreateTransitionConfigEntry(configCategory, SceneChanger.TransitionType.FadeToColor, Color.black, 0.5f, 0.5f, "resetting the current save while keeping it's seed");
            resetSaveWithRandomSeed.CreateTransitionConfigEntry(configCategory, SceneChanger.TransitionType.FadeToColor, Color.black, 0.5f, 0.5f, "resetting the current save with a new randomized seed");
            resetSaveWithPsuedoRandomSeed.CreateTransitionConfigEntry(configCategory, SceneChanger.TransitionType.FadeToColor, Color.black, 0.5f, 0.5f, "resetting the current save with a new psuedo-randomized seed");

            configCategory.SaveToFile();

            LoadOptionsFromConfig();
        }
        public static void LoadOptionsFromConfig()
        {
            foreach (IKatieSetting setting in allSettings)
            {
                setting.SetValuesFromConfig();
            }

            foreach (IKatieActionBase action in allActions)
            {
                action.SetValuesFromConfig();
            }

            LoadActiveActionsList();
            PrintConfig();
        }

        public static void ReloadConfig()
        {
            MelonPreferences.Load();
            LoadOptionsFromConfig();
        }

        public static void PrintConfig()
        {
            string configLog = "";

            configLog += $"Using mod config: \n" +
                $"\tSettings: \n";

            foreach (IKatieSetting setting in allSettings)
            {
                configLog += $"\t\t{setting.InternalName}: {setting.GetValueAsObject()}\n";
            }

            configLog += $"\tKeys: \n";

            foreach (IKatieActionBase action in allActiveActions)
            {
                configLog += $"\t\t{action.InternalName}: {action.Key}\n";
            }

            configLog += $"\tTransitions: \n";

            foreach (KatieTransitionAction action in allActiveTransitionActions)
            {
                configLog += $"\t\t{action.InternalName}:\n" +
                $"\t\t\tType: {action.Transition.Type}\n" +
                $"\t\t\tColor: {action.Transition.hexColor}\n" +
                $"\t\t\tFade-In: {action.Transition.FadeInTime}\n" +
                $"\t\t\tFade-Out: {action.Transition.FadeOutTime}\n";
            }

            MelonLogger.Msg(configLog);
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
