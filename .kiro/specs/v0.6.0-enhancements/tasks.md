# Tasks: v0.6.0 Enhancements

## Task 0: Restructure Package directory — separate installer sources from build output
- [x] 0.1 Create `installer/` directory and move WiX source files (`Package.wxs`, `CustomUI.wxs`, `.wix/wix.json`) from `Package/` to `installer/`
- [x] 0.2 Move `Package/app.ico` to `assets/app.ico`
- [x] 0.3 Update `Package.wxs` icon `SourceFile` path to reference `app.ico` from the build output directory (copied by build script)
- [x] 0.4 Update `build_release.ps1` to copy installer sources (`installer/*.wxs`, `installer/.wix/`) and `assets/app.ico` into `Package/` before WiX build
- [x] 0.5 Update `.gitignore` to keep `Package/` fully ignored (pure build output) and track `installer/` directory
- [x] 0.6 Verify MSI build still works with the new directory structure

## Task 1: Add Dial enum value and ThemePreference model
- [x] 1.1 Add `Dial` value to the `DisplayLayout` enum in `WheelOverlay/Models/AppSettings.cs`
- [x] 1.2 Add `ThemePreference` enum (`System`, `Light`, `Dark`) to `WheelOverlay/Models/AppSettings.cs`
- [x] 1.3 Add `ThemePreference ThemePreference` property with default `ThemePreference.System` to the `AppSettings` class
- [x] 1.4 Verify existing `JsonStringEnumConverter` in `FromJson` handles both new enum values without changes

## Task 2: Create DialPositionConfig data model
- [x] 2.1 Create `WheelOverlay/Models/DialPositionConfig.cs` with `DefaultAngles` dictionary mapping positions 1–8 to angles (0°=12 o'clock, clockwise)
- [x] 2.2 Implement `GetAngles(int positionCount)` method that returns `DefaultAngles` for 8 positions and falls back to even distribution for other counts
- [x] 2.3 Implement `AngleToPoint(double angleDegrees, double radius)` helper that converts angle + radius to (X, Y) offset from center

## Task 3: Create DialLayout view
- [x] 3.1 Create `WheelOverlay/Views/DialLayout.xaml` as a Canvas-based UserControl following the same pattern as existing layout views (GridLayout, VerticalLayout, etc.)
- [x] 3.2 Implement `DialLayout.xaml.cs` code-behind that computes label positions from `DialPositionConfig.GetAngles()` and places TextBlocks on the Canvas
- [x] 3.3 Wire up `IsSelected` foreground color binding and flash animation triggers matching existing layout behavior
- [x] 3.4 Wire up font size, font family, and text rendering mode bindings from the active profile
- [x] 3.5 Handle `SizeChanged` event to recalculate label positions when Canvas resizes
- [x] 3.6 Add "Device Not Found" message display matching other layouts

## Task 4: Create and iterate on rotary knob graphic (manual, developer-in-the-loop)
- [x] 4.1 Create initial rotary knob graphic asset (PNG or SVG) for the Dial layout background
- [x] 4.2 Add the knob graphic to `WheelOverlay/Resources/` and center it on the DialLayout Canvas behind the position labels
- [x] 4.3 Run the application in test mode with Dial layout selected, visually verify knob appearance and label alignment
- [x] 4.4 Iterate on the knob graphic (size, detail, transparency, contrast) until satisfied with the visual result
- [x] 4.5 Verify the knob graphic renders correctly at different overlay window sizes

## Task 5: Integrate Dial layout into MainWindow
- [x] 5.1 Add `DialTemplate` DataTemplate in `MainWindow.xaml` Window.Resources referencing `DialLayout` UserControl
- [x] 5.2 Add DataTrigger for `Layout=Dial` in the ContentControl style to switch to `DialTemplate`

## Task 6: Update SettingsWindow for Dial layout
- [x] 6.1 Ensure the layout mode dropdown in `SettingsWindow.xaml` includes the `Dial` option (it should auto-populate from the enum)
- [x] 6.2 Add visibility logic to hide grid-specific controls (rows, columns, suggested dimensions) when Dial layout is selected
- [x] 6.3 Add `IsGridLayoutSelected` (or equivalent) property to `SettingsViewModel` that returns true only for Grid layout

## Task 7: Create theme resource dictionaries
- [x] 7.1 Create `WheelOverlay/Resources/LightTheme.xaml` with named color resources: ThemeBackground, ThemeForeground, ThemeControlBackground, ThemeControlBorder, ThemeControlForeground, ThemeAccent, ThemeDropShadow
- [x] 7.2 Create `WheelOverlay/Resources/DarkTheme.xaml` with the same resource keys and dark mode color values
- [x] 7.3 Merge the initial theme resource dictionary (LightTheme) in `App.xaml` Application.Resources

## Task 8: Create ThemeService
- [x] 8.1 Create `WheelOverlay/Services/ThemeService.cs` implementing `IDisposable` with `ThemeChanged` event, `IsDarkMode` property, and `Preference` property
- [x] 8.2 Implement `DetectSystemTheme()` that reads `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme` registry key, defaulting to light mode on failure
- [x] 8.3 Implement `ApplyTheme(bool dark)` that swaps the theme resource dictionary in `Application.Current.Resources.MergedDictionaries`
- [x] 8.4 Implement `StartWatching()` using a polling timer (every 2 seconds) to detect system theme changes and fire `ThemeChanged` when preference is `System`
- [x] 8.5 Implement theme resolution logic: Light/Dark preference overrides system detection; System preference follows detected theme

## Task 9: Integrate ThemeService into application lifecycle
- [x] 9.1 Initialize `ThemeService` in `App.xaml.cs` `OnStartup` with the persisted `ThemePreference` from loaded settings
- [x] 9.2 Subscribe to `ThemeChanged` event to update any open windows (SettingsWindow, AboutWindow)
- [x] 9.3 Dispose `ThemeService` in `CleanupResources`

## Task 10: Theme the Settings Window
- [x] 10.1 Convert `SettingsWindow.xaml` hardcoded colors/styles to use `{DynamicResource ThemeBackground}`, `{DynamicResource ThemeForeground}`, etc.
- [x] 10.2 Add theme preference combo box (System Default / Light / Dark) to the Settings Window UI
- [x] 10.3 Bind theme preference combo to `AppSettings.ThemePreference` and trigger `ThemeService.Preference` update on Apply

## Task 11: Theme the About Window
- [x] 11.1 Convert `AboutWindow.xaml` hardcoded colors/styles to use `{DynamicResource}` theme resources

## Task 12: Settings serialization backward compatibility
- [x] 12.1 Verify that `AppSettings.FromJson` correctly handles pre-v0.6.0 JSON missing `ThemePreference` (defaults to `System`) and missing `Dial` layout value (preserves existing layout)
- [x] 12.2 Add unit test for loading pre-v0.6.0 JSON without new properties

## Task 13: Property-based tests
- [x] 13.1 Create `WheelOverlay.Tests/DialPositionConfigPropertyTests.cs` — Property 1: Dial angle even distribution. *For any* position count N (2–20), verify equal angular spacing between consecutive positions. Tag: `Feature: v0.6.0-enhancements, Property 1: Dial angle even distribution` | <PBT>
- [x] 13.2 Create `WheelOverlay.Tests/SettingsViewModelPropertyTests.cs` (or append to existing) — Property 2: Grid controls hidden for non-grid layouts. *For any* DisplayLayout value, grid controls should be applicable only when layout is Grid. Tag: `Feature: v0.6.0-enhancements, Property 2: Grid controls hidden for non-grid layouts` | <PBT>
- [x] 13.3 Create `WheelOverlay.Tests/ThemeServicePropertyTests.cs` — Property 3: Theme resolution from preference. *For any* ThemePreference and system theme combination, verify effective theme matches expected resolution. Tag: `Feature: v0.6.0-enhancements, Property 3: Theme resolution from preference` | <PBT>
- [x] 13.4 Create `WheelOverlay.Tests/AppSettingsSerializationPropertyTests.cs` — Property 4: AppSettings serialization round-trip. *For any* valid AppSettings with all enum values including Dial and ThemePreference, serialize then deserialize should produce equivalent object. Tag: `Feature: v0.6.0-enhancements, Property 4: AppSettings serialization round-trip` | <PBT>

## Task 14: Unit tests
- [x] 14.1 Create `WheelOverlay.Tests/DialPositionConfigTests.cs` with unit tests: default 8-position angles match expected approximate values; config has entries for positions 1–8; `AngleToPoint` returns correct coordinates for known angles
- [x] 14.2 Add unit tests for `ThemeService`: registry-missing fallback returns light mode; known registry values produce expected results
- [x] 14.3 Add unit test for pre-v0.6.0 JSON backward compatibility (missing ThemePreference and Dial properties load with correct defaults)
- [x] 14.4 Add unit test verifying `ThemePreference` enum has exactly three values and `DisplayLayout` enum includes `Dial`
- [x] 14.5 Add unit tests verifying both `LightTheme.xaml` and `DarkTheme.xaml` contain all required resource keys

## Task 15 (Optional): Theme-appropriate icons
- [ ]* 15.1 Create light and dark variants of the system tray icon and add icon-swap logic in `ThemeChanged` handler
- [ ]* 15.2 Create light and dark variants of Settings Window toolbar icons/decorative graphics
- [ ]* 15.3 Manually test all icon and graphic assets in both Light and Dark modes for visual quality
