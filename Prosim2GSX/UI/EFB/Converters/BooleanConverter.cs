using System;
using System.Globalization;
using System.Windows.Data;

namespace Prosim2GSX.UI.EFB.Converters
{
    /// <summary>
    /// Converts a boolean value to another boolean value, with optional inversion.
    /// </summary>
    public class BooleanConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets a value indicating whether to invert the conversion.
        /// If true, true will convert to false and false will convert to true.
        /// If false (default), the value will be passed through unchanged.
        /// </summary>
        public bool Invert { get; set; } = false;

        /// <summary>
        /// Converts a boolean value to another boolean value.
        /// </summary>
        /// <param name="value">The boolean value to convert.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter. If "Invert", the conversion will be inverted.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A boolean value based on the input value and inversion setting.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool invert = Invert;
            
            // Check if the parameter is "Invert"
            if (parameter is string paramString && paramString.Equals("Invert", StringComparison.OrdinalIgnoreCase))
            {
                invert = !invert;
            }
            
            bool boolValue = false;
            
            if (value is bool b)
            {
                boolValue = b;
            }
            else if (value != null)
            {
                boolValue = System.Convert.ToBoolean(value);
            }
            
            return invert ? !boolValue : boolValue;
        }

        /// <summary>
        /// Converts a boolean value back to another boolean value.
        /// </summary>
        /// <param name="value">The boolean value to convert back.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter. If "Invert", the conversion will be inverted.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A boolean value based on the input value and inversion setting.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(value, targetType, parameter, culture);
        }
    }
}
