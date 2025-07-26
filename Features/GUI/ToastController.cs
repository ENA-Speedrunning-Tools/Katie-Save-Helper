using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;
using System;

namespace KatieSaveHelper
{
    public static class ToastSettings
    {
        public static event Action OnStyleChanged;
        public static TMP_FontAsset Font {  get; private set; }
        private static TMP_FontAsset FallbackFont;
        public static int FontSize { get; private set; }
        public static Color FontColor { get; private set; }
        public static TextAlignmentOptions TextAlignment { get; private set; }
        public static float OutlineWidth { get; private set; }
        public static Color OutlineColor { get; private set; }

        public static float Opacity { get; private set; }
        public static float FadeInTime { get; private set; }
        public static float FadeOutTime { get; private set; }
        public static float HoldTime { get; private set; }
        public static float GapTime { get; private set; }
        public static bool UpdatesEnabled { get; private set; } = false;
        public static bool HasUpdatedOnce { get; private set; } = false;

        public static void UpdateFromConfig()
        {
            if (!UpdatesEnabled) return;

            // Try loading the custom font, default to the fallback font if it can't be found
            var font = KatieAssetHandler.LoadFontAsset(KatieSaveHelperModConfig.toastFontFileName.Value);
            if (font == null)
            {
                if (FallbackFont != null)
                {
                    KatieLogger.Warning($"Defaulting to fallback font");
                    font = FallbackFont;
                }
                else
                {
                    KatieLogger.Error($"Could not find fallback font");
                }
            }
            Font = font;

            // Font Color
            var colorTuple = KatieSaveHelperModConfig.toastFontColor.TryGetColorFromValue();
            if (colorTuple.success)
                FontColor = colorTuple.color;

            // Outline Color
            colorTuple = KatieSaveHelperModConfig.toastOutlineColor.TryGetColorFromValue();
            if (colorTuple.success)
                OutlineColor = colorTuple.color;

            TextAlignment = KatieSaveHelperModConfig.toastAlignment.Value;

            FontSize = Mathf.Max(0, KatieSaveHelperModConfig.toastFontSize.Value);
            OutlineWidth = KatieUtil.Clamp((float)KatieSaveHelperModConfig.toastOutlineWidth.Value / 100, 0f, 1f);
            Opacity = KatieUtil.Clamp((float)KatieSaveHelperModConfig.toastOpacity.Value / 100, 0f, 1f);

            FadeInTime = Mathf.Max(0, KatieSaveHelperModConfig.toastFadeInTime.Value);
            FadeOutTime = Mathf.Max(0, KatieSaveHelperModConfig.toastFadeOutTime.Value);
            HoldTime = Mathf.Max(0, KatieSaveHelperModConfig.toastHoldTime.Value);
            GapTime = Mathf.Max(0, KatieSaveHelperModConfig.toastGapTime.Value);

            HasUpdatedOnce = true;
            OnStyleChanged.Invoke();
        }

        public static IEnumerator TryFirstLoad()
        {
            if (FallbackFont != null)
                yield break;

            // Find fallback font
            TMP_FontAsset rsFont = null;
            while (rsFont == null)
            {
                rsFont = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(f => f.name.Contains("RuneScape-ENA"));
                yield return null;
            }

            // If asset handler is syncing, wait for it to finish
            while (KatieAssetHandler.isSyncing)
                yield return null;

            // Set fallback font and allow first update
            FallbackFont = rsFont;
            UpdatesEnabled = true;
            UpdateFromConfig();
        }
    }

    public class ToastInstance
    {
        public string message;
        public float? holdTime;
        public float? gapTime;

        public ToastInstance(string message, float? holdTime = null, float? gapTime = null)
        {
            this.message = message;
            this.holdTime = holdTime;
            this.gapTime = gapTime;
        }
    }

    public class ToastController : MonoBehaviour
    {
        private static readonly OnSceneLoadPatch oslPatcher = new OnSceneLoadPatch(TryCreateObject, patchOnStartup:true);

        private TextMeshProUGUI toast;
        private Coroutine worker;
        readonly Queue<ToastInstance> queue = new Queue<ToastInstance>();

        private static readonly int queueCapacity = 10;

        private float FadeInTime = KatieSaveHelperModConfig.toastFadeInTime.DefaultValue;
        private float HoldTime = KatieSaveHelperModConfig.toastHoldTime.DefaultValue;
        private float FadeOutTime = KatieSaveHelperModConfig.toastFadeOutTime.DefaultValue;
        private float GapTime = KatieSaveHelperModConfig.toastGapTime.DefaultValue;
        private float Opacity = KatieSaveHelperModConfig.toastOpacity.DefaultValue;

        private bool updateToken = false;

        public static ToastController Instance { get; private set; }

        public static void TryCreateObject(Scene scene, LoadSceneMode mode)
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

        public void NotifyOnUpdate()
        {
            updateToken = true;
        }

        public void Awake()
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

        public void Init()
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

        public static bool QueueToast(ToastInstance toastInstance)
        {
            if (Instance == null || Instance.queue.Count >= queueCapacity)
                return false;

            if (!ToastSettings.HasUpdatedOnce)
            {
                KatieLogger.Error("Toast settings not loaded");
                return false;
            }

            if (ToastSettings.Font == null)
            {
                KatieLogger.Error("No font loaded for displaying Toast");
                return false;
            }

            Instance.queue.Enqueue(toastInstance);

            if (Instance.worker == null)
            {
                Instance.worker = Instance.StartCoroutine(Instance.PlayQueuedToasts());
            }

            return true;
        }

        public static bool TryQueueToast(string message)
        {
            if (!KatieSaveHelperModConfig.showToasts.Value)
                return false;

            if (!QueueToast(new ToastInstance(message)))
                return false;

            return true;
        }

        public static bool TryQueueToast(ToastInstance toastInstance)
        {
            if (!KatieSaveHelperModConfig.showToasts.Value)
                return false;

            if (!QueueToast(toastInstance))
                return false;

            return true;
        }

        public static void TryQueueAndLogToast(string message)
        {
            KatieLogger.Info(message);
            TryQueueToast(message);
        }

        IEnumerator PlayQueuedToasts()
        {
            while (queue.Count > 0)
            {
                ToastInstance toastInstance = queue.Dequeue();
                yield return PlaySingleToast(toastInstance);
                if (toastInstance.gapTime != null)
                    yield return new WaitForSecondsRealtime(toastInstance.gapTime.Value);
                else
                    yield return new WaitForSecondsRealtime(GapTime);
            }

            worker = null;
        }

        IEnumerator PlaySingleToast(ToastInstance toastInstance)
        {
            if (updateToken)
                UpdateToastFromSettings();

            toast.text = toastInstance.message;

            toast.enabled = true;

            // Fade‑in
            yield return FadeSmooth(0f, Opacity, FadeInTime);

            // Hold
            if (toastInstance.holdTime != null)
                yield return new WaitForSecondsRealtime(toastInstance.holdTime.Value);
            else
                yield return new WaitForSecondsRealtime(HoldTime);

            // Fade‑out
            yield return FadeSmooth(Opacity, 0f, FadeOutTime);

            toast.enabled = false;
        }

        IEnumerator Fade(float from, float to, float time)
        {
            for (float t = 0; t < time; t += Time.unscaledDeltaTime)
            {
                float alpha = Mathf.Lerp(from, to, t / time);
                toast.alpha = alpha;
                yield return null;
            }
            toast.alpha = to;
            yield return null;
        }

        IEnumerator FadeSmooth(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float u = elapsed / duration;
                u = u * u * (3f - 2f * u);
                toast.alpha = Mathf.Lerp(from, to, u);
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }
            toast.alpha = to;
            yield return null;
        }

        public bool UpdateToastFromSettings()
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

            // Store timings and opacity so values can't change while toast is being played
            FadeInTime = ToastSettings.FadeInTime;
            FadeOutTime = ToastSettings.FadeOutTime;
            HoldTime = ToastSettings.HoldTime;
            GapTime = ToastSettings.GapTime;
            Opacity = ToastSettings.Opacity;

            updateToken = false;
            return true;
        }

        public void OnDestroy()
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
