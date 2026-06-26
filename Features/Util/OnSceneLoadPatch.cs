using System;
using System.Linq;
using System.Reflection;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace KatieSaveHelper.Features.Util
{
    public class OnSceneLoadPatch
    {
        public bool patchApplied { get; private set; } = false;
        public bool startupPatch { get; private set; }

        public UnityAction<Scene, LoadSceneMode> patch { get; private set; }

        public OnSceneLoadPatch(UnityAction<Scene, LoadSceneMode> patch, bool patchOnStartup = false)
        {
            this.patch = patch;
            this.startupPatch = patchOnStartup;
        }

        public bool TryPatch()
        {
            if (patchApplied) return false;
            SceneManager.sceneLoaded += patch;
            patchApplied = true;
            return true;
        }

        public bool TryUnpatch()
        {
            if (!patchApplied) return false;
            SceneManager.sceneLoaded -= patch;
            patchApplied = false;
            return true;
        }

        public static void ApplyStartupPatches()
        {
            var assembly = Assembly.GetExecutingAssembly();

            foreach (var type in assembly.GetTypes())
            {
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                                 .Where(f => f.FieldType == typeof(OnSceneLoadPatch));

                foreach (var field in fields)
                {
                    if (field.IsStatic)
                    {
                        var patcher = field.GetValue(null) as OnSceneLoadPatch;
                        TryInvokeStartupPatch(patcher, type.Name, field.Name);
                    }
                    else
                    {
                        object instance = null;
                        try
                        {
                            instance = Activator.CreateInstance(type);
                        }
                        catch
                        {
                            continue;
                        }

                        if (instance == null)
                            continue;

                        var patcher = field.GetValue(instance) as OnSceneLoadPatch;
                        TryInvokeStartupPatch(patcher, type.Name, field.Name);
                    }
                }
            }
        }

        private static void TryInvokeStartupPatch(OnSceneLoadPatch patcher, string className, string fieldName)
        {
            if (patcher == null || !patcher.startupPatch || patcher.patchApplied) return;

            try
            {
                patcher.TryPatch();
            }
            catch (Exception ex)
            {
                KatieLogger.Error($"Failed to apply scene load patch from {className}.{fieldName}: {ex.Message}");
            }
        }

    }
}
