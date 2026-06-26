using JoelG.ENA4;
using KatieSaveHelper.Features.Util;
using UnityEngine.SceneManagement;

namespace KatieSaveHelper.Patches
{
    public static class Preferences_Patch
    {
        private static readonly OnSceneLoadPatch oslPatcher = new OnSceneLoadPatch(EnsurePreferencesLoaded, true);

        // Ensure the Preferences file is loaded or re-loaded post-patch after FMOD is initialized
        static void EnsurePreferencesLoaded(Scene scene, LoadSceneMode mode)
        {
            Preferences.LoadFromDisk();
            oslPatcher.TryUnpatch();
        }
    }
}
