using System.Collections;
using System.Threading;
using TMPro;
using UnityEngine;

namespace KatieSaveHelper.Features.API
{
    public class ToastPlayback
    {
        private TextMeshProUGUI _textMesh;
        public ToastHandle handle { get; private set; }
        public CancellationTokenSource fadeInHoldCts { get; private set; }
        public CancellationTokenSource fadeOutCts { get; private set; }
        public float holdTimer { get; private set; } = 0f;
        public PlayState state { get; private set; } = PlayState.Inactive;
        public string DisplayedMessage
        {
            get
            {
                if (state == PlayState.Inactive) return string.Empty;

                return _textMesh.text ?? string.Empty;
            }
        }

        public enum PlayState
        {
            Inactive,
            Holding,
            FadingIn,
            FadingOut
        }

        public ToastPlayback(TextMeshProUGUI toastTextMesh, ToastHandle toastHandle)
        {
            _textMesh = toastTextMesh;
            handle = toastHandle;

            _textMesh.text = toastHandle.instance.message;
        }

        public IEnumerator ShowToast()
        {
            if (_textMesh.enabled || state != PlayState.Inactive) yield break;

            _textMesh.enabled = true;
            holdTimer = 0f;
            fadeInHoldCts = new CancellationTokenSource();
            fadeOutCts = new CancellationTokenSource();

            try
            {
                // Fade‑in
                state = PlayState.FadingIn;
                yield return FadeSmooth(0f, handle.instance.Opacity, handle.instance.FadeInTime, fadeInHoldCts.Token);

            HoldToast:
                // Hold
                state = PlayState.Holding;
                yield return HoldToast(fadeInHoldCts.Token);

                // Fade‑out
                state = PlayState.FadingOut;
                yield return FadeSmooth(_textMesh.alpha, 0f, handle.instance.FadeOutTime, fadeOutCts.Token);

                // If fade-out was cancelled, loop back to holding state with a fresh timer and full opacity
                if (fadeOutCts.IsCancellationRequested && !fadeInHoldCts.IsCancellationRequested)
                {
                    fadeOutCts = new CancellationTokenSource();
                    holdTimer = 0f;
                    _textMesh.alpha = handle.instance.Opacity;
                    goto HoldToast;
                }
            }
            finally
            {
                state = PlayState.Inactive;
                _textMesh.enabled = false;
            }
        }

        private IEnumerator HoldToast(CancellationToken token = default)
        {
            while (holdTimer < handle.instance.HoldTime)
            {
                if (token.IsCancellationRequested) yield break;

                yield return null;
                holdTimer += Time.unscaledDeltaTime;
            }
        }

        public void ResetToastHold()
        {
            if (state == PlayState.FadingOut)
                fadeOutCts.Cancel();
            else
                holdTimer = 0f;
        }

        public void ChangeDisplayedMessage(string message)
        {
            if (state == PlayState.Inactive) return;

            _textMesh.text = message;
        }

        public void CancelToast() => fadeInHoldCts.Cancel();

        private IEnumerator FadeSmooth(float from, float to, float duration, CancellationToken token = default)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (token.IsCancellationRequested) yield break;

                float u = elapsed / duration;
                u = u * u * (3f - 2f * u);
                _textMesh.alpha = Mathf.Lerp(from, to, u);
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }
            if (!token.IsCancellationRequested)
                _textMesh.alpha = to;
            yield return null;
        }
    }
}
