using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Prosim2GSX.UI.EFB.Converters
{
    /// <summary>
    /// Converts a RotateTransform to its inverse.
    /// </summary>
    public class InverseRotateTransformConverter : IValueConverter
    {
        /// <summary>
        /// Converts a RotateTransform to its inverse.
        /// </summary>
        /// <param name="value">The RotateTransform to convert.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A RotateTransform with the inverse angle.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is RotateTransform rotateTransform)
            {
                return new RotateTransform(-rotateTransform.Angle);
            }
            
            return new RotateTransform(0);
        }

        /// <summary>
        /// Converts a RotateTransform back to its original.
        /// </summary>
        /// <param name="value">The RotateTransform to convert back.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A RotateTransform with the original angle.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is RotateTransform rotateTransform)
            {
                return new RotateTransform(-rotateTransform.Angle);
            }
            
            return new RotateTransform(0);
        }
    }
}
