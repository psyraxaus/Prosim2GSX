using Prosim2GSX.GSX.Services;
using ProsimInterface;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Prosim2GSX.UI.Converters
{
    /// <summary>
    /// Multi-value converter for Jetway/Stairs Solari indicators.
    /// Bindings: [0] = GsxServiceState, [1] = SolariToggle (bool), [2] = AutomationState.
    /// ConverterParameter: "left" or "right" to select which dot.
    /// Grey when phase is not relevant (TaxiOut, Flight, TaxiIn, PushBack).
    /// Requested (operating/moving) = amber alternating, Active (connected) = green, other = red.
    /// </summary>
    public class SolariJetwayBrushConverter : IMultiValueConverter
    {
        private static readonly SolidColorBrush BrushGray = new(Color.FromArgb(0xFF, 0xD3, 0xD3, 0xD3));
        private static readonly SolidColorBrush BrushRed = new(Color.FromArgb(0xFF, 0xDC, 0x14, 0x3C));
        private static readonly SolidColorBrush BrushGreen = new(Color.FromArgb(0xFF, 0x00, 0xA0, 0x00));
        private static readonly SolidColorBrush BrushAmberBright = new(Color.FromArgb(0xFF, 0xFF, 0xD7, 0x00));
        private static readonly SolidColorBrush BrushAmberDim = new(Color.FromArgb(0xFF, 0x8B, 0x69, 0x14));

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3 || values[0] is not GsxServiceState state)
                return BrushGray;

            // Check phase relevance — only show active colors during ground phases
            if (values[2] is AutomationState phase)
            {
                bool relevant = phase == AutomationState.Preparation
                             || phase == AutomationState.Departure
                             || phase == AutomationState.Arrival
                             || phase == AutomationState.TurnAround;

                if (!relevant)
                    return BrushGray;
            }

            bool toggle = values[1] is bool t && t;
            bool isLeft = string.Equals(parameter as string, "left", StringComparison.OrdinalIgnoreCase);

            if (state == GsxServiceState.Requested)
            {
                // Operating/moving — Solari alternating amber
                bool bright = isLeft ? toggle : !toggle;
                return bright ? BrushAmberBright : BrushAmberDim;
            }

            if (state == GsxServiceState.Active)
            {
                // Connected/in place — solid green
                return BrushGreen;
            }

            // All other states in relevant phases — red
            return BrushRed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
