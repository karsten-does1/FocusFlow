using System;
using System.Globalization;
using FocusFlow.App.Converters;

namespace FocusFlow.App
{
    public class NullToTitleConverter : BaseConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // If value is null, return "Connect Account", otherwise "Edit Account"
            return value == null ? "Connect Account" : "Edit Account";
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

