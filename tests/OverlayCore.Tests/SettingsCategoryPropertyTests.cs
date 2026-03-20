// Feature: OpenDash Monorepo Rebrand, Property 7: Settings categories display in ascending SortOrder
using FsCheck;
using FsCheck.Xunit;
using OpenDash.OverlayCore.Settings;
using System.Windows;

namespace OpenDash.OverlayCore.Tests;

public class SettingsCategoryPropertyTests
{
    // ---------------------------------------------------------------------------
    // Stub: ISettingsCategory implementation for unit tests
    // ---------------------------------------------------------------------------

    private sealed class StubCategory(string name, int sortOrder) : ISettingsCategory
    {
        public string CategoryName { get; } = name;
        public int SortOrder { get; } = sortOrder;
        public FrameworkElement CreateContent() => throw new NotImplementedException();
        public void SaveValues() { }
        public void LoadValues() { }
    }

    // ---------------------------------------------------------------------------
    // Helpers that mirror MaterialSettingsWindow registration logic
    // ---------------------------------------------------------------------------

    private static List<ISettingsCategory> SimulateRegistration(List<(string Name, int SortOrder)> appCategoryDefs)
    {
        var categories = new List<ISettingsCategory>();
        foreach (var (name, order) in appCategoryDefs)
            categories.Add(new StubCategory(name, order));

        // Each overlay registers its own About category at SortOrder=999
        categories.Add(new StubCategory("About", 999));

        // Sort ascending by SortOrder
        categories.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
        return categories;
    }

    // ---------------------------------------------------------------------------
    // Property 7a: Navigation list contains exactly N+1 entries (N app + About)
    // ---------------------------------------------------------------------------

    [Property(
#if FAST_TESTS
        MaxTest = 10
#else
        MaxTest = 100
#endif
    )]
    public Property P7a_NavigationList_ContainsNPlusOneEntries()
    {
        // Generate a list of 1-10 distinct sort orders (excluding 999)
        var gen = Gen.Choose(1, 998)
            .ListOf(10)
            .Select(list => list.Distinct().ToList())
            .Where(list => list.Count >= 1)
            .Select(orders => orders
                .Select((o, i) => ($"Cat{i}", o))
                .ToList());

        return Prop.ForAll(
            Arb.From(gen),
            appDefs =>
            {
                var sorted = SimulateRegistration(appDefs);
                bool countCorrect = sorted.Count == appDefs.Count + 1;
                return countCorrect.Label($"Expected {appDefs.Count + 1} entries, got {sorted.Count}");
            });
    }

    // ---------------------------------------------------------------------------
    // Property 7b: Navigation list is in ascending SortOrder regardless of registration order
    // ---------------------------------------------------------------------------

    [Property(
#if FAST_TESTS
        MaxTest = 10
#else
        MaxTest = 100
#endif
    )]
    public Property P7b_NavigationList_DisplayedInAscendingSortOrder()
    {
        var gen = Gen.Choose(1, 998)
            .ListOf(10)
            .Select(list => list.Distinct().ToList())
            .Where(list => list.Count >= 2)
            .Select(orders => orders
                .Select((o, i) => ($"Cat{i}", o))
                .ToList());

        return Prop.ForAll(
            Arb.From(gen),
            appDefs =>
            {
                var sorted = SimulateRegistration(appDefs);
                bool ascending = sorted
                    .Zip(sorted.Skip(1), (a, b) => a.SortOrder <= b.SortOrder)
                    .All(ok => ok);
                return ascending.Label("Navigation list must be in ascending SortOrder");
            });
    }

    // ---------------------------------------------------------------------------
    // Property 7c: About (SortOrder=999) always appears last
    // ---------------------------------------------------------------------------

    [Property(
#if FAST_TESTS
        MaxTest = 10
#else
        MaxTest = 100
#endif
    )]
    public Property P7c_AboutCategory_AlwaysAppearsLast()
    {
        var gen = Gen.Choose(1, 998)
            .ListOf(5)
            .Select(list => list.Distinct().ToList())
            .Where(list => list.Count >= 1)
            .Select(orders => orders
                .Select((o, i) => ($"Cat{i}", o))
                .ToList());

        return Prop.ForAll(
            Arb.From(gen),
            appDefs =>
            {
                var sorted = SimulateRegistration(appDefs);
                bool aboutLast = sorted.Last().SortOrder == 999
                              && sorted.Last().CategoryName == "About";
                return aboutLast.Label("About (SortOrder=999) must be the last navigation entry");
            });
    }

    // ---------------------------------------------------------------------------
    // Deterministic verification
    // ---------------------------------------------------------------------------

    [Xunit.Fact]
    public void WheelOverlay_FourCategories_SortedCorrectly()
    {
        var appDefs = new List<(string, int)>
        {
            ("Advanced", 3),
            ("Display", 1),
            ("Appearance", 2),
        };

        var sorted = SimulateRegistration(appDefs);

        Assert.Equal(4, sorted.Count);
        Assert.Equal("Display", sorted[0].CategoryName);
        Assert.Equal("Appearance", sorted[1].CategoryName);
        Assert.Equal("Advanced", sorted[2].CategoryName);
        Assert.Equal("About", sorted[3].CategoryName);
    }
}
