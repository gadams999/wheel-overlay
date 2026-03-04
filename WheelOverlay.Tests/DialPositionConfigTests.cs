using System;
using System.Collections.Generic;
using WheelOverlay.Models;
using Xunit;

namespace WheelOverlay.Tests
{
    public class DialPositionConfigTests
    {
        // --- Default 8-position angles match expected approximate values ---

        [Theory]
        [InlineData(1,  30.0)]
        [InlineData(2,  70.0)]
        [InlineData(3, 110.0)]
        [InlineData(4, 150.0)]
        [InlineData(5, 210.0)]
        [InlineData(6, 250.0)]
        [InlineData(7, 290.0)]
        [InlineData(8, 330.0)]
        public void DefaultAngles_MatchExpectedValues(int position, double expectedAngle)
        {
            Assert.Equal(expectedAngle, DialPositionConfig.DefaultAngles[position], precision: 1);
        }

        [Fact]
        public void DefaultAngles_RightArc_SpansApprox1OClockTo5OClock()
        {
            // Right arc positions 1–4 should be roughly 30°–150°
            Assert.InRange(DialPositionConfig.DefaultAngles[1], 20.0, 60.0);
            Assert.InRange(DialPositionConfig.DefaultAngles[4], 140.0, 170.0);
        }

        [Fact]
        public void DefaultAngles_LeftArc_SpansApprox7OClockTo11OClock()
        {
            // Left arc positions 5–8 should be roughly 210°–330°
            Assert.InRange(DialPositionConfig.DefaultAngles[5], 200.0, 230.0);
            Assert.InRange(DialPositionConfig.DefaultAngles[8], 320.0, 340.0);
        }

        // --- Config has entries for positions 1–8 ---

        [Fact]
        public void DefaultAngles_HasExactly8Entries()
        {
            Assert.Equal(8, DialPositionConfig.DefaultAngles.Count);
        }

        [Fact]
        public void DefaultAngles_ContainsAllPositions1Through8()
        {
            for (int i = 1; i <= 8; i++)
            {
                Assert.True(
                    DialPositionConfig.DefaultAngles.ContainsKey(i),
                    $"DefaultAngles missing position {i}");
            }
        }

        [Fact]
        public void DefaultAngles_AllValuesWithin0To360()
        {
            foreach (var kvp in DialPositionConfig.DefaultAngles)
            {
                Assert.InRange(kvp.Value, 0.0, 360.0);
            }
        }

        [Fact]
        public void GetAngles_With8_ReturnsSameAsDefaultAngles()
        {
            var result = DialPositionConfig.GetAngles(8);
            Assert.Same(DialPositionConfig.DefaultAngles, result);
        }

        // --- AngleToPoint returns correct coordinates for known angles ---

        [Fact]
        public void AngleToPoint_0Degrees_PointsStraightUp()
        {
            // 0° = 12 o'clock → X=0, Y=-radius (up in screen coords)
            var (x, y) = DialPositionConfig.AngleToPoint(0.0, 100.0);
            Assert.Equal(0.0, x, precision: 5);
            Assert.Equal(-100.0, y, precision: 5);
        }

        [Fact]
        public void AngleToPoint_90Degrees_PointsRight()
        {
            // 90° = 3 o'clock → X=radius, Y=0
            var (x, y) = DialPositionConfig.AngleToPoint(90.0, 100.0);
            Assert.Equal(100.0, x, precision: 5);
            Assert.Equal(0.0, y, precision: 5);
        }

        [Fact]
        public void AngleToPoint_180Degrees_PointsDown()
        {
            // 180° = 6 o'clock → X=0, Y=radius (down in screen coords)
            var (x, y) = DialPositionConfig.AngleToPoint(180.0, 100.0);
            Assert.Equal(0.0, x, precision: 5);
            Assert.Equal(100.0, y, precision: 5);
        }

        [Fact]
        public void AngleToPoint_270Degrees_PointsLeft()
        {
            // 270° = 9 o'clock → X=-radius, Y=0
            var (x, y) = DialPositionConfig.AngleToPoint(270.0, 100.0);
            Assert.Equal(-100.0, x, precision: 5);
            Assert.Equal(0.0, y, precision: 5);
        }

        [Fact]
        public void AngleToPoint_ZeroRadius_ReturnsOrigin()
        {
            var (x, y) = DialPositionConfig.AngleToPoint(45.0, 0.0);
            Assert.Equal(0.0, x, precision: 5);
            Assert.Equal(0.0, y, precision: 5);
        }

        [Fact]
        public void AngleToPoint_45Degrees_ReturnsCorrectDiagonal()
        {
            // 45° → X = radius * sin(45°), Y = -radius * cos(45°)
            double expected = 100.0 * Math.Sin(Math.PI / 4.0);
            var (x, y) = DialPositionConfig.AngleToPoint(45.0, 100.0);
            Assert.Equal(expected, x, precision: 5);
            Assert.Equal(-expected, y, precision: 5);
        }
    }
}
