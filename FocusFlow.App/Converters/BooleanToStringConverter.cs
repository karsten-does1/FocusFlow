using System.Globalization;
using FocusFlow.App.Converters;

namespace FocusFlow.App
{
    public class BooleanToStringConverter : BaseConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not bool boolValue)
            {
                return string.Empty;
            }

            if (parameter is not string paramString)
            {
                return string.Empty;
            }

            var parts = paramString.Split('|');
            if (parts.Length < 2)
            {
                return string.Empty;
            }

            return boolValue ? parts[0] : parts[1];
        }
    }
}

