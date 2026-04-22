using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;
using FsCheck;
using FsCheck.Xunit;
using OpenDash.SpeakerSight.Models;
using OpenDash.OverlayCore.Models;

namespace OpenDash.SpeakerSight.Tests.Models;

public class AppSettingsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters    = { new JsonStringEnumConverter() }
    };

    // Feature: SpeakerSight, Property 1: AppSettings serialization round-trip
#if FAST_TESTS
    [Property(MaxTest = 10)]
#else
    [Property(MaxTest = 100)]
#endif
    public Property Property_SerializationRoundTrip()
    {
        var opacityGen           = Gen.Choose(10, 100);
        var graceGen             = Gen.Elements(0.0, 0.5, 1.0, 1.5, 2.0);
        var debounceGen          = Gen.Choose(0, 1000);
        var fontSizeGen          = Gen.Choose(8, 32);
        var themeGen             = Arb.From<ThemePreference>().Generator;
        var displayModeGen       = Arb.From<DisplayMode>().Generator;
        var settingsGen = from opacity     in opacityGen
                         from grace       in graceGen
                         from debounce    in debounceGen
                         from fontSize    in fontSizeGen
                         from theme       in themeGen
                         from displayMode in displayModeGen
                         select new AppSettings
                         {
                             WindowLeft          = 20.0,
                             WindowTop           = 20.0,
                             Opacity             = opacity,
                             GracePeriodSeconds  = grace,
                             DebounceThresholdMs = debounce,
                             FontSize            = fontSize,
                             ThemePreference     = theme,
                             DisplayMode         = displayMode,
                         };

        return Prop.ForAll(Arb.From(settingsGen), original =>
        {
            var json     = JsonSerializer.Serialize(original, JsonOptions);
            var restored = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions)!;

            return (restored.Opacity             == original.Opacity             &&
                    restored.GracePeriodSeconds  == original.GracePeriodSeconds  &&
                    restored.DebounceThresholdMs == original.DebounceThresholdMs &&
                    restored.FontSize            == original.FontSize            &&
                    restored.ThemePreference     == original.ThemePreference     &&
                    restored.DisplayMode         == original.DisplayMode)
                .Label($"Round-trip mismatch: JSON={json}");
        });
    }

    // Feature: SpeakerSight, Property 2: AppSettings defaults satisfy all range constraints
#if FAST_TESTS
    [Property(MaxTest = 10)]
#else
    [Property(MaxTest = 100)]
#endif
    public Property Property_DefaultsSatisfyRangeConstraints()
    {
        return Prop.ForAll(Arb.From(Gen.Constant(new AppSettings())), defaults =>
        {
            bool opacityOk   = defaults.Opacity             >= 10  && defaults.Opacity             <= 100;
            bool graceOk     = defaults.GracePeriodSeconds  >= 0.0 && defaults.GracePeriodSeconds  <= 2.0;
            bool debounceOk  = defaults.DebounceThresholdMs >= 0   && defaults.DebounceThresholdMs <= 1000;
            bool fontSizeOk  = defaults.FontSize            >= 8   && defaults.FontSize            <= 32;

            return (opacityOk && graceOk && debounceOk && fontSizeOk)
                .Label($"Defaults out of range: opacity={defaults.Opacity} grace={defaults.GracePeriodSeconds} debounce={defaults.DebounceThresholdMs} fontSize={defaults.FontSize}");
        });
    }

    // Feature: SpeakerSight, Property 3: out-of-bounds position correction always yields position within monitor bounds
#if FAST_TESTS
    [Property(MaxTest = 10)]
#else
    [Property(MaxTest = 100)]
#endif
    public Property Property_OutOfBoundsPositionClamped()
    {
        // Generate positions far off every monitor (large negative or large positive)
        var leftGen = Gen.OneOf(
            Gen.Choose(-100_000, -1000).Select(x => (double)x),
            Gen.Choose(100_000,  200_000).Select(x => (double)x));
        var topGen = Gen.OneOf(
            Gen.Choose(-100_000, -1000).Select(x => (double)x),
            Gen.Choose(100_000,  200_000).Select(x => (double)x));

        return Prop.ForAll(Arb.From(leftGen), Arb.From(topGen), (left, top) =>
        {
            var (clampedLeft, clampedTop) = ScreenBoundsHelper.ClampPosition(left, top);

            var primary = Screen.PrimaryScreen!.WorkingArea;
            bool withinPrimary =
                clampedLeft >= primary.Left && clampedLeft < primary.Right &&
                clampedTop  >= primary.Top  && clampedTop  < primary.Bottom;

            return withinPrimary
                .Label($"Position ({left},{top}) clamped to ({clampedLeft},{clampedTop}) which is outside primary screen {primary}.");
        });
    }
}
