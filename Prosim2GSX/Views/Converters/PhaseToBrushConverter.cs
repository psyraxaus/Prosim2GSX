using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Prosim2GSX.Views.Converters
{
    /// <summary>
    /// Converts phase index to the appropriate brush color
    /// </summary>
    public class PhaseToBrushConverter : IValueConverter
    {
        /// <summary>
        /// Converts a phase index to the appropriate brush color
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isActive = (bool)value;

            if (isActive)
            {
                return new SolidColorBrush(Colors.DodgerBlue);
            }

            return new SolidColorBrush(Colors.LightGray);
        }


        /// <summary>
        /// Not implemented for this converter (one-way only)
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
