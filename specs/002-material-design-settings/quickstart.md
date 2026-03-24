# Quickstart: Adding a Settings Category with Material Design Controls

**Branch**: `wheel-overlay/v0.7.0` | **Spec**: `specs/002-material-design-settings/`

This guide explains how to add a new settings category panel to WheelOverlay after the Material Design upgrade. It assumes `MaterialSettingsWindow` and `MaterialDesignBootstrap` are already integrated.

---

## 1. Implement ISettingsCategory

Create a new class in `src/WheelOverlay/Settings/`:

```csharp
using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
using OpenDash.OverlayCore.Settings;

namespace OpenDash.WheelOverlay.Settings;

public class MySettingsCategory : ISettingsCategory
{
    private readonly AppSettings _settings;
    private ComboBox? _myCombo;

    public MySettingsCategory(AppSettings settings)
    {
        _settings = settings;
    }

    public string CategoryName => "My Category";
    public int SortOrder => 4; // Choose a value; 999 is reserved for About

    public FrameworkElement CreateContent()
    {
        var panel = new StackPanel { Margin = new Thickness(16) };

        // Section heading
        var heading = new TextBlock
        {
            Text = "My Settings",
            Margin = new Thickness(0, 0, 0, 16)
        };
        Typography.SetBody(heading, "Subtitle1");  // MD type scale
        panel.Children.Add(heading);

        // MD2 ComboBox with floating label
        _myCombo = new ComboBox();
        HintAssist.SetHint(_myCombo, "Select Option");
        HintAssist.SetIsFloating(_myCombo, true);
        _myCombo.ItemsSource = new[] { "Option A", "Option B", "Option C" };
        _myCombo.Margin = new Thickness(0, 0, 0, 12);
        panel.Children.Add(_myCombo);

        // MD2 TextBox with floating label
        var textBox = new TextBox();
        HintAssist.SetHint(textBox, "Enter value");
        HintAssist.SetIsFloating(textBox, true);
        textBox.Margin = new Thickness(0, 0, 0, 12);
        panel.Children.Add(textBox);

        return panel;
    }

    public void LoadValues()
    {
        if (_myCombo != null)
            _myCombo.SelectedItem = _settings.MyOption;
    }

    public void SaveValues()
    {
        if (_myCombo?.SelectedItem is string val)
            _settings.MyOption = val;
    }
}
```

---

## 2. Apply MD2 Styles in Code

Because category panels are constructed in C# (not XAML), attach MD styles via static helper methods:

| WPF Control | MD2 Style (code) |
|---|---|
| `Button` (primary action) | `button.Style = (Style)Application.Current.FindResource("MaterialDesignRaisedButton")` |
| `Button` (secondary/flat) | `button.Style = (Style)Application.Current.FindResource("MaterialDesignFlatButton")` |
| `Button` (outlined) | `button.Style = (Style)Application.Current.FindResource("MaterialDesignOutlinedButton")` |
| `ComboBox` | `HintAssist.SetHint(cb, "Label"); HintAssist.SetIsFloating(cb, true)` |
| `TextBox` | `HintAssist.SetHint(tb, "Label"); HintAssist.SetIsFloating(tb, true)` |
| `RadioButton` | `rb.Style = (Style)Application.Current.FindResource("MaterialDesignRadioButton")` |
| `Slider` | `slider.Style = (Style)Application.Current.FindResource("MaterialDesignSlider")` |
| `TextBlock` heading | `Typography.SetBody(tb, "Subtitle1")` — or H5/H6 for larger headings |
| `TextBlock` body | `Typography.SetBody(tb, "Body1")` or `"Body2"` |

---

## 3. Register with MaterialSettingsWindow

In `WheelOverlay/App.xaml.cs`, add the new category where the other categories are registered:

```csharp
_settingsWindow.RegisterCategory(new DisplaySettingsCategory(viewModel));
_settingsWindow.RegisterCategory(new AppearanceSettingsCategory());
_settingsWindow.RegisterCategory(new AdvancedSettingsCategory());
_settingsWindow.RegisterCategory(new MySettingsCategory(_settings)); // ← add here
_settingsWindow.RegisterCategory(new AboutSettingsCategory(_themeService));
```

Categories are automatically sorted by `SortOrder` — no position-dependent ordering needed.

---

## 4. Theme Awareness

MD controls respond to the active Material Design palette automatically via dynamic resource bindings. No per-panel theme code is needed.

If a panel also uses WheelOverlay-specific theme resources (e.g., `ThemeAccent` for the overlay's visual elements), continue using `{DynamicResource ThemeAccent}` — these keys coexist with MDIX's `MaterialDesign*` keys without conflict.

---

## 5. Error Handling Requirements (Constitution Principle V)

If `CreateContent()` can fail (e.g., accessing a file or external resource), wrap the body in a try/catch:

```csharp
public FrameworkElement CreateContent()
{
    try
    {
        // ... build panel
    }
    catch (Exception ex)
    {
        LogService.Error($"MySettingsCategory.CreateContent failed: {ex.Message}");
        return new TextBlock { Text = "Settings unavailable." }; // degraded-but-functional
    }
}
```

`LoadValues()` and `SaveValues()` must similarly not throw — catch and log any failures.
