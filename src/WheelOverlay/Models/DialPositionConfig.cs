using System;
using System.Collections.Generic;

namespace OpenDash.WheelOverlay.Models
{
    /// <summary>
    /// Data-driven configuration for dial layout position angles.
    /// Angles are in degrees where 0° = 12 o'clock, increasing clockwise.
    /// Right arc: Positions 1–4 (≈1 o'clock to ≈5 o'clock)
    /// Left arc:  Positions 5–8 (≈7 o'clock to ≈11 o'clock)
    /// </summary>
    public static class DialPositionConfig
    {
        /// <summary>
        /// Default 8-position angles mapping position index (1-based) to degrees.
        /// 0° = 12 o'clock, angles increase clockwise.
        /// </summary>
        public static readonly IReadOnlyDictionary<int, double> DefaultAngles =
            new Dictionary<int, double>
            {
                { 1,  30.0 },   // ~1 o'clock
                { 2,  70.0 },   // ~2:20
                { 3, 110.0 },   // ~3:40
                { 4, 150.0 },   // ~5 o'clock
                { 5, 210.0 },   // ~7 o'clock
                { 6, 250.0 },   // ~8:20
                { 7, 290.0 },   // ~9:40
                { 8, 330.0 },   // ~11 o'clock
            };

        /// <summary>
        /// Returns position angles for the given count.
        /// For 8 positions, returns <see cref="DefaultAngles"/>.
        /// For any other count, falls back to even distribution across 360°.
        /// </summary>
        public static IReadOnlyDictionary<int, double> GetAngles(int positionCount)
        {
            if (positionCount == DefaultAngles.Count)
                return DefaultAngles;

            var angles = new Dictionary<int, double>(positionCount);
            double step = 360.0 / positionCount;
            for (int i = 1; i <= positionCount; i++)
            {
                angles[i] = (step * (i - 1)) % 360.0;
            }
            return angles;
        }

        /// <summary>
        /// Converts a dial angle and radius to an (X, Y) offset from center.
        /// 0° = 12 o'clock, angles increase clockwise.
        /// Positive X is right, positive Y is down (screen coordinates).
        /// </summary>
        public static (double X, double Y) AngleToPoint(double angleDegrees, double radius)
        {
            double radians = angleDegrees * Math.PI / 180.0;
            double x = radius * Math.Sin(radians);
            double y = -radius * Math.Cos(radians);
            return (x, y);
        }
    }
}
