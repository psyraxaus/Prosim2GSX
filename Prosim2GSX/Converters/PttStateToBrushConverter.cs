using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Prosim2GSX.Converters
{
    /// <summary>
    /// Multi-value converter that chooses a brush based on PTT state
    /// </summary>
    public class PttStateToBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] = IsPttActive
            // values[1] = IsChannelDisabled

            if (values.Length < 2 || !(values[0] is bool) || !(values[1] is bool))
                return new SolidColorBrush(Colors.Gray);

            bool isPttActive = (bool)values[0];
            bool isChannelDisabled = (bool)values[1];

            if (isChannelDisabled)
            {
                // Channel disabled takes precedence
                return new SolidColorBrush(Colors.OrangeRed);
            }
            else if (isPttActive)
            {
                // PTT active state
                return new SolidColorBrush(Colors.LimeGreen);
            }
            else
            {
                // Normal state
                return new SolidColorBrush(Colors.Gray);
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
