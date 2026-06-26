using System.Linq;

namespace KatieSaveHelper.Features.API
{
    public class ToastHandle
    {
        public ToastInstance instance { get; private set; }
        public ToastInstance? InstanceOrNull => instance;
        public bool IsQueued
        {
            get
            {
                if (ToastController.Instance?.Queue?.Contains(this) == true)
                {
                    _markedQueued = true;
                    return true;
                }
                return false;
            }
        }
        public bool IsPlaying
        {
            get
            {
                if (ToastController.Instance?.CurrentlyPlayingToast?.handle == this)
                {
                    _markedPlayed = true;
                    return true;
                }
                return false;
            }
        }

        public bool HasPlayed
        {
            get
            {
                if (_markedPlayed) return true;
                return IsPlaying;
            }
        }

        public bool BeenQueued
        {
            get
            {
                if (_markedQueued) return true;
                return IsQueued;
            }
        }

        private bool _markedPlayed = false;
        private bool _markedQueued = false;

        public ToastHandle(ToastInstance toastInstance)
        {
            instance = toastInstance;
        }

        internal void Update()
        {
            _ = IsPlaying;
            _ = IsQueued;
        }
    }
}
