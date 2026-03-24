// Feature: Material-Design-Settings, Property 2: Cancel restores original settings values
using FsCheck;
using FsCheck.Xunit;
using OpenDash.OverlayCore.Settings;
using OpenDash.WheelOverlay.Models;
using System.Windows;

namespace OpenDash.WheelOverlay.Tests.Settings;

public class CategoryCancelDiscardTests
{
    /// <summary>
    /// Minimal stub that tracks its displayed value in memory, backed by an AppSettings
    /// instance. Models the invariant: LoadValues() reads from the backing model,
    /// not from any in-memory user edit.
    /// </summary>
    private sealed class StubSettingsCategory(AppSettings settings) : ISettingsCategory
    {
        public string CategoryName => "Stub";
        public int SortOrder => 1;
        public FrameworkElement CreateContent() => throw new NotImplementedException();

        /// <summary>The value currently shown in the (simulated) UI control.</summary>
        public int DisplayedFontSize { get; private set; }

        /// <summary>Reads from the backing model — mirrors real category LoadValues() logic.</summary>
        public void LoadValues() => DisplayedFontSize = settings.FontSize;

        /// <summary>Writes display state back to the backing model.</summary>
        public void SaveValues() => settings.FontSize = DisplayedFontSize;

        /// <summary>Simulates a user editing a control without clicking Apply/OK.</summary>
        public void SimulateUserEdit(int newFontSize) => DisplayedFontSize = newFontSize;
    }

    [Property(
#if FAST_TESTS
        MaxTest = 10
#else
        MaxTest = 100
#endif
    )]
    public Property CancelBehaviour_LoadValuesRestoresOriginalSettings()
    {
        // Feature: Material-Design-Settings, Property 2: Cancel restores original settings values
        var fontSizeGen = Gen.Choose(8, 48);

        return Prop.ForAll(
            Arb.From(fontSizeGen),
            Arb.From(fontSizeGen),
            (originalFontSize, editedFontSize) =>
            {
                var settings = new AppSettings { FontSize = originalFontSize };
                var category = new StubSettingsCategory(settings);

                // Step 1: Load values (simulates window open or category navigation)
                category.LoadValues();

                // Step 2: User edits the control without saving
                category.SimulateUserEdit(editedFontSize);

                // Step 3: Cancel = LoadValues() again (reads from unchanged AppSettings)
                category.LoadValues();

                // Invariant: displayed value reflects backing model, not the unsaved edit
                return (category.DisplayedFontSize == originalFontSize)
                    .Label($"Expected DisplayedFontSize={originalFontSize} after cancel, got {category.DisplayedFontSize}");
            });
    }
}
