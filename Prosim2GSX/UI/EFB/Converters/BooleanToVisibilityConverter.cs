using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Prosim2GSX.UI.EFB.Converters
{
    /// <summary>
    /// Converts a boolean value to a Visibility value.
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets a value indicating whether to invert the conversion.
        /// If true, true will convert to Collapsed and false will convert to Visible.
        /// If false (default), true will convert to Visible and false will convert to Collapsed.
        /// </summary>
        public bool Invert { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to use Hidden instead of Collapsed.
        /// If true, false will convert to Hidden instead of Collapsed.
        /// If false (default), false will convert to Collapsed.
        /// </summary>
        public bool UseHidden { get; set; } = false;

        /// <summary>
        /// Converts a boolean value to a Visibility value.
        /// </summary>
        /// <param name="value">The boolean value to convert.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter. If "Invert", the conversion will be inverted.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A Visibility value based on the boolean value.</returns>
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
            
            if (invert)
            {
                boolValue = !boolValue;
            }
            
            return boolValue ? Visibility.Visible : (UseHidden ? Visibility.Hidden : Visibility.Collapsed);
        }

        /// <summary>
        /// Converts a Visibility value back to a boolean value.
        /// </summary>
        /// <param name="value">The Visibility value to convert back.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter. If "Invert", the conversion will be inverted.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A boolean value based on the Visibility value.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool invert = Invert;
            
            // Check if the parameter is "Invert"
            if (parameter is string paramString && paramString.Equals("Invert", StringComparison.OrdinalIgnoreCase))
            {
                invert = !invert;
            }
            
            bool result = false;
            
            if (value is Visibility visibility)
            {
                result = visibility == Visibility.Visible;
            }
            
            if (invert)
            {
                result = !result;
            }
            
            return result;
        }
    }
}
