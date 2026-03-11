using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using OpenDash.WheelOverlay.Services;
using OpenDash.OverlayCore.Services;
using OpenDash.OverlayCore.Models;

namespace OpenDash.WheelOverlay
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        private ThemeService? _themeService;

        public AboutWindow()
        {
            InitializeComponent();
            LoadVersionInfo();
            
            // Handle Escape key to close dialog
            this.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Escape)
                {
                    Close();
                }
            };
        }

        /// <summary>
        /// Sets the ThemeService and subscribes to theme changes for icon swapping.
        /// Call this after construction, before ShowDialog.
        /// </summary>
        public void SetThemeService(ThemeService themeService)
        {
            _themeService = themeService;
            UpdateAboutIcon(_themeService.IsDarkMode);
            _themeService.ThemeChanged += OnThemeChanged;
        }

        private void OnThemeChanged(object? sender, bool isDark)
        {
            UpdateAboutIcon(isDark);
        }

        private void UpdateAboutIcon(bool isDark)
        {
            try
            {
                var fileName = isDark ? "about_icon_light.png" : "about_icon_dark.png";
                var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                if (System.IO.File.Exists(path))
                {
                    AboutIcon.Source = new BitmapImage(new Uri(path, UriKind.Absolute));
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Failed to update about icon", ex);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_themeService != null)
                _themeService.ThemeChanged -= OnThemeChanged;
            base.OnClosed(e);
        }

        private void LoadVersionInfo()
        {
            try
            {
                VersionTextBlock.Text = VersionInfo.GetFullVersionString();
            }
            catch (Exception ex)
            {
                LogService.Error("Failed to read version information", ex);
                VersionTextBlock.Text = "Version Unknown";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)
                {
                    UseShellExecute = true
                });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                LogService.Error("Failed to open GitHub link", ex);
                System.Windows.MessageBox.Show(
                    "Failed to open the GitHub repository link. Please visit:\n" + e.Uri.AbsoluteUri,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
