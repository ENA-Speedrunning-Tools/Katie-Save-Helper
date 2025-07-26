using JoelG.ENA4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace KatieSaveHelper
{
    public class PsuedoTargetEvent
    {
        public string Name { get; private set; }

        private Func<int, bool> Event;

        public static PsuedoTargetEvent Create(string name, Func<int, bool> function)
        {
            return new PsuedoTargetEvent
            {
                Name = name,
                Event = (seed) => function(seed)
            };
        }

        public static PsuedoTargetEvent Create<T>(string name, Func<int, T, bool> function, T value)
        {
            return new PsuedoTargetEvent
            {
                Name = name,
                Event = (seed) => function(seed, value)
            };
        }

        public static PsuedoTargetEvent Create<T1, T2>(string name, Func<int, T1, T2, bool> function, T1 value1, T2 value2)
        {
            return new PsuedoTargetEvent
            {
                Name = name,
                Event = (seed) => function(seed, value1, value2)
            };
        }

        public static PsuedoTargetEvent Create<T1, T2, T3>(string name, Func<int, T1, T2, T3, bool> function, T1 value1, T2 value2, T3 value3)
        {
            return new PsuedoTargetEvent
            {
                Name = name,
                Event = (seed) => function(seed, value1, value2, value3)
            };
        }

        public bool Evaluate(int currentSeed)
        {
            return Event(currentSeed);
        }
    }

    internal abstract class KatiePsuedoRandomizerBase
    {
        internal List<PsuedoTargetEvent> targetEventList = new List<PsuedoTargetEvent>();
        private Dictionary<string, int> eventFailCountMap = new Dictionary<string, int>();

        public abstract void FillTargetEventList();

        protected void MapEventFailCounts()
        {
            eventFailCountMap.Clear();
            foreach (var @event in targetEventList)
                eventFailCountMap[@event.Name] = 0;
        }

        protected void PrintEventFailCounts()
        {
            string failStr = "Attempt Fails:\n";
            foreach (var @event in targetEventList)
                failStr += $"\t{@event.Name} - {eventFailCountMap[@event.Name]}\n";
            KatieLogger.Info(failStr);
        }

        public (bool success, int seed) GeneratePsuedoRandomSeed()
        {
            MapEventFailCounts();

            KatieLogger.Info("Searching for seed...");

            KatieLogger.Info($"Active Events: {(targetEventList.Any() ? string.Join(", ", targetEventList.Select(x => x.Name)) : "None")}");

            int maxAllowedAttempts = KatieSaveHelperModConfig.psuedoRandomMaxAttempts.Value;

            bool searchDirectionFlag = new Random().NextDouble() >= 0.5;

            int attemptCount = 0;

            int minValue;
            Func<int> rollSeed;
            Func<bool> boundsCheck;
            Action iterateSeed;

            // Set the function to roll/reroll the seed based on the configured 'natural seeds only' mod option

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

            // Set the functions to iterate and check the bounds of the seed for each iteration based on the generated search direction flag
            if (searchDirectionFlag)
            {
                iterateSeed = () => currentSeed++;
                boundsCheck = () => currentSeed > int.MaxValue;
            }
            else
            {
                iterateSeed = () => currentSeed--;
                boundsCheck = () => currentSeed < minValue;
            }

        // Begin searching for a seed that passes each active Target Event
        NewAttempt:
            while (attemptCount < maxAllowedAttempts)
            {
                attemptCount++;

                // Reroll seed every 10,000 attempts or if the value has reached a bound of the int range
                if (boundsCheck() || attemptCount % 10000 == 0)
                {
                    currentSeed = rollSeed();
                }

                foreach (PsuedoTargetEvent @event in targetEventList)
                {
                    if (!@event.Evaluate(currentSeed))
                    {
                        iterateSeed();
                        eventFailCountMap[@event.Name]++;
                        goto NewAttempt;
                    }
                }

                break;
            }

            if (attemptCount >= maxAllowedAttempts)
            {
                KatieLogger.Warning("Seed not found.");
                KatieLogger.Info($"Searched {attemptCount} total seeds");
                PrintEventFailCounts();
                return (false, 0);
            }
            else
            {
                KatieLogger.Info($"Seed found! {currentSeed}");
                KatieLogger.Info($"Searched {attemptCount} total seeds");
                return (true, currentSeed);
            }
        }

        protected static int GetRandomInt32()
        {
            byte[] bytes = new byte[4];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }
    }

    internal static class KatiePsuedoRandomizer
    {
        public static readonly KatiePsuedoSaveRandomizer SaveMode = new KatiePsuedoSaveRandomizer();
        public static readonly KatiePsuedoSessionRandomizer SessionMode = new KatiePsuedoSessionRandomizer();
        public static readonly KatiePsuedoHardwareRandomizer HardwareMode = new KatiePsuedoHardwareRandomizer();
        public static void FillTargetEventLists()
        {
            SaveMode.FillTargetEventList();
            SessionMode.FillTargetEventList();
            HardwareMode.FillTargetEventList();
        }
    }
}
