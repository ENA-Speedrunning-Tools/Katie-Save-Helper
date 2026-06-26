using HarmonyLib;
using JoelG.ENA4.UI;
using LMirman.Utilities.UI;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using System.Threading;
using UnityEngine.SceneManagement;
using KatieSaveHelper.Features.Util;
using KatieSaveHelper.Features.API;

namespace KatieSaveHelper.Patches
{
    // If configured by the mod, use a custom transition coroutine when selecting a save that does not include the 2 second time delay

    [HarmonyPatch(typeof(MainMenu), nameof(MainMenu.TransitionThenContinueSave))]
    public static class MainMenu_Patch
    {
        private static readonly FieldInfo isSelectingSaveField = AccessTools.Field(typeof(MainMenu), "isSelectingSave");
        private static readonly FieldInfo panelGroupField = AccessTools.Field(typeof(MainMenu), "panelGroup");
        private static readonly PropertyInfo selectedSlotIndexProp = AccessTools.Property(typeof(MenuSaveSlotUI), "SelectedSlotIndex");
        private static readonly MethodInfo ContinueGameMethod = AccessTools.Method(typeof(MainMenu), "ContinueGame");
        private static readonly MethodInfo RefreshAllMethod = AccessTools.Method(typeof(MenuSaveSlotUI), "RefreshAll");
        private static readonly Func<MainMenu, int, IEnumerator> originalTransitionThenPickSave = AccessTools.MethodDelegate<Func<MainMenu, int, IEnumerator>>(AccessTools.Method(typeof(MainMenu), "TransitionThenPickSave"));
        public static bool SaveTransitionCoroutineActive { get; private set; } = false;
        private static CancellationTokenSource CancellationSource = null;

        public static bool Prefix(MainMenu __instance, int index)
        {
            __instance.StartCoroutine(TransitionThenPickSave(__instance, index));
            return false;
        }

        public static bool TryCancelSaveTransition()
        {
            if (SaveTransitionCoroutineActive && CancellationSource != null)
            {
                CancellationSource.Cancel();
                return true;
            }
            return false;
        }

        private static IEnumerator SafePanelTransition(MainMenuPanelGroup panelGroup, string panel, Action onTimeout, float timeout = 10f)
        {
            float timer = 0f;
            panelGroup.TransitionToPanel(panel);

            while (panelGroup.TransitionTarget != null)
            {
                yield return null;
                timer += Time.deltaTime;

                if (timer > 10f)
                {
                    onTimeout?.Invoke();
                    yield break;
                }
            }
        }

        private static IEnumerator ExitSaveTransitionState(MainMenuPanelGroup panelGroup)
        {
            if (panelGroup == null || SceneManager.GetActiveScene().name != "Menu") yield break;

            var go = KatieUtil.FindGameObjectByPath("Main Menu/Content (Masked)/File Select");
            var saveSlotUI = go?.GetComponent<MenuSaveSlotUI>();
            if (saveSlotUI == null)
            {
                KatieLogger.Error("Could not cancel save selection as intended, attempting to cancel manually");
                bool timedOut = false;
                yield return SafePanelTransition(panelGroup, "null", () => timedOut = true);
                if (timedOut)
                {
                    KatieLogger.Error("Manual save selection cancel failed, re-loading Main Menu scene");
                    SceneManager.LoadScene("Menu");
                    yield break;
                }
                yield return SafePanelTransition(panelGroup, "file_select", () => timedOut = true);
                if (timedOut)
                {
                    KatieLogger.Error("Manual save selection cancel failed, re-loading Main Menu scene");
                    SceneManager.LoadScene("Menu");
                    yield break;
                }
            }
            else
            {
                selectedSlotIndexProp.SetValue(saveSlotUI, -1);

                // Wait until both mouse buttons are lifted to avoid triggering other save click events
                while (Input.GetKey(KeyCode.Mouse0) || Input.GetKey(KeyCode.Mouse1))
                    yield return null;

                RefreshAllMethod.Invoke(saveSlotUI, null);
            }
        }

        private static IEnumerator TransitionThenPickSave(MainMenu instance, int index)
        {
            var panelGroup = (MainMenuPanelGroup)panelGroupField.GetValue(instance);

            if (panelGroup == null)
            {
                KatieLogger.Error("Could not run modified TransitionThenPickSave, falling back to original coroutine");
                instance.StartCoroutine(originalTransitionThenPickSave(instance, index));
                yield break;
            }

            if (SaveTransitionCoroutineActive)
            {
                KatieLogger.Warning("Could not run modified TransitionThenPickSave, duplicate coroutine already active");
                yield break;
            }

            SaveTransitionCoroutineActive = true;
            CancellationSource = new CancellationTokenSource();

            var resetModeGo = KatieUtil.FindGameObjectByPath("Main Menu/Content (Masked)/File Select/Confirm Buttons/Special Function Button (1)");
            if (resetModeGo == null)
                KatieLogger.Error("Could not find the Reset Mode Button game object");
            else
                resetModeGo.SetActive(false);

            try
            {
                isSelectingSaveField.SetValue(instance, true);
                UIFunctions.AddFocus(instance);

                var loadSaveTask = LoadSave_Patch.LoadSaveAsync(index, token: CancellationSource.Token);
                bool announced = false;
                float loadTimeElapsed = 0f;

                while (!loadSaveTask.IsCompleted)
                {
                    yield return null;

                    loadTimeElapsed += Time.deltaTime;
                    if ((!announced && loadTimeElapsed >= 2f) || Input.GetKeyDown(KeyCode.Mouse0))
                    {
                        ToastBehaviours.Notice(
                            new ToastInstance(
                                "Generating seeds before loading the Save, cancel with right click",
                                "KSH.SeedGenBusy.OnLoadSave",
                                holdTime: 3f
                                ),
                            sendToLog: false,
                            pushToFront: true
                        );
                        announced = true;
                    }

                    if (Input.GetKeyDown(KeyCode.Mouse1) || SceneManager.GetActiveScene().name != "Menu")
                        CancellationSource.Cancel();
                }

                ToastController.TryCancelQueuedOrPlayingToasts(handle => handle.instance.groupName == "KSH.SeedGenBusy.OnLoadSave");

                if (CancellationSource.IsCancellationRequested)
                {
                    yield return ExitSaveTransitionState(panelGroup);
                    yield break;
                }

                if (loadSaveTask.Result.returnCode != SeedGeneratorReturnCode.Success)
                {
                    ToastBehaviours.Notice(
                        new ToastInstance(
                            "Could not load Save, seed not found",
                            "KSH.SeedGen.SeedNotFound",
                            holdTime: 3f
                            ),
                        sendToLog: false,
                        pushToFront: true
                    );
                    yield return ExitSaveTransitionState(panelGroup);
                    yield break;
                }

                if (KatieConfig.Settings.disableSaveSelectDelay.Value == false)
                {
                    float delayTime = 1.5f - loadTimeElapsed;
                    if (delayTime > 0f) yield return new WaitForSeconds(delayTime);
                }

                panelGroup.TransitionToPanel("null");

                yield return null;
                float timer = Time.deltaTime;
                while (timer < 10f && panelGroup.TransitionTarget != null)
                {
                    yield return null;
                    timer += Time.deltaTime;
                }

                if (KatieConfig.Settings.disableSaveSelectDelay.Value == false)
                    yield return new WaitForSeconds(0.5f);

                ContinueGameMethod.Invoke(instance, null);
            }
            finally
            {
                UIFunctions.RemoveFocus(instance);
                // Small frame delay so the Reset Mode button pops up at the same time as the Back Button
                instance.StartCoroutine(KatieUtil.DelayedStateChange(resetModeGo, go => go.SetActive(true), frameDelay: 3));
                isSelectingSaveField.SetValue(instance, false);
                SaveTransitionCoroutineActive = false;
            }
        }
    }
}
