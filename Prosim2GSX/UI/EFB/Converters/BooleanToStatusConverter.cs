using System;
using System.Globalization;
using System.Windows.Data;
using Prosim2GSX.UI.EFB.Controls;

namespace Prosim2GSX.UI.EFB.Converters
{
    /// <summary>
    /// Converts a boolean value to a StatusIndicator.StatusType value.
    /// </summary>
    public class BooleanToStatusConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets a value indicating whether to invert the conversion.
        /// </summary>
        public bool Invert { get; set; } = false;
        
        /// <summary>
        /// Converts a boolean value to a StatusIndicator.StatusType value.
        /// </summary>
        /// <param name="value">The boolean value to convert.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A StatusIndicator.StatusType value based on the boolean value.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                if (Invert)
                {
                    boolValue = !boolValue;
                }
                
                return boolValue ? StatusIndicator.StatusType.Success : StatusIndicator.StatusType.Error;
            }
            
            return StatusIndicator.StatusType.Inactive;
        }
        
        /// <summary>
        /// Converts a StatusIndicator.StatusType value back to a boolean value.
        /// </summary>
        /// <param name="value">The StatusIndicator.StatusType value to convert back.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A boolean value based on the StatusIndicator.StatusType value.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is StatusIndicator.StatusType statusType)
            {
                bool result = statusType == StatusIndicator.StatusType.Success;
                
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
