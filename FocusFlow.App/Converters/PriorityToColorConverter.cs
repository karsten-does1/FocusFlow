using System;
using System.Globalization;
using System.Windows.Media;

using MediaBrush = System.Windows.Media.Brush;
using MediaBrushes = System.Windows.Media.Brushes;

namespace FocusFlow.App.Converters
{
    public sealed class PriorityToColorConverter : BaseConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not int priorityScore)
            {
                return MediaBrushes.Gray;
            }

            MediaBrush brush = priorityScore switch
            {
                >= 80 => MediaBrushes.Red,
                >= 50 => MediaBrushes.OrangeRed,
                > 0 => MediaBrushes.DarkGoldenrod,
                _ => MediaBrushes.Gray
            };

            return brush;
        }
    }
}
