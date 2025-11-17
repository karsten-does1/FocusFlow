using System.Globalization;
using System.Windows;
using FocusFlow.App.Converters;

namespace FocusFlow.App
{
    public class BooleanToTextDecorationConverter : BaseConverter
    {
        public override object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isDone && isDone)
            {
                return TextDecorations.Strikethrough;
            }
            return null;
        }
    }
}

