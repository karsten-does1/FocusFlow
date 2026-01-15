using System.Globalization;
using System.Windows.Media;

namespace FocusFlow.App.Converters
{
    public class PriorityToColorConverter : BaseConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not int priorityScore)
            {
                return Brushes.Gray;
            }

            if (priorityScore >= 80)
            {
                return Brushes.Red;
            }

            if (priorityScore >= 50)
            {
                return Brushes.OrangeRed;
            }

            if (priorityScore > 0)
            {
                return Brushes.DarkGoldenrod;
            }

            return Brushes.Gray;
        }
    }
}