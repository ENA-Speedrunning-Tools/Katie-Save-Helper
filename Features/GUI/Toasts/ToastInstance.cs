using System;

namespace KatieSaveHelper.Features.API
{
    public struct ToastInstance
    {
        private float? _fadeInTime;
        private float? _fadeOutTime;
        private float? _holdTime;
        private float? _gapTime;
        private float? _opacity;

        public string message { get; private set; }
        public string groupName { get; private set; }
        public float FadeInTime => _fadeInTime.HasValue ? Math.Max(_fadeInTime.Value, ToastSettings.FadeInTime) : ToastSettings.FadeInTime;
        public float FadeOutTime => _fadeOutTime.HasValue ? Math.Max(_fadeOutTime.Value, ToastSettings.FadeOutTime) : ToastSettings.FadeOutTime;
        public float HoldTime => _holdTime.HasValue ? Math.Max(_holdTime.Value, ToastSettings.HoldTime) : ToastSettings.HoldTime;
        public float GapTime => _gapTime.HasValue ? Math.Max(_gapTime.Value, ToastSettings.GapTime) : ToastSettings.GapTime;
        public float Opacity => _opacity.HasValue ? Math.Max(_opacity.Value, ToastSettings.Opacity) : ToastSettings.Opacity;

        public ToastInstance(string message, string groupName = "None", float? fadeInTime = null, float? fadeOutTime = null, float? holdTime = null, float? gapTime = null, float? opacity = null)
        {
            this.message = message;
            this.groupName = groupName;
            _fadeInTime = fadeInTime;
            _fadeOutTime = fadeOutTime;
            _holdTime = holdTime;
            _gapTime = gapTime;
            _opacity = opacity;
        }

        public ToastInstance(ToastInstance toastInstance)
        {
            message = toastInstance.message;
            groupName = toastInstance.groupName;
            _fadeInTime = toastInstance.FadeInTime;
            _fadeOutTime = toastInstance.FadeOutTime;
            _holdTime = toastInstance.HoldTime;
            _gapTime = toastInstance.GapTime;
            _opacity = toastInstance.Opacity;
        }
    }
}
