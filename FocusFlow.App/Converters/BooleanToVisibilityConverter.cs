using System.Globalization;
using System.Windows;

namespace FocusFlow.App.Converters
{
    public class BooleanToVisibilityConverter : BaseConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }
    }
}
