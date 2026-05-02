using Prosim2GSX.GSX.Services;
using ProsimInterface;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Prosim2GSX.UI.Converters
{
    /// <summary>
    /// Multi-value converter for Jetway/Stairs single-indicator display.
    /// Bindings: [0] = GsxServiceState, [1] = AutomationState.
    /// Grey when phase is not relevant (TaxiOut, Flight, TaxiIn).
    /// Requested (operating/moving) = amber, Active (connected) = green,
    /// Callable/Unknown = grey, NotAvailable/Bypassed = red.
    /// PushBack is included as relevant because the bridge/stairs are
    /// physically still attached until the departure sequence retracts them.
    /// </summary>
    public class SolariJetwayBrushConverter : IMultiValueConverter
    {
        private static readonly SolidColorBrush BrushGray = new(Color.FromArgb(0xFF, 0xD3, 0xD3, 0xD3));
        private static readonly SolidColorBrush BrushRed = new(Color.FromArgb(0xFF, 0xDC, 0x14, 0x3C));
        private static readonly SolidColorBrush BrushGreen = new(Color.FromArgb(0xFF, 0x00, 0xA0, 0x00));
        private static readonly SolidColorBrush BrushAmber = new(Color.FromArgb(0xFF, 0xFF, 0xD7, 0x00));

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] is not GsxServiceState state)
                return BrushGray;

            // Check phase relevance — only show active colors during ground phases
            if (values[1] is AutomationState phase)
            {
                bool relevant = phase == AutomationState.Preparation
                             || phase == AutomationState.Departure
                             || phase == AutomationState.PushBack
                             || phase == AutomationState.Arrival
                             || phase == AutomationState.TurnAround;

                if (!relevant)
                    return BrushGray;
            }

            return state switch
            {
                GsxServiceState.Requested => BrushAmber,
                GsxServiceState.Active => BrushGreen,
                GsxServiceState.Completed => BrushGreen,
                GsxServiceState.NotAvailable => BrushRed,
                GsxServiceState.Bypassed => BrushRed,
                _ => BrushGray
            };
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
