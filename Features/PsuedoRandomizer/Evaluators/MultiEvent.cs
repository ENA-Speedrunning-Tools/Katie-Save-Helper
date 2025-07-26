using JoelG.ENA4;
using System.Collections.Generic;
using System.Linq;

namespace KatieSaveHelper
{
    public class MultiEventInstance
    {
        public string Name { get; }
        public int Weight { get; }

        public MultiEventInstance(string name, int weight)
        {
            Name = name;
            Weight = weight;
        }
    }

    public class MultiEventRunner
    {
        private string eventID;
        private int eventHash;
        private List<MultiEventInstance> events;
        private int totalWeights;

        public MultiEventRunner(string eventID, params MultiEventInstance[] events)
        {
            this.eventID = eventID;
            this.eventHash = SaveRandomizer.GetStableHashCode(this.eventID);
            this.events = new List<MultiEventInstance>(events);
            this.totalWeights = events?.Sum(e => e.Weight) ?? 0;
        }

        public bool Evaluate(int baseHash, int desiredIndex)
        {
            if (desiredIndex == -1)
                return true;

            int index = 0;
            int num = 0;
            double chanceRollValue = KatieUtil.GetChanceRollValue(baseHash, eventHash);
            foreach (MultiEventInstance @event in events)
            {
                num += @event.Weight;
                if (chanceRollValue <= (double)num / (double)totalWeights)
                    return index == desiredIndex;
                index++;
            }
            return false;
        }

        public MultiEventInstance Evaluate(int baseHash)
        {
            int index = 0;
            int num = 0;
            double chanceRollValue = KatieUtil.GetChanceRollValue(baseHash, eventHash);
            foreach (MultiEventInstance @event in events)
            {
                num += @event.Weight;
                if (chanceRollValue <= (double)num / (double)totalWeights)
                    return @event;
                index++;
            }
            return null;
        }
    }
}
