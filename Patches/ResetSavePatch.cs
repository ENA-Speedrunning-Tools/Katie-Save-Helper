using JoelG.ENA4;
using KatieSaveHelper.PsuedoRandomizer;
using System.Reflection;
using HarmonyLib;

namespace KatieSaveHelper.Patches
{
    // Insert custom psuedo-random seed into new save if the 'use psuedo random seed on new saves' option is enabled

    [HarmonyPatch(typeof(SaveFile), nameof(SaveFile.ResetSave))]
    public class ResetSavePatch
    {
        private static readonly FieldInfo saveHashField = typeof(SaveFileData).GetField("saveHash", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo getGameFileMethod = typeof(SaveFile).GetMethod("GetGameFile", BindingFlags.NonPublic | BindingFlags.Static);
        static bool Prefix(int index)
        {

            RemoteGameFile<SaveFileData> gameFile = (RemoteGameFile<SaveFileData>)getGameFileMethod.Invoke(null, new object[] { index });

            gameFile.Data = new SaveFileData();

            if (KatieSaveHelperModConfig.createPsuedoRandomSaves.Value && !KatieSaveHelperMod.forceCustomSeedOnReset)
            {
                var psuedoResult = KatiePsuedoRandomizer.GeneratePsuedoRandomSeed();
                if (psuedoResult.success)
                {
                    saveHashField.SetValue(gameFile.Data, psuedoResult.seed);
                }
            }

            gameFile.ValidData = true;
            gameFile.WriteFile();
            return false;
        }

    }
}
