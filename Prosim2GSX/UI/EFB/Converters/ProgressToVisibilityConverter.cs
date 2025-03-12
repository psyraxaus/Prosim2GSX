using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Prosim2GSX.UI.EFB.Converters
{
    /// <summary>
    /// Converts a progress value to a Visibility value.
    /// </summary>
    public class ProgressToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets a value indicating whether to invert the conversion.
        /// </summary>
        public bool Invert { get; set; } = false;
        
        /// <summary>
        /// Gets or sets a value indicating whether to use Hidden instead of Collapsed.
        /// </summary>
        public bool UseHidden { get; set; } = false;
        
        /// <summary>
        /// Gets or sets the threshold value.
        /// </summary>
        public double Threshold { get; set; } = 0.01;
        
        /// <summary>
        /// Converts a progress value to a Visibility value.
        /// </summary>
        /// <param name="value">The progress value to convert.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A Visibility value based on the progress value.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double progress = 0;
            
            if (value is double doubleValue)
            {
                progress = doubleValue;
            }
            else if (value is int intValue)
            {
                progress = intValue;
            }
            else if (value is float floatValue)
            {
                progress = floatValue;
            }
            else if (value != null)
            {
                progress = System.Convert.ToDouble(value);
            }
            
            bool isVisible = progress > Threshold && progress < 1.0;
            
            if (Invert)
            {
                isVisible = !isVisible;
            }
            
            return isVisible ? Visibility.Visible : (UseHidden ? Visibility.Hidden : Visibility.Collapsed);
        }
        
        /// <summary>
        /// Converts a Visibility value back to a progress value.
        /// </summary>
        /// <param name="value">The Visibility value to convert back.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A progress value based on the Visibility value.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                bool isVisible = visibility == Visibility.Visible;
                
                if (Invert)
                {
                    isVisible = !isVisible;
                }
                
                return isVisible ? 0.5 : 0.0;
            }
            
            return 0.0;
        }
    }
}
