# Contract: GlobalHotkeyService

**Location**: `src/OverlayCore/Services/GlobalHotkeyService.cs`
**Namespace**: `OpenDash.OverlayCore.Services`
**Type**: Public class, implements `IDisposable`
**Consumers**: Overlay application main windows

---

## Class Definition

```csharp
namespace OpenDash.OverlayCore.Services;

public class GlobalHotkeyService : IDisposable
{
    /// <summary>
    /// Fired on the UI thread when Alt+F6 is pressed system-wide.
    /// Overlay windows subscribe to this to toggle between overlay and positioning mode.
    /// </summary>
    public event EventHandler? ToggleModeRequested;

    /// <summary>
    /// Registers Alt+F6 as a global hotkey.
    /// Returns true on success.
    /// On failure (e.g., key held by another app): logs descriptive error via LogService and returns false.
    /// Application continues operating without the hotkey — no exception thrown.
    /// </summary>
    /// <param name="windowHandle">HWND of the hidden helper window (or main window).</param>
    public bool Register(IntPtr windowHandle);

    /// <summary>Unregisters the global hotkey. Safe to call if Register returned false.</summary>
    public void Unregister();

    /// <summary>
    /// Must be called from the window's WndProc to process WM_HOTKEY messages.
    /// Fires ToggleModeRequested when msg == WM_HOTKEY and wParam == hotkey ID.
    /// </summary>
    public void ProcessMessage(int msg, IntPtr wParam);

    /// <summary>Unregisters hotkey and disposes resources.</summary>
    public void Dispose();
}
```

---

## Win32 Constants (internal implementation detail, documented for test authors)

| Constant | Value | Meaning |
|----------|-------|---------|
| `HOTKEY_ID` | `0x0001` | Arbitrary ID for this hotkey registration |
| `MOD_ALT` | `0x0001` | Alt modifier |
| `VK_F6` | `0x75` | F6 virtual key |
| `WM_HOTKEY` | `0x0312` | Windows hotkey message |

---

## Usage Pattern in MainWindow

```csharp
// Startup: register after window is loaded (HWND available)
_hotkeyService = new GlobalHotkeyService();
bool registered = _hotkeyService.Register(new WindowInteropHelper(this).Handle);
// if !registered: hotkey unavailable, system tray menu is fallback — already logged

// Subscribe to mode toggle
_hotkeyService.ToggleModeRequested += (_, _) => ToggleOverlayMode();

// Shutdown: dispose (Unregister called automatically)
_hotkeyService.Dispose();
```

---

## Mode Toggle Logic (MainWindow responsibility)

```csharp
private void ToggleOverlayMode()
{
    if (_configMode)
    {
        // Exiting positioning mode: save position (same as Enter key)
        _configModeBehavior.Exit(confirm: true);
    }
    else
    {
        // Entering positioning mode
        _configModeBehavior.Enter(this);
    }
}
```

---

## Error Handling Contract

If `RegisterHotKey` fails:
- `LogService.Error($"Failed to register global hotkey Alt+F6. Another application may be using this key combination. Error code: {Marshal.GetLastWin32Error()}")` is called
- `Register()` returns `false`
- No exception is thrown
- `ToggleModeRequested` is never fired
- Overlay continues operating; system tray "Configure overlay position" remains functional

---

## Property Test Coverage

**Property 5**: Overlay mode state machine alternation
- For any initial mode and N toggles: even N → initial mode, odd N → opposite mode
- Positioning→Overlay transition via toggle MUST trigger confirm (position saved)
- Test file: `tests/OverlayCore.Tests/OverlayModePropertyTests.cs`
