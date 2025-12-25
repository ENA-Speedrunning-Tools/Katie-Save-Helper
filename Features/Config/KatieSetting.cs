using BepInEx.Configuration;
using UnityEngine;

namespace KatieSaveHelper
{
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
        public string InternalName { get; private set; }
        public T Value { get; private set; }
        public T DefaultValue { get; private set; }
        public KatieSettingConfig<T> Config { get; private set; }

        public KatieSetting(string displayName, T defaultValue)
        {
            DisplayName = displayName;
            InternalName = DisplayName.Replace(" ", "");
            DefaultValue = defaultValue;
            Value = defaultValue;
            Config = new KatieSettingConfig<T>();
        }

        public KatieSetting(string displayName, string internalName, T defaultValue)
        {
            DisplayName = displayName;
            InternalName = internalName;
            DefaultValue = defaultValue;
            Value = defaultValue;
            Config = new KatieSettingConfig<T>();
        }

        public object GetValueAsObject() => Value;

        public void CreateValueConfigEntry(string description)
        {
            Config.Value = KatieMain.Instance.Config.Bind("Settings", InternalName, DefaultValue, description);
        }

        public void SetValuesFromConfig()
        {
            Value = Config.Value.Value;
        }

        public void SetValuesFromDefault()
        {
            Value = DefaultValue;
        }

    }

    public static class KatieSettingExtensions
    {
        public static string TryGetRealValue(this KatieSetting<string> setting)
        {
            if (string.IsNullOrEmpty(setting.Value))
            {
                KatieLogger.Warning($"No mod config entry found for setting '{setting.InternalName}', defaulting to '{setting.DefaultValue}'");
                return setting.DefaultValue;
            }
            return setting.Value;
        }

        public static string TryGetRealValue(this KatieSetting<string> setting, string defaultValue)
        {
            if (string.IsNullOrEmpty(setting.Value))
            {
                KatieLogger.Warning($"No mod config entry found for setting '{setting.InternalName}', defaulting to '{defaultValue}'");
                return defaultValue;
            }
            return setting.Value;
        }

        public static (bool success, Color color) TryGetColorFromValue(this KatieSetting<string> setting)
        {
            string hex = setting.Value;

            if (!hex.StartsWith("#"))
                hex = "#" + hex;

            if (ColorUtility.TryParseHtmlString(hex, out Color color))
                return (true, color);

            string hex2 = setting.DefaultValue;

            if (ColorUtility.TryParseHtmlString(hex2, out Color color2))
            {
                KatieLogger.Warning($"Failed to parse color from '{hex}' in setting '{setting.InternalName}', defaulting to '{hex2}'");
                return (true, color2);
            }

            KatieLogger.Error($"Failed to parse color from all values in setting '{setting.InternalName}'");
            return (false, Color.black);
        }

        public static (bool success, Color color) TryGetColorFromValue(this KatieSetting<string> setting, string defaultValue)
        {
            string hex = setting.Value;

            if (!hex.StartsWith("#"))
                hex = "#" + hex;

            if (ColorUtility.TryParseHtmlString(hex, out Color color))
                return (true, color);

            string hex2 = defaultValue;

            if (ColorUtility.TryParseHtmlString(hex2, out Color color2))
            {
                KatieLogger.Warning($"Failed to parse color from '{hex}' in setting '{setting.InternalName}', defaulting to '{hex2}'");
                return (true, color2);
            }

            KatieLogger.Error($"Failed to parse color from '{hex}' in setting '{setting.InternalName}' and '{hex2}' from the default argument");
            return (false, Color.black);
        }
    }

    public class KatieSettingConfig<T>
    {
        public ConfigEntry<T> Value = default;
    }
}
