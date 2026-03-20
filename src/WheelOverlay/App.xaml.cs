using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;
using OpenDash.WheelOverlay.Models;
using OpenDash.WheelOverlay.Services;
using OpenDash.WheelOverlay.Settings;
using OpenDash.WheelOverlay.ViewModels;
using OpenDash.OverlayCore.Services;
using OpenDash.OverlayCore.Models;
using OpenDash.OverlayCore.Settings;

namespace OpenDash.WheelOverlay
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private NotifyIcon? _notifyIcon;
        private MainWindow? _mainWindow;
        private MaterialSettingsWindow? _settingsWindow;
        private ToolStripMenuItem? _configModeMenuItem;
        private ToolStripMenuItem? _minimizeMenuItem;
        private ToolStripMenuItem? _minimizeActionMenuItem;
        private ThemeService? _themeService;

        public App()
        {
            // Explicit constructor prevents hard crash in SingleFile/Release mode
            LogService.Info("App constructor called.");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            LogService.Info("Application identifying startup sequence...");

            // Handle session ending (logout, shutdown, etc.)
            SessionEnding += App_SessionEnding;

            // Global Exception Handling
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                LogService.Error($"AppDomain Unhandled Exception", args.ExceptionObject as Exception ?? new Exception("Unknown"));
            };

            DispatcherUnhandledException += (s, args) =>
            {
                LogService.Error($"Dispatcher Unhandled Exception", args.Exception);
                // args.Handled = true; // Optional: prevents crash, but maybe we want crash?
            };

            TaskScheduler.UnobservedTaskException += (s, args) =>
            {
                LogService.Error($"TaskScheduler Unobserved Exception", args.Exception);
            };

            try 
            {
                LogService.Info("Initializing MainWindow...");
                // Create main window but don't show it yet
                _mainWindow = new MainWindow();
                LogService.Info("MainWindow initialized.");

                // Create system tray icon
                _notifyIcon = new NotifyIcon
                {
                    Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath),
                    Visible = true,
                    Text = "Wheel Overlay"
                };

                // Create context menu
                var contextMenu = new ContextMenuStrip();
                contextMenu.Items.Add("Show Overlay", null, (s, args) => ShowOverlay());
                contextMenu.Items.Add("Hide Overlay", null, (s, args) => HideOverlay());
                contextMenu.Items.Add("-");
                
                // Add "Minimize" menu item (visible only when MinimizeToTaskbar setting is enabled)
                var settings = AppSettings.Load();

                // Initialize theme service with persisted preference
                _themeService = new ThemeService(settings.ThemePreference);
                _themeService.ApplyTheme(_themeService.IsDarkMode);
                _themeService.StartWatching();
                _themeService.ThemeChanged += OnThemeChanged;
                LogService.Info($"ThemeService initialized (preference={settings.ThemePreference}, dark={_themeService.IsDarkMode})");

                // Set initial tray icon to match current theme
                UpdateTrayIcon(_themeService.IsDarkMode);

                // Set initial window icons to match current theme
                UpdateWindowIcons(_themeService.IsDarkMode);

                _minimizeActionMenuItem = new ToolStripMenuItem("Minimize");
                _minimizeActionMenuItem.Click += (s, args) => MinimizeToTaskbar();
                _minimizeActionMenuItem.Visible = settings.MinimizeToTaskbar;
                contextMenu.Items.Add(_minimizeActionMenuItem);
                
                _minimizeMenuItem = new ToolStripMenuItem("Minimize to Taskbar");
                _minimizeMenuItem.CheckOnClick = true;
                _minimizeMenuItem.Click += (s, args) => ToggleMinimize(_minimizeMenuItem.Checked);
                contextMenu.Items.Add(_minimizeMenuItem);
                
                _configModeMenuItem = new ToolStripMenuItem("Move Overlay...");
                _configModeMenuItem.CheckOnClick = true;
                _configModeMenuItem.Click += (s, args) => _mainWindow?.ToggleOverlayMode();
                contextMenu.Items.Add(_configModeMenuItem);
                contextMenu.Items.Add("-");
                contextMenu.Items.Add("Settings...", null, (s, args) => OpenSettings());
                contextMenu.Items.Add("-");
                contextMenu.Items.Add("Exit", null, (s, args) => ExitApplication());

                _notifyIcon.ContextMenuStrip = contextMenu;
                _notifyIcon.DoubleClick += (s, args) => ToggleOverlay();

                // Show overlay by default
                ShowOverlay();
                
                // Hook into window closing event to hide instead of close
                _mainWindow.Closing += MainWindow_Closing;
                
                LogService.Info("Startup sequence completed successfully.");
            }
            catch (Exception ex)
            {
                LogService.Error("Startup crashed!", ex);
                throw; // Rethrow to let the app die properly after logging
            }
        }

        private void ShowOverlay()
        {
            _mainWindow?.Show();
        }

        private void HideOverlay()
        {
            _mainWindow?.Hide();
        }

        private void ToggleOverlay()
        {
            if (_mainWindow?.IsVisible == true)
                HideOverlay();
            else
                ShowOverlay();
        }

        private void OpenSettings()
        {
            // Reuse existing settings window if open
            if (_settingsWindow != null)
            {
                _settingsWindow.Activate();
                return;
            }

            var viewModel = new SettingsViewModel();
            _settingsWindow = new MaterialSettingsWindow();
            _settingsWindow.RegisterCategory(new DisplaySettingsCategory(viewModel));
            _settingsWindow.RegisterCategory(new AppearanceSettingsCategory());
            _settingsWindow.RegisterCategory(new AdvancedSettingsCategory());
            _settingsWindow.RegisterCategory(new AboutSettingsCategory(_themeService));

            // Set window icon to match current theme
            if (_themeService != null)
            {
                try
                {
                    var iconFileName = _themeService.IsDarkMode ? "tray_icon_light.ico" : "tray_icon_dark.ico";
                    var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, iconFileName);
                    if (System.IO.File.Exists(iconPath))
                        _settingsWindow.Icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri(iconPath, UriKind.Absolute));
                }
                catch (Exception ex)
                {
                    LogService.Error("Failed to set settings window icon", ex);
                }
            }

            _settingsWindow.SettingsApplied += (s, e) =>
            {
                var settings = AppSettings.Load();

                if (_mainWindow != null)
                    _mainWindow.ApplySettings(settings);

                UpdateMinimizeMenuItemVisibility();

                if (_themeService != null)
                    _themeService.Preference = settings.ThemePreference;
            };
            _settingsWindow.Closed += (s, e) => _settingsWindow = null;
            _settingsWindow.Show();
        }

        /// <summary>
        /// Updates the visibility of the "Minimize" menu item based on the MinimizeToTaskbar setting.
        /// </summary>
        private void UpdateMinimizeMenuItemVisibility()
        {
            if (_minimizeActionMenuItem != null)
            {
                var settings = AppSettings.Load();
                _minimizeActionMenuItem.Visible = settings.MinimizeToTaskbar;
            }
        }

        private void MinimizeToTaskbar()
        {
            if (_mainWindow != null)
            {
                _mainWindow.WindowState = WindowState.Minimized;
            }
        }

        private void ToggleMinimize(bool enabled)
        {
            if (_mainWindow != null)
            {
                if (enabled)
                {
                    _mainWindow.WindowState = WindowState.Minimized;
                }
                else
                {
                    _mainWindow.WindowState = WindowState.Normal;
                }
            }
        }

        private void ToggleConfigMode(bool enabled)
        {
            if (_mainWindow != null)
            {
                _mainWindow.ConfigMode = enabled;
            }
        }

        public void ClearConfigModeCheckmark()
        {
            if (_configModeMenuItem != null)
            {
                _configModeMenuItem.Checked = false;
            }
        }

        public void SetConfigModeCheckmark()
        {
            if (_configModeMenuItem != null)
            {
                _configModeMenuItem.Checked = true;
            }
        }

        public void ClearMinimizeCheckmark()
        {
            if (_minimizeMenuItem != null)
            {
                _minimizeMenuItem.Checked = false;
            }
        }

        public void ExitApplication()
        {
            LogService.Info("Exit requested");
            
            // Close context menu immediately to prevent it from blocking
            if (_notifyIcon?.ContextMenuStrip != null)
            {
                _notifyIcon.ContextMenuStrip.Close();
            }
            
            // Use BeginInvoke to defer shutdown until after the menu click event completes
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    LogService.Info("Beginning application shutdown");
                    Shutdown();
                }
                catch (Exception ex)
                {
                    LogService.Error("Error during shutdown", ex);
                    // Force exit if normal shutdown fails
                    Environment.Exit(0);
                }
            }), System.Windows.Threading.DispatcherPriority.Normal);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            LogService.Info("OnExit called");
            CleanupResources();
            base.OnExit(e);
        }

        private void App_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            LogService.Info($"Session ending: {e.ReasonSessionEnding}");
            CleanupResources();
        }

        private void OnThemeChanged(object? sender, bool isDarkMode)
        {
            LogService.Info($"Theme changed: dark={isDarkMode}");

            // Swap system tray icon to match the active theme
            UpdateTrayIcon(isDarkMode);

            // Swap window icons (title bar, taskbar, Alt+Tab) to match theme
            UpdateWindowIcons(isDarkMode);

            // DynamicResource bindings in open windows update automatically
            // when ThemeService.ApplyTheme swaps the resource dictionary.
            // Invalidate visual state on open windows to ensure any non-dynamic
            // elements refresh immediately.
            _settingsWindow?.InvalidateVisual();
        }

        private void UpdateTrayIcon(bool isDarkMode)
        {
            if (_notifyIcon == null) return;

            try
            {
                // Dark theme ? use light icon (for visibility on dark taskbar)
                // Light theme ? use dark icon (for visibility on light taskbar)
                var iconFileName = isDarkMode ? "tray_icon_light.ico" : "tray_icon_dark.ico";
                var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, iconFileName);

                if (System.IO.File.Exists(iconPath))
                {
                    var oldIcon = _notifyIcon.Icon;
                    _notifyIcon.Icon = new System.Drawing.Icon(iconPath);
                    oldIcon?.Dispose();
                    LogService.Info($"Tray icon updated to {iconFileName}");
                }
                else
                {
                    LogService.Info($"Tray icon file not found: {iconPath}");
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Failed to update tray icon", ex);
            }
        }

        private void UpdateWindowIcons(bool isDarkMode)
        {
            try
            {
                // Use the same light/dark ico files as the tray icon for window title bar,
                // taskbar, and Alt+Tab icons. Dark theme ? light icon, Light theme ? dark icon.
                var iconFileName = isDarkMode ? "tray_icon_light.ico" : "tray_icon_dark.ico";
                var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, iconFileName);

                if (!System.IO.File.Exists(iconPath))
                {
                    LogService.Info($"Window icon file not found: {iconPath}");
                    return;
                }

                var iconUri = new Uri(iconPath, UriKind.Absolute);
                var bitmapFrame = System.Windows.Media.Imaging.BitmapFrame.Create(iconUri);

                if (_mainWindow != null) _mainWindow.Icon = bitmapFrame;
                if (_settingsWindow != null) _settingsWindow.Icon = bitmapFrame;

                LogService.Info($"Window icons updated to {iconFileName}");
            }
            catch (Exception ex)
            {
                LogService.Error("Failed to update window icons", ex);
            }
        }

        private void CleanupResources()
        {
            LogService.Info("Cleaning up resources");
            
            // Close any open child windows first
            try
            {
                if (_settingsWindow != null)
                {
                    LogService.Info("Closing Settings window");
                    _settingsWindow.Close();
                    _settingsWindow = null;
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error closing child windows", ex);
            }

            // Close main window
            try
            {
                if (_mainWindow != null)
                {
                    LogService.Info("Closing main window");
                    // Remove the Closing event handler to prevent it from canceling the close
                    _mainWindow.Closing -= MainWindow_Closing;
                    _mainWindow.Close();
                    _mainWindow = null;
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error closing main window", ex);
            }
            
            // Dispose theme service
            try
            {
                if (_themeService != null)
                {
                    LogService.Info("Disposing ThemeService");
                    _themeService.ThemeChanged -= OnThemeChanged;
                    _themeService.Dispose();
                    _themeService = null;
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error disposing ThemeService", ex);
            }

            // Then cleanup notify icon
            CleanupNotifyIcon();
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // When user closes window from taskbar, exit the application
            LogService.Info("MainWindow closing requested - initiating app shutdown");
            
            // Close any open child windows first
            try
            {
                if (_settingsWindow != null)
                {
                    LogService.Info("Closing Settings window before shutdown");
                    _settingsWindow.Close();
                    _settingsWindow = null;
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Error closing child windows", ex);
            }
            
            // Cancel the close to prevent immediate window destruction
            e.Cancel = true;
            
            // Trigger app shutdown through the App class
            ((App)System.Windows.Application.Current).ExitApplication();
        }

        private void CleanupNotifyIcon()
        {
            if (_notifyIcon != null)
            {
                try
                {
                    LogService.Info("Cleaning up NotifyIcon");
                    
                    // Remove event handlers to prevent callbacks during disposal
                    _notifyIcon.DoubleClick -= (s, args) => ToggleOverlay();
                    
                    // Hide icon first to ensure it's removed from system tray
                    _notifyIcon.Visible = false;
                    
                    // Dispose of context menu separately with a small delay
                    var contextMenu = _notifyIcon.ContextMenuStrip;
                    _notifyIcon.ContextMenuStrip = null;
                    
                    // Dispose of the icon itself
                    _notifyIcon.Dispose();
                    _notifyIcon = null;
                    
                    // Dispose context menu after icon is disposed
                    contextMenu?.Dispose();
                    
                    LogService.Info("NotifyIcon cleanup complete");
                }
                catch (Exception ex)
                {
                    LogService.Error("Error cleaning up NotifyIcon", ex);
                }
            }
        }
    }
}
