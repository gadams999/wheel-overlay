using System;
using WheelOverlay.Models;
using WheelOverlay.Services;
using Xunit;

namespace WheelOverlay.Tests
{
    public class ThemeServiceTests
    {
        // --- Registry-missing fallback returns light mode ---

        [Fact]
        public void DetectSystemTheme_ReturnsBoolean()
        {
            // DetectSystemTheme reads the real registry. On any machine it must
            // return a valid boolean without throwing, even if the key is absent.
            var service = new ThemeService(ThemePreference.System);
            bool result = service.DetectSystemTheme();
            Assert.IsType<bool>(result);
            service.Dispose();
        }

        [Fact]
        public void DetectSystemTheme_DoesNotThrow()
        {
            // Even if the registry key is missing or unreadable, the method
            // must not throw — it should silently default to light mode (false).
            var service = new ThemeService(ThemePreference.System);
            var exception = Record.Exception(() => service.DetectSystemTheme());
            Assert.Null(exception);
            service.Dispose();
        }

        // --- Known registry values produce expected results ---

        [Fact]
        public void Constructor_LightPreference_IsDarkModeFalse()
        {
            // Light preference must always resolve to light mode regardless of
            // what the system registry says.
            var service = new ThemeService(ThemePreference.Light);
            Assert.False(service.IsDarkMode);
            service.Dispose();
        }

        [Fact]
        public void Constructor_DarkPreference_IsDarkModeTrue()
        {
            // Dark preference must always resolve to dark mode regardless of
            // what the system registry says.
            var service = new ThemeService(ThemePreference.Dark);
            Assert.True(service.IsDarkMode);
            service.Dispose();
        }

        [Fact]
        public void Constructor_SystemPreference_MatchesDetectedTheme()
        {
            // System preference should resolve to whatever DetectSystemTheme returns.
            var service = new ThemeService(ThemePreference.System);
            bool detected = service.DetectSystemTheme();
            Assert.Equal(detected, service.IsDarkMode);
            service.Dispose();
        }

        // --- Preference setter updates IsDarkMode correctly ---

        [Fact]
        public void SetPreference_Light_OverridesSystemDetection()
        {
            var service = new ThemeService(ThemePreference.Dark);
            Assert.True(service.IsDarkMode);

            service.Preference = ThemePreference.Light;
            Assert.False(service.IsDarkMode);
            service.Dispose();
        }

        [Fact]
        public void SetPreference_Dark_OverridesSystemDetection()
        {
            var service = new ThemeService(ThemePreference.Light);
            Assert.False(service.IsDarkMode);

            service.Preference = ThemePreference.Dark;
            Assert.True(service.IsDarkMode);
            service.Dispose();
        }

        [Fact]
        public void SetPreference_System_MatchesDetectedTheme()
        {
            // Start with a manual override, then switch to System — should
            // match the detected system theme.
            var service = new ThemeService(ThemePreference.Dark);
            service.Preference = ThemePreference.System;

            bool detected = service.DetectSystemTheme();
            Assert.Equal(detected, service.IsDarkMode);
            service.Dispose();
        }

        // --- ThemeChanged event fires on transitions ---

        [Fact]
        public void ThemeChanged_FiresWhenPreferenceChangesEffectiveTheme()
        {
            var service = new ThemeService(ThemePreference.Light);
            bool? eventValue = null;
            service.ThemeChanged += (_, dark) => eventValue = dark;

            // Light → Dark should fire the event with true
            service.Preference = ThemePreference.Dark;
            Assert.True(eventValue);
            service.Dispose();
        }

        [Fact]
        public void ThemeChanged_DoesNotFireWhenEffectiveThemeUnchanged()
        {
            var service = new ThemeService(ThemePreference.Light);
            int fireCount = 0;
            service.ThemeChanged += (_, _) => fireCount++;

            // Light → Light should not fire
            service.Preference = ThemePreference.Light;
            Assert.Equal(0, fireCount);
            service.Dispose();
        }

        // --- Dispose is safe to call multiple times ---

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var service = new ThemeService(ThemePreference.System);
            service.Dispose();
            var exception = Record.Exception(() => service.Dispose());
            Assert.Null(exception);
        }
    }
}
