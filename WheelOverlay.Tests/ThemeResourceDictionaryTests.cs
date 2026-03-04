using System;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using Xunit;

namespace WheelOverlay.Tests
{
    public class ThemeResourceDictionaryTests
    {
        // The required resource keys as defined in the design document.
        private static readonly string[] RequiredBrushKeys = new[]
        {
            "ThemeBackground",
            "ThemeForeground",
            "ThemeControlBackground",
            "ThemeControlBorder",
            "ThemeControlForeground",
            "ThemeAccent",
        };

        private const string RequiredColorKey = "ThemeDropShadow";

        private static ResourceDictionary LoadThemeFromFile(string themeName)
        {
            // Walk up from the test bin directory to the repo root, then into the source.
            var dir = AppContext.BaseDirectory;
            var root = Path.GetFullPath(Path.Combine(dir, "..", "..", "..", ".."));
            var xamlPath = Path.Combine(root, "WheelOverlay", "Resources", $"{themeName}.xaml");

            if (!File.Exists(xamlPath))
                throw new FileNotFoundException($"Theme file not found: {xamlPath}");

            using var stream = File.OpenRead(xamlPath);
            return (ResourceDictionary)XamlReader.Load(stream);
        }

        // --- LightTheme.xaml tests ---

        [StaFact]
        public void LightTheme_ContainsAllRequiredBrushKeys()
        {
            var dict = LoadThemeFromFile("LightTheme");
            foreach (var key in RequiredBrushKeys)
            {
                Assert.True(dict.Contains(key), $"LightTheme.xaml is missing required key: {key}");
                Assert.IsType<SolidColorBrush>(dict[key]);
            }
        }

        [StaFact]
        public void LightTheme_ContainsThemeDropShadowColor()
        {
            var dict = LoadThemeFromFile("LightTheme");
            Assert.True(dict.Contains(RequiredColorKey), $"LightTheme.xaml is missing required key: {RequiredColorKey}");
            Assert.IsType<Color>(dict[RequiredColorKey]);
        }

        // --- DarkTheme.xaml tests ---

        [StaFact]
        public void DarkTheme_ContainsAllRequiredBrushKeys()
        {
            var dict = LoadThemeFromFile("DarkTheme");
            foreach (var key in RequiredBrushKeys)
            {
                Assert.True(dict.Contains(key), $"DarkTheme.xaml is missing required key: {key}");
                Assert.IsType<SolidColorBrush>(dict[key]);
            }
        }

        [StaFact]
        public void DarkTheme_ContainsThemeDropShadowColor()
        {
            var dict = LoadThemeFromFile("DarkTheme");
            Assert.True(dict.Contains(RequiredColorKey), $"DarkTheme.xaml is missing required key: {RequiredColorKey}");
            Assert.IsType<Color>(dict[RequiredColorKey]);
        }

        // --- Both themes define the same keys ---

        [StaFact]
        public void BothThemes_HaveMatchingResourceKeys()
        {
            var light = LoadThemeFromFile("LightTheme");
            var dark = LoadThemeFromFile("DarkTheme");

            var allRequired = new[] {
                "ThemeBackground", "ThemeForeground", "ThemeControlBackground",
                "ThemeControlBorder", "ThemeControlForeground", "ThemeAccent",
                "ThemeDropShadow"
            };

            foreach (var key in allRequired)
            {
                Assert.True(light.Contains(key), $"LightTheme.xaml missing: {key}");
                Assert.True(dark.Contains(key), $"DarkTheme.xaml missing: {key}");
            }
        }
    }
}
