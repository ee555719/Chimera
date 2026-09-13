// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Chimera.Host.Converters;

public class BooleanToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isUserMessage)
        {
            return isUserMessage ? 
                new SolidColorBrush(Color.FromRgb(0, 120, 212)) : // Blue for user
                new SolidColorBrush(Color.FromRgb(240, 240, 240)); // Light gray for assistant
        }
        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
