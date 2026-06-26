using JoelG.ENA4;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using UnityEngine;
using System.Dynamic;

namespace KatieSaveHelper
{
    public enum RangeType
    {
        EqualTo,
        LessOrEqual,
        GreaterOrEqual,
        WithinRange
    }

    internal class KatiePseudoSessionRandomizer : KatiePseudoRandomizerBase
    {
        public override string DisplayName => "Session Mode";
        private float normalBlinkProbability = Mathf.Clamp01(1f / (float)1000);
        private float coreBlinkProbability = Mathf.Clamp01(1f / (float)100);
        public override List<PseudoTargetEvent> GetNewTargetEventList()
        {
            var targetEventList = new List<PseudoTargetEvent>();

            var blinkRangeTuple = ParseRange(KatieConfig.Settings.pseudoRandomTargetBlinkAttempt, "0");

            bool blinkFlag = false;
            switch (blinkRangeTuple.Item1)
            {
                case RangeType.EqualTo:
                case RangeType.LessOrEqual:
                    if (blinkRangeTuple.Item2[0] > 0)
                        blinkFlag = true;
                    break;
                case RangeType.GreaterOrEqual:
                    if (blinkRangeTuple.Item2[0] > 1)
                        blinkFlag = true;
                    break;
                case RangeType.WithinRange:
                    if (blinkRangeTuple.Item2[0] > 0 && blinkRangeTuple.Item2[1] > 0)
                        blinkFlag = true;
                    break;
            }

            if (blinkFlag)
            {
                float blinkProbability = KatieConfig.Settings.pseudoRandomBlinkAssumeInCore.Value ? coreBlinkProbability : normalBlinkProbability;
                switch (blinkRangeTuple.Item1)
                {
                    case RangeType.EqualTo:
                        targetEventList.Add(PseudoTargetEvent.Create("FirstBlink_EqualTo", EvaluateFirstBlinkEqualTo, blinkRangeTuple.Item2[0], blinkProbability));
                        break;
                    case RangeType.LessOrEqual:
                        targetEventList.Add(PseudoTargetEvent.Create("FirstBlink_LessOrEqual", EvaluateFirstBlinkLessOrEqual, blinkRangeTuple.Item2[0], blinkProbability));
                        break;
                    case RangeType.GreaterOrEqual:
                        targetEventList.Add(PseudoTargetEvent.Create("FirstBlink_GreaterOrEqual", EvaluateFirstBlinkGreaterOrEqual, blinkRangeTuple.Item2[0], blinkProbability));
                        break;
                    case RangeType.WithinRange:
                        targetEventList.Add(PseudoTargetEvent.Create("FirstBlink_WithinRange", EvaluateFirstBlinkWithinRange, blinkRangeTuple.Item2[0], blinkRangeTuple.Item2[1], blinkProbability));
                        break;
                }
            }

            return targetEventList;
        }

        private bool EvaluateFirstBlinkEqualTo(int baseHash, int targetAttemptNumber, float blinkProbability)
        {
            System.Random random = new System.Random(baseHash);

            for (int i = 1; i < targetAttemptNumber; i++)
                if (random.NextRange(0f, 1f) <= blinkProbability)
                    return false;

            return random.NextRange(0f, 1f) <= blinkProbability;
        }

        private bool EvaluateFirstBlinkLessOrEqual(int baseHash, int targetAttemptNumber, float blinkProbability)
        {
            System.Random random = new System.Random(baseHash);

            for (int i = 1; i <= targetAttemptNumber; i++)
                if (random.NextRange(0f, 1f) <= blinkProbability)
                    return true;

            return false;
        }

        private bool EvaluateFirstBlinkGreaterOrEqual(int baseHash, int targetAttemptNumber, float blinkProbability)
        {
            System.Random random = new System.Random(baseHash);

            for (int i = 1; i < targetAttemptNumber; i++)
                if (random.NextRange(0f, 1f) <= blinkProbability)
                    return false;

            return true;
        }

        private bool EvaluateFirstBlinkWithinRange(int baseHash, int targetAttemptLowerBound, int targetAttemptUpperBound, float blinkProbability)
        {
            System.Random random = new System.Random(baseHash);

            for (int i = 1; i < targetAttemptLowerBound; i++)
                if (random.NextRange(0f, 1f) <= blinkProbability)
                    return false;

            for (int i = targetAttemptLowerBound; i <= targetAttemptUpperBound; i++)
            {
                if (random.NextRange(0f, 1f) <= blinkProbability)
                    return true;
            }

            return false;
        }
        
        public (int firstNormalBlinkAttempt, int firstCoreBlinkAttempt) EvaluateFirstBlink(int baseHash)
        {
            System.Random random = new System.Random(baseHash);

            (int firstNormalBlinkAttempt, int firstCoreBlinkAttempt) blinkTuple = (0, 0);
            bool coreBlink = false;
            float rand;

            for (int i = 1; i < 10000000; i++)
            {
                rand = random.NextRange(0f, 1f);

                if (rand <= normalBlinkProbability)
                {
                    blinkTuple.firstNormalBlinkAttempt = i;
                    if (!coreBlink)
                        blinkTuple.firstCoreBlinkAttempt = i;
                    break;
                }

                if (!coreBlink && rand <= coreBlinkProbability)
                {
                    coreBlink = true;
                    blinkTuple.firstCoreBlinkAttempt = i;
                }
            }

            return blinkTuple;
        }

        private static (RangeType, List<int>) ParseRange(ModSetting<string> stringSetting, string defaultValue = null)
        {
            string input = defaultValue != null ? stringSetting.TryGetRealValue(defaultValue) : stringSetting.TryGetRealValue();

            // Patterns
            string exactPattern = @"^\d+$";
            string lessThanPattern = @"^\.\.(\d+)$";
            string greaterThanPattern = @"^(\d+)\.\.$";
            string rangePattern = @"^(\d+)\.\.(\d+)$";

            if (Regex.IsMatch(input, exactPattern))
            {
                return (RangeType.EqualTo, new List<int> { int.Parse(input) });
            }
            else if (Regex.IsMatch(input, lessThanPattern))
            {
                var match = Regex.Match(input, lessThanPattern);
                return (RangeType.LessOrEqual, new List<int> { int.Parse(match.Groups[1].Value) });
            }
            else if (Regex.IsMatch(input, greaterThanPattern))
            {
                var match = Regex.Match(input, greaterThanPattern);
                return (RangeType.GreaterOrEqual, new List<int> { int.Parse(match.Groups[1].Value) });
            }
            else if (Regex.IsMatch(input, rangePattern))
            {
                var match = Regex.Match(input, rangePattern);
                int lowerBound = int.Parse(match.Groups[1].Value);
                int upperBound = int.Parse(match.Groups[2].Value);

                if (upperBound < lowerBound)
                {
                    KatieLogger.Warning($"Invalid range format for '{input}', the upper range bound cannot be less than the lower range bound");
                    KatieLogger.Warning($"Defaulting to fallback value");
                    return (
                        RangeType.EqualTo,
                        new List<int> { 0 }
                    );
                }

                // If range bounds are equal, convert it to an "equal to" range condition
                if (lowerBound == upperBound)
                {
                    return (
                        RangeType.EqualTo,
                        new List<int> { lowerBound }
                    );
                }

                return (
                    RangeType.WithinRange,
                    new List<int> { int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value) }
                );
            }
            else
            {
                KatieLogger.Warning($"Could not parse range format for '{input}'");
                KatieLogger.Warning($"Defaulting to fallback value");
                return (
                    RangeType.EqualTo,
                    new List<int> { 0 }
                );
            }
        }
    }
}
