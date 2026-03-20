using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Binding = System.Windows.Data.Binding;
using Button = System.Windows.Controls.Button;
using ComboBox = System.Windows.Controls.ComboBox;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Orientation = System.Windows.Controls.Orientation;
using TextBox = System.Windows.Controls.TextBox;
using OpenDash.OverlayCore.Models;
using OpenDash.OverlayCore.Resources.Fonts;
using OpenDash.OverlayCore.Settings;
using OpenDash.WheelOverlay.Models;

namespace OpenDash.WheelOverlay.Settings;

/// <summary>
/// Appearance category: theme, text colors, font family, and font size.
/// </summary>
public sealed class AppearanceSettingsCategory : ISettingsCategory
{
    // Control references
    private ComboBox? _themeComboBox;
    private TextBox? _selectedColorTextBox;
    private TextBox? _nonSelectedColorTextBox;
    private ComboBox? _fontFamilyComboBox;
    private Slider? _fontSizeSlider;
    private Slider? _spacingSlider;

    private AppSettings _settings;

    public AppearanceSettingsCategory()
    {
        _settings = AppSettings.Load();
    }

    public string CategoryName => "Appearance";
    public int SortOrder => 2;

    // -----------------------------------------------------------------------
    // ISettingsCategory
    // -----------------------------------------------------------------------

    public FrameworkElement CreateContent()
    {
        var root = new StackPanel();
        var title = new TextBlock
        {
            Text = "Appearance Settings",
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 20)
        };
        title.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
        root.Children.Add(title);

        // Theme
        AddLabel(root, "Theme");
        _themeComboBox = new ComboBox { Width = 200, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 15) };
        _themeComboBox.Items.Add(new ComboBoxItem { Content = "System Default", Tag = "System" });
        _themeComboBox.Items.Add(new ComboBoxItem { Content = "Light", Tag = "Light" });
        _themeComboBox.Items.Add(new ComboBoxItem { Content = "Dark", Tag = "Dark" });
        root.Children.Add(_themeComboBox);

        // Selected text color
        AddLabel(root, "Selected Text Color");
        _selectedColorTextBox = AddColorPicker(root);

        // Non-selected text color
        AddLabel(root, "Non-Selected Text Color");
        _nonSelectedColorTextBox = AddColorPicker(root);

        // Font family
        AddLabel(root, "Font Family");
        _fontFamilyComboBox = BuildFontFamilyCombo();
        root.Children.Add(_fontFamilyComboBox);

        // Font size
        AddLabel(root, "Font Size", "Text size for overlay labels (10-80 pt)");
        var (fontSizePanel, fontSizeSlider) = MakeSlider(10, 80, 1, 20);
        _fontSizeSlider = fontSizeSlider;
        root.Children.Add(fontSizePanel);

        // Item spacing
        AddLabel(root, "Item Spacing", "Space between items in pixels");
        var (spacingPanel, spacingSlider) = MakeSlider(0, 20, 1, 0);
        _spacingSlider = spacingSlider;
        root.Children.Add(spacingPanel);

        LoadValues();
        return root;
    }

    public void LoadValues()
    {
        _settings = AppSettings.Load();

        // Theme
        if (_themeComboBox != null)
        {
            foreach (ComboBoxItem item in _themeComboBox.Items)
            {
                if (item.Tag?.ToString() == _settings.ThemePreference.ToString())
                {
                    _themeComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        if (_selectedColorTextBox != null)
            _selectedColorTextBox.Text = _settings.SelectedTextColor;

        if (_nonSelectedColorTextBox != null)
            _nonSelectedColorTextBox.Text = _settings.NonSelectedTextColor;

        // Font family
        if (_fontFamilyComboBox != null)
        {
            foreach (ComboBoxItem item in _fontFamilyComboBox.Items)
            {
                if (item.Tag?.ToString() == _settings.FontFamily)
                {
                    _fontFamilyComboBox.SelectedItem = item;
                    break;
                }
            }
            if (_fontFamilyComboBox.SelectedItem == null && _fontFamilyComboBox.Items.Count > 0)
                _fontFamilyComboBox.SelectedIndex = 0;
        }

        if (_fontSizeSlider != null)
            _fontSizeSlider.Value = _settings.FontSize;

        if (_spacingSlider != null)
            _spacingSlider.Value = _settings.ItemSpacing;
    }

    public void SaveValues()
    {
        if (_themeComboBox?.SelectedItem is ComboBoxItem themeItem && themeItem.Tag != null)
            _settings.ThemePreference = Enum.Parse<ThemePreference>(themeItem.Tag.ToString()!);

        if (_selectedColorTextBox != null)
            _settings.SelectedTextColor = _selectedColorTextBox.Text;

        if (_nonSelectedColorTextBox != null)
            _settings.NonSelectedTextColor = _nonSelectedColorTextBox.Text;

        if (_fontFamilyComboBox?.SelectedItem is ComboBoxItem fontItem && fontItem.Tag != null)
            _settings.FontFamily = fontItem.Tag.ToString()!;

        if (_fontSizeSlider != null)
            _settings.FontSize = (int)_fontSizeSlider.Value;

        if (_spacingSlider != null)
            _settings.ItemSpacing = (int)_spacingSlider.Value;

        _settings.Save();
    }

    // -----------------------------------------------------------------------
    // UI helpers
    // -----------------------------------------------------------------------

    private ComboBox BuildFontFamilyCombo()
    {
        var combo = new ComboBox { Width = 220, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 15) };

        // Provide a curated list of common system fonts
        var fonts = new[]
        {
            "Segoe UI", "Arial", "Calibri", "Consolas", "Courier New",
            "Tahoma", "Times New Roman", "Trebuchet MS", "Verdana"
        };

        foreach (var font in fonts)
        {
            combo.Items.Add(new ComboBoxItem
            {
                Content = font,
                Tag = font,
                FontFamily = FontUtilities.GetFontFamily(font)
            });
        }
        return combo;
    }

    private TextBox AddColorPicker(StackPanel root)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 15) };
        var textBox = new TextBox { Width = 100, VerticalAlignment = VerticalAlignment.Center };
        var pickBtn = new Button { Content = "Pick", Width = 50, Margin = new Thickness(10, 0, 0, 0) };
        pickBtn.Click += (s, e) =>
        {
            var dialog = new System.Windows.Forms.ColorDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var c = dialog.Color;
                textBox.Text = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
            }
        };
        panel.Children.Add(textBox);
        panel.Children.Add(pickBtn);
        root.Children.Add(panel);
        return textBox;
    }

    private static void AddLabel(StackPanel root, string text, string? tooltip = null)
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
        root.Children.Add(container);
    }

    private static (StackPanel Panel, Slider Slider) MakeSlider(double min, double max, double tick, double value)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 15) };
        var slider = new Slider { Minimum = min, Maximum = max, Width = 200, TickFrequency = tick, IsSnapToTickEnabled = true, Value = value };
        var valueTb = new TextBlock { Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        valueTb.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
        valueTb.SetBinding(TextBlock.TextProperty, new Binding("Value") { Source = slider });
        panel.Children.Add(slider);
        panel.Children.Add(valueTb);
        return (panel, slider);
    }
}
