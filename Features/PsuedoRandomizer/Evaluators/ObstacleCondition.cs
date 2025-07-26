using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KatieSaveHelper
{
    public enum PurgeObstacle : int
    {
        FishBar = 0,
        FishChain = 1,
        FishChomp = 2,
        FishStruggle = 3,
        FishThrash = 4,
        ManyFishFlipped = 5,
        WanderingFish = 6,
        SneakAttack = 7
    }

    public enum LogicType
    {
        Any,
        MustBe,
        MustNotBe
    }

    public class ObstacleCondition
    {
        public LogicType Logic { get; set; }
        public PurgeObstacle[] Values { get; set; } = Array.Empty<PurgeObstacle>();

        private HashSet<PurgeObstacle> valueSet;

        public void Compile()
        {
            valueSet = new HashSet<PurgeObstacle>(Values);
        }

        public bool Evaluate(PurgeObstacle value)
        {
            switch (Logic)
            {
                case LogicType.Any:
                    return true;
                case LogicType.MustBe:
                    return valueSet.Contains(value);
                case LogicType.MustNotBe:
                    return !valueSet.Contains(value);
                default:
                    return false;
            }
        }

        public ObstacleCondition Copy()
        {
            var copy = new ObstacleCondition
            {
                Logic = this.Logic,
                Values = (PurgeObstacle[])this.Values.Clone()
            };
            copy.Compile();
            return copy;
        }

        public static List<ObstacleCondition> FullParseConditionList(string input, List<PurgeDirection> roomOrder)
        {
            var tuple = ParseConditionTuple(input);
            return ResolveConditionTuple(tuple, roomOrder);
        }

        public static List<ObstacleCondition> ConvertStringListToConditionList(List<string> entries)
        {
            var result = new List<ObstacleCondition>();
            ObstacleCondition cond;

            Action addAnyCond = () =>
            {
                cond = new ObstacleCondition { Logic = LogicType.Any };
                cond.Compile();
                result.Add(cond);
            };

            foreach (var rawEntry in entries)
            {
                var entry = rawEntry.Trim();

                LogicType logicType;
                if (entry.StartsWith("!") && entry.Length > 1)
                {
                    logicType = LogicType.MustNotBe;
                    entry = entry.Substring(1).Trim();
                }
                else
                {
                    logicType = LogicType.MustBe;
                }

                if (string.Equals(entry, "Any", StringComparison.OrdinalIgnoreCase))
                {
                    addAnyCond();
                    continue;
                }

                string[] names;

                if (entry.StartsWith("[") && entry.EndsWith("]"))
                {
                    var inner = entry.Substring(1, entry.Length - 2); // safely strip []
                    names = inner.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                }
                else
                {
                    names = new[] { entry };
                }

                var parsedNames = names
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToArray();

                if (parsedNames.Length == 0)
                {
                    addAnyCond();
                    continue;
                }

                var values = new List<PurgeObstacle>();

                foreach (var name in parsedNames)
                {
                    if (Enum.TryParse<PurgeObstacle>(name, true, out var parsed))
                        values.Add(parsed);
                }

                if (values.Count == 0)
                {
                    addAnyCond();
                    continue;
                }

                cond = new ObstacleCondition { Logic = logicType, Values = values.ToArray() };
                cond.Compile();
                result.Add(cond);
            }

            return result;
        }
        public static (int shorthandLevel, List<ObstacleCondition> conditions) ParseConditionTuple(string input)
        {
            // Parse shorthand level
            int shorthandLevel = 0;
            while (shorthandLevel < input.Length && input[shorthandLevel] == '*')
                shorthandLevel++;
            input = input.Substring(shorthandLevel).Trim();

            // Split input into string list by top level commas

            string segment;
            List<string> stringEntries = new List<string>();
            int bracketDepth = 0;
            int lastSplit = 0;

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '[')
                    bracketDepth++;
                else if (input[i] == ']')
                    bracketDepth--;
                else if (input[i] == ',' && bracketDepth == 0)
                {
                    // Split here
                    segment = input.Substring(lastSplit, i - lastSplit).Trim();
                    if (!string.IsNullOrWhiteSpace(segment))
                        stringEntries.Add(segment);
                    lastSplit = i + 1;
                }
            }
            // Add last segment
            if (lastSplit < input.Length)
                stringEntries.Add(input.Substring(lastSplit).Trim());

            // Clamp string entries based on shorthand level
            switch (shorthandLevel)
            {
                case 0:
                    break;
                case 1:
                    stringEntries = stringEntries.Take(5).ToList();
                    break;
                case 2:
                    stringEntries = stringEntries.Take(2).ToList();
                    break;
                default:
                    stringEntries = stringEntries.Take(1).ToList();
                    break;
            }

            // Convert strings to conditions
            List<ObstacleCondition> conditionList = ConvertStringListToConditionList(stringEntries);

            return (shorthandLevel, conditionList);
        }

        public static List<ObstacleCondition> ResolveConditionTuple((int shorthandLevel, List<ObstacleCondition> conditions) conditionTuple, List<PurgeDirection> roomOrder)
        {
            if (conditionTuple.shorthandLevel <= 0 || conditionTuple.conditions.Count == 0) return conditionTuple.conditions;

            var tupleCondList = conditionTuple.conditions;
            var parsedCondList = new List<ObstacleCondition>();
            var roomOrderCopy = roomOrder.Take(4).ToList();

            ObstacleCondition cond;
            Action addAnyCond = () =>
            {
                cond = new ObstacleCondition { Logic = LogicType.Any };
                cond.Compile();
                parsedCondList.Add(cond);
            };

            Action<PurgeDirection, int> addDirectionalCond = (dir, i) =>
            {
                switch (dir)
                {
                    case PurgeDirection.Forward:
                        addAnyCond();
                        parsedCondList.Add(tupleCondList[i].Copy());
                        addAnyCond();
                        break;
                    case PurgeDirection.Left:
                        parsedCondList.Add(tupleCondList[i].Copy());
                        addAnyCond();
                        addAnyCond();
                        break;
                    case PurgeDirection.Right:
                        addAnyCond();
                        addAnyCond();
                        parsedCondList.Add(tupleCondList[i].Copy());
                        break;
                    case PurgeDirection.Any:
                        parsedCondList.Add(tupleCondList[i].Copy());
                        parsedCondList.Add(tupleCondList[i].Copy());
                        parsedCondList.Add(tupleCondList[i].Copy());
                        break;
                }
            };

            switch (conditionTuple.shorthandLevel)
            {
                case 1:
                    int index = 0;
                    roomOrderCopy.Insert(0, PurgeDirection.Forward);
                    foreach (PurgeDirection direction in roomOrderCopy)
                    {
                        if (index >= tupleCondList.Count)
                            break;

                        addDirectionalCond(direction, index);

                        index++;
                    }
                    break;
                case 2:
                    // Forward
                    addDirectionalCond(PurgeDirection.Forward, 0);

                    if (tupleCondList.Count < 2)
                        break;

                    foreach (PurgeDirection direction in roomOrderCopy)
                    {
                        addDirectionalCond(direction, 1);
                    }
                    break;
                default: // 3 or above
                    roomOrderCopy.Insert(0, PurgeDirection.Forward);
                    foreach (PurgeDirection direction in roomOrderCopy)
                    {
                        addDirectionalCond(direction, 0);
                    }
                    break;

            }
            return parsedCondList;
        }

    }
}
