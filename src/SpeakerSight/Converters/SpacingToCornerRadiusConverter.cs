using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OpenDash.SpeakerSight.Converters;

public class SpacingToCornerRadiusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double spacing = value switch { int i => i, double d => d, _ => 0.0 };
        double radius = spacing > 0 ? 4.0 : 0.0;
        return new CornerRadius(radius);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
