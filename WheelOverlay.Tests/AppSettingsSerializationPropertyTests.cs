using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using FsCheck;
using FsCheck.Xunit;
using WheelOverlay.Models;
using Xunit;

namespace WheelOverlay.Tests
{
    public class AppSettingsSerializationPropertyTests
    {
        // Feature: v0.6.0-enhancements, Property 4: AppSettings serialization round-trip
        // Validates: Requirements 9.1, 9.2, 9.4, 2.4, 7.4
        // For any valid AppSettings with all enum values including Dial and ThemePreference,
        // serialize then deserialize should produce equivalent object.

        /// <summary>
        /// Generates a random valid Profile with constrained values.
        /// Grid dimensions are guaranteed to accommodate the position count,
        /// matching the invariant enforced by FromJson normalization.
        /// </summary>
        private static Gen<Profile> GenProfile()
        {
            var layoutGen = Gen.Elements(
                Enum.GetValues(typeof(DisplayLayout)).Cast<DisplayLayout>().ToArray());
            var posCountGen = Gen.Choose(1, 12);
            var labelGen = Gen.Elements("DASH", "TC2", "MAP", "FUEL", "BRGT", "VOL", "BOX", "DIFF", "ABS", "");
            var fontWeightGen = Gen.Elements("Normal", "Bold", "SemiBold");
            var renderModeGen = Gen.Elements("Aliased", "Auto", "ClearType");

            return from layout in layoutGen
                   from posCount in posCountGen
                   from labels in Gen.ListOf(posCount, labelGen)
                   from fontSize in Gen.Choose(8, 48)
                   from fontWeight in fontWeightGen
                   from renderMode in renderModeGen
                   from gridRows in Gen.Choose(1, 4)
                   from knobScale in Gen.Elements(1.0, 2.5, 5.0, 7.5, 10.0)
                   from gapPercent in Gen.Choose(10, 20)
                   let minCols = (int)Math.Ceiling((double)posCount / gridRows)
                   let gridCols = Math.Max(minCols, 1)
                   select new Profile
                   {
                       Id = Guid.NewGuid(),
                       Name = $"Profile_{posCount}",
                       DeviceName = "BavarianSimTec Alpha",
                       Layout = layout,
                       TextLabels = labels.ToList(),
                       PositionCount = posCount,
                       GridRows = gridRows,
                       GridColumns = gridCols,
                       FontSize = fontSize,
                       FontWeight = fontWeight,
                       TextRenderingMode = renderMode,
                       DialKnobScale = knobScale,
                       DialLabelGapPercent = gapPercent
                   };
        }

        /// <summary>
        /// Generates a random valid AppSettings with all enum combinations.
        /// </summary>
        private static Gen<AppSettings> GenAppSettings()
        {
            var themeGen = Gen.Elements(
                Enum.GetValues(typeof(ThemePreference)).Cast<ThemePreference>().ToArray());
            var layoutGen = Gen.Elements(
                Enum.GetValues(typeof(DisplayLayout)).Cast<DisplayLayout>().ToArray());
            var colorGen = Gen.Elements("#FFFFFF", "#000000", "#808080", "#FF0000", "#00FF00", "#CC808080");
            var profilesGen = Gen.NonEmptyListOf(GenProfile()).Select(list => list.ToList());

            return from theme in themeGen
                   from layout in layoutGen
                   from selectedColor in colorGen
                   from nonSelectedColor in colorGen
                   from fontSize in Gen.Choose(8, 48)
                   from profiles in profilesGen
                   from enableAnims in Arb.Generate<bool>()
                   from minimizeToTaskbar in Arb.Generate<bool>()
                   from windowLeft in Gen.Choose(0, 3000).Select(x => (double)x)
                   from windowTop in Gen.Choose(0, 2000).Select(x => (double)x)
                   select new AppSettings
                   {
                       ThemePreference = theme,
                       Layout = layout,
                       SelectedTextColor = selectedColor,
                       NonSelectedTextColor = nonSelectedColor,
                       FontSize = fontSize,
                       Profiles = profiles,
                       SelectedProfileId = profiles.First().Id,
                       EnableAnimations = enableAnims,
                       MinimizeToTaskbar = minimizeToTaskbar,
                       WindowLeft = windowLeft,
                       WindowTop = windowTop
                   };
        }

        /// <summary>
        /// Serializes AppSettings to JSON using the same approach as AppSettings.Save().
        /// </summary>
        private static string Serialize(AppSettings settings)
        {
            return JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        }

#if FAST_TESTS
        [Property(MaxTest = 10)]
#else
        [Property(MaxTest = 100)]
#endif
        public Property Property_SerializationRoundTrip_PreservesAllProperties()
        {
            return Prop.ForAll(
                Arb.From(GenAppSettings()),
                original =>
                {
                    // Normalize profiles before serialization (same as Save() does)
                    foreach (var profile in original.Profiles)
                    {
                        profile.NormalizeTextLabels();
                    }

                    var json = Serialize(original);
                    var deserialized = AppSettings.FromJson(json);

                    // Top-level settings
                    bool themeMatch = deserialized.ThemePreference == original.ThemePreference;
                    bool layoutMatch = deserialized.Layout == original.Layout;
                    bool selectedColorMatch = deserialized.SelectedTextColor == original.SelectedTextColor;
                    bool nonSelectedColorMatch = deserialized.NonSelectedTextColor == original.NonSelectedTextColor;
                    bool fontSizeMatch = deserialized.FontSize == original.FontSize;
                    bool animMatch = deserialized.EnableAnimations == original.EnableAnimations;
                    bool minimizeMatch = deserialized.MinimizeToTaskbar == original.MinimizeToTaskbar;
                    bool windowPosMatch = Math.Abs(deserialized.WindowLeft - original.WindowLeft) < 0.01
                                       && Math.Abs(deserialized.WindowTop - original.WindowTop) < 0.01;

                    // Profile count and content
                    bool profileCountMatch = deserialized.Profiles.Count == original.Profiles.Count;
                    bool selectedProfileMatch = deserialized.SelectedProfileId == original.SelectedProfileId;

                    bool profilesMatch = profileCountMatch && original.Profiles.Zip(deserialized.Profiles, (o, d) =>
                        o.Id == d.Id
                        && o.Name == d.Name
                        && o.Layout == d.Layout
                        && o.PositionCount == d.PositionCount
                        && o.TextLabels.SequenceEqual(d.TextLabels)
                        && o.GridRows == d.GridRows
                        && o.GridColumns == d.GridColumns
                        && o.FontSize == d.FontSize
                        && o.FontWeight == d.FontWeight
                        && o.TextRenderingMode == d.TextRenderingMode
                        && Math.Abs(o.DialKnobScale - d.DialKnobScale) < 0.01
                        && o.DialLabelGapPercent == d.DialLabelGapPercent
                    ).All(x => x);

                    bool allMatch = themeMatch && layoutMatch && selectedColorMatch
                        && nonSelectedColorMatch && fontSizeMatch && animMatch
                        && minimizeMatch && windowPosMatch && selectedProfileMatch && profilesMatch;

                    return allMatch.Label(
                        $"theme={themeMatch}, layout={layoutMatch}, color={selectedColorMatch}, " +
                        $"profiles={profilesMatch}(count={profileCountMatch}), selectedProfile={selectedProfileMatch}, " +
                        $"window={windowPosMatch}, anims={animMatch}");
                });
        }

#if FAST_TESTS
        [Property(MaxTest = 10)]
#else
        [Property(MaxTest = 100)]
#endif
        public Property Property_AllEnumValues_SurviveRoundTrip()
        {
            // Specifically verify every DisplayLayout and ThemePreference enum value round-trips.
            var layoutGen = Gen.Elements(
                Enum.GetValues(typeof(DisplayLayout)).Cast<DisplayLayout>().ToArray());
            var themeGen = Gen.Elements(
                Enum.GetValues(typeof(ThemePreference)).Cast<ThemePreference>().ToArray());

            return Prop.ForAll(
                Arb.From(layoutGen),
                Arb.From(themeGen),
                (layout, theme) =>
                {
                    var settings = new AppSettings
                    {
                        Layout = layout,
                        ThemePreference = theme,
                        Profiles = new List<Profile>
                        {
                            new Profile { Layout = layout, Name = "Test" }
                        }
                    };
                    settings.SelectedProfileId = settings.Profiles.First().Id;

                    var json = Serialize(settings);
                    var deserialized = AppSettings.FromJson(json);

                    bool topLayoutMatch = deserialized.Layout == layout;
                    bool themeMatch = deserialized.ThemePreference == theme;
                    bool profileLayoutMatch = deserialized.Profiles.First().Layout == layout;

                    return (topLayoutMatch && themeMatch && profileLayoutMatch)
                        .Label($"Layout={layout}({topLayoutMatch}), Theme={theme}({themeMatch}), ProfileLayout={profileLayoutMatch}");
                });
        }
    }
}
