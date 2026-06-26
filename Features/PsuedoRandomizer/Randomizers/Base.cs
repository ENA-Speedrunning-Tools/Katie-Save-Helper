using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using KatieSaveHelper.Features.Util;

namespace KatieSaveHelper
{
    public class PseudoTargetEvent
    {
        public string Name { get; private set; }

        private Func<int, bool> Event;

        public static PseudoTargetEvent Create(string name, Func<int, bool> function)
        {
            return new PseudoTargetEvent
            {
                Name = name,
                Event = (seed) => function(seed)
            };
        }

        public static PseudoTargetEvent Create<T>(string name, Func<int, T, bool> function, T value)
        {
            return new PseudoTargetEvent
            {
                Name = name,
                Event = (seed) => function(seed, value)
            };
        }

        public static PseudoTargetEvent Create<T1, T2>(string name, Func<int, T1, T2, bool> function, T1 value1, T2 value2)
        {
            return new PseudoTargetEvent
            {
                Name = name,
                Event = (seed) => function(seed, value1, value2)
            };
        }

        public static PseudoTargetEvent Create<T1, T2, T3>(string name, Func<int, T1, T2, T3, bool> function, T1 value1, T2 value2, T3 value3)
        {
            return new PseudoTargetEvent
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

    internal abstract class KatiePseudoRandomizerBase
    {
        internal List<PseudoTargetEvent> targetEventList = new List<PseudoTargetEvent>();
        public abstract string DisplayName { get; }
        public abstract List<PseudoTargetEvent> GetNewTargetEventList();

        public void ResetTargetEventList()
        {
            targetEventList = GetNewTargetEventList();
        }

        protected Dictionary<string, int> MapEventFailCounts(List<PseudoTargetEvent> targetEventList)
        {
            var eventFailCountMap = new Dictionary<string, int>();
            foreach (var @event in targetEventList)
                eventFailCountMap[@event.Name] = 0;
            return eventFailCountMap;
        }

        protected void PrintEventFailCounts(List<PseudoTargetEvent> targetEventList, Dictionary<string, int> eventFailCountMap)
        {
            string failStr = "Attempt Fails:\n";
            foreach (var @event in targetEventList)
                failStr += $"\t{@event.Name} - {eventFailCountMap[@event.Name]}\n";
            KatieLogger.Info(failStr);
        }

        public TrackedTask<(bool success, int seed)> GeneratePseudoRandomSeed(CancellationToken token = default)
        {
            string identifier = $"KSH.Action.FindSeed.{DisplayName.Replace(" ", "")}";

            if (token.IsCancellationRequested)
                return TrackedTask<(bool, int)>.FromCanceled(token, identifier);

            return TrackedTask<(bool, int)>.Start(() => FindSeed(token), identifier);
        }
        private (bool success, int seed) FindSeed(CancellationToken token)
        {
            List<PseudoTargetEvent> targetEventListCopy = targetEventList.ToList();

            var eventFailCountMap = MapEventFailCounts(targetEventListCopy);

            KatieLogger.Info($"(Seed Pseudo-Randomizer: {DisplayName}) Searching for seed...");

            KatieLogger.Info($"(Seed Pseudo-Randomizer: {DisplayName}) Active Events: {(targetEventListCopy.Any() ? string.Join(", ", targetEventListCopy.Select(x => x.Name)) : "None")}");

            long maxAttemptsConfig = KatieConfig.Settings.pseudoRandomMaxAttempts.Value;
            bool naturalSeedsOnlyConfig = KatieConfig.Settings.pseudoRandomNaturalSeedsOnly.Value;

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

                foreach (PseudoTargetEvent @event in targetEventListCopy)
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
                KatieLogger.Warning($"(Seed Pseudo-Randomizer: {DisplayName}) Seed not found.");
                KatieLogger.Info($"(Seed Pseudo-Randomizer: {DisplayName}) Searched {attemptCount} total seeds");
                PrintEventFailCounts(targetEventListCopy, eventFailCountMap);
                return (false, 0);
            }
            else if (token.IsCancellationRequested)
            {
                KatieLogger.Warning($"(Seed Pseudo-Randomizer: {DisplayName}) Search cancelled.");
                KatieLogger.Info($"(Seed Pseudo-Randomizer: {DisplayName}) Searched {attemptCount} total seeds");
                return (false, 0);
            }
            else
            {
                KatieLogger.Info($"(Seed Pseudo-Randomizer: {DisplayName}) Seed found! {currentSeed}");
                KatieLogger.Info($"(Seed Pseudo-Randomizer: {DisplayName}) Searched {attemptCount} total seeds");
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

    internal static class KatiePseudoRandomizer
    {
        public static readonly KatiePseudoSaveRandomizer SaveMode = new KatiePseudoSaveRandomizer();
        public static readonly KatiePseudoSessionRandomizer SessionMode = new KatiePseudoSessionRandomizer();
        public static readonly KatiePseudoHardwareRandomizer HardwareMode = new KatiePseudoHardwareRandomizer();
        public static void ResetTargetEventLists()
        {
            SaveMode.ResetTargetEventList();
            SessionMode.ResetTargetEventList();
            HardwareMode.ResetTargetEventList();
        }
    }
}
