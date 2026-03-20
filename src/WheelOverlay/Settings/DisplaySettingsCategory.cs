using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Binding = System.Windows.Data.Binding;
using Button = System.Windows.Controls.Button;
using ComboBox = System.Windows.Controls.ComboBox;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MessageBox = System.Windows.MessageBox;
using Orientation = System.Windows.Controls.Orientation;
using TextBox = System.Windows.Controls.TextBox;
using OpenDash.OverlayCore.Services;
using OpenDash.OverlayCore.Settings;
using OpenDash.WheelOverlay.Models;
using OpenDash.WheelOverlay.ViewModels;

namespace OpenDash.WheelOverlay.Settings;

/// <summary>
/// Display category: device selection, profile management, layout, position count,
/// grid/dial controls, and position text labels.
/// </summary>
public sealed class DisplaySettingsCategory : ISettingsCategory
{
    private readonly SettingsViewModel _viewModel;

    // Cached root panel (created once by CreateContent)
    private StackPanel? _root;

    // Control references populated by CreateContent, refreshed by LoadValues
    private ComboBox? _deviceComboBox;
    private ComboBox? _profileComboBox;
    private ComboBox? _layoutComboBox;
    private ComboBox? _positionCountComboBox;
    private ComboBox? _gridRowsComboBox;
    private ComboBox? _gridColumnsComboBox;
    private StackPanel? _gridControlsPanel;
    private StackPanel? _dialControlsPanel;
    private Slider? _dialKnobScaleSlider;
    private Slider? _dialLabelGapSlider;
    private ItemsControl? _gridPreviewControl;
    private TextBlock? _gridCapacityText;
    private StackPanel? _labelsContainer;
    private TextBox[]? _labelTextBoxes;

    // Live settings — reloaded each time LoadValues is called
    private AppSettings _settings;
    private Profile? _currentProfile;

    public DisplaySettingsCategory(SettingsViewModel viewModel)
    {
        _viewModel = viewModel;
        _settings = AppSettings.Load();
    }

    public string CategoryName => "Display";
    public int SortOrder => 1;

    // -----------------------------------------------------------------------
    // ISettingsCategory
    // -----------------------------------------------------------------------

    public FrameworkElement CreateContent()
    {
        _root = new StackPanel { Margin = new Thickness(0) };
        BuildContent();
        return _root;
    }

    public void LoadValues()
    {
        _settings = AppSettings.Load();
        if (_root != null)
        {
            _root.Children.Clear();
            BuildContent();
        }
    }

    public void SaveValues()
    {
        if (_settings == null || _currentProfile == null) return;

        if (_deviceComboBox?.SelectedItem is string dev)
            _settings.SelectedDeviceName = dev;

        if (_layoutComboBox?.SelectedItem is ComboBoxItem layoutItem && layoutItem.Tag != null)
            _currentProfile.Layout = Enum.Parse<DisplayLayout>(layoutItem.Tag.ToString()!);

        if (_positionCountComboBox?.SelectedItem is int posCount)
            _currentProfile.PositionCount = posCount;

        if (_gridRowsComboBox?.SelectedItem is int rows)
            _currentProfile.GridRows = rows;
        if (_gridColumnsComboBox?.SelectedItem is int cols)
            _currentProfile.GridColumns = cols;

        if (_dialKnobScaleSlider != null)
            _currentProfile.DialKnobScale = Math.Round(_dialKnobScaleSlider.Value * 2) / 2;
        if (_dialLabelGapSlider != null)
            _currentProfile.DialLabelGapPercent = (int)_dialLabelGapSlider.Value;

        if (_labelTextBoxes != null)
        {
            for (int i = 0; i < _labelTextBoxes.Length; i++)
            {
                while (_currentProfile.TextLabels.Count <= i) _currentProfile.TextLabels.Add("");
                _currentProfile.TextLabels[i] = _labelTextBoxes[i].Text;
            }
        }

        _settings.Save();
    }

    // -----------------------------------------------------------------------
    // UI construction
    // -----------------------------------------------------------------------

    private void BuildContent()
    {
        if (_root == null) return;
        _settings = AppSettings.Load();
        _currentProfile = GetCurrentProfile();
        if (_currentProfile == null) return;

        _viewModel.SelectedProfile = _currentProfile;

        var title = MakeTitle("Display & Device Settings");
        _root.Children.Add(title);

        BuildDeviceSection();
        BuildProfileSection();
        BuildPositionCountSection();
        BuildGridControlsSection();
        BuildLabelsSection();
        BuildLayoutSection();
        BuildDialControlsSection();
    }

    private void BuildDeviceSection()
    {
        AddLabel("Wheel Device");
        _deviceComboBox = new ComboBox { Margin = new Thickness(0, 0, 0, 15) };
        foreach (var wheel in WheelDefinition.SupportedWheels)
            _deviceComboBox.Items.Add(wheel.DeviceName);
        _deviceComboBox.SelectedItem = _settings.SelectedDeviceName;
        _deviceComboBox.SelectionChanged += (s, e) =>
        {
            if (_deviceComboBox.SelectedItem is string newDevice && newDevice != _settings.SelectedDeviceName)
            {
                _settings.SelectedDeviceName = newDevice;
                _settings.SelectedProfileId = Guid.Empty;
                LoadValues(); // Rebuild entire display
            }
        };
        _root!.Children.Add(_deviceComboBox);
    }

    private void BuildProfileSection()
    {
        if (_currentProfile == null) return;
        AddLabel("Profile (for this device)");

        var profilePanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 15)
        };

        _profileComboBox = new ComboBox { Width = 200, Margin = new Thickness(0, 0, 10, 0) };
        var profiles = GetProfilesForCurrentDevice();
        foreach (var p in profiles)
        {
            var item = new ComboBoxItem { Content = p.Name, Tag = p.Id };
            _profileComboBox.Items.Add(item);
            if (p.Id == _currentProfile.Id) _profileComboBox.SelectedItem = item;
        }
        _profileComboBox.SelectionChanged += (s, e) =>
        {
            if (_profileComboBox.SelectedItem is ComboBoxItem sel && (Guid)sel.Tag != _settings.SelectedProfileId)
            {
                SaveValues();
                _settings.SelectedProfileId = (Guid)sel.Tag;
                LoadValues();
            }
        };

        var newBtn = new Button { Content = "New", Width = 60, Margin = new Thickness(0, 0, 5, 0) };
        newBtn.Click += (s, e) =>
        {
            SaveValues();
            var newProfile = CreateNewProfile("New Profile", _currentProfile!.DeviceName);
            newProfile.Layout = _currentProfile.Layout;
            newProfile.TextLabels = new List<string>(_currentProfile.TextLabels);
            _settings.SelectedProfileId = newProfile.Id;
            LoadValues();
        };

        var renameBtn = new Button { Content = "Rename", Width = 60, Margin = new Thickness(0, 0, 5, 0) };
        renameBtn.Click += (s, e) => RenameProfile();

        var delBtn = new Button { Content = "Delete", Width = 60 };
        delBtn.Click += (s, e) => DeleteProfile();

        profilePanel.Children.Add(_profileComboBox);
        profilePanel.Children.Add(newBtn);
        profilePanel.Children.Add(renameBtn);
        profilePanel.Children.Add(delBtn);
        _root!.Children.Add(profilePanel);
    }

    private void BuildPositionCountSection()
    {
        if (_currentProfile == null) return;
        AddLabel("Number of Positions", "Configure how many positions your wheel has (2-20)");
        _positionCountComboBox = new ComboBox { Margin = new Thickness(0, 0, 0, 15) };
        foreach (int count in _viewModel.AvailablePositionCounts)
            _positionCountComboBox.Items.Add(count);
        _positionCountComboBox.SelectedItem = _currentProfile.PositionCount;
        _positionCountComboBox.SelectionChanged += PositionCount_Changed;
        _root!.Children.Add(_positionCountComboBox);
    }

    private void BuildGridControlsSection()
    {
        if (_currentProfile == null) return;
        _gridControlsPanel = new StackPanel
        {
            Visibility = _currentProfile.Layout == DisplayLayout.Grid ? Visibility.Visible : Visibility.Collapsed
        };

        var dimLabel = MakeSectionLabel("Grid Layout Dimensions");
        _gridControlsPanel.Children.Add(dimLabel);

        var dimRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };
        var rowsLabel = MakeInlineLabel("Rows:", 50);
        _gridRowsComboBox = new ComboBox { Width = 60, Margin = new Thickness(0, 0, 10, 0) };
        foreach (int r in _viewModel.AvailableRows) _gridRowsComboBox.Items.Add(r);
        _gridRowsComboBox.SelectedItem = _currentProfile.GridRows;
        _gridRowsComboBox.SelectionChanged += GridDimensions_Changed;

        var timesLabel = MakeInlineLabel("×", 12);
        timesLabel.Margin = new Thickness(0, 0, 10, 0);

        var colsLabel = MakeInlineLabel("Columns:", 70);
        _gridColumnsComboBox = new ComboBox { Width = 60 };
        foreach (int c in _viewModel.AvailableColumns) _gridColumnsComboBox.Items.Add(c);
        _gridColumnsComboBox.SelectedItem = _currentProfile.GridColumns;
        _gridColumnsComboBox.SelectionChanged += GridDimensions_Changed;

        dimRow.Children.Add(rowsLabel);
        dimRow.Children.Add(_gridRowsComboBox);
        dimRow.Children.Add(timesLabel);
        dimRow.Children.Add(colsLabel);
        dimRow.Children.Add(_gridColumnsComboBox);
        _gridControlsPanel.Children.Add(dimRow);

        // Grid preview
        _gridCapacityText = new TextBlock
        {
            Text = _viewModel.GridCapacityDisplay,
            FontSize = 11,
            Margin = new Thickness(0, 5, 0, 5)
        };
        _gridCapacityText.SetResourceReference(TextBlock.ForegroundProperty, "ThemeSubtext");
        _gridControlsPanel.Children.Add(_gridCapacityText);

        _gridPreviewControl = new ItemsControl { Margin = new Thickness(0, 5, 0, 10) };
        _gridPreviewControl.ItemsSource = _viewModel.GridPreviewCells;
        RefreshGridPreviewTemplate();
        _gridControlsPanel.Children.Add(_gridPreviewControl);

        _root!.Children.Add(_gridControlsPanel);
    }

    private void BuildLabelsSection()
    {
        if (_currentProfile == null) return;
        AddLabel("Position Labels");

        // Sync label list with position count
        while (_currentProfile.TextLabels.Count < _currentProfile.PositionCount)
            _currentProfile.TextLabels.Add("");
        if (_currentProfile.TextLabels.Count > _currentProfile.PositionCount)
            _currentProfile.TextLabels.RemoveRange(_currentProfile.PositionCount,
                _currentProfile.TextLabels.Count - _currentProfile.PositionCount);

        var scrollViewer = new ScrollViewer
        {
            MaxHeight = 250,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Margin = new Thickness(0, 5, 0, 15)
        };

        _labelsContainer = new StackPanel();
        _labelTextBoxes = new TextBox[_currentProfile.PositionCount];

        for (int i = 0; i < _currentProfile.PositionCount; i++)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
            var lbl = MakeInlineLabel($"Position {i + 1}:", 80);
            var val = i < _currentProfile.TextLabels.Count ? _currentProfile.TextLabels[i] : "";
            var tb = new TextBox { Width = 180, Text = val };
            _labelTextBoxes[i] = tb;
            row.Children.Add(lbl);
            row.Children.Add(tb);
            _labelsContainer.Children.Add(row);
        }

        scrollViewer.Content = _labelsContainer;
        _root!.Children.Add(scrollViewer);
    }

    private void BuildLayoutSection()
    {
        if (_currentProfile == null) return;
        AddLabel("Display Layout");
        _layoutComboBox = new ComboBox { Margin = new Thickness(0, 0, 0, 15) };
        _layoutComboBox.Items.Add(MakeComboItem("Single Text", "Single"));
        _layoutComboBox.Items.Add(MakeComboItem("Vertical List", "Vertical"));
        _layoutComboBox.Items.Add(MakeComboItem("Horizontal List", "Horizontal"));
        _layoutComboBox.Items.Add(MakeComboItem("Grid", "Grid"));
        _layoutComboBox.Items.Add(MakeComboItem("Dial", "Dial"));

        foreach (ComboBoxItem item in _layoutComboBox.Items)
        {
            if (item.Tag?.ToString() == _currentProfile.Layout.ToString())
            {
                _layoutComboBox.SelectedItem = item;
                break;
            }
        }

        _layoutComboBox.SelectionChanged += (s, e) =>
        {
            if (_layoutComboBox.SelectedItem is ComboBoxItem sel)
            {
                var tag = sel.Tag?.ToString();
                if (_gridControlsPanel != null)
                    _gridControlsPanel.Visibility = tag == "Grid" ? Visibility.Visible : Visibility.Collapsed;
                if (_dialControlsPanel != null)
                    _dialControlsPanel.Visibility = tag == "Dial" ? Visibility.Visible : Visibility.Collapsed;
            }
        };
        _root!.Children.Add(_layoutComboBox);
    }

    private void BuildDialControlsSection()
    {
        if (_currentProfile == null) return;
        _dialControlsPanel = new StackPanel
        {
            Visibility = _currentProfile.Layout == DisplayLayout.Dial ? Visibility.Visible : Visibility.Collapsed
        };

        _dialControlsPanel.Children.Add(MakeSectionLabel("Dial Knob Size"));
        var (knobPanel, knobSlider) = MakeSlider(1, 10, 0.5, Math.Round(_currentProfile.DialKnobScale * 2) / 2);
        knobSlider.IsSnapToTickEnabled = true;
        _dialKnobScaleSlider = knobSlider;
        _dialControlsPanel.Children.Add(knobPanel);

        _dialControlsPanel.Children.Add(MakeSectionLabel("Label Gap"));
        var (gapPanel, gapSlider) = MakeSlider(10, 20, 1, _currentProfile.DialLabelGapPercent);
        gapSlider.IsSnapToTickEnabled = true;
        _dialLabelGapSlider = gapSlider;
        _dialControlsPanel.Children.Add(gapPanel);

        _root!.Children.Add(_dialControlsPanel);
    }

    // -----------------------------------------------------------------------
    // Event handlers
    // -----------------------------------------------------------------------

    private void PositionCount_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_positionCountComboBox == null || _currentProfile == null) return;
        if (_positionCountComboBox.SelectedItem is not int newCount) return;

        int oldCount = _currentProfile.TextLabels.Count;
        if (newCount < oldCount)
        {
            bool hasText = _currentProfile.TextLabels.Skip(newCount).Any(l => !string.IsNullOrWhiteSpace(l));
            if (hasText)
            {
                var result = MessageBox.Show(
                    $"Reducing position count will remove labels for positions {newCount + 1}–{oldCount}. Continue?",
                    "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    _positionCountComboBox.SelectedItem = oldCount;
                    return;
                }
            }
        }

        _viewModel.UpdatePositionCount(newCount);
        ValidateAndAdjustGridDimensions();
        LoadValues(); // Rebuild to show updated label inputs
    }

    private void GridDimensions_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_gridRowsComboBox == null || _gridColumnsComboBox == null || _currentProfile == null) return;
        if (_gridRowsComboBox.SelectedItem is int rows) _currentProfile.GridRows = rows;
        if (_gridColumnsComboBox.SelectedItem is int cols) _currentProfile.GridColumns = cols;

        var validation = ProfileValidator.ValidateGridDimensions(_currentProfile);
        if (!validation.IsValid)
        {
            MessageBox.Show(validation.Message, "Invalid Grid Configuration",
                MessageBoxButton.OK, MessageBoxImage.Error);
            _currentProfile.AdjustGridToDefault();
            if (_gridRowsComboBox != null) _gridRowsComboBox.SelectedItem = _currentProfile.GridRows;
            if (_gridColumnsComboBox != null) _gridColumnsComboBox.SelectedItem = _currentProfile.GridColumns;
        }

        _viewModel.RefreshGridPreview();
        if (_gridCapacityText != null) _gridCapacityText.Text = _viewModel.GridCapacityDisplay;
        RefreshGridPreviewTemplate();
    }

    // -----------------------------------------------------------------------
    // Profile helpers
    // -----------------------------------------------------------------------

    private Profile? GetCurrentProfile()
    {
        var profile = _settings.Profiles.FirstOrDefault(p => p.Id == _settings.SelectedProfileId);
        if (profile == null)
        {
            var valid = GetProfilesForCurrentDevice();
            profile = valid.Count > 0 ? valid[0] : CreateNewProfile("Default", _settings.SelectedDeviceName);
            _settings.SelectedProfileId = profile.Id;
        }
        return profile;
    }

    private List<Profile> GetProfilesForCurrentDevice() =>
        _settings.Profiles.Where(p => p.DeviceName == _settings.SelectedDeviceName).ToList();

    private Profile CreateNewProfile(string name, string deviceName)
    {
        var wheelDef = WheelDefinition.SupportedWheels.FirstOrDefault(w => w.DeviceName == deviceName)
                       ?? WheelDefinition.SupportedWheels[0];
        var profile = new Profile
        {
            Name = name,
            DeviceName = deviceName,
            TextLabels = Enumerable.Repeat("DASH", wheelDef.TextFieldCount).ToList()
        };
        if (deviceName == "BavarianSimTec Alpha")
            profile.TextLabels = new List<string> { "DASH", "TC2", "MAP", "FUEL", "BRGT", "VOL", "BOX", "DIFF" };
        _settings.Profiles.Add(profile);
        return profile;
    }

    private void RenameProfile()
    {
        if (_currentProfile == null) return;
        var dialog = new Window
        {
            Title = "Rename Profile",
            Width = 350,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };
        var panel = new StackPanel { Margin = new Thickness(10) };
        panel.Children.Add(new TextBlock { Text = "Enter new profile name:", Margin = new Thickness(0, 0, 0, 10) });
        var tb = new TextBox { Text = _currentProfile.Name, Margin = new Thickness(0, 0, 0, 10) };
        panel.Children.Add(tb);
        var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var ok = new Button { Content = "OK", Width = 70, Margin = new Thickness(0, 0, 5, 0) };
        ok.Click += (_, _) => { dialog.DialogResult = true; dialog.Close(); };
        var cancel = new Button { Content = "Cancel", Width = 70 };
        cancel.Click += (_, _) => { dialog.DialogResult = false; dialog.Close(); };
        btns.Children.Add(ok);
        btns.Children.Add(cancel);
        panel.Children.Add(btns);
        dialog.Content = panel;
        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(tb.Text))
        {
            _currentProfile.Name = tb.Text.Trim();
            LoadValues();
        }
    }

    private void DeleteProfile()
    {
        if (_currentProfile == null) return;
        var profiles = GetProfilesForCurrentDevice();
        if (profiles.Count <= 1)
        {
            MessageBox.Show("Cannot delete the only profile.", "Warning");
            return;
        }
        if (MessageBox.Show($"Delete profile '{_currentProfile.Name}'?", "Confirm",
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        {
            _settings.Profiles.Remove(_currentProfile);
            _settings.SelectedProfileId = profiles.First(p => p.Id != _currentProfile.Id).Id;
            LoadValues();
        }
    }

    private void ValidateAndAdjustGridDimensions()
    {
        if (_currentProfile == null || _currentProfile.IsValidGridConfiguration()) return;
        _currentProfile.AdjustGridToDefault();
        _viewModel.RefreshGridPreview();
        if (_gridRowsComboBox != null) _gridRowsComboBox.SelectedItem = _currentProfile.GridRows;
        if (_gridColumnsComboBox != null) _gridColumnsComboBox.SelectedItem = _currentProfile.GridColumns;
        MessageBox.Show(
            $"Grid dimensions adjusted to {_currentProfile.GridRows}×{_currentProfile.GridColumns} to accommodate {_currentProfile.PositionCount} positions.",
            "Grid Adjusted", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void RefreshGridPreviewTemplate()
    {
        if (_gridPreviewControl == null || _currentProfile == null) return;
        var factory = new FrameworkElementFactory(typeof(UniformGrid));
        factory.SetValue(UniformGrid.RowsProperty, _currentProfile.GridRows);
        factory.SetValue(UniformGrid.ColumnsProperty, _currentProfile.GridColumns);
        _gridPreviewControl.ItemsPanel = new ItemsPanelTemplate { VisualTree = factory };

        // Cell DataTemplate
        var cellBorderFactory = new FrameworkElementFactory(typeof(Border));
        cellBorderFactory.SetResourceReference(Border.BorderBrushProperty, "ThemeControlBorder");
        cellBorderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        cellBorderFactory.SetValue(Border.WidthProperty, 30.0);
        cellBorderFactory.SetValue(Border.HeightProperty, 30.0);
        cellBorderFactory.SetValue(Border.MarginProperty, new Thickness(2));
        var cellTextFactory = new FrameworkElementFactory(typeof(TextBlock));
        cellTextFactory.SetBinding(TextBlock.TextProperty, new Binding());
        cellTextFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        cellTextFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        cellTextFactory.SetValue(TextBlock.FontSizeProperty, 10.0);
        cellTextFactory.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
        cellBorderFactory.AppendChild(cellTextFactory);
        _gridPreviewControl.ItemTemplate = new DataTemplate { VisualTree = cellBorderFactory };
    }

    // -----------------------------------------------------------------------
    // UI helpers
    // -----------------------------------------------------------------------

    private TextBlock MakeTitle(string text)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 20)
        };
        tb.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
        return tb;
    }

    private TextBlock MakeSectionLabel(string text)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 10, 0, 5)
        };
        tb.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
        return tb;
    }

    private TextBlock MakeInlineLabel(string text, double width)
    {
        var tb = new TextBlock
        {
            Text = text,
            Width = width,
            VerticalAlignment = VerticalAlignment.Center
        };
        tb.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
        return tb;
    }

    private void AddLabel(string text, string? tooltip = null)
    {
        var container = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };
        var label = new TextBlock { Text = text, FontSize = 14, FontWeight = FontWeights.SemiBold };
        label.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
        container.Children.Add(label);

        if (tooltip != null)
        {
            var info = new Border
            {
                Width = 16, Height = 16, CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(6, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Background = System.Windows.Media.Brushes.Transparent,
                ToolTip = tooltip
            };
            info.SetResourceReference(Border.BorderBrushProperty, "ThemeSubtext");
            var infoTb = new TextBlock
            {
                Text = "i", FontSize = 10, FontStyle = FontStyles.Italic,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            infoTb.SetResourceReference(TextBlock.ForegroundProperty, "ThemeSubtext");
            info.Child = infoTb;
            container.Children.Add(info);
        }
        _root!.Children.Add(container);
    }

    private static (StackPanel Panel, Slider Slider) MakeSlider(double min, double max, double tick, double value)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 15) };
        var slider = new Slider { Minimum = min, Maximum = max, Width = 200, TickFrequency = tick, Value = value };
        var valueTb = new TextBlock { Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        valueTb.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
        valueTb.SetBinding(TextBlock.TextProperty, new Binding("Value") { Source = slider });
        panel.Children.Add(slider);
        panel.Children.Add(valueTb);
        return (panel, slider);
    }

    private static ComboBoxItem MakeComboItem(string content, string tag) =>
        new ComboBoxItem { Content = content, Tag = tag };
}
