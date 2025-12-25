using JoelG.ENA4;
using System;
using UnityEngine;
using HarmonyLib;
using System.Reflection;

namespace KatieSaveHelper.Patches
{
    [Serializable]
    public class KatieSceneChanger : SceneChanger
    {
        public enum Origin
        {
            Natural,
            Manual
        }

        public struct Destination
        {
            public string Name;
            public string EntranceFlag;

            public Destination(string name, string entranceFlag = "")
            {
                Name = name;
                EntranceFlag = entranceFlag;
            }
        }

        public static Origin CurrentSceneOrigin { get; set; }
        public Origin origin { get; private set; }

        public KatieSceneChanger(string destinationName, SceneChanger.NotifyType notifySaveOnChange = SceneChanger.NotifyType.None, Origin origin = Origin.Natural)
            : base(destinationName, notifySaveOnChange)
        {
            this.origin = origin;
        }

        public KatieSceneChanger(string destinationName, Color fadeColor, float enterDuration, float exitDuration, SceneChanger.NotifyType notifySaveOnChange = SceneChanger.NotifyType.None, Origin origin = Origin.Natural)
            : base(destinationName, fadeColor, enterDuration, exitDuration, notifySaveOnChange)
        {
            this.origin = origin;
        }

        public KatieSceneChanger(string destinationName, Transition transition, SceneChanger.NotifyType notifySaveOnChange = SceneChanger.NotifyType.None, Origin origin = Origin.Natural)
    : base(destinationName, transition.Color, transition.FadeInTime, transition.FadeOutTime, notifySaveOnChange)
        {
            this.origin = origin;
            SceneChanger_Patch.transitionField.SetValue(this, transition.Type);
        }

        public void SetOrigin(Origin origin)
        {
            this.origin = origin;
        }

    }

    [HarmonyPatch(typeof(SceneChanger), nameof(SceneChanger.CommitToScene))]
    public static class SceneChanger_Patch
    {
        public static readonly FieldInfo transitionField = AccessTools.Field(typeof(SceneChanger), "transition");
        public static readonly FieldInfo destinationNameField = AccessTools.Field(typeof(SceneChanger), "destinationName");
        public static readonly FieldInfo entranceFlagField = AccessTools.Field(typeof(SceneChanger), "entranceFlag");

        public static bool Prefix(SceneChanger __instance)
        {
            if (__instance is KatieSceneChanger ksc)
            {
                KatieSceneChanger.CurrentSceneOrigin = ksc.origin;
            }
            else
            {
                KatieSceneChanger.CurrentSceneOrigin = KatieSceneChanger.Origin.Natural;
            }
            return true;
        }

        public static void SetDestination(this SceneChanger sceneChanger, KatieSceneChanger.Destination destination)
        {
            sceneChanger.SetDestination(destination.Name, destination.EntranceFlag);
        }

        public static void SetDestination(this SceneChanger sceneChanger, string destinationName, string entranceFlag = "")
        {
            sceneChanger.SetDestination(destinationName, entranceFlag);
        }

        public static KatieSceneChanger.Destination GetDestination(this SceneChanger sceneChanger)
        {
            string destinationName = (string)destinationNameField.GetValue(sceneChanger);
            string entranceFlag = (string)entranceFlagField.GetValue(sceneChanger);
            return new KatieSceneChanger.Destination(destinationName, entranceFlag);
        }
    }
}
