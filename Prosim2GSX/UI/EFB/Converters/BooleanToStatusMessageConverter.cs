using System;
using System.Globalization;
using System.Windows.Data;

namespace Prosim2GSX.UI.EFB.Converters
{
    /// <summary>
    /// Converts a boolean value to a status message.
    /// </summary>
    public class BooleanToStatusMessageConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets a value indicating whether to invert the conversion.
        /// </summary>
        public bool Invert { get; set; } = false;
        
        /// <summary>
        /// Gets or sets the message to display when the value is true.
        /// </summary>
        public string TrueMessage { get; set; } = "Connected";
        
        /// <summary>
        /// Gets or sets the message to display when the value is false.
        /// </summary>
        public string FalseMessage { get; set; } = "Disconnected";
        
        /// <summary>
        /// Gets or sets the message to display when the value is null.
        /// </summary>
        public string NullMessage { get; set; } = "Unknown";
        
        /// <summary>
        /// Converts a boolean value to a status message.
        /// </summary>
        /// <param name="value">The boolean value to convert.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A status message based on the boolean value.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return NullMessage;
            }
            
            if (value is bool boolValue)
            {
                if (Invert)
                {
                    boolValue = !boolValue;
                }
                
                return boolValue ? TrueMessage : FalseMessage;
            }
            
            return NullMessage;
        }
        
        /// <summary>
        /// Converts a status message back to a boolean value.
        /// </summary>
        /// <param name="value">The status message to convert back.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A boolean value based on the status message.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                bool result = stringValue == TrueMessage;
                
                if (Invert)
                {
                    result = !result;
                }
                
                return result;
            }
            
            return false;
        }
    }
}
