using System.Globalization;

namespace FocusFlow.App.Converters
{
    public class NullToTitleConverter : BaseConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? "Connect Account" : "Edit Account";
        }
    }
}

