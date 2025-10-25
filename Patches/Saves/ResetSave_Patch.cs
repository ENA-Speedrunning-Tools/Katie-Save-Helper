using JoelG.ENA4;
using System.Reflection;
using HarmonyLib;

namespace KatieSaveHelper.Patches
{
    // Insert custom psuedo-random seed into new save if the 'use psuedo random seed on new saves' option is enabled

    [HarmonyPatch(typeof(SaveFile), nameof(SaveFile.ResetSave))]
    public static class ResetSave_Patch
    {
        private static readonly FieldInfo saveHashField = AccessTools.Field(typeof(SaveFileData), "saveHash");
        private static readonly MethodInfo getGameFileMethod = AccessTools.Method(typeof(SaveFile), "GetGameFile");
        static bool Prefix(int index)
        {
            KatieUtil.TriggerCustomEvent(CustomEventType.OnCreateSave);

            RemoteGameFile<SaveFileData> gameFile = (RemoteGameFile<SaveFileData>)getGameFileMethod.Invoke(null, new object[] { index });

            gameFile.Data = new SaveFileData();

            int newSeed;

            if (KatieSaveHelperModActions.customSeedOnReset.IsReady)
            {
                newSeed = KatieSaveHelperModActions.customSeedOnReset.TakeValue();
                ToastController.TryQueueAndLogToast($"Reset Save {index} with seed {newSeed}");
            }
            else
            {
                newSeed = KatieUtil.GenerateSaveSeed().seed;
            }

            saveHashField.SetValue(gameFile.Data, newSeed);

            gameFile.ValidData = true;
            gameFile.WriteFile();

            return false;
        }

    }
}
