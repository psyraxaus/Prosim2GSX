using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Prosim2GSX.Converters
{
    /// <summary>
    /// Converts a boolean value to Visibility
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean value to Visibility
        /// </summary>
        /// <param name="value">The boolean value to convert</param>
        /// <param name="targetType">The target type (unused)</param>
        /// <param name="parameter">Optional parameter to invert the conversion</param>
        /// <param name="culture">Culture info (unused)</param>
        /// <returns>Visibility.Visible if true, Visibility.Collapsed if false (or reversed if parameter is specified)</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                bool invert = parameter != null && bool.TryParse(parameter.ToString(), out bool invertParam) && invertParam;

                if (invert)
                {
                    return !boolValue ? Visibility.Visible : Visibility.Collapsed;
                }

                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        /// <summary>
        /// Converts back from Visibility to boolean
        /// </summary>
        /// <param name="value">The Visibility value to convert</param>
        /// <param name="targetType">The target type (unused)</param>
        /// <param name="parameter">Optional parameter to invert the conversion</param>
        /// <param name="culture">Culture info (unused)</param>
        /// <returns>True if Visible, False otherwise (or reversed if parameter is specified)</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                bool invert = parameter != null && bool.TryParse(parameter.ToString(), out bool invertParam) && invertParam;
                bool result = visibility == Visibility.Visible;

                return invert ? !result : result;
            }

            return false;
        }
    }
}
