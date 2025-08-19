using HarmonyLib;
using JoelG.ENA4;
using LMirman.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;


namespace KatieSaveHelper
{
    [HarmonyPatch(typeof(GameFile<SaveFileData>))]
    public static class GameFile_Patch
    {
        private static readonly FieldInfo dataPathField = AccessTools.Field(typeof(GameFile<SaveFileData>), "dataPath");
        private static readonly FieldInfo fileDirectoryField = AccessTools.Field(typeof(GameFile<SaveFileData>), "fileDirectory");
        private static readonly FieldInfo fileWrittenField = AccessTools.Field(typeof(GameFile<SaveFileData>), "FileWritten");
        private static readonly FieldInfo fileReadField = AccessTools.Field(typeof(GameFile<SaveFileData>), "FileRead");
        private static readonly MethodInfo readFileMethod = AccessTools.Method(typeof(GameFile<SaveFileData>), "ReadFile");
        private static readonly MethodInfo readFileFromJsonMethod = AccessTools.Method(typeof(GameFile<SaveFileData>), "ReadFileFromJson");
        private static readonly MethodInfo readFileFromBytesMethod = AccessTools.Method(typeof(GameFile<SaveFileData>), "ReadFileFromBytes");
        private static readonly MethodInfo getDataAsJsonByteArrayMethod = AccessTools.Method(typeof(GameFile<SaveFileData>), "GetDataAsJsonByteArray");
        private static readonly MethodInfo peekFileMethod = AccessTools.Method(typeof(GameFile<SaveFileData>), "PeekFile");

        // Extension to write JSON data to a .dat file instead of a .json file

        public static void WriteFileAsJsonDat(this GameFile<SaveFileData> __instance)
        {
            if (!__instance.ValidData) return;

            string dataPath = (string)dataPathField.GetValue(__instance);
            byte[] jsonByteArray = (byte[])getDataAsJsonByteArrayMethod.Invoke(__instance, null);
            string fileDirectory = (string)fileDirectoryField.GetValue(__instance);
            var fileWrittenDelegate = fileWrittenField.GetValue(__instance) as Action<GameFile<SaveFileData>>;

            Directory.CreateDirectory(fileDirectory);
            File.WriteAllBytes(dataPath, jsonByteArray);
            __instance.UpdateFileSyncTime();
            fileWrittenDelegate?.Invoke(__instance);
        }

        // Modified to make use of the modified PeekFile method

        [HarmonyPatch("ReadFile")]
        [HarmonyPrefix]
        public static bool ReadFile_Prefix(GameFile<SaveFileData> __instance, ref bool __result)
        {
            object[] parameters = new object[] { null };
            bool peekResult = (bool)peekFileMethod.Invoke(__instance, parameters);

            if (!peekResult)
            {
                __result = false;
                return false;
            }

            SaveFileData data = (SaveFileData)parameters[0];

            __instance.Data = data;
            __instance.ValidData = true;
            __instance.UpdateFileSyncTime();

            var fileReadDelegate = fileReadField.GetValue(__instance) as Action<GameFile<SaveFileData>>;
            fileReadDelegate?.Invoke(__instance);

            __result = true;
            return false;
        }

        // Instead of trying to read from exclusively a .dat or .json file, trying reading both encrypted and JSON byte format from the .dat file, and use whichever works

        [HarmonyPatch("PeekFile")]
        [HarmonyPrefix]
        public static bool PeekFile_Patch(GameFile<SaveFileData> __instance, ref SaveFileData data, ref bool __result)
        {
            string dataPath = (string)dataPathField.GetValue(__instance);

            if (!File.Exists(dataPath))
            {
                data = default;
                __result = false;
                return false;
            }

            byte[] bytes = File.ReadAllBytes(dataPath);

            try
            {
                data = __instance.GetDataFromEncryptedByteArray(bytes);
            }
            catch
            {
                data = __instance.GetDataFromJsonByteArray(bytes);
            }

            __result = true;
            return false;
        }

        // Prevent the game from automatically overwritting pre-existing save data on disk with Steam Remote Storage save data on launch

        [HarmonyPatch("WriteFile", new Type[] { })]
        [HarmonyPrefix]
        public static bool WriteFile_Prefix(object __instance)
        {
            if (!(__instance.GetType().IsGenericType && __instance.GetType().GetGenericTypeDefinition() == typeof(RemoteGameFile<>)) || KatieSaveHelperModConfig.disableRemoteSaveSync.Value == false)
                return true;

            var stackTrace = new StackTrace();
            var frames = stackTrace.GetFrames();

            if (frames == null)
                return true;

            foreach (var frame in frames)
            {
                var method = frame.GetMethod();
                if (method == null) continue;

                if (method.DeclaringType.IsGenericType && method.DeclaringType.GetGenericTypeDefinition() == typeof(RemoteGameFile<>) && method.Name.Contains("::MigrateSaveFromRemote>"))
                    return false;
            }

            return true;
        }

        // This patch is just to force one of the methods mentioned just above to show up in the stack trace

        [HarmonyPatch(typeof(RemoteGameFile<SaveFileData>), nameof(RemoteGameFile<SaveFileData>.MigrateSaveFromRemote))]
        public static class RemoteGameFile_Patch
        {
            public static bool Prefix()
            {
                return true;
            }
        }
    }
}