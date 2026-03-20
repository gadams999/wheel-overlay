using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using OpenDash.OverlayCore.Services;

namespace OpenDash.OverlayCore.Settings;

/// <summary>
/// Two-column settings window with left-side navigation.
/// Overlay apps call <see cref="RegisterCategory"/> to add panels, including their own About category.
/// </summary>
public partial class MaterialSettingsWindow : Window
{
    private readonly ObservableCollection<ISettingsCategory> _categories = new();
    private ISettingsCategory? _currentCategory;

    /// <summary>Fired after OK or Apply saves all category values.</summary>
    public event EventHandler? SettingsApplied;

    public MaterialSettingsWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Registers a category. Categories are displayed sorted ascending by <see cref="ISettingsCategory.SortOrder"/>.
    /// </summary>
    public void RegisterCategory(ISettingsCategory category)
    {
        _categories.Add(category);

        // Keep the ListBox items sorted by SortOrder
        var sorted = _categories.OrderBy(c => c.SortOrder).ToList();
        CategoryListBox.Items.Clear();
        foreach (var cat in sorted)
        {
            var item = new ListBoxItem
            {
                Content = cat.CategoryName,
                Tag = cat
            };
            CategoryListBox.Items.Add(item);
        }

        // Select the first item if nothing is selected yet
        if (CategoryListBox.SelectedIndex < 0 && CategoryListBox.Items.Count > 0)
            CategoryListBox.SelectedIndex = 0;
    }

    // -----------------------------------------------------------------------
    // Navigation
    // -----------------------------------------------------------------------

    private void CategoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryListBox.SelectedItem is not ListBoxItem selected)
            return;
        if (selected.Tag is not ISettingsCategory newCategory)
            return;
        if (ReferenceEquals(newCategory, _currentCategory))
            return;

        // Save the departing category
        try { _currentCategory?.SaveValues(); }
        catch (Exception ex) { LogService.Error("Error saving category values on navigation", ex); }

        // Load the arriving category
        _currentCategory = newCategory;
        try
        {
            FrameworkElement content = newCategory.CreateContent();
            CategoryContent.Content = content;
            newCategory.LoadValues();
        }
        catch (Exception ex)
        {
            LogService.Error($"Error loading category '{newCategory.CategoryName}'", ex);
        }
    }

    private void ContentScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer sv)
        {
            sv.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta / 3.0);
            e.Handled = true;
        }
    }

    // -----------------------------------------------------------------------
    // Buttons
    // -----------------------------------------------------------------------

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        SaveAll();
        Close();
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        SaveAll();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SaveAll()
    {
        // Only save the current category — its controls hold the user's latest edits.
        // Non-current categories are saved automatically by the navigation handler when
        // the user switches away from them, so calling SaveValues() on them here would
        // overwrite the current category's just-saved data with a stale settings copy.
        try { _currentCategory?.SaveValues(); }
        catch (Exception ex) { LogService.Error("Error saving current category", ex); }

        SettingsApplied?.Invoke(this, EventArgs.Empty);
    }
}
