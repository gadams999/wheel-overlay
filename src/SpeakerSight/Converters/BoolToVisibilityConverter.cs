using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OpenDash.SpeakerSight.Converters;

/// <summary>
/// Converts bool to Visibility: true → Visible, false → Hidden.
/// Uses Hidden (not Collapsed) to preserve column width in the avatar layout (FR-014b-layout).
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Hidden;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Visible;
}
