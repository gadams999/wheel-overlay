using System;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using WheelOverlay.Models;
using WheelOverlay.ViewModels;
using Xunit;

namespace WheelOverlay.Tests
{
    public class SettingsViewModelPropertyTests
    {
        // Feature: v0.6.0-enhancements, Property 2: Grid controls hidden for non-grid layouts
        // Validates: Requirement 2.2
        // For any DisplayLayout value, grid controls should be applicable only when layout is Grid.
#if FAST_TESTS
        [Property(MaxTest = 10)]
#else
        [Property(MaxTest = 100)]
#endif
        public Property Property_GridControlsApplicable_OnlyForGridLayout()
        {
            var layoutGen = Gen.Elements(
                Enum.GetValues(typeof(DisplayLayout)).Cast<DisplayLayout>().ToArray());

            return Prop.ForAll(
                Arb.From(layoutGen),
                layout =>
                {
                    var vm = new SettingsViewModel();
                    var profile = new Profile { Layout = layout };
                    vm.SelectedProfile = profile;

                    bool expected = layout == DisplayLayout.Grid;
                    bool actual = vm.IsGridLayoutSelected;

                    return (actual == expected)
                        .Label($"Layout={layout}: IsGridLayoutSelected={actual}, expected={expected}");
                });
        }
    }
}
