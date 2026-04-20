using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using ComboBox = System.Windows.Controls.ComboBox;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Orientation = System.Windows.Controls.Orientation;
using TextBox = System.Windows.Controls.TextBox;
using OpenDash.SpeakerSight.Models;
using OpenDash.OverlayCore.Models;
using OpenDash.OverlayCore.Resources.Fonts;
using OpenDash.OverlayCore.Services;
using OpenDash.OverlayCore.Settings;

namespace OpenDash.SpeakerSight.Settings;

/// <summary>
/// Appearance category: overlay position, opacity, color theme, font family, font size, and font color.
/// All changes apply live; values are persisted on Save.
/// Controls use the same MD2 style variants as WheelOverlay (MaterialDesignComboBox,
/// MaterialDesignSlider, MaterialDesignTextBox, MaterialDesignOutlinedButton).
/// </summary>
public class AppearanceSettingsCategory : ISettingsCategory
{
    private readonly AppSettings  _settings;
    private readonly MainWindow   _mainWindow;
    private readonly ThemeService _themeService;

    private TextBox?   _leftTextBox;
    private TextBox?   _topTextBox;
    private Slider?    _opacitySlider;
    private TextBlock? _opacityLabel;
    private ComboBox?  _themeComboBox;
    private ComboBox?  _fontFamilyComboBox;
    private Slider?    _fontSizeSlider;
    private TextBlock? _fontSizeLabel;
    private CheckBox?  _boldCheckBox;
    private TextBox?   _fontColorTextBox;
    private TextBox?   _backgroundColorTextBox;
    private Slider?    _spacingSlider;
    private TextBlock? _spacingLabel;

    public string CategoryName => "Appearance";
    public int SortOrder       => 30;

    public AppearanceSettingsCategory(AppSettings settings, MainWindow mainWindow, ThemeService themeService)
    {
        _settings     = settings;
        _mainWindow   = mainWindow;
        _themeService = themeService;
    }

    public FrameworkElement CreateContent()
    {
        var panel = new StackPanel { Margin = new Thickness(16) };

        // ── Position ───────────────────────────────────────────────────────

        panel.Children.Add(MakeSectionHeader("Position"));

        var posRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 16) };

        posRow.Children.Add(MakeLabel("Left:", width: 36));

        _leftTextBox = new TextBox
        {
            Text   = _settings.WindowLeft.ToString("F0"),
            Width  = 70,
            Margin = new Thickness(0, 0, 16, 0)
        };
        ApplyTextBoxStyle(_leftTextBox, "Left");
        _leftTextBox.GotFocus  += (_, _) => _mainWindow.SuspendClickThrough();
        _leftTextBox.LostFocus += OnPositionLostFocus;
        posRow.Children.Add(_leftTextBox);

        posRow.Children.Add(MakeLabel("Top:", width: 36));

        _topTextBox = new TextBox
        {
            Text  = _settings.WindowTop.ToString("F0"),
            Width = 70
        };
        ApplyTextBoxStyle(_topTextBox, "Top");
        _topTextBox.GotFocus  += (_, _) => _mainWindow.SuspendClickThrough();
        _topTextBox.LostFocus += OnPositionLostFocus;
        posRow.Children.Add(_topTextBox);

        panel.Children.Add(posRow);

        // ── Opacity ────────────────────────────────────────────────────────

        panel.Children.Add(MakeSectionHeader("Opacity (%)"));

        var opacityRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
        _opacityLabel = MakeValueLabel(_settings.Opacity.ToString(), width: 36);
        opacityRow.Children.Add(_opacityLabel);
        panel.Children.Add(opacityRow);

        _opacitySlider = MakeSlider(minimum: 10, maximum: 100, value: _settings.Opacity, tickFrequency: 5);
        _opacitySlider.Margin = new Thickness(0, 0, 0, 16);
        _opacitySlider.ValueChanged += (_, e) =>
        {
            int pct = (int)e.NewValue;
            if (_opacityLabel != null) _opacityLabel.Text = pct.ToString();
            _mainWindow.Opacity = pct / 100.0;
        };
        panel.Children.Add(_opacitySlider);

        // ── Theme ──────────────────────────────────────────────────────────

        panel.Children.Add(MakeSectionHeader("Color theme"));

        _themeComboBox = new ComboBox { Width = 200, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 16) };
        ApplyComboBoxStyle(_themeComboBox, hint: "Theme");
        _themeComboBox.Items.Add(new ComboBoxItem { Content = "System default", Tag = ThemePreference.System });
        _themeComboBox.Items.Add(new ComboBoxItem { Content = "Light",          Tag = ThemePreference.Light  });
        _themeComboBox.Items.Add(new ComboBoxItem { Content = "Dark",           Tag = ThemePreference.Dark   });
        _themeComboBox.SelectionChanged += OnThemeSelectionChanged;
        panel.Children.Add(_themeComboBox);

        // ── Font family ────────────────────────────────────────────────────

        panel.Children.Add(MakeSectionHeader("Font family"));

        _fontFamilyComboBox = BuildFontFamilyCombo();
        panel.Children.Add(_fontFamilyComboBox);

        // ── Font size ──────────────────────────────────────────────────────

        panel.Children.Add(MakeSectionHeader("Font size (pt)"));

        var fontRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
        _fontSizeLabel = MakeValueLabel(_settings.FontSize.ToString(), width: 36);
        fontRow.Children.Add(_fontSizeLabel);
        panel.Children.Add(fontRow);

        _fontSizeSlider = MakeSlider(minimum: 8, maximum: 32, value: _settings.FontSize, tickFrequency: 1);
        _fontSizeSlider.Margin = new Thickness(0, 0, 0, 16);
        _fontSizeSlider.ValueChanged += (_, e) =>
        {
            int pt = (int)e.NewValue;
            if (_fontSizeLabel != null) _fontSizeLabel.Text = pt.ToString();
            _mainWindow.FontSize = pt;
        };
        panel.Children.Add(_fontSizeSlider);

        // ── Bold ──────────────────────────────────────────────────────────

        _boldCheckBox = new CheckBox { Content = "Bold", Margin = new Thickness(0, 0, 0, 16) };
        _boldCheckBox.Style = (Style)System.Windows.Application.Current.FindResource("MaterialDesignCheckBox");
        _boldCheckBox.SetResourceReference(CheckBox.ForegroundProperty, "ThemeForeground");
        _boldCheckBox.Click += (_, _) =>
        {
            bool bold = _boldCheckBox.IsChecked == true;
            _mainWindow.FontWeight = bold ? FontWeights.Bold : FontWeights.Normal;
        };
        panel.Children.Add(_boldCheckBox);

        // ── Font color ─────────────────────────────────────────────────────

        panel.Children.Add(MakeSectionHeader("Font color"));
        _fontColorTextBox = BuildColorPicker(panel, ApplyFontColor);

        // ── Background color ───────────────────────────────────────────────

        panel.Children.Add(MakeSectionHeader("Background color"));
        _backgroundColorTextBox = BuildColorPicker(panel, ApplyBackgroundColor);

        // ── Speaker spacing ────────────────────────────────────────────────

        panel.Children.Add(MakeSectionHeader("Speaker spacing (px)"));

        var spacingRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
        _spacingLabel = MakeValueLabel(_settings.SpeakerSpacing.ToString(), width: 36);
        spacingRow.Children.Add(_spacingLabel);
        panel.Children.Add(spacingRow);

        _spacingSlider = MakeSlider(minimum: 0, maximum: 24, value: _settings.SpeakerSpacing, tickFrequency: 1);
        _spacingSlider.Margin = new Thickness(0, 0, 0, 16);
        _spacingSlider.ValueChanged += (_, e) =>
        {
            int px = (int)e.NewValue;
            if (_spacingLabel != null) _spacingLabel.Text = px.ToString();
            _mainWindow.SpeakerSpacing = px;
        };
        panel.Children.Add(_spacingSlider);

        LoadValues();

        // Keep position text boxes in sync whenever the overlay window moves (e.g. after dragging)
        _mainWindow.LocationChanged += OnMainWindowLocationChanged;
        panel.Unloaded += (_, _) => _mainWindow.LocationChanged -= OnMainWindowLocationChanged;

        return panel;
    }

    public void SaveValues()
    {
        // Position
        if (double.TryParse(_leftTextBox?.Text, out double left))
            _settings.WindowLeft = left;
        if (double.TryParse(_topTextBox?.Text, out double top))
            _settings.WindowTop = top;

        // Opacity
        if (_opacitySlider != null)
            _settings.Opacity = (int)_opacitySlider.Value;

        // Theme
        if (_themeComboBox?.SelectedItem is ComboBoxItem item && item.Tag is ThemePreference pref)
            _settings.ThemePreference = pref;

        // Font family
        if (_fontFamilyComboBox?.SelectedItem is ComboBoxItem fontItem && fontItem.Tag is string fontName)
            _settings.FontFamily = fontName;

        // Font size
        if (_fontSizeSlider != null)
            _settings.FontSize = (int)_fontSizeSlider.Value;

        // Bold
        if (_boldCheckBox != null)
            _settings.FontBold = _boldCheckBox.IsChecked == true;

        // Font color
        if (_fontColorTextBox != null)
            _settings.FontColor = _fontColorTextBox.Text;

        // Background color
        if (_backgroundColorTextBox != null)
            _settings.BackgroundColor = _backgroundColorTextBox.Text;

        // Speaker spacing
        if (_spacingSlider != null)
            _settings.SpeakerSpacing = (int)_spacingSlider.Value;

        _settings.Save();
    }

    public void LoadValues()
    {
        if (_leftTextBox  != null) _leftTextBox.Text  = _settings.WindowLeft.ToString("F0");
        if (_topTextBox   != null) _topTextBox.Text   = _settings.WindowTop.ToString("F0");

        if (_opacitySlider != null)
        {
            _opacitySlider.Value = _settings.Opacity;
            if (_opacityLabel != null) _opacityLabel.Text = _settings.Opacity.ToString();
        }

        if (_themeComboBox != null)
        {
            foreach (ComboBoxItem cbi in _themeComboBox.Items)
            {
                if (cbi.Tag is ThemePreference p && p == _settings.ThemePreference)
                {
                    _themeComboBox.SelectedItem = cbi;
                    break;
                }
            }
        }

        if (_fontFamilyComboBox != null)
        {
            foreach (ComboBoxItem cbi in _fontFamilyComboBox.Items)
            {
                if (cbi.Tag?.ToString() == _settings.FontFamily)
                {
                    _fontFamilyComboBox.SelectedItem = cbi;
                    break;
                }
            }
            if (_fontFamilyComboBox.SelectedItem == null && _fontFamilyComboBox.Items.Count > 0)
                _fontFamilyComboBox.SelectedIndex = 0;
        }

        if (_fontSizeSlider != null)
        {
            _fontSizeSlider.Value = _settings.FontSize;
            if (_fontSizeLabel != null) _fontSizeLabel.Text = _settings.FontSize.ToString();
        }

        if (_boldCheckBox != null)
            _boldCheckBox.IsChecked = _settings.FontBold;

        if (_fontColorTextBox != null)
            _fontColorTextBox.Text = _settings.FontColor;

        if (_backgroundColorTextBox != null)
            _backgroundColorTextBox.Text = _settings.BackgroundColor;

        if (_spacingSlider != null)
        {
            _spacingSlider.Value = _settings.SpeakerSpacing;
            if (_spacingLabel != null) _spacingLabel.Text = _settings.SpeakerSpacing.ToString();
        }
    }

    // ── Control builders ───────────────────────────────────────────────────

    private ComboBox BuildFontFamilyCombo()
    {
        var combo = new ComboBox { Width = 220, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 16) };
        ApplyComboBoxStyle(combo, hint: "Font Family");

        foreach (var font in FontUtilities.CuratedFonts)
        {
            combo.Items.Add(new ComboBoxItem
            {
                Content    = font,
                Tag        = font,
                FontFamily = FontUtilities.GetFontFamily(font)
            });
        }

        combo.SelectionChanged += (_, _) =>
        {
            if (combo.SelectedItem is ComboBoxItem item && item.Tag is string fontName)
                _mainWindow.FontFamily = FontUtilities.GetFontFamily(fontName);
        };

        return combo;
    }

    /// <summary>
    /// Builds a hex color text box + "Pick" button row and appends it to <paramref name="parent"/>.
    /// Returns the text box so the caller can read/write the value.
    /// </summary>
    private TextBox BuildColorPicker(StackPanel parent, Action<string> applyColor)
    {
        var row     = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 16) };
        var textBox = new TextBox { Width = 100, VerticalAlignment = VerticalAlignment.Center };
        ApplyTextBoxStyle(textBox, hint: "#RRGGBB");

        var pickBtn = new Button { Content = "Pick", MinWidth = 60, Margin = new Thickness(10, 0, 0, 0) };
        pickBtn.Style = (Style)System.Windows.Application.Current.FindResource("MaterialDesignOutlinedButton");

        pickBtn.Click += (_, _) =>
        {
            var dialog = new System.Windows.Forms.ColorDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var c = dialog.Color;
                textBox.Text = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
                applyColor(textBox.Text);
            }
        };

        textBox.LostFocus += (_, _) => applyColor(textBox.Text);

        row.Children.Add(textBox);
        row.Children.Add(pickBtn);
        parent.Children.Add(row);
        return textBox;
    }

    private void ApplyFontColor(string colorHex)
    {
        try
        {
            var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex);
            _mainWindow.Foreground = new System.Windows.Media.SolidColorBrush(color);
        }
        catch { /* ignore invalid hex strings */ }
    }

    private void ApplyBackgroundColor(string colorHex)
    {
        try
        {
            var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex);
            _mainWindow.SpeakerBackground = new System.Windows.Media.SolidColorBrush(color);
        }
        catch { /* ignore invalid hex strings */ }
    }

    // ── Style helpers (mirrors WheelOverlay pattern) ───────────────────────

    private static void ApplyComboBoxStyle(ComboBox combo, string hint)
    {
        combo.SetResourceReference(ComboBox.ForegroundProperty, "ThemeForeground");
        MaterialDesignThemes.Wpf.HintAssist.SetHint(combo, hint);
        combo.Style = (Style)System.Windows.Application.Current.FindResource("MaterialDesignComboBox");
    }

    private static void ApplyTextBoxStyle(TextBox textBox, string hint)
    {
        textBox.SetResourceReference(TextBox.ForegroundProperty, "ThemeForeground");
        MaterialDesignThemes.Wpf.HintAssist.SetHint(textBox, hint);
        MaterialDesignThemes.Wpf.HintAssist.SetIsFloating(textBox, true);
        textBox.Style = (Style)System.Windows.Application.Current.FindResource("MaterialDesignFloatingHintTextBox");
    }

    private static Slider MakeSlider(double minimum, double maximum, double value, double tickFrequency)
    {
        var slider = new Slider
        {
            Minimum          = minimum,
            Maximum          = maximum,
            Value            = value,
            TickFrequency    = tickFrequency,
            IsSnapToTickEnabled = true,
            SmallChange      = tickFrequency,
            LargeChange      = tickFrequency * 5
        };
        slider.Style = (Style)System.Windows.Application.Current.FindResource("MaterialDesignSlider");
        return slider;
    }

    private static TextBlock MakeSectionHeader(string text)
    {
        var tb = new TextBlock
        {
            Text       = text,
            FontSize   = 14,
            FontWeight = FontWeights.SemiBold,
            Margin     = new Thickness(0, 0, 0, 6)
        };
        tb.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
        return tb;
    }

    private static TextBlock MakeLabel(string text, double width = 0)
    {
        var tb = new TextBlock
        {
            Text              = text,
            FontSize          = 14,
            VerticalAlignment = VerticalAlignment.Center
        };
        if (width > 0) tb.Width = width;
        tb.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
        return tb;
    }

    private static TextBlock MakeValueLabel(string text, double width = 0)
    {
        var tb = new TextBlock
        {
            Text              = text,
            FontSize          = 14,
            VerticalAlignment = VerticalAlignment.Center
        };
        if (width > 0) tb.Width = width;
        tb.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
        return tb;
    }

    // ── Event handlers ─────────────────────────────────────────────────────

    private void OnMainWindowLocationChanged(object? sender, EventArgs e)
    {
        if (_leftTextBox != null) _leftTextBox.Text = _mainWindow.Left.ToString("F0");
        if (_topTextBox  != null) _topTextBox.Text  = _mainWindow.Top.ToString("F0");
    }

    private void OnPositionLostFocus(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(_leftTextBox?.Text, out double left))
            _mainWindow.Left = left;
        if (double.TryParse(_topTextBox?.Text, out double top))
            _mainWindow.Top = top;

        _mainWindow.RestoreClickThrough();
    }

    private void OnThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_themeComboBox?.SelectedItem is ComboBoxItem item && item.Tag is ThemePreference pref)
            _themeService.Preference = pref;
    }
}
