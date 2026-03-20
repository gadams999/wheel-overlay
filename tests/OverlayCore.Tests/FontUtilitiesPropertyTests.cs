using FsCheck;
using FsCheck.Xunit;
using OpenDash.OverlayCore.Resources.Fonts;
using System.Windows;
using System.Windows.Media;

namespace OpenDash.OverlayCore.Tests;

// Feature: OpenDash Monorepo Rebrand, Property 8: FontUtilities returns valid results for all string inputs
public class FontUtilitiesPropertyTests
{
    private static readonly string[] ValidWeightNames =
    [
        "Thin", "ExtraLight", "UltraLight", "Light", "Normal", "Regular",
        "Medium", "SemiBold", "DemiBold", "Bold", "ExtraBold", "UltraBold",
        "Black", "Heavy", "ExtraBlack", "UltraBlack"
    ];

    private static readonly Dictionary<string, FontWeight> ExpectedWeights = new()
    {
        ["Thin"] = FontWeights.Thin,
        ["ExtraLight"] = FontWeights.ExtraLight,
        ["UltraLight"] = FontWeights.ExtraLight,
        ["Light"] = FontWeights.Light,
        ["Normal"] = FontWeights.Normal,
        ["Regular"] = FontWeights.Normal,
        ["Medium"] = FontWeights.Medium,
        ["SemiBold"] = FontWeights.SemiBold,
        ["DemiBold"] = FontWeights.SemiBold,
        ["Bold"] = FontWeights.Bold,
        ["ExtraBold"] = FontWeights.ExtraBold,
        ["UltraBold"] = FontWeights.ExtraBold,
        ["Black"] = FontWeights.Black,
        ["Heavy"] = FontWeights.Black,
        ["ExtraBlack"] = FontWeights.ExtraBlack,
        ["UltraBlack"] = FontWeights.ExtraBlack,
    };

    /// <summary>
    /// Property 8a: GetFontFamily returns a non-null FontFamily for any string input.
    /// Unrecognized names fall back to Segoe UI; the result is never null.
    /// </summary>
    [Property(
#if FAST_TESTS
        MaxTest = 10
#else
        MaxTest = 100
#endif
    )]
    public Property GetFontFamilyNeverReturnsNull(string? familyName)
    {
        FontFamily result = FontUtilities.GetFontFamily(familyName);

        return (result != null)
            .Label($"GetFontFamily({familyName ?? "null"}) must never return null");
    }

    /// <summary>
    /// Property 8b: GetFontFamily falls back to Segoe UI for null, empty, or whitespace input.
    /// </summary>
    [Property(
#if FAST_TESTS
        MaxTest = 10,
#else
        MaxTest = 100,
#endif
        Arbitrary = new[] { typeof(NullOrWhitespaceArbitrary) }
    )]
    public Property GetFontFamilyFallsBackToSegoeUIForNullOrEmpty(NullOrWhitespaceArbitrary.NullOrWhitespace input)
    {
        FontFamily result = FontUtilities.GetFontFamily(input.Value);

        bool isSegoeUI = result.Source.Equals("Segoe UI", StringComparison.OrdinalIgnoreCase);
        return isSegoeUI
            .Label($"GetFontFamily({input.Value ?? "null"}) should return Segoe UI fallback, got: {result.Source}");
    }

    /// <summary>
    /// Property 8c: ToFontWeight returns the correct FontWeight for all recognized weight names.
    /// </summary>
    [Property(
#if FAST_TESTS
        MaxTest = 10,
#else
        MaxTest = 100,
#endif
        Arbitrary = new[] { typeof(ValidWeightNameArbitrary) }
    )]
    public Property ToFontWeightReturnsCorrectWeightForValidNames(string weightName)
    {
        FontWeight result = FontUtilities.ToFontWeight(weightName);
        FontWeight expected = ExpectedWeights[weightName];

        return (result == expected)
            .Label($"ToFontWeight({weightName}) expected {expected}, got {result}");
    }

    /// <summary>
    /// Property 8d: ToFontWeight falls back to Normal for null, empty, or whitespace input.
    /// </summary>
    [Property(
#if FAST_TESTS
        MaxTest = 10,
#else
        MaxTest = 100,
#endif
        Arbitrary = new[] { typeof(NullOrWhitespaceArbitrary) }
    )]
    public Property ToFontWeightFallsBackToNormalForNullOrEmpty(NullOrWhitespaceArbitrary.NullOrWhitespace input)
    {
        FontWeight result = FontUtilities.ToFontWeight(input.Value);

        return (result == FontWeights.Normal)
            .Label($"ToFontWeight({input.Value ?? "null"}) should return Normal fallback, got {result}");
    }

    /// <summary>
    /// Property 8e: ToFontWeight falls back to Normal for unrecognized non-empty strings.
    /// </summary>
    [Property(
#if FAST_TESTS
        MaxTest = 10,
#else
        MaxTest = 100,
#endif
        Arbitrary = new[] { typeof(UnrecognizedWeightArbitrary) }
    )]
    public Property ToFontWeightFallsBackToNormalForUnrecognizedInput(string unrecognized)
    {
        FontWeight result = FontUtilities.ToFontWeight(unrecognized);

        return (result == FontWeights.Normal)
            .Label($"ToFontWeight({unrecognized}) should return Normal fallback for unrecognized name, got {result}");
    }

    /// <summary>
    /// Generates null and whitespace-only string values wrapped in a record to support
    /// nullable string generation with FsCheck.
    /// </summary>
    public static class NullOrWhitespaceArbitrary
    {
        public record NullOrWhitespace(string? Value);

        public static Arbitrary<NullOrWhitespace> Generate()
        {
            var whitespaceGen = Gen.Choose(0, 5)
                .Select(n => new NullOrWhitespace(new string(' ', n)));
            var nullGen = Gen.Constant(new NullOrWhitespace(null));
            return Gen.OneOf(nullGen, whitespaceGen).ToArbitrary();
        }
    }

    /// <summary>
    /// Generates recognized font weight names from the supported set.
    /// </summary>
    public class ValidWeightNameArbitrary
    {
        public static Arbitrary<string> WeightName()
        {
            return Gen.Elements(ValidWeightNames).ToArbitrary();
        }
    }

    /// <summary>
    /// Generates non-empty strings that are not recognized weight names.
    /// </summary>
    public class UnrecognizedWeightArbitrary
    {
        private static readonly HashSet<string> _validSet =
            new(ValidWeightNames, StringComparer.Ordinal);

        public static Arbitrary<string> UnrecognizedWeight()
        {
            return Arb.Default.NonEmptyString()
                .Generator
                .Select(s => s.Get)
                .Where(s => !_validSet.Contains(s.Trim()))
                .ToArbitrary();
        }
    }
}
