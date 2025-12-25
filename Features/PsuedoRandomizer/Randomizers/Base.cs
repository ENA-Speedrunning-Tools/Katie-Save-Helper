using JoelG.ENA4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
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
        public abstract string DisplayName { get; }
        public abstract List<PsuedoTargetEvent> GetNewTargetEventList();

        public void ResetTargetEventList()
        {
            targetEventList = GetNewTargetEventList();
        }

        protected Dictionary<string, int> MapEventFailCounts(List<PsuedoTargetEvent> targetEventList)
        {
            var eventFailCountMap = new Dictionary<string, int>();
            foreach (var @event in targetEventList)
                eventFailCountMap[@event.Name] = 0;
            return eventFailCountMap;
        }

        protected void PrintEventFailCounts(List<PsuedoTargetEvent> targetEventList, Dictionary<string, int> eventFailCountMap)
        {
            string failStr = "Attempt Fails:\n";
            foreach (var @event in targetEventList)
                failStr += $"\t{@event.Name} - {eventFailCountMap[@event.Name]}\n";
            KatieLogger.Info(failStr);
        }

        public TrackedTask<(bool success, int seed)> GeneratePsuedoRandomSeed(CancellationToken token = default)
        {
            string identifier = $"KSH.Action.FindSeed.{DisplayName.Replace(" ", "")}";

            if (token.IsCancellationRequested)
                return TrackedTask<(bool, int)>.FromCanceled(token, identifier);

            return TrackedTask<(bool, int)>.Start(() => FindSeed(token), identifier);
        }
        private (bool success, int seed) FindSeed(CancellationToken token)
        {
            List<PsuedoTargetEvent> targetEventListCopy = targetEventList.ToList();

            var eventFailCountMap = MapEventFailCounts(targetEventListCopy);

            KatieLogger.Info($"(Seed Psuedo-Randomizer: {DisplayName}) Searching for seed...");

            KatieLogger.Info($"(Seed Psuedo-Randomizer: {DisplayName}) Active Events: {(targetEventListCopy.Any() ? string.Join(", ", targetEventListCopy.Select(x => x.Name)) : "None")}");

            long maxAttemptsConfig = KatieConfig.Settings.psuedoRandomMaxAttempts.Value;
            bool naturalSeedsOnlyConfig = KatieConfig.Settings.psuedoRandomNaturalSeedsOnly.Value;

            long maxAllowedAttempts = naturalSeedsOnlyConfig
                ? Math.Min(maxAttemptsConfig, int.MaxValue)
                : Math.Min(maxAttemptsConfig, (long)uint.MaxValue);

            var seedStream = new SplitMixSeedStream(naturalOnly: naturalSeedsOnlyConfig);
            long attemptCount = 0;
            int currentSeed = seedStream.Next();

            // Begin searching for a seed that passes each active Target Event
            while (attemptCount < maxAllowedAttempts)
            {
                if (token.IsCancellationRequested) break;

                attemptCount++;

                bool allPassed = true;

                foreach (PsuedoTargetEvent @event in targetEventListCopy)
                {
                    if (!@event.Evaluate(currentSeed))
                    {
                        allPassed = false;
                        currentSeed = seedStream.Next();
                        eventFailCountMap[@event.Name]++;
                        break;
                    }
                }

                if (allPassed) break;
            }

            if (attemptCount >= maxAllowedAttempts)
            {
                KatieLogger.Warning($"(Seed Psuedo-Randomizer: {DisplayName}) Seed not found.");
                KatieLogger.Info($"(Seed Psuedo-Randomizer: {DisplayName}) Searched {attemptCount} total seeds");
                PrintEventFailCounts(targetEventListCopy, eventFailCountMap);
                return (false, 0);
            }
            else if (token.IsCancellationRequested)
            {
                KatieLogger.Warning($"(Seed Psuedo-Randomizer: {DisplayName}) Search cancelled.");
                KatieLogger.Info($"(Seed Psuedo-Randomizer: {DisplayName}) Searched {attemptCount} total seeds");
                return (false, 0);
            }
            else
            {
                KatieLogger.Info($"(Seed Psuedo-Randomizer: {DisplayName}) Seed found! {currentSeed}");
                KatieLogger.Info($"(Seed Psuedo-Randomizer: {DisplayName}) Searched {attemptCount} total seeds");
                return (true, currentSeed);
            }
        }
    }
    
    // Allows semi-randomly stepping through every possible number in the specified integer range without repeats
    public struct SplitMixSeedStream
    {
        private readonly bool _naturalOnly;
        private uint _state;
        private const uint NaturalMax = 0x7FFFFFFF;

        public bool NaturalSeedsOnly => _naturalOnly;
        public int CurrentState => unchecked((int)_state);

        public int CurrentSeed
        {
            get
            {
                uint z = _state;
                z ^= z >> 16;
                z *= 0x85EBCA6B;
                z ^= z >> 13;
                z *= 0xC2B2AE35;
                z ^= z >> 16;

                return _naturalOnly ? (int)(z & NaturalMax) : unchecked((int)z);
            }
        }

        public SplitMixSeedStream(int? startSeed = null, bool naturalOnly = false)
        {
            if (startSeed == null) startSeed = KatieUtil.GetRandomInt32();
            _state = unchecked((uint)startSeed);
            if (naturalOnly)
                _state &= NaturalMax;
            _naturalOnly = naturalOnly;
        }

        public int Next()
        {
            if (_naturalOnly)
            {
                _state += 0x1D872B41;
                _state &= NaturalMax;
            }
            else
            {
                _state += 0x9E3779B9;
            }

            return CurrentSeed;
        }
    }

    internal static class KatiePsuedoRandomizer
    {
        public static readonly KatiePsuedoSaveRandomizer SaveMode = new KatiePsuedoSaveRandomizer();
        public static readonly KatiePsuedoSessionRandomizer SessionMode = new KatiePsuedoSessionRandomizer();
        public static readonly KatiePsuedoHardwareRandomizer HardwareMode = new KatiePsuedoHardwareRandomizer();
        public static void ResetTargetEventLists()
        {
            SaveMode.ResetTargetEventList();
            SessionMode.ResetTargetEventList();
            HardwareMode.ResetTargetEventList();
        }
    }
}
