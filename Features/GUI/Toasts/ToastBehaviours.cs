using System;

namespace KatieSaveHelper
{
    public static class ToastBehaviours
    {

        // First try extending any playing toasts in the same group. If this fails, and there are no other toasts with the same group queued, queue a new toast
        public static void Notice(string message, string groupName, bool tryExtend = true, bool sendToLog = true, bool pushToFront = false)
        {
            if (tryExtend && ToastController.TryExtendPlayingToast(toast => toast.instance.groupName == groupName)) return;

            if (!ToastController.AnyToastQueued(toast => toast.groupName == groupName))
            {
                ToastController.TryQueueToast(message, groupName, pushToFront);
                if (sendToLog) KatieLogger.Info(message);
            }
        }

        public static void Notice(ToastInstance instance, bool tryExtend = true, bool sendToLog = true, bool pushToFront = false)
        {
            if (tryExtend && ToastController.TryExtendPlayingToast(toast => toast.instance.groupName == instance.groupName)) return;

            if (!ToastController.AnyToastQueued(toast => toast.groupName == instance.groupName))
            {
                ToastController.TryQueueToast(instance, pushToFront);
                if (sendToLog) KatieLogger.Info(instance.message);
            }
        }

        // First try extending any playing toasts in the same group. If this fails, cancel any queued toasts in the same group and queue a new toast
        public static void QuickAction(string message, string groupName, bool tryExtend = true, bool sendToLog = true)
        {
            if (tryExtend && ToastController.TryExtendPlayingToast(toast => toast.instance.groupName == groupName)) return;

            ToastController.TryCancelQueuedToasts(toast => toast.instance.groupName == groupName);
            ToastController.TryQueueToast(message, groupName);
            if (sendToLog) KatieLogger.Info(message);
        }

        public static void QuickAction(ToastInstance instance, bool tryExtend = true, bool sendToLog = true)
        {
            if (tryExtend && ToastController.TryExtendPlayingToast(toast => toast.instance.groupName == instance.groupName)) return;

            ToastController.TryCancelQueuedToasts(toast => toast.instance.groupName == instance.groupName);
            ToastController.TryQueueToast(instance);
            if (sendToLog) KatieLogger.Info(instance.message);
        }

        // If there is a currently playing toast with a matching group, extend it and change it's displayed message. If not, use the 'quick action' toast behaviour
        public static void Toggle(string message, string groupName, bool sendToLog = true)
        {
            if (ToastController.Instance?.CurrentlyPlayingToast?.handle?.InstanceOrNull?.groupName == groupName)
            {
                ToastController.Instance.CurrentlyPlayingToast.ChangeDisplayedMessage(message);
                ToastController.Instance.CurrentlyPlayingToast.ResetToastHold();
                if (sendToLog) KatieLogger.Info(message);
            }
            else
            {
                QuickAction(message, groupName, tryExtend: false, sendToLog);
            }
        }

        // Begins an activity toast sequence by either extending an existing toast in the same group or queueing a new one
        // Always returns a valid ToastHandle (real or fallback) for use with EndActivity()
        public static ToastHandle BeginActivity(string message, string groupName, bool tryExtend = true, bool sendToLog = true)
        {
            ToastHandle introHandle = null;

            if (tryExtend && ToastController.TryExtendPlayingToast(toast => toast.instance.groupName == groupName))
            {
                introHandle = ToastController.Instance?.CurrentlyPlayingToast?.handle;
            }
            else
            {
                var toastTuple = ToastController.TryQueueToast(message, groupName);
                if (toastTuple.success)
                    introHandle = toastTuple.toastHandle;
            }

            // Return a dummy handle
            if (introHandle == null) introHandle = new ToastHandle(new ToastInstance(message, groupName));

            if (sendToLog) KatieLogger.Info(message);

            return introHandle;
        }

        // Completes an activity toast sequence, ensures the starting and finishing toasts play as a pair while cancelling related toasts
        public static void EndActivity(ToastHandle introHandle, string message, string groupName, Func<ToastHandle, bool> additionalCancelPredicate = null, bool sendToLog = true)
        {
            ToastController.TryCancelQueuedOrPlayingToasts(h =>
            {
                bool isTarget =
                    h.instance.groupName == introHandle.instance.groupName ||
                    h.instance.groupName == groupName ||
                    (additionalCancelPredicate?.Invoke(h) == true);

                // Do not cancel the playing toast if it's handle matches introHandle
                if (introHandle != null && h == introHandle && h.IsPlaying)
                    return false;

                return isTarget;
            });

            // Queue (or re-queue) the introHandle's toast if it hasn't been played yet
            if (introHandle.HasPlayed != true)
                ToastController.TryQueueToast(introHandle.instance);

            ToastController.TryQueueToast(message, groupName, pushToFront: introHandle.IsPlaying);

            if (sendToLog) KatieLogger.Info(message);
        }
    }
}
