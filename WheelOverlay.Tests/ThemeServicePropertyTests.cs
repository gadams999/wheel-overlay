using System;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using WheelOverlay.Models;
using WheelOverlay.Services;
using Xunit;

namespace WheelOverlay.Tests
{
    public class ThemeServicePropertyTests
    {
        // Feature: v0.6.0-enhancements, Property 3: Theme resolution from preference
        // Validates: Requirements 7.2, 7.3, 7.5
        // For any ThemePreference and system theme combination, verify effective theme
        // matches expected resolution:
        //   Light  → always light (IsDarkMode = false)
        //   Dark   → always dark  (IsDarkMode = true)
        //   System → matches DetectSystemTheme()

#if FAST_TESTS
        [Property(MaxTest = 10)]
#else
        [Property(MaxTest = 100)]
#endif
        public Property Property_ThemeResolution_MatchesExpected()
        {
            var preferenceGen = Gen.Elements(
                Enum.GetValues(typeof(ThemePreference)).Cast<ThemePreference>().ToArray());

            return Prop.ForAll(
                Arb.From(preferenceGen),
                preference =>
                {
                    var service = new ThemeService(preference);

                    bool expected = preference switch
                    {
                        ThemePreference.Light => false,
                        ThemePreference.Dark => true,
                        ThemePreference.System => service.DetectSystemTheme(),
                        _ => service.DetectSystemTheme()
                    };

                    bool actual = service.IsDarkMode;

                    // Clean up (stop any timers if started)
                    service.Dispose();

                    return (actual == expected)
                        .Label($"Preference={preference}: IsDarkMode={actual}, expected={expected}");
                });
        }

#if FAST_TESTS
        [Property(MaxTest = 10)]
#else
        [Property(MaxTest = 100)]
#endif
        public Property Property_ManualOverride_IgnoresSystemTheme()
        {
            // For any non-System preference, changing the preference after construction
            // must produce the correct effective theme regardless of what the system reports.
            var preferenceGen = Gen.Elements(ThemePreference.Light, ThemePreference.Dark);

            return Prop.ForAll(
                Arb.From(preferenceGen),
                preference =>
                {
                    // Start with System preference, then switch to manual override
                    var service = new ThemeService(ThemePreference.System);
                    service.Preference = preference;

                    bool expected = preference == ThemePreference.Dark;
                    bool actual = service.IsDarkMode;

                    service.Dispose();

                    return (actual == expected)
                        .Label($"Override preference={preference}: IsDarkMode={actual}, expected={expected}");
                });
        }

#if FAST_TESTS
        [Property(MaxTest = 10)]
#else
        [Property(MaxTest = 100)]
#endif
        public Property Property_PreferenceRoundTrip_PreservesTheme()
        {
            // For any sequence of preference changes, the final IsDarkMode must match
            // the resolution of the last-set preference.
            var preferenceGen = Gen.Elements(
                Enum.GetValues(typeof(ThemePreference)).Cast<ThemePreference>().ToArray());
            var sequenceGen = Gen.NonEmptyListOf(preferenceGen)
                .Select(list => list.ToArray());

            return Prop.ForAll(
                Arb.From(sequenceGen),
                preferences =>
                {
                    var service = new ThemeService(ThemePreference.System);

                    // Apply each preference in sequence
                    foreach (var pref in preferences)
                    {
                        service.Preference = pref;
                    }

                    var lastPref = preferences.Last();
                    bool expected = lastPref switch
                    {
                        ThemePreference.Light => false,
                        ThemePreference.Dark => true,
                        ThemePreference.System => service.DetectSystemTheme(),
                        _ => service.DetectSystemTheme()
                    };

                    bool actual = service.IsDarkMode;

                    service.Dispose();

                    return (actual == expected)
                        .Label($"After {preferences.Length} changes, last={lastPref}: IsDarkMode={actual}, expected={expected}");
                });
        }
    }
}
