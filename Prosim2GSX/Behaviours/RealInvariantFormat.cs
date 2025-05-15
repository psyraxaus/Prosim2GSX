using System;
using System.Globalization;

namespace Prosim2GSX.Behaviours
{
    /// <summary>
    /// Provides culture-invariant formatting for real numbers.
    /// This class detects the decimal separator used in a string and provides 
    /// the appropriate formatting to parse it correctly.
    /// </summary>
    public class RealInvariantFormat : IFormatProvider
    {
        /// <summary>
        /// Number format information that will be used for parsing and formatting.
        /// </summary>
        private NumberFormatInfo _formatInfo = CultureInfo.InvariantCulture.NumberFormat;

        /// <summary>
        /// Gets the number format information.
        /// </summary>
        public NumberFormatInfo FormatInfo => _formatInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="RealInvariantFormat"/> class.
        /// </summary>
        /// <param name="value">The string value to determine the appropriate number format from.
        /// The method will detect whether periods or commas are used as decimal separators.</param>
        public RealInvariantFormat(string value)
        {
            if (value == null)
            {
                // Default to US format (period as decimal separator) if no value is provided
                _formatInfo = new CultureInfo("en-US").NumberFormat;
                return;
            }

            // Detect the decimal separator by looking at the last occurrence of period and comma
            int lastPoint = value.LastIndexOf('.');
            int lastComma = value.LastIndexOf(',');

            if (lastComma > lastPoint)
            {
                // If the last comma appears after the last period, assume comma is the decimal separator (e.g., German format)
                _formatInfo = new CultureInfo("de-DE").NumberFormat;
            }
            else
            {
                // Otherwise assume period is the decimal separator (e.g., US format)
                _formatInfo = new CultureInfo("en-US").NumberFormat;
            }
        }

        /// <summary>
        /// Returns an object that provides formatting services for the specified type.
        /// </summary>
        /// <param name="formatType">The type of format object to return.</param>
        /// <returns>
        /// The <see cref="NumberFormatInfo"/> object if <paramref name="formatType"/> is <see cref="NumberFormatInfo"/>;
        /// otherwise, <c>null</c>.
        /// </returns>
        public object GetFormat(Type formatType)
        {
            if (formatType == typeof(NumberFormatInfo))
            {
                return _formatInfo;
            }
            else
                return null;
        }
    }
}
