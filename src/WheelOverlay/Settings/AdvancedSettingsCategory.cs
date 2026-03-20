using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Binding = System.Windows.Data.Binding;
using Button = System.Windows.Controls.Button;
using Control = System.Windows.Controls.Control;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MessageBox = System.Windows.MessageBox;
using Orientation = System.Windows.Controls.Orientation;
using TextBlock = System.Windows.Controls.TextBlock;
using OpenDash.OverlayCore.Services;
using OpenDash.OverlayCore.Settings;
using OpenDash.WheelOverlay.Models;

namespace OpenDash.WheelOverlay.Settings;

/// <summary>
/// Advanced category: target process path configuration, overlay opacity, settings folder, and reset.
/// </summary>
public sealed class AdvancedSettingsCategory : ISettingsCategory
{
    private TextBlock? _targetExeDisplay;
    private Button? _clearButton;
    private Slider? _opacitySlider;

    private AppSettings _settings;

    public AdvancedSettingsCategory()
    {
        _settings = AppSettings.Load();
    }

    public string CategoryName => "Advanced";
    public int SortOrder => 3;

    // -----------------------------------------------------------------------
    // ISettingsCategory
    // -----------------------------------------------------------------------

    public FrameworkElement CreateContent()
    {
        var root = new StackPanel();

        var title = new TextBlock
        {
            Text = "Advanced Settings",
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 20)
        };
        title.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
        root.Children.Add(title);

        // --- Conditional Visibility (target process) ---
        AddLabel(root, "Conditional Visibility", "Show overlay only when this application is running");

        var filePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 15) };

        _targetExeDisplay = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0),
            MinWidth = 200
        };
        _targetExeDisplay.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");

        var browseBtn = new Button { Content = "Browse...", Width = 80, Margin = new Thickness(0, 0, 5, 0) };
        browseBtn.Click += OnBrowseClick;

        _clearButton = new Button { Content = "Clear", Width = 60 };
        _clearButton.Click += OnClearClick;

        filePanel.Children.Add(_targetExeDisplay);
        filePanel.Children.Add(browseBtn);
        filePanel.Children.Add(_clearButton);
        root.Children.Add(filePanel);

        // --- Move Overlay Opacity ---
        AddLabel(root, "Move Overlay Opacity", "Overlay transparency when repositioning (0 = invisible, 100 = fully opaque)");
        var (opacityPanel, opacitySlider) = MakeSlider(0, 100, 10, 80);
        _opacitySlider = opacitySlider;
        root.Children.Add(opacityPanel);

        // --- Review / Reset section ---
        var sep = new Border { BorderThickness = new Thickness(0, 1, 0, 0), Margin = new Thickness(0, 20, 0, 0) };
        sep.SetResourceReference(Border.BorderBrushProperty, "ThemeControlBorder");
        root.Children.Add(sep);

        var sectionLabel = new TextBlock
        {
            Text = "Review or Reset Settings",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 20, 0, 10)
        };
        sectionLabel.SetResourceReference(TextBlock.ForegroundProperty, "ThemeForeground");
        root.Children.Add(sectionLabel);

        var openFolderBtn = new Button
        {
            Content = "Open Settings Folder",
            Width = 180,
            Margin = new Thickness(0, 0, 0, 8),
            HorizontalAlignment = HorizontalAlignment.Left,
            ToolTip = "Opens the folder containing your settings and log files in File Explorer"
        };
        openFolderBtn.SetResourceReference(Control.BackgroundProperty, "ThemeControlBackground");
        openFolderBtn.SetResourceReference(Control.ForegroundProperty, "ThemeControlForeground");
        openFolderBtn.SetResourceReference(Control.BorderBrushProperty, "ThemeControlBorder");
        openFolderBtn.Click += OpenSettingsFolder_Click;
        root.Children.Add(openFolderBtn);

        var resetBtn = new Button
        {
            Content = "Reset to Defaults",
            Width = 180,
            Margin = new Thickness(0, 0, 0, 8),
            HorizontalAlignment = HorizontalAlignment.Left,
            ToolTip = "Deletes your settings file and restores all options to their default values"
        };
        resetBtn.SetResourceReference(Control.BackgroundProperty, "ThemeControlBackground");
        resetBtn.SetResourceReference(Control.ForegroundProperty, "ThemeControlForeground");
        resetBtn.SetResourceReference(Control.BorderBrushProperty, "ThemeControlBorder");
        resetBtn.Click += ResetSettings_Click;
        root.Children.Add(resetBtn);

        LoadValues();
        return root;
    }

    public void LoadValues()
    {
        _settings = AppSettings.Load();
        var currentProfile = GetCurrentProfile();

        if (_targetExeDisplay != null)
            UpdateTargetDisplay(currentProfile?.TargetExecutablePath);

        if (_clearButton != null)
            _clearButton.IsEnabled = !string.IsNullOrEmpty(currentProfile?.TargetExecutablePath);

        if (_opacitySlider != null)
            _opacitySlider.Value = _settings.MoveOverlayOpacity;
    }

    public void SaveValues()
    {
        if (_opacitySlider != null)
            _settings.MoveOverlayOpacity = (int)_opacitySlider.Value;

        _settings.Save();
    }

    // -----------------------------------------------------------------------
    // Event handlers
    // -----------------------------------------------------------------------

    private void OnBrowseClick(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
            Title = "Select Target Application"
        };
        if (dialog.ShowDialog() == true)
        {
            var profile = GetCurrentProfile();
            if (profile != null)
            {
                profile.TargetExecutablePath = dialog.FileName;
                UpdateTargetDisplay(dialog.FileName);
                if (_clearButton != null) _clearButton.IsEnabled = true;
            }
        }
    }

    private void OnClearClick(object sender, RoutedEventArgs e)
    {
        var profile = GetCurrentProfile();
        if (profile != null)
        {
            profile.TargetExecutablePath = null;
            UpdateTargetDisplay(null);
            if (_clearButton != null) _clearButton.IsEnabled = false;
        }
    }

    private void UpdateTargetDisplay(string? path)
    {
        if (_targetExeDisplay == null) return;
        _targetExeDisplay.Text = string.IsNullOrEmpty(path)
            ? "(any application)"
            : System.IO.Path.GetFileName(path);
        _targetExeDisplay.ToolTip = path;
    }

    private void OpenSettingsFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dir = AppSettings.GetSettingsDirectory();
            if (System.IO.Directory.Exists(dir))
                Process.Start("explorer.exe", dir);
            else
                MessageBox.Show(
                    "Settings folder does not exist yet. It will be created when settings are first saved.",
                    "Settings Folder", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            LogService.Error("Failed to open settings folder", ex);
        }
    }

    private void ResetSettings_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "This will reset all settings to their default values.\n\nAre you sure?",
            "Reset Settings", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            var path = AppSettings.GetSettingsPath();
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
                LogService.Info("Settings file deleted by user request (reset to defaults)");
            }
            _settings = AppSettings.Load();
            _settings.Save();
            LoadValues();
        }
        catch (Exception ex)
        {
            LogService.Error("Failed to reset settings", ex);
            MessageBox.Show("Failed to reset settings. See log for details.",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private Profile? GetCurrentProfile()
    {
        return _settings.Profiles.FirstOrDefault(p => p.Id == _settings.SelectedProfileId)
               ?? _settings.Profiles.FirstOrDefault();
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
