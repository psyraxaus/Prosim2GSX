using Prosim2GSX.GSX.Services;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Prosim2GSX.UI.Converters
{
    /// <summary>
    /// Multi-value converter for the Solari board effect on GSX service indicators.
    /// Bindings: [0] = GsxServiceState, [1] = SolariToggle (bool).
    /// ConverterParameter: "left" or "right" to select which dot.
    /// When Active, the two dots alternate between bright amber and dim amber.
    /// </summary>
    public class SolariServiceBrushConverter : IMultiValueConverter
    {
        private static readonly SolidColorBrush BrushGray = new(Color.FromArgb(0xFF, 0xD3, 0xD3, 0xD3));
        private static readonly SolidColorBrush BrushBlue = new(Color.FromArgb(0xFF, 0x1E, 0x90, 0xFF));
        private static readonly SolidColorBrush BrushGreen = new(Color.FromArgb(0xFF, 0x00, 0xA0, 0x00));
        private static readonly SolidColorBrush BrushAmberBright = new(Color.FromArgb(0xFF, 0xFF, 0xD7, 0x00));
        private static readonly SolidColorBrush BrushAmberDim = new(Color.FromArgb(0xFF, 0x8B, 0x69, 0x14));

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] is not GsxServiceState state)
                return BrushGray;

            bool toggle = values[1] is bool t && t;
            bool isLeft = string.Equals(parameter as string, "left", StringComparison.OrdinalIgnoreCase);

            if (state == GsxServiceState.Active)
            {
                // Solari alternating: left is bright when toggle=true, right is bright when toggle=false
                bool bright = isLeft ? toggle : !toggle;
                return bright ? BrushAmberBright : BrushAmberDim;
            }

            return state switch
            {
                GsxServiceState.Requested => BrushBlue,
                GsxServiceState.Completed => BrushGreen,
                _ => BrushGray
            };
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
