using System.Globalization;
using System.Windows;
using FocusFlow.App.Converters;

namespace FocusFlow.App
{
    public class NullToInverseVisibilityConverter : BaseConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}

