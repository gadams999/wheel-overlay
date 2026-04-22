using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OpenDash.SpeakerSight.Converters;

public class SpacingToThicknessConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double spacing = value switch { int i => i, double d => d, _ => 0.0 };
        return new Thickness(0, 0, 0, spacing);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
