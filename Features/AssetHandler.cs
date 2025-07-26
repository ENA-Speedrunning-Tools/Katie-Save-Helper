using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO.Compression;
using TMPro;
using UnityEngine.TextCore.LowLevel;
using UnityEngine;
using System.Threading;

namespace KatieSaveHelper
{

    internal static class KatieAssetHandler
    {
        private static readonly string Owner = "ENA-Speedrunning-Tools";
        private static readonly string Repo = "Katie-Save-Helper";
        private static readonly string Branch = "assets";
        private static readonly string assetUrl = $"https://codeload.github.com/{Owner}/{Repo}/zip/refs/heads/{Branch}";

        private static readonly int shaCheckTimeout = 10;
        private static readonly int zipDownloadTimeout = 20;

        public static readonly string dllDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static readonly string appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), KatieSaveHelperMod.modGUID);

        public static readonly string updateCacheFile = Path.Combine(appDataDir, $"cached_update.sha");
        public static readonly string assetDir = GetAssetDir();
        public static readonly string tempDir = Path.Combine(Path.GetTempPath(), KatieSaveHelperMod.modGUID);
        public static readonly string tempAssetZip = Path.Combine(tempDir, $"new_assets.zip");
        public static readonly string tempAssetDir = Path.Combine(tempDir, $"new_assets");

        public static bool isSyncing { get; private set; } = false;

        public static void OnStartup()
        {
            TryCreateDefaultAssetDir();
            if (KatieSaveHelperModConfig.assetSubcriber.Value)
                _ = SyncAssetsAsync();
        }

        private static bool CanWriteHere(string dir, bool cleanAfter = true)
        {
            bool dirExisted = Directory.Exists(dir);
            string testFile = Path.Combine(dir, Guid.NewGuid().ToString() + ".tmp");

            try
            {
                if (!dirExisted)
                    Directory.CreateDirectory(dir);

                using (File.Create(testFile, 1, FileOptions.DeleteOnClose)) { }

                return true;
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
            finally
            {
                if (cleanAfter && !dirExisted && Directory.Exists(dir))
                {
                    try { Directory.Delete(dir, recursive: false); } catch { }
                }
            }

            return false;
        }

        private static string GetAssetDir()
        {
            string candidate = Path.Combine(dllDir, KatieSaveHelperMod.modGUID);

            if (CanWriteHere(candidate))
                return candidate;

            string fallback = Path.Combine(appDataDir, KatieSaveHelperMod.modGUID);
            return fallback;
        }

        public static void TryCreateDefaultAssetDir()
        {
            string candidate = Path.Combine(assetDir, "Fonts");

            CanWriteHere(candidate, cleanAfter:false);
        }

        private static HttpClient CreateClient()
        {
            var c = new HttpClient
            {
                Timeout = Timeout.InfiniteTimeSpan
            };
            c.DefaultRequestHeaders.UserAgent.ParseAdd("AssetSync/1.0");
            c.DefaultRequestHeaders.ConnectionClose = true;
            return c;
        }

        private static void DisposeClient(HttpClient http)
        {
            http.CancelPendingRequests();
            http.Dispose();
            http = null;
        }

        private static async Task<string> GetLatestCommitShaAsync(HttpClient http)
        {
            string api = $"https://api.github.com/repos/{Owner}/{Repo}/commits/{Branch}";

            Directory.CreateDirectory(appDataDir);

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(shaCheckTimeout)))
            {
                // pass the token here
                using (var response = await http.GetAsync(api, cts.Token))
                {
                    response.EnsureSuccessStatusCode();

                    string json = await response.Content.ReadAsStringAsync();   // convert to string
                    var m = Regex.Match(json, "\"sha\"\\s*:\\s*\"([0-9a-f]{40})\"");
                    return m.Success ? m.Groups[1].Value : null;
                }
            }
        }

        public static async Task<int> SyncAssetsAsync()
        {
            isSyncing = true;

            HttpClient http = CreateClient();

            try
            {
                string latestSha = await GetLatestCommitShaAsync(http);
                if (string.IsNullOrEmpty(latestSha))
                {
                    KatieLogger.Error("Could not fetch latest commit SHA, unable to attempt asset sync");
                    return 2;
                }

                KatieLogger.Info("Syncing assets...");

                string cachedSha = File.Exists(updateCacheFile) ? File.ReadAllText(updateCacheFile) : null;

                if (latestSha == cachedSha && Directory.Exists(assetDir))
                {
                    KatieLogger.Info($"Assets already up-to-date ({latestSha.Substring(0, 7)})");
                    return 1;
                }

                KatieLogger.Info($"Assets found! ({latestSha.Substring(0, 7)})");

                bool success = await DownloadAndMergeZipAsync(http);

                if (success)
                {
                    File.WriteAllText(updateCacheFile, latestSha);
                    KatieLogger.Info($"Asset update complete!");
                    return 0;
                }
                else
                {
                    KatieLogger.Error("Asset update failed");
                    return 2;
                }
            }
            finally
            {
                DisposeClient(http);
                isSyncing = false;
            }
        }

        private static async Task<bool> DownloadWithProgressAndTimeoutAsync(HttpClient http, string url, string destinationFile, TimeSpan inactivityTimeout)
        {
            try
            {
                using (var response = await http.GetAsync(url,
                                   HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    using (var src = await response.Content.ReadAsStreamAsync())
                    using (var dst = File.Create(destinationFile))
                    {
                        var buffer = new byte[8192];
                        int n;
                        while (true)
                        {
                            using (var cts = new CancellationTokenSource(inactivityTimeout))
                            {
                                n = await src.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                            }
                            if (n == 0) break;
                            await dst.WriteAsync(buffer, 0, n);
                        }
                    }
                }
                return true;
            }
            catch (OperationCanceledException)
            {
                KatieLogger.Error($"Download stalled: no data received for {inactivityTimeout.Seconds}s");
                return false;
            }
            catch (Exception ex)
            {
                KatieLogger.Error($"Download failed: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> DownloadAndMergeZipAsync(HttpClient http)
        {
            Directory.CreateDirectory(tempDir);

            KatieLogger.Info($"Downloading asset zip... ({assetUrl})");

            bool downloadSuccess = await DownloadWithProgressAndTimeoutAsync(http, assetUrl, tempAssetZip, TimeSpan.FromSeconds(zipDownloadTimeout));
            if (!downloadSuccess)
                return false;

            KatieLogger.Info("Extracting asset zip...");

            if (Directory.Exists(tempAssetDir))
                Directory.Delete(tempAssetDir, true);
            Directory.CreateDirectory(tempAssetDir);

            try
            {
                using (var stream = File.OpenRead(tempAssetZip))
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Name)) continue; // skip directories

                        if (entry.FullName.StartsWith("__MACOSX") || entry.FullName.EndsWith("/"))
                            continue;

                        string relPath = RemoveFirstDirectory(entry.FullName);
                        string targetPath = Path.Combine(assetDir, relPath);

                        bool fileExists = File.Exists(targetPath);

                        // If there is a matching file with the same name at the target location, check if they are identical
                        if (fileExists)
                        {
                            // Check file lengths first
                            var targetInfo = new FileInfo(targetPath);
                            if (targetInfo.Length == entry.Length)
                            {
                                // write entry to a temporary file inside tempAssetDir so their data can be compared
                                string tmpFile = Path.Combine(tempAssetDir, Guid.NewGuid().ToString());
                                Directory.CreateDirectory(Path.GetDirectoryName(tmpFile));

                                using (var tmpStream = File.Create(tmpFile))
                                using (var entryStream = entry.Open())
                                    await entryStream.CopyToAsync(tmpStream);

                                bool filesIdentical = FilesAreEqual(tmpFile, targetPath);
                                File.Delete(tmpFile);

                                if (filesIdentical)
                                {
                                    KatieLogger.Info($"Unchanged: {relPath}");
                                    continue;
                                }
                            }
                        }

                        // Copy entry file to new file at target path
                        Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                        using (var entryStream = entry.Open())
                        using (var fileStream = File.Create(targetPath))
                            await entryStream.CopyToAsync(fileStream);

                        if (!fileExists)
                            KatieLogger.Info($"Added: {relPath}");
                        else
                            KatieLogger.Info($"Updated: {relPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                KatieLogger.Error($"Extraction failed: {ex.Message}");
                return false;
            }
            finally
            {
                if (File.Exists(tempAssetZip)) File.Delete(tempAssetZip);
                if (Directory.Exists(tempAssetDir)) Directory.Delete(tempAssetDir, true);
            }
            return true;
        }

        private static bool FilesAreEqual(string pathA, string pathB)
        {
            // Check data byte by byte
            const int bufferSize = 1024 * 1024; // 1  MB
            byte[] bufferA = new byte[bufferSize];
            byte[] bufferB = new byte[bufferSize];

            using (var fsA = File.OpenRead(pathA))
            using (var fsB = File.OpenRead(pathB))
            {
                int readA;
                while ((readA = fsA.Read(bufferA, 0, bufferA.Length)) > 0)
                {
                    int readB = fsB.Read(bufferB, 0, bufferB.Length);
                    if (readA != readB)
                        return false; // shouldn’t happen if lengths are equal

                    for (int i = 0; i < readA; i++)
                        if (bufferA[i] != bufferB[i])
                            return false;
                }
            }
            return true; // all chunks matched
        }

        private static string RemoveFirstDirectory(string path)
        {
            // From "repo-branch/folder/file.txt" -> "folder/file.txt"
            int slash = path.IndexOf('/');
            return (slash >= 0 && slash < path.Length - 1) ? path.Substring(slash + 1) : path;
        }

        public static TMP_FontAsset LoadFontAsset(string fontName)
        {
            string ttfPath = Path.Combine(assetDir, "Fonts", $"{fontName}.ttf");

            if (!File.Exists(ttfPath))
            {
                KatieLogger.Warning($"Font file not found: {ttfPath}");
                return null;
            }

            try
            {
                var dynFont = new Font(ttfPath);
                if (dynFont == null)
                {
                    KatieLogger.Error($"Failed to load Font from '{ttfPath}'");
                    return null;
                }

                TMP_FontAsset tmp = TMP_FontAsset.CreateFontAsset(dynFont, 90, 32, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);

                if (tmp == null)
                {
                    KatieLogger.Error($"TMP could not create font asset for '{fontName}'");
                    return null;
                }

                tmp.name = fontName;
                return tmp;
            }
            catch (Exception ex)
            {
                KatieLogger.Error($"Exception while loading font '{fontName}': {ex}");
                return null;
            }
        }
    }
}
