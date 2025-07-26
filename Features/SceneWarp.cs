using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;

namespace KatieSaveHelper
{
    public static class KatieSceneWarp
    {
        private static int totalScenes = SceneManager.sceneCountInBuildSettings;

        private static HashSet<string> filteredScenesList = new HashSet<string>
        {
            "Boot",
            "Menu"
        };

        public static bool WarpNext()
        {
            int loadedIndex = SceneManager.GetActiveScene().buildIndex;
            int attempts = 0;

            int currentIndex = loadedIndex;

            do
            {
                currentIndex = (currentIndex + 1) % totalScenes;
                string sceneName = GetSceneNameByBuildIndex(currentIndex);

                if (!filteredScenesList.Contains(sceneName))
                {
                    KatieUtil.ChangeScene(sceneName, transition: KatieSaveHelperModConfig.warpToNextScene.Transition, stopAudio: true);
                    return true;
                }

                attempts++;
            }
            while (attempts < totalScenes);

            KatieLogger.Warning("No valid next scene found");
            return false;
        }

        public static bool WarpPrev()
        {
            int loadedIndex = SceneManager.GetActiveScene().buildIndex;
            int attempts = 0;

            int currentIndex = loadedIndex;

            do
            {
                currentIndex = (currentIndex - 1 + totalScenes) % totalScenes;
                string sceneName = GetSceneNameByBuildIndex(currentIndex);

                if (!filteredScenesList.Contains(sceneName))
                {
                    KatieUtil.ChangeScene(sceneName, transition: KatieSaveHelperModConfig.warpToPrevScene.Transition, stopAudio: true);
                    return true;
                }

                attempts++;
            }
            while (attempts < totalScenes);

            KatieLogger.Warning("No valid previous scene found");
            return false;
        }

        private static string GetSceneNameByBuildIndex(int index)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(index);
            return Path.GetFileNameWithoutExtension(path);
        }
    }
}
