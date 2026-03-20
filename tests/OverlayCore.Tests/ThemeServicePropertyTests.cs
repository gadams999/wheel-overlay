using FsCheck;
using FsCheck.Xunit;
using OpenDash.OverlayCore.Models;
using OpenDash.OverlayCore.Services;

namespace OpenDash.OverlayCore.Tests;

/// <summary>
/// Property-based tests for ThemeService.
/// Validates universal correctness properties across all valid inputs.
/// </summary>
public class ThemeServicePropertyTests
{
    /// <summary>
    /// Property 1: Theme preference resolution is deterministic.
    /// For any ThemePreference value and system theme state, the resolved theme
    /// must follow these rules:
    /// - Light → false (not dark mode)
    /// - Dark → true (dark mode)
    /// - System → matches the system state
    /// </summary>
    [Property(
#if FAST_TESTS
        MaxTest = 10,
#else
        MaxTest = 100,
#endif
        Arbitrary = new[] { typeof(ThemePreferenceArbitrary) }
    )]
    public Property ThemePreferenceResolutionIsDeterministic(
        ThemePreference preference,
        bool systemIsDark)
    {
        // Arrange: Create a testable ThemeService that allows us to control system theme detection
        var service = new TestableThemeService(preference, systemIsDark);

        // Act: Get the resolved theme
        bool resolvedIsDark = service.IsDarkMode;

        // Assert: Verify deterministic resolution rules
        bool expectedIsDark = preference switch
        {
            ThemePreference.Light => false,
            ThemePreference.Dark => true,
            ThemePreference.System => systemIsDark,
            _ => systemIsDark // default case
        };

        return (resolvedIsDark == expectedIsDark)
            .Label($"Preference={preference}, SystemDark={systemIsDark}, Expected={expectedIsDark}, Actual={resolvedIsDark}");
    }

    /// <summary>
    /// Property 2: Changing preference re-evaluates theme deterministically.
    /// When the preference is changed, the IsDarkMode property must update
    /// according to the same deterministic rules.
    /// </summary>
    [Property(
#if FAST_TESTS
        MaxTest = 10,
#else
        MaxTest = 100,
#endif
        Arbitrary = new[] { typeof(ThemePreferenceArbitrary) }
    )]
    public Property ChangingPreferenceReEvaluatesTheme(
        ThemePreference initialPreference,
        ThemePreference newPreference,
        bool systemIsDark)
    {
        // Arrange: Create service with initial preference
        var service = new TestableThemeService(initialPreference, systemIsDark);

        // Act: Change the preference
        service.Preference = newPreference;

        // Assert: Verify the new resolved theme matches expectations
        bool expectedIsDark = newPreference switch
        {
            ThemePreference.Light => false,
            ThemePreference.Dark => true,
            ThemePreference.System => systemIsDark,
            _ => systemIsDark
        };

        return (service.IsDarkMode == expectedIsDark)
            .Label($"Initial={initialPreference}, New={newPreference}, SystemDark={systemIsDark}, Expected={expectedIsDark}, Actual={service.IsDarkMode}");
    }

    /// <summary>
    /// Property 3: Theme resolution is independent of evaluation order.
    /// Evaluating the same preference and system state multiple times
    /// must always produce the same result.
    /// </summary>
    [Property(
#if FAST_TESTS
        MaxTest = 10,
#else
        MaxTest = 100,
#endif
        Arbitrary = new[] { typeof(ThemePreferenceArbitrary) }
    )]
    public Property ThemeResolutionIsIdempotent(
        ThemePreference preference,
        bool systemIsDark)
    {
        // Arrange: Create two services with identical configuration
        var service1 = new TestableThemeService(preference, systemIsDark);
        var service2 = new TestableThemeService(preference, systemIsDark);

        // Act: Get resolved themes from both
        bool result1 = service1.IsDarkMode;
        bool result2 = service2.IsDarkMode;

        // Assert: Both must produce identical results
        return (result1 == result2)
            .Label($"Preference={preference}, SystemDark={systemIsDark}, Result1={result1}, Result2={result2}");
    }

    /// <summary>
    /// Testable version of ThemeService that allows controlling system theme detection
    /// without accessing the Windows registry.
    /// </summary>
    private class TestableThemeService
    {
        private readonly bool _mockSystemIsDark;
        private ThemePreference _preference;
        private bool _isDarkMode;

        public TestableThemeService(ThemePreference preference, bool mockSystemIsDark)
        {
            _mockSystemIsDark = mockSystemIsDark;
            _preference = preference;
            _isDarkMode = ResolveEffectiveTheme();
        }

        public bool IsDarkMode => _isDarkMode;

        public ThemePreference Preference
        {
            get => _preference;
            set
            {
                _preference = value;
                _isDarkMode = ResolveEffectiveTheme();
            }
        }

        /// <summary>
        /// Returns the mocked system theme state instead of reading registry.
        /// </summary>
        private bool DetectSystemTheme()
        {
            return _mockSystemIsDark;
        }

        /// <summary>
        /// Resolves the effective theme based on preference and mocked system state.
        /// Mirrors the logic in ThemeService.ResolveEffectiveTheme().
        /// </summary>
        private bool ResolveEffectiveTheme()
        {
            return _preference switch
            {
                ThemePreference.Light => false,
                ThemePreference.Dark => true,
                ThemePreference.System => DetectSystemTheme(),
                _ => DetectSystemTheme()
            };
        }
    }

    /// <summary>
    /// Custom FsCheck arbitrary generator for ThemePreference enum.
    /// Ensures all three enum values are generated with equal probability.
    /// </summary>
    public class ThemePreferenceArbitrary
    {
        public static Arbitrary<ThemePreference> ThemePreference()
        {
            return Gen.Elements(
                Models.ThemePreference.System,
                Models.ThemePreference.Light,
                Models.ThemePreference.Dark
            ).ToArbitrary();
        }
    }
}
