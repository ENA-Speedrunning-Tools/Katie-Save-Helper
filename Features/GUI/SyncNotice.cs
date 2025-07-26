using JoelG.ENA4.UI;
using KatieSaveHelper.Patches;
using LMirman.Utilities.UI;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KatieSaveHelper
{
    public class SyncNotice : MonoBehaviour
    {
        public static bool HasSeenNotice => File.Exists(Path.Combine(KatieAssetHandler.appDataDir, ".sync_notice_seen"));
        public static void MarkNoticeAsSeen() => File.WriteAllText(Path.Combine(KatieAssetHandler.appDataDir, ".sync_notice_seen"), "seen");

        private static readonly OnSceneLoadPatch oslPatcher = new OnSceneLoadPatch(TryCreateSyncNoticeObject, patchOnStartup:true);

        private static void TryCreateSyncNoticeObject(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Menu" || HasSeenNotice)
                return;

            if (KatieSaveHelperModConfig.assetSubcriber.Value == true)
                MarkNoticeAsSeen();
            else
                new GameObject("KatieSyncNotice").AddComponent<SyncNotice>();

            oslPatcher.TryUnpatch();
        }

        public void Awake()
        {
            MainMenuPanelGroup panelGroup = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(go => go.name == "Main Menu").GetComponent<MainMenuPanelGroup>();
            if (panelGroup == null)
            {
                KatieLogger.Error("Could not load Sync Notice, no panel group found");
                Destroy(this);
            }
            DontDestroyOnLoad(this);

            StartCoroutine(NotifyOnPanelLoad(panelGroup, MainMenuPanelType.Main));
        }

        private IEnumerator NotifyOnPanelLoad(MainMenuPanelGroup panelGroup, MainMenuPanelType targetPanelType)
        {
            int targetPanelIndex = (int)targetPanelType;
            int currentPanelIndex;
            bool accessFailed;

            while (true)
            {
                if (panelGroup == null)
                {
                    KatieLogger.Error("Could not load Sync Notice, panel group became null while waiting for panel");
                    yield break;
                }

                currentPanelIndex = -1;
                accessFailed = false;

                if (panelGroup.CurrentItem != null)
                {
                    try
                    {
                        currentPanelIndex = panelGroup.CurrentIndex;
                    }
                    catch (Exception ex)
                    {
                        KatieLogger.Error($"Exception reading CurrentIndex: {ex.Message}");
                        accessFailed = true;
                    }
                }

                if (!accessFailed && currentPanelIndex == targetPanelIndex)
                    break;

                yield return new WaitForSecondsRealtime(0.1f);
            }
            // Panel found
            yield return new WaitForSecondsRealtime(0.5f);

            ShowSyncNotice();
        }

        public void ShowSyncNotice()
        {
            GameObject saveSlot = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(go => go.name == "Save Slot");
            if (saveSlot == null)
            {
                KatieLogger.Error("Unable to show Sync Notice, no Game Object found with the name 'Save Slot'");
                return;
            }

            UIFunctions.CreateConfirmationWindow(new ENAConfirmationWindow.CustomRequest(new Action(StartSubscribeAndSync), delegate
            {
            }, "Enable Auto-Syncing?",
            "Katie Save Helper can bundle optional fonts and other assets into the mod automatically by syncing them from a secure GitHub repository. " +
            "This ensures everything works as intended without manual setup. You can turn this feature off at any time from the mod's config file. " +
            "Do you want to enable auto-sync?",
            "<color=#67eb39>Enable</color>", "No, thanks", 10f),
            Resources.Load<GameObject>("UI/Confirmation Window"),
            saveSlot.GetComponentInParent<Canvas>());

            MarkNoticeAsSeen();
        }

        private void StartSubscribeAndSync()
        {
            KatieSaveHelperModConfig.assetSubcriber.Config.Value.Value = true;
            KatieSaveHelperModConfig.SaveConfig();

            StartCoroutineSafe(RunAssetSyncCoroutine());
        }

        private IEnumerator RunAssetSyncCoroutine()
        {
            ToastController.TryQueueToast(new ToastInstance("Syncing assets with GitHub...", holdTime:5f));

            Task<int> task = KatieAssetHandler.SyncAssetsAsync();

            // Wait until the task completes
            while (!task.IsCompleted)
                yield return null;

            string doneToast;
            switch (task.Result)
            {
                case 0:
                    doneToast = "Sync complete, restart the game or reload the config to apply changes";
                    break;
                case 1:
                    doneToast = "Sync complete, assets already up-to-date";
                    break;
                default:
                    doneToast = "Sync failed, check logs for details";
                    break;
            }

            ToastController.TryQueueToast(new ToastInstance(doneToast, holdTime:5f));

            Destroy(gameObject);
        }

        private bool StartCoroutineSafe(IEnumerator routine)
        {
            try
            {
                StartCoroutine(routine);
                return true;
            }
            catch (Exception ex)
            {
                KatieLogger.Error("Coroutine failed to start: " + ex);
                return false;
            }
        }
    }
}
