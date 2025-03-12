using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Prosim2GSX.UI.EFB.Converters
{
    /// <summary>
    /// Converts a boolean value to a CornerRadius value.
    /// </summary>
    public class BooleanToCornerRadiusConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean value to a CornerRadius value.
        /// </summary>
        /// <param name="value">The boolean value to convert.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter. Format: "trueCornerRadius|falseCornerRadius" where each cornerRadius is either a single value or a comma-separated list of 4 values.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A CornerRadius value based on the boolean value.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool boolValue))
                return DependencyProperty.UnsetValue;

            if (!(parameter is string paramString))
                return DependencyProperty.UnsetValue;

            string[] parts = paramString.Split('|');
            if (parts.Length != 2)
                return DependencyProperty.UnsetValue;

            string cornerRadiusString = boolValue ? parts[0] : parts[1];
            
            return ParseCornerRadius(cornerRadiusString);
        }

        /// <summary>
        /// Converts a CornerRadius value back to a boolean value.
        /// </summary>
        /// <param name="value">The CornerRadius value to convert back.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A boolean value based on the CornerRadius value.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // This converter does not support converting back
            return DependencyProperty.UnsetValue;
        }

        /// <summary>
        /// Parses a string into a CornerRadius value.
        /// </summary>
        /// <param name="cornerRadiusString">The string to parse. Can be a single value or a comma-separated list of 4 values.</param>
        /// <returns>A CornerRadius value.</returns>
        private CornerRadius ParseCornerRadius(string cornerRadiusString)
        {
            string[] parts = cornerRadiusString.Split(',');
            
            if (parts.Length == 1)
            {
                if (double.TryParse(parts[0], out double radius))
                {
                    return new CornerRadius(radius);
                }
            }
            else if (parts.Length == 4)
            {
                if (double.TryParse(parts[0], out double topLeft) &&
                    double.TryParse(parts[1], out double topRight) &&
                    double.TryParse(parts[2], out double bottomRight) &&
                    double.TryParse(parts[3], out double bottomLeft))
                {
                    return new CornerRadius(topLeft, topRight, bottomRight, bottomLeft);
                }
            }
            
            return new CornerRadius(0);
        }
    }
}
