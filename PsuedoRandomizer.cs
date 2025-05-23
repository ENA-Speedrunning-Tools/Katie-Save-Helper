using JoelG.ENA4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace KatieSaveHelper.PsuedoRandomizer
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
    public enum PurgeDirection
    {
        Any,
        Forward,
        Left,
        Right
    }

    public enum TaxiHead : int
    {
        Any = -1,
        Creisi = 0,
        Doom = 1,
        Socio = 2
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

        public bool EvaluateVerbose(PurgeObstacle value)
        {
            var str = "Condition Members : ";
            foreach (PurgeObstacle obstacle in valueSet)
            {
                str += $"{obstacle.ToString()}, ";
            }
            KatieSaveHelperMod.mls.LogInfo(str);

            switch (Logic)
            {
                case LogicType.Any:
                    KatieSaveHelperMod.mls.LogInfo("Logic: Any, returning True");
                    return true;
                case LogicType.MustBe:
                    KatieSaveHelperMod.mls.LogInfo($"Logic: MustBe, returning {valueSet.Contains(value)}");
                    return valueSet.Contains(value);
                case LogicType.MustNotBe:
                    KatieSaveHelperMod.mls.LogInfo($"Logic: MustNotBe, returning {!valueSet.Contains(value)}");
                    return !valueSet.Contains(value);
                default:
                    KatieSaveHelperMod.mls.LogInfo($"Logic: Default, returning False");
                    return false;
            }
        }

        public static List<ObstacleCondition> ParseConditionList(List<string> configEntries)
        {
            var result = new List<ObstacleCondition>();
            ObstacleCondition cond;

            foreach (var rawEntry in configEntries)
            {
                var entry = rawEntry.Trim();

                if (string.Equals(entry, "Any", StringComparison.OrdinalIgnoreCase))
                {
                    cond = new ObstacleCondition { Logic = LogicType.Any };
                    cond.Compile();
                    result.Add(cond);
                    continue;
                }

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
                    cond = new ObstacleCondition { Logic = LogicType.Any };
                    cond.Compile();
                    result.Add(cond);
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
                    result.Add(new ObstacleCondition { Logic = LogicType.Any });
                    continue;
                }

                cond = new ObstacleCondition { Logic = logicType, Values = values.ToArray() };
                cond.Compile();
                result.Add(cond);
            }

            while (result.Count < 4)
            {
                cond = new ObstacleCondition { Logic = LogicType.Any };
                cond.Compile();
                result.Add(cond);
            }

            return result;
        }

        public static List<string> ParseEntryList(string input)
        {
            var result = new List<string>();
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
                    result.Add(input.Substring(lastSplit, i - lastSplit).Trim());
                    lastSplit = i + 1;
                }
            }

            // Add last segment
            if (lastSplit < input.Length)
                result.Add(input.Substring(lastSplit).Trim());

            return result;
        }

        public static List<string> ParseEntryList(string input, List<PurgeDirection> roomOrder)
        {
            var result1 = new List<string>();
            var result2 = new List<string>();
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
                    result1.Add(input.Substring(lastSplit, i - lastSplit).Trim());
                    lastSplit = i + 1;
                }
            }

            // Add last segment
            if (lastSplit < input.Length)
                result1.Add(input.Substring(lastSplit).Trim());

            if (!result1[0].StartsWith("*"))
            {
                return result1;
            }

            result1[0] = result1[0].Substring(1).Trim();

            int index = 0;
            var roomOrderCopy = roomOrder.Take(4).ToList();
            roomOrderCopy.Insert(0, PurgeDirection.Forward);
            foreach (PurgeDirection direction in roomOrderCopy)
            {
                switch (direction)
                {
                    case PurgeDirection.Any:
                        result2.Add(result1[index]);
                        result2.Add(result1[index]);
                        result2.Add(result1[index]);
                        break;
                    case PurgeDirection.Forward:
                        result2.Add("Any");
                        result2.Add(result1[index]);
                        result2.Add("Any");
                        break;
                    case PurgeDirection.Left:
                        result2.Add(result1[index]);
                        result2.Add("Any");
                        result2.Add("Any");
                        break;
                    case PurgeDirection.Right:
                        result2.Add("Any");
                        result2.Add("Any");
                        result2.Add(result1[index]);
                        break;

                }
                index++;
            }
            return result2;
        }

    }

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

    internal class MultiEventRunner
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
            {
                return true;
            }

            int index = 0;
            int num = 0;
            double chanceRollValue = KatieSaveRandomizer.GetChanceRollValue(baseHash, eventHash);
            foreach (MultiEventInstance @event in events)
            {
                num += @event.Weight;
                if (chanceRollValue <= (double)num / (double)this.totalWeights)
                {
                    return index == desiredIndex;
                }
                index++;
            }
            return false;
        }
    }

    internal class KatiePsuedoRandomizer
    {
        private static int maxAllowedAttempts = 0;
        public static bool isBusy = false;
        static int targetTaxiHead;
        static List<PurgeDirection> targetPurgeRooms;
        static List<ObstacleCondition> targetPurgeObstacles;
        static readonly int purgeGoalsIdHash = SaveRandomizer.GetStableHashCode("purge_goals");
        static readonly int purgeRoomsIdHash = SaveRandomizer.GetStableHashCode("purge_rooms");
        static readonly MultiEventRunner taxiHeads = new MultiEventRunner(
                "UB_TaxiDriver",
                new MultiEventInstance("Creisi", 1),
                new MultiEventInstance("Doom", 1),
                new MultiEventInstance("Socio", 1)
                );

        public static (bool success, int seed) GeneratePsuedoRandomSeed()
        {
            isBusy = true;
            KatieSaveHelperMod.mls.LogInfo("Searching for seed...");

            maxAllowedAttempts = KatieSaveHelperModConfig.psuedoRandomMaxAttempts.Value;
            targetTaxiHead = (int)KatieSaveHelperModConfig.psuedoRandomTargetTaxiHead.Value;
            targetPurgeRooms = ParseRoomOrder(KatieSaveHelperModConfig.psuedoRandomTargetPurgeRoomGoals.Value);
            targetPurgeObstacles = ObstacleCondition.ParseConditionList(ObstacleCondition.ParseEntryList(KatieSaveHelperModConfig.psuedoRandomTargetPurgeRoomObstacles.Value, targetPurgeRooms));

            bool searchDirectionFlag = new Random().NextDouble() >= 0.5;

            int attemptCount = 0;

            int minValue;
            Func<int> rollSeed;

            if (KatieSaveHelperModConfig.psuedoRandomNaturalSeedsOnly.Value)
            {
                rollSeed = SaveRandomizer.GetAbsolutelyRandomValue;
                minValue = 0;
            }
            else
            {
                rollSeed = GetRandomInt32;
                minValue = int.MinValue;
            }

            int currentSeed = rollSeed();
            int headFails = 0;
            int roomFails = 0;
            int obstacleFails = 0;

            if (searchDirectionFlag)
            {
                while (attemptCount < maxAllowedAttempts)
                {
                    attemptCount++;

                    if (currentSeed > int.MaxValue || attemptCount % 10000 == 0)
                    {
                        currentSeed = rollSeed();
                    }

                    if (!taxiHeads.Evaluate(currentSeed, targetTaxiHead))
                    {
                        currentSeed++;
                        headFails++;
                        continue;
                    }

                    if (!EvaluatePurgeRooms(currentSeed, targetPurgeRooms))
                    {
                        currentSeed++;
                        roomFails++;
                        continue;
                    }

                    if (!EvaluatePurgeObstacles(currentSeed, targetPurgeObstacles))
                    {
                        currentSeed++;
                        obstacleFails++;
                        continue;
                    }

                    break;
                }
            }
            else
            {
                while (attemptCount < maxAllowedAttempts)
                {
                    attemptCount++;

                    if (currentSeed < minValue || attemptCount % 10000 == 0)
                    {
                        currentSeed = rollSeed();
                    }

                    if (!taxiHeads.Evaluate(currentSeed, targetTaxiHead))
                    {
                        currentSeed--;
                        headFails++;
                        continue;
                    }

                    if (!EvaluatePurgeRooms(currentSeed, targetPurgeRooms))
                    {
                        currentSeed--;
                        roomFails++;
                        continue;
                    }

                    if (!EvaluatePurgeObstacles(currentSeed, targetPurgeObstacles))
                    {
                        currentSeed--;
                        obstacleFails++;
                        continue;
                    }

                    break;
                }
            }

            isBusy = false;

            if (attemptCount >= maxAllowedAttempts)
            {
                KatieSaveHelperMod.mls.LogInfo("Seed not found.");
                KatieSaveHelperMod.mls.LogInfo($"Searched {attemptCount} total seeds");
                KatieSaveHelperMod.mls.LogInfo($"Fails : Head - {headFails}, Rooms - {roomFails}, Obstacles - {obstacleFails}");
                return (false, 0);
            }
            else
            {
                KatieSaveHelperMod.mls.LogInfo($"Seed found! {currentSeed}");
                KatieSaveHelperMod.mls.LogInfo($"Searched {attemptCount} total seeds");
                return (true, currentSeed);
            }
        }

        static bool EvaluatePurgeRooms(int baseHash, List<PurgeDirection> desiredRoomOrder)
        {
            System.Random random = new System.Random(baseHash + purgeGoalsIdHash);
            bool flag;

            PurgeDirection purgeDirection = PurgeDirection.Forward;
            for (int i = 0; i < 6; i++)
            {
                flag = random.NextRange(0f, 1f) >= 0.5f;
                PurgeDirection purgeDirection2 = PurgeDirection.Forward;
                switch (purgeDirection)
                {
                    case PurgeDirection.Forward:
                        purgeDirection2 = flag ? PurgeDirection.Left : PurgeDirection.Right;
                        break;
                    case PurgeDirection.Left:
                        purgeDirection2 = flag ? PurgeDirection.Right : PurgeDirection.Forward;
                        break;
                    case PurgeDirection.Right:
                        purgeDirection2 = flag ? PurgeDirection.Forward : PurgeDirection.Left;
                        break;
                }
                purgeDirection = purgeDirection2;

                if (desiredRoomOrder[i] != PurgeDirection.Any && purgeDirection != desiredRoomOrder[i])
                    return false;

            }

            return true;
        }

        static bool EvaluatePurgeObstacles(int baseHash, List<ObstacleCondition> conditions)
        {
            System.Random random = new System.Random(baseHash + purgeRoomsIdHash);
            int randIndex;

            foreach (ObstacleCondition condition in conditions)
            {
                randIndex = random.Next(0, 8);
                if (!condition.Evaluate((PurgeObstacle)randIndex))
                {
                    return false;
                }
                random.Next();
            }
            return true;
        }

        static bool EvaluatePurgeObstaclesVerbose(int baseHash, List<ObstacleCondition> conditions)
        {
            System.Random random = new System.Random(baseHash + purgeRoomsIdHash);
            int randIndex;
            foreach (ObstacleCondition condition in conditions)
            {
                randIndex = random.Next(0, 8);
                KatieSaveHelperMod.mls.LogInfo($"Rand is {randIndex}");
                PurgeObstacle popsicle = (PurgeObstacle)randIndex;
                KatieSaveHelperMod.mls.LogInfo($"Rand obstacle is {popsicle}");
                if (!condition.EvaluateVerbose(popsicle))
                {
                    KatieSaveHelperMod.mls.LogInfo($"Eval Returned False");
                    return false;

                }
                random.Next();
            }
            KatieSaveHelperMod.mls.LogInfo($"Eval Returned True");
            return true;
        }

        static List<PurgeDirection> ParseRoomOrder(string input)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("Input must be a string of exactly 6 characters.", nameof(input));
            if (input.Length != 6)
            {
                input.PadRight(6, 'A').Substring(0, 6);
            }

            var directions = new List<PurgeDirection>(6);

            foreach (char c in input)
            {
                switch (char.ToUpperInvariant(c))
                {
                    case 'F':
                        directions.Add(PurgeDirection.Forward);
                        break;
                    case 'L':
                        directions.Add(PurgeDirection.Left);
                        break;
                    case 'R':
                        directions.Add(PurgeDirection.Right);
                        break;
                    case 'A':
                    default:
                        directions.Add(PurgeDirection.Any);
                        break;
                }
            }

            bool hasInvalidInput = false;
            PurgeDirection previousDirection = PurgeDirection.Forward;

            for (int i = 0; i < directions.Count; i++)
            {
                if (directions[i] == previousDirection && directions[i] != PurgeDirection.Any)
                {
                    directions[i] = PurgeDirection.Any;
                    hasInvalidInput = true;
                }
                previousDirection = directions[i];
            }

            if (hasInvalidInput)
            {
                KatieSaveHelperMod.mls.LogWarning($"The loaded mod config entry for the setting '{KatieSaveHelperModConfig.psuedoRandomTargetPurgeRoomGoals.InternalName}' represents an impossible goal direction order for the Purge Event Maze : '{KatieSaveHelperModConfig.psuedoRandomTargetPurgeRoomGoals.Value}'");
                KatieSaveHelperMod.mls.LogWarning("Impossible goal directions have been replaced with 'A' (Any)");
            }

            return directions;
        }

        static int GetRandomInt32()
        {
            byte[] bytes = new byte[4];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return BitConverter.ToInt32(bytes, 0);
        }
    }

    internal static class KatieSaveRandomizer
    {
        public static double GetChanceRollValue(int baseHash, int idHash)
        {
            return GetRandomInstance(baseHash, idHash).NextDouble();
        }

        public static System.Random GetRandomInstance(int baseHash, int idHash)
        {
            return new System.Random(baseHash + idHash);
        }
    }
}
