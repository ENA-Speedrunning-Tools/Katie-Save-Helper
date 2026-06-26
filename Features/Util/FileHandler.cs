using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using System.Threading;

namespace KatieSaveHelper.Features.Util
{
    public class FileHandler
    {
        public string filePath { get; private set; }
        public string groupIdentifier { get; private set; }

        private int maxRetries;
        private float retryDelaySeconds;

        public FileHandler(string filePath, string groupIdentifier, int actionMaxRetries = 10, float actionRetryDelaySeconds = 0.05f)
        {
            this.filePath = filePath;
            this.groupIdentifier = groupIdentifier;
            this.maxRetries = Math.Max(0, actionMaxRetries);
            this.retryDelaySeconds = Math.Max(0f, actionRetryDelaySeconds);
        }

        // Method for debug purposes
        private void SimulateFileAccess(FileAccess access, float timeInSeconds)
        {
            Task.Run(() =>
            {
                FileStream fs = null;
                try
                {
                    fs = new FileStream(filePath, FileMode.Open, access, FileShare.None);
                    KatieLogger.Info($"File at '{filePath}' opened with '{access}' permissions for {timeInSeconds} seconds");
                    int sleepTime = Math.Max(0, (int)(timeInSeconds * 1000));
                    Thread.Sleep(sleepTime);
                    KatieLogger.Info($"File at '{filePath}' closed");
                }
                catch (Exception ex)
                {
                    KatieLogger.Error($"SimulateFileAccess error: {ex}");
                }
                finally
                {
                    if (fs != null)
                        fs.Dispose();
                }
            });
        }

        public async Task FileActionAsync(
            FileMode requiredMode,
            FileAccess requiredAccess,
            Action fileAction,
            CancellationToken token = default,
            Action onSuccess = null,
            Action onFail = null,
            Action onFinish = null,
            FileShare requiredSharing = FileShare.Read
        )
        {
            if (fileAction == null) return;

            int attempts = 0;
            bool loaded = false;

            while (!loaded && attempts < maxRetries)
            {
                if (token.IsCancellationRequested) break;

                bool ioError = false;

                try
                {
                    FileAccess allowedAccess = FileAccess.ReadWrite;
                    if (File.Exists(filePath) && (File.GetAttributes(filePath) & FileAttributes.ReadOnly) != 0)
                        allowedAccess = FileAccess.Read;

                    if ((requiredAccess & allowedAccess) != requiredAccess)
                    {
                        KatieLogger.Warning($"Required file access ({requiredAccess}) is not permitted for '{filePath}'. Skipping file operation.");
                        break;
                    }

                    using (var fs = new FileStream(filePath, requiredMode, allowedAccess, requiredSharing)) { }

                    fileAction();
                    loaded = true;
                    onSuccess?.Invoke();
                }
                catch (IOException)
                {
                    ioError = true;
                    attempts++;
                }
                catch (Exception ex)
                {
                    KatieLogger.Warning($"Unexpected file error: {ex}");
                    break;
                }

                if (!loaded && ioError)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds), token);
                    }
                    catch (TaskCanceledException)
                    {
                        // Cancellation requested during delay
                        break;
                    }
                }
            }

            if (!loaded)
                onFail?.Invoke();

            onFinish?.Invoke();
        }

        public IEnumerator FileActionRoutine(
            FileMode requiredMode,
            FileAccess requiredAccess,
            Action fileAction,
            CancellationToken token = default,
            Action onSuccess = null,
            Action onFail = null,
            Action onFinish = null,
            FileShare requiredSharing = FileShare.Read
        )
        {
            if (fileAction == null) yield break;

            if (token.IsCancellationRequested) yield break;

            int attempts = 0;
            bool loaded = false;

            while (!loaded && attempts < maxRetries)
            {
                bool ioError = false;

                try
                {
                    FileAccess allowedAccess = FileAccess.ReadWrite;
                    if (File.Exists(filePath) && (File.GetAttributes(filePath) & FileAttributes.ReadOnly) != 0)
                        allowedAccess = FileAccess.Read;

                    if ((requiredAccess & allowedAccess) != requiredAccess)
                    {
                        KatieLogger.Warning($"Required file access ({requiredAccess}) is not permitted for '{filePath}'. Skipping file operation.");
                        break;
                    }

                    using (FileStream fs = new FileStream(filePath, requiredMode, allowedAccess, requiredSharing)) { }

                    fileAction();
                    loaded = true;
                    onSuccess?.Invoke();
                }
                catch (IOException)
                {
                    ioError = true;
                    attempts++;
                }
                catch (Exception ex)
                {
                    KatieLogger.Warning($"Unexpected file error: {ex}");
                    break;
                }

                if (!loaded && ioError)
                    yield return new WaitForSeconds(retryDelaySeconds);

                if (token.IsCancellationRequested)
                    break;
            }

            if (!loaded)
                onFail?.Invoke();

            onFinish?.Invoke();
        }

        public object RunWithFile(string identifier, FileMode requiredMode, FileAccess requiredAccess, Action fileAction, bool async = false, Action onSuccess = null, Action onFail = null, Action onFinish = null, FileShare requiredSharing = FileShare.Read)
        {
            if (async)
                return RunWithFileAsync(identifier, requiredMode, requiredAccess, fileAction, onSuccess, onFail, onFinish, requiredSharing);
            else
                return RunWithFileRoutine(identifier, requiredMode, requiredAccess, fileAction, onSuccess, onFail, onFinish, requiredSharing);
        }

        public StaticCoroutine RunWithFileRoutine(string identifier, FileMode requiredMode, FileAccess requiredAccess, Action fileAction, Action onSuccess = null, Action onFail = null, Action onFinish = null, FileShare requiredSharing = FileShare.Read) =>
            StaticCoroutine.Start(sc =>
                FileActionRoutine(requiredMode, requiredAccess, fileAction, sc.CancelToken, onSuccess, onFail, onFinish, requiredSharing),
                identifier,
                groupIdentifier
            );

        public TrackedTask RunWithFileAsync(string identifier, FileMode requiredMode, FileAccess requiredAccess, Action fileAction, Action onSuccess = null, Action onFail = null, Action onFinish = null, FileShare requiredSharing = FileShare.Read) =>
            TrackedTask.Start(tt =>
                FileActionAsync(requiredMode, requiredAccess, fileAction, tt.CancelToken, onSuccess, onFail, onFinish, requiredSharing),
                identifier,
                groupIdentifier
            );
    }
}
