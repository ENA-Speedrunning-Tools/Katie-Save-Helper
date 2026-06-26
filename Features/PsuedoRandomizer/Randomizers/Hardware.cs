using JoelG.ENA4;
using System.Collections.Generic;
using KatieSaveHelper.Features.Util;

namespace KatieSaveHelper
{
    public enum EnaTaxiMood
    {
        Any,
        Salesman,
        Meanie
    }
    internal class KatiePseudoHardwareRandomizer : KatiePseudoRandomizerBase
    {
        public override string DisplayName => "Hardware Mode";
        private readonly int enaTaxiMoodIdHash = SaveRandomizer.GetStableHashCode("ENATaxiMoodRand");
        public override List<PseudoTargetEvent> GetNewTargetEventList()
        {
            var targetEventList = new List<PseudoTargetEvent>();

            if (KatieConfig.Settings.pseudoRandomEnaTaxiMood.Value != EnaTaxiMood.Any)
                targetEventList.Add(PseudoTargetEvent.Create("EnaTaxiMood", EvaluateEnaTaxiMood, KatieConfig.Settings.pseudoRandomEnaTaxiMood.Value));

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
