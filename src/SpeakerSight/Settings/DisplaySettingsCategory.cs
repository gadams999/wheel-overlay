using System.Windows;
using System.Windows.Controls;
using RadioButton = System.Windows.Controls.RadioButton;
using Orientation = System.Windows.Controls.Orientation;
using OpenDash.SpeakerSight.Models;
using OpenDash.OverlayCore.Settings;

namespace OpenDash.SpeakerSight.Settings;

public class DisplaySettingsCategory : ISettingsCategory
{
    private readonly AppSettings _settings;

    private RadioButton? _speakersOnlyRadio;
    private RadioButton? _allMembersRadio;
    private Slider?      _graceSlider;
    private TextBlock?   _graceValueLabel;
    private Slider?      _debounceSlider;
    private TextBlock?   _debounceValueLabel;

    public string CategoryName => "Display";
    public int SortOrder       => 20;

    public DisplaySettingsCategory(AppSettings settings)
    {
        _settings = settings;
    }

    public FrameworkElement CreateContent()
    {
        var panel = new StackPanel { Margin = new Thickness(16) };

        // ── Display mode ───────────────────────────────────────────────────

        panel.Children.Add(MakeSectionHeader("Display mode"));

        _speakersOnlyRadio = new RadioButton
        {
            Content   = "Speakers only (active + fading speakers)",
            GroupName = "DisplayMode",
            Margin    = new Thickness(0, 0, 0, 4),
            IsChecked = _settings.DisplayMode == DisplayMode.SpeakersOnly
        };
        _speakersOnlyRadio.SetResourceReference(RadioButton.ForegroundProperty, "ThemeForeground");
        panel.Children.Add(_speakersOnlyRadio);

        _allMembersRadio = new RadioButton
        {
            Content   = "All members (speakers first, then silent)",
            GroupName = "DisplayMode",
            Margin    = new Thickness(0, 0, 0, 16),
            IsChecked = _settings.DisplayMode == DisplayMode.AllMembers
        };
        _allMembersRadio.SetResourceReference(RadioButton.ForegroundProperty, "ThemeForeground");
        panel.Children.Add(_allMembersRadio);

        // ── Grace period (fade duration) ───────────────────────────────────

        var graceRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
        graceRow.Children.Add(MakeLabel("Fade duration (seconds)", width: 200));
        _graceValueLabel = MakeValueLabel(_settings.GracePeriodSeconds.ToString("F1"), width: 40);
        graceRow.Children.Add(_graceValueLabel);
        panel.Children.Add(graceRow);

        _graceSlider = MakeSlider(minimum: 0.0, maximum: 2.0, value: _settings.GracePeriodSeconds, tickFrequency: 0.1);
        _graceSlider.Margin = new Thickness(0, 0, 0, 16);
        _graceSlider.ValueChanged += (_, e) =>
        {
            if (_graceValueLabel != null)
                _graceValueLabel.Text = e.NewValue.ToString("F1");
        };
        panel.Children.Add(_graceSlider);

        // ── Debounce threshold (noise gate) ────────────────────────────────

        var debounceRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
        debounceRow.Children.Add(MakeLabel("Noise gate (ms, 0 = disabled)", width: 200));
        _debounceValueLabel = MakeValueLabel(_settings.DebounceThresholdMs.ToString(), width: 40);
        debounceRow.Children.Add(_debounceValueLabel);
        panel.Children.Add(debounceRow);

        _debounceSlider = MakeSlider(minimum: 0, maximum: 1000, value: _settings.DebounceThresholdMs, tickFrequency: 50);
        _debounceSlider.Margin = new Thickness(0, 0, 0, 8);
        _debounceSlider.ValueChanged += (_, e) =>
        {
            if (_debounceValueLabel != null)
                _debounceValueLabel.Text = ((int)e.NewValue).ToString();
        };
        panel.Children.Add(_debounceSlider);

        return panel;
    }

    public void SaveValues()
    {
        if (_speakersOnlyRadio?.IsChecked == true)
            _settings.DisplayMode = DisplayMode.SpeakersOnly;
        else
            _settings.DisplayMode = DisplayMode.AllMembers;

        if (_graceSlider != null)
            _settings.GracePeriodSeconds = _graceSlider.Value;

        if (_debounceSlider != null)
            _settings.DebounceThresholdMs = (int)_debounceSlider.Value;

        _settings.Save();
    }

    public void LoadValues()
    {
        if (_speakersOnlyRadio != null)
            _speakersOnlyRadio.IsChecked = _settings.DisplayMode == DisplayMode.SpeakersOnly;
        if (_allMembersRadio != null)
            _allMembersRadio.IsChecked = _settings.DisplayMode == DisplayMode.AllMembers;

        if (_graceSlider != null)
        {
            _graceSlider.Value = _settings.GracePeriodSeconds;
            if (_graceValueLabel != null)
                _graceValueLabel.Text = _settings.GracePeriodSeconds.ToString("F1");
        }

        if (_debounceSlider != null)
        {
            _debounceSlider.Value = _settings.DebounceThresholdMs;
            if (_debounceValueLabel != null)
                _debounceValueLabel.Text = _settings.DebounceThresholdMs.ToString();
        }
    }

    // ── Style helpers ──────────────────────────────────────────────────────

    private static Slider MakeSlider(double minimum, double maximum, double value, double tickFrequency)
    {
        var slider = new Slider
        {
            Minimum             = minimum,
            Maximum             = maximum,
            Value               = value,
            TickFrequency       = tickFrequency,
            IsSnapToTickEnabled = true,
            SmallChange         = tickFrequency,
            LargeChange         = tickFrequency * 5
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
            Margin     = new Thickness(0, 0, 0, 8)
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
}
