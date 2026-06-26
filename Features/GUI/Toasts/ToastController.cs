using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;
using System;
using KatieSaveHelper.Features.Util;

namespace KatieSaveHelper.Features.API
{
    public class ToastController : MonoBehaviour
    {
        private static readonly OnSceneLoadPatch oslPatcher = new OnSceneLoadPatch(TryCreateObject, patchOnStartup:true);

        private TextMeshProUGUI toast;
        private Coroutine worker;
        private readonly Queue<ToastHandle> _queue = new Queue<ToastHandle>();
        public IReadOnlyCollection<ToastHandle> Queue => _queue;
        public ToastPlayback CurrentlyPlayingToast { get; private set; } = null;

        private static readonly int queueCapacity = 10;

        private bool updateToken = false;

        public static ToastController Instance { get; private set; }

        private static void TryCreateObject(Scene scene, LoadSceneMode mode)
        {
            if (Instance == null)
            {
                new GameObject("KatieToaster").AddComponent<ToastController>();
            }
        }

        public void TryInitObject(Scene scene, LoadSceneMode mode)
        {
            if (toast == null)
            {
                StartCoroutine(ToastSettings.TryFirstLoad());
                Init();
            }
        }

        private void NotifyOnUpdate()
        {
            updateToken = true;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Subscribe to scene loaded
            SceneManager.sceneLoaded += TryInitObject;
            // Subscribe to Toast Settings updates
            ToastSettings.OnStyleChanged += NotifyOnUpdate;
        }

        private void Init()
        {
            // Canvas
            var canvasGO = new GameObject("ToastCanvas");
            canvasGO.transform.SetParent(transform);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32750;

            // Canvas Scaler
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Screen.width, Screen.height);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // Text
            var textGO = new GameObject("ToastText");
            textGO.transform.SetParent(canvasGO.transform);
            toast = textGO.AddComponent<TextMeshProUGUI>();
            toast.alpha = 0f;

            // Transform
            var rt = toast.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f); // bottom-left of screen
            rt.anchorMax = new Vector2(1f, 1f); // top-right of screen
            rt.pivot = new Vector2(0.5f, 0.5f); // center of screen
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            float margin = 20f;
            rt.offsetMin = new Vector2(margin, margin);
            rt.offsetMax = new Vector2(-margin, -margin);

            toast.enabled = false;
        }

        private static (bool success, ToastHandle toastHandle) QueueToast(ToastInstance toastInstance, bool pushToFront = false)
        {
            if (Instance == null || Instance._queue.Count >= queueCapacity)
                return (false, null);

            if (!ToastSettings.HasUpdatedOnce)
            {
                KatieLogger.Error("Toast settings not loaded");
                return (false, null);
            }

            if (ToastSettings.Font == null)
            {
                KatieLogger.Error("No font loaded for displaying Toast");
                return (false, null);
            }

            var handle = new ToastHandle(toastInstance);

            if (pushToFront && Instance._queue.Count > 0)
            {
                var oldItems = Instance._queue.ToArray();
                Instance._queue.Clear();
                Instance._queue.Enqueue(handle);
                foreach (var item in oldItems)
                    Instance._queue.Enqueue(item);
            }
            else
            {
                Instance._queue.Enqueue(handle);
            }

            // Ensure the worker coroutine is running
            if (Instance.worker == null)
                Instance.worker = Instance.StartCoroutine(Instance.PlayQueuedToasts());

            return (true, handle);
        }

        public static (bool success, ToastHandle toastHandle) TryQueueToast(string message, string groupName = "None", bool pushToFront = false)
        {
            if (!KatieConfig.Settings.showToasts.Value)
                return (false, null);

            return QueueToast(new ToastInstance(message, groupName), pushToFront);
        }

        public static (bool success, ToastHandle toastHandle) TryQueueToast(ToastInstance toastInstance, bool pushToFront = false)
        {
            if (!KatieConfig.Settings.showToasts.Value)
                return (false, null);

            return QueueToast(toastInstance, pushToFront);
        }

        public static (bool success, ToastHandle toastHandle) TryQueueAndLogToast(string message, string groupName = "None", bool pushToFront = false)
        {
            KatieLogger.Info(message);
            return TryQueueToast(message, groupName, pushToFront);
        }

        public static (bool success, ToastHandle toastHandle) TryQueueAndLogToast(ToastInstance toastInstance, bool pushToFront = false)
        {
            KatieLogger.Info(toastInstance.message);
            return TryQueueToast(toastInstance, pushToFront);
        }
        public static bool TryCancelPlayingToast(Func<ToastHandle, bool> predicate = null)
        {
            if (Instance == null || Instance.CurrentlyPlayingToast == null) return false;

            if (predicate == null || predicate(Instance.CurrentlyPlayingToast.handle))
            {
                Instance.CurrentlyPlayingToast.CancelToast();
                return true;
            }

            return false;
        }

        public static int TryCancelQueuedToasts(Func<ToastHandle, bool> predicate = null)
        {
            if (Instance == null || Instance._queue.Count == 0)
                return 0;

            int dequeueCount = 0;
            int queuedCount = Instance._queue.Count;

            if (predicate != null)
            {
                for (int i = 0; i < queuedCount; i++)
                {
                    var handle = Instance._queue.Dequeue();
                    if (!predicate(handle))
                        Instance._queue.Enqueue(handle);
                    else
                        dequeueCount++;
                }
            }
            else
            {
                Instance._queue.Clear();
                dequeueCount = queuedCount;
            }

            return dequeueCount;
        }

        public static int TryCancelQueuedOrPlayingToasts(Func<ToastHandle, bool> predicate = null)
        {
            int totalToastsCancelled = 0;

            if (TryCancelPlayingToast(predicate)) totalToastsCancelled++;

            totalToastsCancelled += TryCancelQueuedToasts(predicate);

            return totalToastsCancelled;
        }

        public static bool TryExtendPlayingToast(Func<ToastHandle, bool> predicate = null)
        {
            if (Instance == null || Instance.CurrentlyPlayingToast == null) return false;

            if (predicate == null || predicate(Instance.CurrentlyPlayingToast.handle))
            {
                Instance.CurrentlyPlayingToast.ResetToastHold();
                return true;
            }

            return false;
        }

        private IEnumerator PlayQueuedToasts()
        {
            while (_queue.Count > 0)
            {
                ToastHandle toastHandle = _queue.Dequeue();
                yield return PlaySingleToast(toastHandle);
                yield return new WaitForSecondsRealtime(toastHandle.instance.GapTime);
            }

            worker = null;
        }

        private IEnumerator PlaySingleToast(ToastHandle toastHandle)
        {
            if (updateToken)
                UpdateToastFromSettings();

            CurrentlyPlayingToast = new ToastPlayback(toast, toastHandle);
            toastHandle.Update();

            yield return CurrentlyPlayingToast.ShowToast();

            CurrentlyPlayingToast = null;
        }

        internal bool UpdateToastFromSettings()
        {
            toast.font = ToastSettings.Font;
            toast.fontSize = ToastSettings.FontSize;
            toast.color = ToastSettings.FontColor;
            toast.alignment = ToastSettings.TextAlignment;

            // Safetly create/replace custom outline material
            if (toast.fontMaterial != null && toast.fontMaterial != toast.font.material)
                Destroy(toast.fontMaterial);
            Material newMat = new Material(toast.font.material);
            newMat.EnableKeyword("OUTLINE_ON");
            newMat.SetFloat(ShaderUtilities.ID_OutlineWidth, ToastSettings.OutlineWidth);
            newMat.SetColor(ShaderUtilities.ID_OutlineColor, ToastSettings.OutlineColor);
            toast.fontMaterial = newMat;
            toast.UpdateMeshPadding();
            toast.havePropertiesChanged = true;

            updateToken = false;
            return true;
        }

        public static bool AnyToastQueued(Func<ToastInstance, bool> predicate = null)
        {
            if (Instance == null || Instance._queue.Count == 0)
                return false;

            if (predicate == null)
                return Instance._queue.Any();
            else
                return Instance._queue.Any(handle => predicate(handle.instance));
        }

        public static bool AnyToastPlaying(Func<ToastInstance, bool> predicate = null)
        {
            if (Instance == null || Instance.CurrentlyPlayingToast == null)
                return false;

            if (predicate == null)
            {
                return Instance.CurrentlyPlayingToast != null;
            }
            else
            {
                return predicate(Instance.CurrentlyPlayingToast.handle.instance);
            }
        }

        public static bool AnyToastQueuedOrPlaying(Func<ToastInstance, bool> predicate = null)
        {
            if (Instance == null || (Instance._queue.Count == 0 && Instance.CurrentlyPlayingToast == null))
                return false;

            if (predicate == null)
            {
                return Instance._queue.Any() || Instance.CurrentlyPlayingToast != null;
            }
            else
            {
                if (Instance.CurrentlyPlayingToast != null)
                    return Instance._queue.Any(handle => predicate(handle.instance)) || predicate(Instance.CurrentlyPlayingToast.handle.instance);
                else
                    return Instance._queue.Any(handle => predicate(handle.instance));
            }
        }

        private void OnDestroy()
        {
            KatieLogger.Error("ToastController object destroyed");

            // Unsubscribe from Toast Settings updates
            ToastSettings.OnStyleChanged -= NotifyOnUpdate;

            // Unsubscribe from scene loaded
            SceneManager.sceneLoaded -= TryInitObject;

            // Destroy static instance
            if (Instance == this)
                Instance = null;
        }

    }
}
