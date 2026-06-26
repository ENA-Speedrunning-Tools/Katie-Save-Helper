using JoelG.ENA4;
using Newtonsoft.Json;
using HarmonyLib;
using System.Reflection;
using KatieSaveHelper.Features.API;
using System.Runtime.CompilerServices;
using System;

namespace KatieSaveHelper.Patches
{
    // Extension method to deep copy game data instances

    public static class SaveFileData_Patch
    {
        private static FieldInfo localDialogueDataField = AccessTools.Field(typeof(SaveFileData), "localDialogueData");
        private static FieldInfo sessionTraversalHistoryField = AccessTools.Field(typeof(SaveFileData), "sessionTraversalHistory");

        public static SaveFileData Clone(this SaveFileData data)
        {
            var copy = JsonConvert.DeserializeObject<SaveFileData>(
                JsonConvert.SerializeObject(data)
            );

            // Handle json ignored fields

            var localDialogueData = (CaseInsensitiveDataBank)localDialogueDataField.GetValue(data);
            var sessionTraversalHistory = (SaveDataNodeTraversalHistory)sessionTraversalHistoryField.GetValue(data);

            if (localDialogueData != null)
            {
                localDialogueDataField.SetValue(copy,
                    JsonConvert.DeserializeObject<CaseInsensitiveDataBank>(
                        JsonConvert.SerializeObject(localDialogueData)
                    )
                );
            }

            if (sessionTraversalHistory != null)
            {
                sessionTraversalHistoryField.SetValue(copy, JsonConvert.DeserializeObject<SaveDataNodeTraversalHistory>(
                    JsonConvert.SerializeObject(sessionTraversalHistory)
                ));
            }

            // Copy modded save data

            data.CopyCustomDataTo(copy);

            return copy;
        }
    }
}
