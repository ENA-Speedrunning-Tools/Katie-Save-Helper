using UnityEngine;
using TMPro;
using System.Collections;
using System.Linq;
using System;

namespace KatieSaveHelper
{
    public static class ToastSettings
    {
        public static event Action OnStyleChanged;
        public static TMP_FontAsset Font { get; private set; }
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
            var font = KatieAssetHandler.LoadFontAsset(KatieConfig.Settings.toastFontFileName.Value);
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
            var colorTuple = KatieConfig.Settings.toastFontColor.TryGetColorFromValue();
            if (colorTuple.success)
                FontColor = colorTuple.color;

            // Outline Color
            colorTuple = KatieConfig.Settings.toastOutlineColor.TryGetColorFromValue();
            if (colorTuple.success)
                OutlineColor = colorTuple.color;

            TextAlignment = KatieConfig.Settings.toastAlignment.Value;

            FontSize = Mathf.Max(0, KatieConfig.Settings.toastFontSize.Value);
            OutlineWidth = KatieUtil.Clamp((float)KatieConfig.Settings.toastOutlineWidth.Value / 100, 0f, 1f);
            Opacity = KatieUtil.Clamp((float)KatieConfig.Settings.toastOpacity.Value / 100, 0f, 1f);

            FadeInTime = Mathf.Max(0, KatieConfig.Settings.toastFadeInTime.Value);
            FadeOutTime = Mathf.Max(0, KatieConfig.Settings.toastFadeOutTime.Value);
            HoldTime = Mathf.Max(0, KatieConfig.Settings.toastHoldTime.Value);
            GapTime = Mathf.Max(0, KatieConfig.Settings.toastGapTime.Value);

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
}
