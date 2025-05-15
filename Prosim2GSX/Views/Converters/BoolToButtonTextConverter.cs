using System;
using System.Globalization;
using System.Windows.Data;

namespace Prosim2GSX.Views.Converters
{
    /// <summary>
    /// Converts a boolean value to different text strings based on true/false state
    /// </summary>
    public class BoolToButtonTextConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean value to a string
        /// </summary>
        /// <param name="value">The source boolean value</param>
        /// <param name="targetType">The target type (not used)</param>
        /// <param name="parameter">Format: "Text if false|Text if true"</param>
        /// <param name="culture">The culture (not used)</param>
        /// <returns>The appropriate text string based on the boolean value</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = (bool)value;
            string param = parameter as string;

            if (string.IsNullOrEmpty(param))
                return boolValue.ToString();

            string[] options = param.Split('|');

            if (options.Length != 2)
                return param;

            return boolValue ? options[1] : options[0];
        }

        /// <summary>
        /// Converts back from a string to a boolean value (not implemented)
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
