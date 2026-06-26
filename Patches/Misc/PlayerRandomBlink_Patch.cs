using HarmonyLib;
using JoelG.ENA4;
using System.Reflection;
using KatieSaveHelper.Features.API;

namespace KatieSaveHelper.Patches
{
    [HarmonyPatch(typeof(PlayerRandomBlink))]
    public static class PlayerRandomBlink_Patch
    {
        private static readonly PropertyInfo CanBlinkProp = AccessTools.Property(typeof(PlayerRandomBlink), "CanBlink");
        private static readonly FieldInfo blinkChanceGeneratorField = AccessTools.Field(typeof(PlayerRandomBlink), "blinkChanceGenerator");
        private static readonly FieldInfo sessionHashField = AccessTools.Field(typeof(SaveRandomizerHashes), "PlaySessionHash");

        private static bool firstBlink = false;
        private static int blinkAttemptNumber = 0;

        [HarmonyPatch("AttemptBlink")]
        [HarmonyPostfix]
        public static void AttemptBlink_Postfix(PlayerRandomBlink __instance)
        {
            bool CanBlink = (bool)CanBlinkProp.GetValue(__instance);
            if (!CanBlink || firstBlink) return;

            blinkAttemptNumber += 1;
            if (KatieConfig.Settings.notifyOnFirstBlinkAttempts.Value)
                ToastController.TryQueueToast($"Blink Attempt #{blinkAttemptNumber}");
        }

        [HarmonyPatch("Blink")]
        [HarmonyPostfix]
        public static void Blink_Postfix()
        {
            firstBlink = true;
        }

        public static void ResetBlinkChanceGenerator()
        {
            int currentSessionHash = (int)sessionHashField.GetValue(null);
            KatieLogger.Info($"Blink Randomizer reset with seed {currentSessionHash}");
            var newRandom = new System.Random(currentSessionHash);
            blinkChanceGeneratorField.SetValue(null, newRandom);

            firstBlink = false;
            blinkAttemptNumber = 0;
        }
    }
}
