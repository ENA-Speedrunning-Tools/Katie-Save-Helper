using BepInEx.Configuration;
using JoelG.ENA4;
using System;
using System.Collections;
using UnityEngine;

namespace KatieSaveHelper
{
    public interface IKatieActionBase
    {
        string DisplayName { get; }
        string InternalName { get; }
        KeyCode Key { get; }
        void Run();
        void SetValuesFromConfig();
    }

    public class KatieAction : IKatieActionBase
    {
        public string DisplayName { get; private set; }
        public string InternalName { get; private set; }
        public KeyCode Key { get; private set; }
        public KeyCode DefaultKey { get; private set; }
        public KatieActionConfig Config { get; private set; }
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
            Config.Key = KatieMain.Instance.Config.Bind("Hotkeys", $"{InternalName}_Key", DefaultKey, $"Key used to {description}");
        }

        public KatieAction(string displayName, Action action, KeyCode defaultKey)
        {
            DisplayName = displayName;
            InternalName = DisplayName.Replace(" ", "");
            _action = action;
            DefaultKey = defaultKey;
            Key = DefaultKey;
            Config = new KatieActionConfig();
        }

        public KatieAction(string displayName, string internalName, Action action, KeyCode defaultKey)
        {
            DisplayName = displayName;
            InternalName = internalName;
            _action = action;
            DefaultKey = defaultKey;
            Key = DefaultKey;
            Config = new KatieActionConfig();
        }

        public KatieAction(string displayName, Func<StaticCoroutine, IEnumerator> enumFactory, KeyCode defaultKey, string customCoroutineName = null, string customCoroutineGroup = null)
        {
            DisplayName = displayName;
            InternalName = DisplayName.Replace(" ", "");
            _enumFactory = enumFactory;
            DefaultKey = defaultKey;
            Key = DefaultKey;
            Config = new KatieActionConfig();
            _customCoroutineName = customCoroutineName;
            _customCoroutineGroup = customCoroutineGroup;
        }

        public KatieAction(string displayName, string internalName, Func<StaticCoroutine, IEnumerator> enumFactory, KeyCode defaultKey, string customCoroutineName = null, string customCoroutineGroup = null)
        {
            DisplayName = displayName;
            InternalName = internalName;
            _enumFactory = enumFactory;
            DefaultKey = defaultKey;
            Key = DefaultKey;
            Config = new KatieActionConfig();
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
    }

    public class KatieActionConfig
    {
        public ConfigEntry<KeyCode> Key;
    }

    public class KatieTransitionAction : IKatieActionBase
    {
        public string DisplayName { get; private set; }
        public string InternalName { get; private set; }
        public KeyCode Key { get; private set; }
        public KeyCode DefaultKey { get; private set; }
        public Transition DefaultTransition { get; private set; }
        public Transition Transition { get; private set; }
        public KatieTransitionActionConfig Config { get; private set; }
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
                string group = CustomCoroutineGroup ?? "Default";
                StaticCoroutine.Start(sc => _enumFactory(sc), "KSH.Action." + identifier, "KSH.Action." + group);
            }
            else
            {
                _action?.Invoke();
            }
        }

        public KatieTransitionAction(string displayName, Action action, KeyCode defaultKey, Transition defaultTransition)
        {
            DisplayName = displayName;
            InternalName = DisplayName.Replace(" ", "");
            DefaultKey = defaultKey;
            Key = DefaultKey;
            DefaultTransition = defaultTransition;
            Transition = DefaultTransition;
            Config = new KatieTransitionActionConfig();
            _action = action;
        }

        public KatieTransitionAction(string displayName, string internalName, Action action, KeyCode defaultKey, Transition defaultTransition)
        {
            DisplayName = displayName;
            InternalName = internalName;
            DefaultKey = defaultKey;
            Key = DefaultKey;
            DefaultTransition = defaultTransition;
            Transition = DefaultTransition;
            Config = new KatieTransitionActionConfig();
            _action = action;
        }

        public KatieTransitionAction(string displayName, Func<StaticCoroutine, IEnumerator> enumFactory, KeyCode defaultKey, Transition defaultTransition, string customCoroutineName = null, string customCoroutineGroup = null)
        {
            DisplayName = displayName;
            InternalName = DisplayName.Replace(" ", "");
            DefaultKey = defaultKey;
            Key = DefaultKey;
            DefaultTransition = defaultTransition;
            Transition = DefaultTransition;
            Config = new KatieTransitionActionConfig();
            _enumFactory = enumFactory;
            _customCoroutineName = customCoroutineName;
            _customCoroutineGroup = customCoroutineGroup;
        }

        public KatieTransitionAction(string displayName, string internalName, Func<StaticCoroutine, IEnumerator> enumFactory, KeyCode defaultKey, Transition defaultTransition, string customCoroutineName = null, string customCoroutineGroup = null)
        {
            DisplayName = displayName;
            InternalName = internalName;
            DefaultKey = defaultKey;
            Key = DefaultKey;
            DefaultTransition = defaultTransition;
            Transition = DefaultTransition;
            Config = new KatieTransitionActionConfig();
            _enumFactory = enumFactory;
            _customCoroutineName = customCoroutineName;
            _customCoroutineGroup = customCoroutineGroup;
        }

        public void CreateKeyConfigEntry(string description)
        {
            Config.Key = KatieMain.Instance.Config.Bind("Hotkeys", $"{InternalName}_Key", DefaultKey, $"Key used to {description}");
        }

        public void CreateTransitionConfigEntry()
        {
            string name = InternalName;
            Config.Transition.Type = KatieMain.Instance.Config.Bind("Scene Transitions", $"{name}_TransitionType", DefaultTransition.Type, $"Transition type to use for the '{DisplayName}' action's transition");
            Config.Transition.Color = KatieMain.Instance.Config.Bind("Scene Transitions", $"{name}_TransitionColor", KatieUtil.GetHexFromColor(DefaultTransition.Color), $"Transition color to use for the '{DisplayName}' action's transition");
            Config.Transition.FadeInTime = KatieMain.Instance.Config.Bind("Scene Transitions", $"{name}_TransitionFadeInTime", DefaultTransition.FadeInTime, $"Transition fade-in time to use for the '{DisplayName}' action's transition");
            Config.Transition.FadeOutTime = KatieMain.Instance.Config.Bind("Scene Transitions", $"{name}_TransitionFadeOutTime", DefaultTransition.FadeOutTime, $"Transition fade-out time to use for the '{DisplayName}' action's transition");
        }

        public void CreateFullConfigEntry(string description)
        {
            CreateKeyConfigEntry(description);
            CreateTransitionConfigEntry();
        }

        public void SetValuesFromConfig()
        {
            Key = Config.Key.Value;
            Transition = new Transition(
                Config.Transition.Type.Value,
                KatieUtil.GetColorFromHex(Config.Transition.Color.Value),
                Config.Transition.FadeInTime.Value,
                Config.Transition.FadeOutTime.Value
                ); ;
        }

        public void SetValuesFromDefault()
        {
            Key = DefaultKey;
            Transition = DefaultTransition;
        }
    }
    public class KatieTransitionActionConfig
    {
        public ConfigEntry<KeyCode> Key;
        public TransitionConfig Transition;

        public KatieTransitionActionConfig()
        {
            Transition = new TransitionConfig();
        }
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
