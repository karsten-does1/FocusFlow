using System;
using System.Globalization;
using System.Windows;

namespace FocusFlow.App.Converters
{
    public class ZeroToVisibilityConverter : BaseConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Visibility.Collapsed;
            }

            if (value is int intValue)
            {
                return intValue == 0 ? Visibility.Collapsed : Visibility.Visible;
            }

            if (value is double doubleValue)
            {
                return Math.Abs(doubleValue) < double.Epsilon ? Visibility.Collapsed : Visibility.Visible;
            }

            return Visibility.Collapsed;
        }
    }
}