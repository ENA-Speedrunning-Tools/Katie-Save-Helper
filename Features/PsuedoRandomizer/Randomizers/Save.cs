using JoelG.ENA4;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KatieSaveHelper
{
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

    public enum FrankDoor : int
    {
        Any = -1,
        Single = 0,
        Multiple = 1
    }
    internal class KatiePsuedoSaveRandomizer : KatiePsuedoRandomizerBase
    {
        private readonly int purgeGoalsIdHash = SaveRandomizer.GetStableHashCode("purge_goals");
        private readonly int purgeRoomsIdHash = SaveRandomizer.GetStableHashCode("purge_rooms");
        private readonly string purgeGoalsFallbackSetting = "*AAAAAA";
        public readonly MultiEventRunner taxiHeads = new MultiEventRunner(
            "UB_TaxiDriver",
            new MultiEventInstance("Creisi", 1),
            new MultiEventInstance("Doom", 1),
            new MultiEventInstance("Socio", 1)
            );
        public readonly MultiEventRunner frankDoor = new MultiEventRunner(
            "",
            new MultiEventInstance("Single", 50),
            new MultiEventInstance("Multiple", 50)
            );

        public override void FillTargetEventList()
        {
            targetEventList.Clear();

            List<PurgeDirection> targetPurgeGoals = ParseGoalOrder(KatieSaveHelperModConfig.psuedoRandomTargetPurgeRoomGoals, purgeGoalsFallbackSetting);
            var targetPurgeObstaclesTuple = ObstacleCondition.ParseConditionTuple(KatieSaveHelperModConfig.psuedoRandomTargetPurgeRoomObstacles.Value);

            bool purgeRoomsFlag = !targetPurgeGoals.All(c => c == PurgeDirection.Any);
            bool purgeObstaclesFlag = !targetPurgeObstaclesTuple.conditions.All(ob => ob.Logic == LogicType.Any) && targetPurgeObstaclesTuple.conditions.Count > 0;

            // Add Frank Door event if the name is not set to Any

            if (KatieSaveHelperModConfig.psuedoRandomTargetFrankDoor.Value != FrankDoor.Any)
            {
                targetEventList.Add(PsuedoTargetEvent.Create("FrankDoor", frankDoor.Evaluate, (int)KatieSaveHelperModConfig.psuedoRandomTargetFrankDoor.Value));
            }

            // Add Taxi Driver Heads event if the head name is not set to Any

            if (KatieSaveHelperModConfig.psuedoRandomTargetTaxiHead.Value != TaxiHead.Any)
            {
                targetEventList.Add(PsuedoTargetEvent.Create("TaxiHeads", taxiHeads.Evaluate, (int)KatieSaveHelperModConfig.psuedoRandomTargetTaxiHead.Value));
            }

            // Add Purge Special event if there is at least one 'Any' direction in the desired goal order, if purge obstacles has a single non-Any obstacle, and the configured obstacles setting uses any shorthand format

            if (targetPurgeGoals.Contains(PurgeDirection.Any) && purgeObstaclesFlag && targetPurgeObstaclesTuple.shorthandLevel > 0)
            {
                targetEventList.Add(PsuedoTargetEvent.Create("PurgeSpecial", EvaluatePurgeSpecial, targetPurgeGoals, targetPurgeObstaclesTuple));
            }
            else
            {
                // Add Purge Rooms event if there is a single non-Any direction

                if (purgeRoomsFlag)
                {
                    targetEventList.Add(PsuedoTargetEvent.Create("PurgeRooms", EvaluatePurgeGoals, targetPurgeGoals));
                }

                // Add Purge Obstacles event if it has at least one entry and there is at least one non-Any obstacle

                if (purgeObstaclesFlag)
                {
                    List<ObstacleCondition> targetPurgeObstacles = ObstacleCondition.ResolveConditionTuple(targetPurgeObstaclesTuple, targetPurgeGoals);
                    targetEventList.Add(PsuedoTargetEvent.Create("PurgeObstacles", EvaluatePurgeObstacles, targetPurgeObstacles));
                }
            }
        }

        private bool EvaluatePurgeGoals(int baseHash, List<PurgeDirection> desiredGoalOrder)
        {
            System.Random random = new System.Random(baseHash + purgeGoalsIdHash);

            bool flag;
            PurgeDirection purgeDirection = PurgeDirection.Forward;
            PurgeDirection purgeDirection2 = PurgeDirection.Forward;

            for (int i = 0; i < 6; i++)
            {
                flag = random.NextRange(0f, 1f) >= 0.5f;
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

                if (desiredGoalOrder[i] != PurgeDirection.Any && purgeDirection != desiredGoalOrder[i])
                    return false;

            }

            return true;
        }

        public List<PurgeDirection> EvaluatePurgeGoals(int baseHash)
        {
            System.Random random = new System.Random(baseHash + purgeGoalsIdHash);

            bool flag;
            PurgeDirection purgeDirection = PurgeDirection.Forward;
            PurgeDirection purgeDirection2 = PurgeDirection.Forward;

            List<PurgeDirection> purgeGoals = new List<PurgeDirection> { PurgeDirection.Forward };

            for (int i = 0; i < 6; i++)
            {
                flag = random.NextRange(0f, 1f) >= 0.5f;
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
                purgeGoals.Add(purgeDirection);

            }

            return purgeGoals;
        }
             
        private bool EvaluatePurgeObstacles(int baseHash, List<ObstacleCondition> conditions)
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

        private bool EvaluatePurgeSpecial(int baseHash, List<PurgeDirection> desiredRoomOrder, (int shorthandLevel, List<ObstacleCondition> conditions) obstaclesTuple)
        {
            // Evaluate Rooms

            System.Random random = new System.Random(baseHash + purgeGoalsIdHash);

            bool flag;
            List<PurgeDirection> actualRoomOrder = desiredRoomOrder.ToList();
            PurgeDirection purgeDirection = PurgeDirection.Forward;
            PurgeDirection purgeDirection2 = PurgeDirection.Forward;

            for (int i = 0; i < 6; i++)
            {
                flag = random.NextRange(0f, 1f) >= 0.5f;
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
                // Replace 'Any' directions found in the actualRoomOrder list with the actual goal direction found at the current index
                else if (desiredRoomOrder[i] == PurgeDirection.Any)
                    actualRoomOrder[i] = purgeDirection;
            }

            // Create new obstacle conditions list based on actual room order
            List<ObstacleCondition> conditions = ObstacleCondition.ResolveConditionTuple(obstaclesTuple, actualRoomOrder);

            // Evaluate Obstacles

            random = new System.Random(baseHash + purgeRoomsIdHash);
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

        public (List<PurgeDirection> purgeGoals, List<PurgeObstacle> purgeObstacles) EvaluatePurgeSpecial(int baseHash)
        {
            List<PurgeDirection> purgeGoals = EvaluatePurgeGoals(baseHash);

            // Evaluate Obstacles

            List<PurgeObstacle> purgeObstacles = new List<PurgeObstacle>();

            System.Random random = new System.Random(baseHash + purgeRoomsIdHash);

            Func<PurgeObstacle> generateRoom = () =>
            {
                int randIndex = random.Next(0, 8);
                random.Next();
                return (PurgeObstacle)randIndex;
            };

            foreach (PurgeDirection direction in purgeGoals.Take(5))
            {
                switch (direction)
                {
                    case PurgeDirection.Left:
                        purgeObstacles.Add(generateRoom());
                        generateRoom();
                        generateRoom();
                        break;
                    case PurgeDirection.Right:
                        generateRoom();
                        generateRoom();
                        purgeObstacles.Add(generateRoom());
                        break;
                    case PurgeDirection.Forward:
                        generateRoom();
                        purgeObstacles.Add(generateRoom());
                        generateRoom();
                        break;
                    default:
                        throw new Exception("Internal error, contact the mod developer");
                }
            }

            return (purgeGoals, purgeObstacles);
        }

        private List<PurgeDirection> ParseGoalOrder(KatieSetting<string> stringSetting, string defaultValue = null)
        {
            string input = defaultValue != null ? stringSetting.TryGetRealValue(defaultValue) : stringSetting.TryGetRealValue();

            var directions = new List<PurgeDirection>(6);

            // Get direction list from input

            // Handle shorthand format
            if (input.StartsWith("*"))
            {
                input = input.Substring(1).Trim();
                foreach (char c in input.Take(6))
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
            }
            else
            {
                var roomStringList = input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                roomStringList = roomStringList
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToArray();
                foreach (string name in roomStringList.Take(6))
                {
                    if (Enum.TryParse<PurgeDirection>(name, true, out var parsed))
                        directions.Add(parsed);
                }
            }

            // Fill missing directions with 'Any'

            while (directions.Count < 6)
            {
                directions.Add(PurgeDirection.Any);
            }

            // Verify direction list is legal

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
                KatieLogger.Warning($"The loaded mod config entry for the setting '{stringSetting.InternalName}' represents an impossible goal direction order for the Purge Event Maze : '{input}'");
                KatieLogger.Warning("Impossible goal directions have been replaced with 'Any'");
            }

            return directions;
        }
    }
}
