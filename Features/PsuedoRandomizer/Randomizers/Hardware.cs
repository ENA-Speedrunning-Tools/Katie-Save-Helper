using JoelG.ENA4;
using System.Collections.Generic;

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
        public override string DisplayName => "Hardware Mode";
        private readonly int enaTaxiMoodIdHash = SaveRandomizer.GetStableHashCode("ENATaxiMoodRand");
        public override List<PsuedoTargetEvent> GetNewTargetEventList()
        {
            var targetEventList = new List<PsuedoTargetEvent>();

            if (KatieConfig.Settings.psuedoRandomEnaTaxiMood.Value != EnaTaxiMood.Any)
                targetEventList.Add(PsuedoTargetEvent.Create("EnaTaxiMood", EvaluateEnaTaxiMood, KatieConfig.Settings.psuedoRandomEnaTaxiMood.Value));

            return targetEventList;
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
