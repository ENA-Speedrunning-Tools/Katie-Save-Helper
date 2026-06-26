using HarmonyLib;
using JoelG.ENA4;
using LMirman.Utilities;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Linq;
using KatieSaveHelper.Features.Util;

namespace KatieSaveHelper.Patches
{

    [HarmonyPatch]
    public static class GetDataAsEncryptedByteArray_Patch
    {
        static MethodBase TargetMethod()
        {
            var method = typeof(GameFile<SaveFileData>)
                .GetMethods()
                .First(m =>
                    m.Name == "GetDataAsEncryptedByteArray" &&
                    m.GetParameters().Length == 1);

            return method;
        }

        [HarmonyPrefix]
        public static bool Prefix(object __instance, object data, ref byte[] __result)
        {
            try
            {
                if (__instance == null)
                    return true;

                Type runtimeType = __instance.GetType();

                Type gameFileType = null;

                while (runtimeType != null)
                {
                    if (runtimeType.IsGenericType && runtimeType.GetGenericTypeDefinition() == typeof(GameFile<>))
                    {
                        gameFileType = runtimeType;
                        break;
                    }

                    runtimeType = runtimeType.BaseType;
                }

                if (gameFileType == null)
                    return true;

                Type saveType = gameFileType.GetGenericArguments()[0];

                dynamic gameFile = __instance;

                string json = KatieUtil.SerializeExtendedSave(data, gameFile.jsonSerializeSettings, Formatting.None);

                __result = gameFile.encryptor.Encrypt(json);

                return false;
            }
            catch (Exception ex)
            {
                KatieLogger.Error($"Error during GetDataAsEncryptedByteArray: {ex.ToString()}");
                throw;
            }
        }
    }

    [HarmonyPatch]
    public static class GetDataAsJsonByteArray_Patch
    {
        static MethodBase TargetMethod()
        {
            var method = typeof(GameFile<SaveFileData>)
                .GetMethods()
                .First(m =>
                    m.Name == "GetDataAsJsonByteArray" &&
                    m.GetParameters().Length == 1);

            return method;
        }

        [HarmonyPrefix]
        public static bool Prefix(object __instance, object data, ref byte[] __result)
        {
            try
            {
                if (__instance == null)
                    return true;

                Type runtimeType = __instance.GetType();

                Type gameFileType = null;

                while (runtimeType != null)
                {
                    if (runtimeType.IsGenericType && runtimeType.GetGenericTypeDefinition() == typeof(GameFile<>))
                    {
                        gameFileType = runtimeType;
                        break;
                    }

                    runtimeType = runtimeType.BaseType;
                }

                if (gameFileType == null)
                    return true;

                Type saveType = gameFileType.GetGenericArguments()[0];

                dynamic gameFile = __instance;

                string json = KatieUtil.SerializeExtendedSave(data, gameFile.jsonSerializeSettings, Formatting.Indented);

                __result = Encoding.UTF8.GetBytes(json);

                return false;
            }
            catch (Exception ex)
            {
                KatieLogger.Error($"Error during GetDataAsJsonByteArray: {ex.ToString()}");
                throw;
            }
        }
    }

    [HarmonyPatch(typeof(GameFile<SaveFileData>))]
    public static class GameFile_Patch
    {
        // Ensure modded save data is deserialized along with vanilla save data

        [HarmonyPatch(typeof(GameFile<SaveFileData>), nameof(GameFile<SaveFileData>.LoadData))]
        public static class LoadData_Patch
        {

            [HarmonyPrefix]
            public static bool Prefix(object __instance, byte[] bytes)
            {
                try
                {
                    if (__instance == null)
                        return true;

                    Type runtimeType = __instance.GetType();

                    Type gameFileType = null;

                    while (runtimeType != null)
                    {
                        if (runtimeType.IsGenericType && runtimeType.GetGenericTypeDefinition() == typeof(GameFile<>))
                        {
                            gameFileType = runtimeType;
                            break;
                        }

                        runtimeType = runtimeType.BaseType;
                    }

                    if (gameFileType == null)
                        return true;

                    Type saveType = gameFileType.GetGenericArguments()[0];

                    dynamic gameFile = __instance;

                    string json;

                    try
                    {
                        json = gameFile.encryptor.Decrypt(bytes);
                    }
                    catch (Exception ex)
                    {
                        json = Encoding.UTF8.GetString(bytes);
                    }

                    object data = KatieUtil.DeserializeExtendedSave(json, saveType, gameFile.jsonDeserializeSettings);

                    PropertyInfo dataProperty = gameFileType.GetProperty("Data");
                    dataProperty?.SetValue(__instance, data);

                    PropertyInfo validDataProperty = gameFileType.GetProperty("ValidData");
                    validDataProperty?.SetValue(__instance, true);

                    MethodInfo updateFileSyncTime = gameFileType.GetMethod("UpdateFileSyncTime");
                    updateFileSyncTime?.Invoke(__instance, null);

                    FieldInfo fileReadField = AccessTools.Field(gameFileType, "FileRead");
                    var fileReadDelegate = fileReadField.GetValue(__instance);
                    MethodInfo invokeMethod = fileReadDelegate?.GetType().GetMethod("Invoke");
                    invokeMethod?.Invoke(fileReadDelegate, new object[] { __instance });

                    return false;
                }
                catch (Exception ex)
                {
                    KatieLogger.Error($"Error during LoadData: {ex.ToString()}");
                    throw;
                }
            }
        }

        // Helper method to write JSON data to a .dat file instead of a .json file

        public static void WriteFileAsJsonDat(object instance)
        {
            try
            {
                if (instance == null)
                    return;

                Type runtimeType = instance.GetType();

                Type gameFileType = null;

                while (runtimeType != null)
                {
                    if (runtimeType.IsGenericType && runtimeType.GetGenericTypeDefinition() == typeof(GameFile<>))
                    {
                        gameFileType = runtimeType;
                        break;
                    }

                    runtimeType = runtimeType.BaseType;
                }

                if (gameFileType == null)
                    return;

                PropertyInfo validDataProperty = gameFileType.GetProperty("ValidData");

                bool validData = (bool)validDataProperty.GetValue(instance);

                if (!validData)
                    return;

                FieldInfo dataPathField = AccessTools.Field(gameFileType, "dataPath");
                FieldInfo fileDirectoryField = AccessTools.Field(gameFileType, "fileDirectory");
                FieldInfo fileWrittenField = AccessTools.Field(gameFileType, "FileWritten");

                string dataPath = (string)dataPathField.GetValue(instance);

                string fileDirectory = (string)fileDirectoryField.GetValue(instance);

                MethodInfo getJsonMethod = gameFileType.GetMethod("GetDataAsJsonByteArray", new[] { gameFileType.GetGenericArguments()[0] });

                byte[] jsonBytes;

                if (getJsonMethod != null)
                {
                    object data = gameFileType.GetProperty("Data").GetValue(instance);

                    jsonBytes = (byte[])getJsonMethod.Invoke(instance, new[] { data });
                }
                else
                {
                    KatieLogger.Error("Error during WriteFileToJsonDat: Could not find method GetDataAsJsonByteArray(T)");
                    return;
                }

                Directory.CreateDirectory(fileDirectory);

                File.WriteAllBytes(dataPath, jsonBytes);

                MethodInfo updateFileSyncTime = gameFileType.GetMethod("UpdateFileSyncTime");
                updateFileSyncTime?.Invoke(instance, null);

                object fileWrittenDelegate = fileWrittenField.GetValue(instance);
                fileWrittenDelegate?.GetType().GetMethod("Invoke")?.Invoke(fileWrittenDelegate, new[] { instance });
            }
            catch (Exception ex)
            {
                KatieLogger.Error($"Error during WriteFileAsJsonDat: {ex.ToString()}");
                throw;
            }
        }

        // Modified to make use of the modified PeekFile method

        [HarmonyPatch("ReadFile")]
        [HarmonyPrefix]
        public static bool ReadFile_Prefix(object __instance, ref bool __result)
        {
            try
            {
                if (__instance == null)
                    return true;

                Type runtimeType = __instance.GetType();

                Type gameFileType = null;

                while (runtimeType != null)
                {
                    if (runtimeType.IsGenericType && runtimeType.GetGenericTypeDefinition() == typeof(GameFile<>))
                    {
                        gameFileType = runtimeType;
                        break;
                    }

                    runtimeType = runtimeType.BaseType;
                }

                if (gameFileType == null)
                    return true;

                Type saveType = gameFileType.GetGenericArguments()[0];

                MethodInfo peekFileMethod = runtimeType.GetMethod("PeekFile");
                if (peekFileMethod == null)
                {
                    KatieLogger.Error($"Error during ReadFile: Could not get method 'PeekFile' from type '{gameFileType.GetType().Name}' ({gameFileType.GetType().FullName})");
                    return true;
                }

                object[] args = { null };

                bool peekResult = (bool)peekFileMethod.Invoke(__instance, args);

                if (!peekResult)
                {
                    __result = false;
                    return false;
                }

                object data = args[0];

                PropertyInfo dataProperty = gameFileType.GetProperty("Data");
                if (dataProperty == null)
                {
                    KatieLogger.Error($"Error during ReadFile: Could not get property 'Data' from type '{gameFileType.GetType().Name}' ({gameFileType.GetType().FullName})");
                    return true;
                }
                dataProperty.SetValue(__instance, data);

                PropertyInfo validDataProperty = gameFileType.GetProperty("ValidData");
                if (validDataProperty == null)
                {
                    KatieLogger.Error($"Error during ReadFile: Could not get property 'ValidData' from type '{gameFileType.GetType().Name}' ({gameFileType.GetType().FullName})");
                    return true;
                }
                validDataProperty?.SetValue(__instance, true);


                MethodInfo updateFileSyncTimeMethod = gameFileType.GetMethod("UpdateFileSyncTime");
                if (updateFileSyncTimeMethod == null)
                {
                    KatieLogger.Error($"Error during ReadFile: Could not get method 'UpdateFileSyncTime' from type '{gameFileType.GetType().Name}' ({gameFileType.GetType().FullName})");
                    return true;
                }
                updateFileSyncTimeMethod?.Invoke(__instance, null);


                FieldInfo fileReadField = KatieUtil.GetField(runtimeType, "FileRead");
                if (fileReadField == null)
                {
                    KatieLogger.Error($"Error during ReadFile: Could not get field 'FileRead' from type '{gameFileType.GetType().Name}' ({gameFileType.GetType().FullName})");
                    return true;
                }

                var fileReadDelegate = fileReadField.GetValue(__instance);
                if (fileReadDelegate == null)
                {
                    KatieLogger.Error($"Error during ReadFile: Could not get a value from field 'FileRead' on an object of type '{gameFileType.GetType().Name}' ({gameFileType.GetType().FullName})");
                    return true;
                }

                MethodInfo invokeMethod = fileReadDelegate.GetType().GetMethod("Invoke");
                if (invokeMethod == null)
                {
                    KatieLogger.Error($"Error during ReadFile: Could not get method 'Invoke' from type '{fileReadDelegate.GetType().Name}' ({fileReadDelegate.GetType().FullName})");
                    return true;
                }
                invokeMethod?.Invoke(fileReadDelegate, new object[] { __instance });

                __result = true;

                return false;
            }
            catch (Exception ex)
            {
                KatieLogger.Error($"Error during ReadFile: {ex.ToString()}");
                throw;
            }
        }

        // Instead of trying to read from exclusively a .dat or .json file, trying reading both encrypted and JSON byte format from the .dat file, and use whichever works

        [HarmonyPatch("PeekFile")]
        [HarmonyPrefix]
        public static bool PeekFile_Patch(object __instance, ref object data, ref bool __result)
        {
            if (__instance == null)
                return true;

            Type runtimeType = __instance.GetType();

            Type gameFileType = null;

            while (runtimeType != null)
            {
                if (runtimeType.IsGenericType && runtimeType.GetGenericTypeDefinition() == typeof(GameFile<>))
                {
                    gameFileType = runtimeType;
                    break;
                }

                runtimeType = runtimeType.BaseType;
            }

            if (gameFileType == null)
                return true;

            Type saveType = gameFileType.GetGenericArguments()[0];

            PropertyInfo pathProp = AccessTools.Property(gameFileType, "Path");
            if (pathProp == null)
            {
                KatieLogger.Error($"Error during PeekFile: Could not get property 'Path' from type '{gameFileType.GetType().Name}' ({gameFileType.GetType().FullName})");
            }
            string path = (string)pathProp.GetValue(__instance);

            if (!File.Exists(path))
            {
                data = null;
                __result = false;
                return false;
            }

            byte[] bytes = File.ReadAllBytes(path);

            dynamic instance = __instance;

            string json;

            try
            {
                json = instance.encryptor.Decrypt(bytes);
            }
            catch (Exception ex)
            {
                json = Encoding.UTF8.GetString(bytes);
            }

            try
            {
                data = KatieUtil.DeserializeExtendedSave(json, saveType, instance.jsonDeserializeSettings);

                __result = true;
                return false;
            }
            catch (Exception ex)
            {
                KatieLogger.Error($"Error during DeserializeExtendedSave: {ex}");
                throw;
            }
        }

        // Prevent the game from automatically overwritting pre-existing save data on disk with Steam Remote Storage save data on launch, if configured by the mod

        [HarmonyPatch("WriteFile", new Type[] { })]
        [HarmonyPrefix]
        public static bool WriteFile_Prefix(object __instance)
        {
            if (!(__instance.GetType().IsGenericType && __instance.GetType().GetGenericTypeDefinition() == typeof(RemoteGameFile<>)) || KatieConfig.Settings.disableRemoteSaveSync.Value == false)
                return true;

            var stackTrace = new StackTrace();
            var frames = stackTrace.GetFrames();

            if (frames == null)
                return true;

            foreach (var frame in frames)
            {
                var method = frame.GetMethod();
                if (method == null) continue;

                if (method.DeclaringType.IsGenericType && method.DeclaringType.GetGenericTypeDefinition() == typeof(RemoteGameFile<>) && method.Name.Contains("MigrateSaveFromRemote"))
                    return false;
            }

            return true;
        }

        // Prevent WriteFile from writing encrypted saves if the setting to disable save file encryption is enabled

        [HarmonyPatch("WriteFile", new Type[] { })]
        [HarmonyPrefix]
        public static bool BaseWriteFile_Prefix(object __instance, ref bool __result)
        {
            if (__instance == null)
                return true;

            Type runtimeType = __instance.GetType();

            Type gameFileType = null;

            while (runtimeType != null)
            {
                if (runtimeType.IsGenericType && runtimeType.GetGenericTypeDefinition() == typeof(GameFile<>))
                {
                    gameFileType = runtimeType;
                    break;
                }

                runtimeType = runtimeType.BaseType;
            }

            if (gameFileType == null)
                return true;

            Type saveType = gameFileType.GetGenericArguments()[0];

            if ((saveType == typeof(MetaSaveFileData) && KatieConfig.Settings.disableMetaSaveFileEncryption.Value == true) ||
                (saveType == typeof(SaveFileData) && KatieConfig.Settings.disableSaveFileEncryption.Value == true))
            {
                WriteFileAsJsonDat(__instance);

                __result = true;
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