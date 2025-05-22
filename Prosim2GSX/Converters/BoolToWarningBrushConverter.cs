using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Prosim2GSX.Converters
{
    /// <summary>
    /// Converts a boolean value to a brush for warning/error indication
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Brush))]
    public class BoolToWarningBrushConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean value to a brush
        /// </summary>
        /// <param name="value">The boolean value to convert</param>
        /// <param name="targetType">The type of the binding target property</param>
        /// <param name="parameter">Optional parameter to customize behavior</param>
        /// <param name="culture">The culture to use in the converter</param>
        /// <returns>A brush based on the boolean value</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool))
                return new SolidColorBrush(Colors.Gray);

            bool isWarning = (bool)value;

            if (isWarning)
            {
                // Return a warning color (orange-red) for true values
                return new SolidColorBrush(Colors.OrangeRed);
            }
            else
            {
                // Return the default color (gray) for false values
                return new SolidColorBrush(Colors.Gray);
            }
        }

        /// <summary>
        /// Converts a brush back to a boolean value (not implemented)
        /// </summary>
        /// <param name="value">The brush to convert back</param>
        /// <param name="targetType">The type of the binding target property</param>
        /// <param name="parameter">Optional parameter to customize behavior</param>
        /// <param name="culture">The culture to use in the converter</param>
        /// <returns>A boolean based on the brush (not implemented)</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // This converter doesn't support two-way binding
            return Binding.DoNothing;
        }
    }
}
