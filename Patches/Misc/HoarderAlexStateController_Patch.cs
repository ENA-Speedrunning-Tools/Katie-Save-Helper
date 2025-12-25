using System.Reflection;
using UnityEngine.Events;
using UnityEngine;
using UnityEngine.SceneManagement;
using HarmonyLib;
using JoelG.ENA4;
using JoelG.ENA4.Locations;

namespace KatieSaveHelper.Patches
{
    public static class HoarderAlexStateController_Patch
    {
        private static readonly MethodInfo SaveGameMethod = AccessTools.Method(typeof(SaveGameEmitter), "SaveGame");
        private static readonly FieldInfo alexDeadGeneralField = AccessTools.Field(typeof(HoarderAlexStateController), "alexDeadGeneral");

        private static readonly OnSceneLoadPatch oslPatcher = new OnSceneLoadPatch(DisableAlexSaveListener, patchOnStartup: true);

        public static void DisableAlexSaveListener(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "USBright" || KatieSceneChanger.CurrentSceneOrigin != KatieSceneChanger.Origin.Manual) return;

            var controller = GameObject.FindObjectOfType<HoarderAlexStateController>();
            if (controller == null)
            {
                KatieLogger.Error("Error when attempting to disable Alex Save Listener, could not find any game object with a 'HoarderAlexStateController' component");
                return;
            }

            var unityEvent = (UnityEvent)alexDeadGeneralField.GetValue(controller);
            if (unityEvent == null)
            {
                KatieLogger.Error("Error when attempting to disable Alex Save Listener, could not find the field 'alexDeadGeneral' inside the target game object's 'HoarderAlexStateController' component");
                return;
            }

            var emitter = controller.GetComponent<SaveGameEmitter>(); ;
            if (emitter == null)
            {
                KatieLogger.Error("Error when attempting to disable Alex Save Listener, could not find any 'SaveGameEmmitter' component inside the target game object");
                return;
            }

            KatieUtil.RemoveListenerFromUnityEvent(unityEvent, emitter, SaveGameMethod, KatieUtil.UnityEventListenerType.Both);
        }
    }
}
