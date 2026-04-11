using ProsimInterface;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Prosim2GSX.UI.Converters
{
    /// <summary>
    /// Multi-value converter for Pushback single-indicator display with phase awareness.
    /// Bindings: [0] = ServicePushback (string "{State} ({PushStatus})"), [1] = AutomationState.
    /// Grey when phase is not relevant. Blue = Requested, Amber = Active, Green = Completed.
    /// </summary>
    public class SolariPushbackBrushConverter : IMultiValueConverter
    {
        private static readonly SolidColorBrush BrushGray = new(Color.FromArgb(0xFF, 0xD3, 0xD3, 0xD3));
        private static readonly SolidColorBrush BrushBlue = new(Color.FromArgb(0xFF, 0x1E, 0x90, 0xFF));
        private static readonly SolidColorBrush BrushAmber = new(Color.FromArgb(0xFF, 0xFF, 0xD7, 0x00));
        private static readonly SolidColorBrush BrushGreen = new(Color.FromArgb(0xFF, 0x00, 0xA0, 0x00));

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] is not string state)
                return BrushGray;

            // Check phase relevance — pushback only relevant during Departure and PushBack phases
            if (values[1] is AutomationState phase)
            {
                bool relevant = phase == AutomationState.Departure
                             || phase == AutomationState.PushBack;

                if (!relevant)
                    return BrushGray;
            }

            if (state.StartsWith("Active", StringComparison.Ordinal))
                return BrushAmber;

            if (state.StartsWith("Requested", StringComparison.Ordinal))
                return BrushBlue;

            if (state.StartsWith("Completed", StringComparison.Ordinal))
                return BrushGreen;

            return BrushGray;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
