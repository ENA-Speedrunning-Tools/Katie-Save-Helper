using JoelG.ENA4;
using System;
using System.Collections;
using UnityEngine;
using KatieSaveHelper.Features.Util;
using BepInEx.Configuration;

namespace KatieSaveHelper
{
    public interface IModActionBase
    {
        string DisplayName { get; }
        string InternalName { get; }
        KeyCode Key { get; }
        void Run();
        void SetValuesFromConfig();
        void DisableDefaultKey();
    }

    public class ModAction : IModActionBase
    {
        public string DisplayName { get; private set; }
        public string InternalName { get; private set; }
        public KeyCode Key { get; private set; } = KeyCode.None;
        public KeyCode DefaultKey { get; private set; }
        public ModActionConfig Config { get; private set; }
        private readonly Action _action;
        private readonly Func<StaticCoroutine, IEnumerator> _enumFactory;
        private readonly string _customCoroutineName = null;
        private readonly string _customCoroutineGroup = null;
        public bool IsCoroutineAction => _enumFactory != null;
        public string CustomCoroutineName
        {
            get
            {
                if (!IsCoroutineAction) return null;
                return _customCoroutineName;
            }
        }

        public string CustomCoroutineGroup
        {
            get
            {
                if (!IsCoroutineAction) return null;
                return _customCoroutineGroup;
            }
        }

        public void Run()
        {
            if (IsCoroutineAction)
            {
                string identifier = CustomCoroutineName ?? InternalName;
                string group = CustomCoroutineGroup ?? "None";
                StaticCoroutine.Start(sc => _enumFactory(sc), "KSH.Action." + identifier, group);
            }
            else
            {
                _action?.Invoke();
            }
        }

        public void CreateKeyConfigEntry(string description)
        {
            Config.Key = KatieMain.Instance.Config.Bind("Hotkeys", $"{InternalName}_Key", DefaultKey, description);
        }

        public ModAction(string displayName, Action action, KeyCode defaultKey)
        {
            DisplayName = displayName;
            InternalName = DisplayName.Replace(" ", "");
            _action = action;
            DefaultKey = defaultKey;
            Config = new ModActionConfig();
        }

        public ModAction(string displayName, string internalName, Action action, KeyCode defaultKey)
        {
            DisplayName = displayName;
            InternalName = internalName;
            _action = action;
            DefaultKey = defaultKey;
            Config = new ModActionConfig();
        }

        public ModAction(string displayName, Func<StaticCoroutine, IEnumerator> enumFactory, KeyCode defaultKey, string customCoroutineName = null, string customCoroutineGroup = null)
        {
            DisplayName = displayName;
            InternalName = DisplayName.Replace(" ", "");
            _enumFactory = enumFactory;
            DefaultKey = defaultKey;
            Config = new ModActionConfig();
            _customCoroutineName = customCoroutineName;
            _customCoroutineGroup = customCoroutineGroup;
        }

        public ModAction(string displayName, string internalName, Func<StaticCoroutine, IEnumerator> enumFactory, KeyCode defaultKey, string customCoroutineName = null, string customCoroutineGroup = null)
        {
            DisplayName = displayName;
            InternalName = internalName;
            _enumFactory = enumFactory;
            DefaultKey = defaultKey;
            Config = new ModActionConfig();
            _customCoroutineName = customCoroutineName;
            _customCoroutineGroup = customCoroutineGroup;
        }

        public void SetValuesFromConfig()
        {
            Key = Config.Key.Value;
        }

        public void SetValuesFromDefault()
        {
            Key = DefaultKey;
        }

        public void DisableDefaultKey() =>
            DefaultKey = KeyCode.None;
    }

    public class ModActionConfig
    {
        public ConfigEntry<KeyCode> Key;
    }

    public struct Transition
    {
        public SceneChanger.TransitionType Type;
        public Color Color;
        public float FadeInTime;
        public float FadeOutTime;

        public string hexColor => KatieUtil.GetHexFromColor(Color);

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
    }

    public class TransitionConfig
    {
        public ConfigEntry<SceneChanger.TransitionType> Type;
        public ConfigEntry<string> Color;
        public ConfigEntry<float> FadeInTime;
        public ConfigEntry<float> FadeOutTime;
    }
}
