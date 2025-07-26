using JoelG.ENA4;

namespace KatieSaveHelper
{
    public enum EnaTaxiMood
    {
        Any,
        Salesman,
        Meanie
    }
    internal class KatiePsuedoHardwareRandomizer : KatiePsuedoRandomizerBase
    {

        private readonly int enaTaxiMoodIdHash = SaveRandomizer.GetStableHashCode("ENATaxiMoodRand");
        public override void FillTargetEventList()
        {
            targetEventList.Clear();

            if (KatieSaveHelperModConfig.psuedoRandomEnaTaxiMood.Value != EnaTaxiMood.Any)
                targetEventList.Add(PsuedoTargetEvent.Create("EnaTaxiMood", EvaluateEnaTaxiMood, KatieSaveHelperModConfig.psuedoRandomEnaTaxiMood.Value));
        }

        private bool EvaluateEnaTaxiMood(int baseHash, EnaTaxiMood mood)
        {
            bool isSalesman = KatieUtil.GetChance(baseHash, enaTaxiMoodIdHash, 0.5f);
            return isSalesman == (mood == EnaTaxiMood.Salesman);
        }

        public EnaTaxiMood EvaluateEnaTaxiMood(int baseHash)
        {
            bool isSalesman = KatieUtil.GetChance(baseHash, enaTaxiMoodIdHash, 0.5f);
            return isSalesman ? EnaTaxiMood.Salesman : EnaTaxiMood.Meanie;
        }
    }
}
