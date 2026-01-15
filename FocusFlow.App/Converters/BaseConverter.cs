using System;
using System.Globalization;
using System.Windows.Data;

namespace FocusFlow.App.Converters
{
    public abstract class BaseConverter : IValueConverter
    {
        public abstract object Convert(object value, Type targetType, object parameter, CultureInfo culture);

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException("This converter only supports one-way conversion.");
    }
}

