using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using WheelOverlay.Models;
using Xunit;

namespace WheelOverlay.Tests
{
    public class DialPositionConfigPropertyTests
    {
        // Feature: v0.6.0-enhancements, Property 1: Dial angle even distribution
        // Validates: Requirements 1.7, 3.3, 3.4
        // For any position count N (2–20), verify equal angular spacing between consecutive positions.
        // For the default 8-position config, right arc (1–4) and left arc (5–8) are each evenly spaced.
        // For non-8 counts (fallback), all N positions are evenly distributed across 360°.
#if FAST_TESTS
        [Property(MaxTest = 10)]
#else
        [Property(MaxTest = 100)]
#endif
        public Property Property_EvenAngularSpacing_ForAnyPositionCount()
        {
            return Prop.ForAll(
                Arb.From(Gen.Choose(2, 20)),
                positionCount =>
                {
                    var angles = DialPositionConfig.GetAngles(positionCount);

                    // Must have exactly positionCount entries
                    bool correctCount = angles.Count == positionCount;

                    if (positionCount == 8)
                    {
                        // For default 8 positions: right arc (1–4) evenly spaced, left arc (5–8) evenly spaced
                        var rightArc = new[] { angles[1], angles[2], angles[3], angles[4] };
                        var leftArc = new[] { angles[5], angles[6], angles[7], angles[8] };

                        bool rightEven = HasEvenSpacing(rightArc);
                        bool leftEven = HasEvenSpacing(leftArc);

                        return (correctCount && rightEven && leftEven)
                            .Label($"8-position: count={angles.Count}, rightEvenSpacing={rightEven}, leftEvenSpacing={leftEven}");
                    }
                    else
                    {
                        // For non-8 counts: even distribution across full 360°
                        double expectedStep = 360.0 / positionCount;
                        var sortedAngles = Enumerable.Range(1, positionCount)
                            .Select(i => angles[i])
                            .ToArray();

                        bool evenlyDistributed = true;
                        for (int i = 0; i < sortedAngles.Length; i++)
                        {
                            double expected = (expectedStep * i) % 360.0;
                            if (Math.Abs(sortedAngles[i] - expected) > 0.001)
                            {
                                evenlyDistributed = false;
                                break;
                            }
                        }

                        return (correctCount && evenlyDistributed)
                            .Label($"N={positionCount}: count={angles.Count}, evenlyDistributed={evenlyDistributed}, expectedStep={expectedStep:F2}°");
                    }
                });
        }

        /// <summary>
        /// Checks that an ordered sequence of angles has equal spacing between consecutive values.
        /// </summary>
        private static bool HasEvenSpacing(double[] angles, double tolerance = 0.001)
        {
            if (angles.Length < 2) return true;

            double spacing = angles[1] - angles[0];
            for (int i = 2; i < angles.Length; i++)
            {
                double gap = angles[i] - angles[i - 1];
                if (Math.Abs(gap - spacing) > tolerance)
                    return false;
            }
            return true;
        }
    }
}
