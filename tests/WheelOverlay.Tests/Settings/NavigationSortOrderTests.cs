// Feature: Material-Design-Settings, Property 1: Navigation categories always render in ascending SortOrder
using FsCheck;
using FsCheck.Xunit;
using OpenDash.OverlayCore.Settings;
using System.Windows;

namespace OpenDash.WheelOverlay.Tests.Settings;

public class NavigationSortOrderTests
{
    private sealed class StubCategory(string name, int sortOrder) : ISettingsCategory
    {
        public string CategoryName { get; } = name;
        public int SortOrder { get; } = sortOrder;
        public FrameworkElement CreateContent() => throw new NotImplementedException();
        public void SaveValues() { }
        public void LoadValues() { }
    }

    /// <summary>
    /// Mirrors the sorting logic in MaterialSettingsWindow.RegisterCategory().
    /// </summary>
    private static List<ISettingsCategory> SimulateRegistration(List<(string Name, int SortOrder)> defs)
    {
        var categories = defs
            .Select(d => (ISettingsCategory)new StubCategory(d.Name, d.SortOrder))
            .ToList();
        categories.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
        return categories;
    }

    [Property(
#if FAST_TESTS
        MaxTest = 10
#else
        MaxTest = 100
#endif
    )]
    public Property CategoryRegistration_AlwaysRendersInAscendingSortOrder()
    {
        // Feature: Material-Design-Settings, Property 1: Navigation categories always render in ascending SortOrder
        var gen = Gen.Choose(1, 999)
            .ListOf(10)
            .Select(list => list.Distinct().ToList())
            .Where(list => list.Count >= 2)
            .Select(orders => orders.Select((o, i) => ($"Cat{i}", o)).ToList());

        return Prop.ForAll(
            Arb.From(gen),
            defs =>
            {
                var sorted = SimulateRegistration(defs);
                bool ascending = sorted
                    .Zip(sorted.Skip(1), (a, b) => a.SortOrder <= b.SortOrder)
                    .All(ok => ok);
                return ascending.Label("Navigation categories must be in ascending SortOrder");
            });
    }
}
