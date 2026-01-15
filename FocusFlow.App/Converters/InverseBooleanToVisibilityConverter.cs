using System.Globalization;
using System.Windows;

namespace FocusFlow.App.Converters
{
    public class InverseBooleanToVisibilityConverter : BaseConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isVisible)
            {
                return isVisible ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }
    }
}

