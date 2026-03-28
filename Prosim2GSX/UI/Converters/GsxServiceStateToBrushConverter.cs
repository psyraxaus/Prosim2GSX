using Prosim2GSX.GSX.Services;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Prosim2GSX.UI.Converters
{
    public class GsxServiceStateToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush BrushGray = new(Color.FromArgb(0xFF, 0xD3, 0xD3, 0xD3));
        private static readonly SolidColorBrush BrushBlue = new(Color.FromArgb(0xFF, 0x1E, 0x90, 0xFF));
        private static readonly SolidColorBrush BrushGold = new(Color.FromArgb(0xFF, 0xFF, 0xD7, 0x00));
        private static readonly SolidColorBrush BrushGreen = new(Color.FromArgb(0xFF, 0x00, 0xA0, 0x00));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is GsxServiceState state)
            {
                return state switch
                {
                    GsxServiceState.Requested => BrushBlue,
                    GsxServiceState.Active => BrushGold,
                    GsxServiceState.Completed => BrushGreen,
                    _ => BrushGray
                };
            }
            return BrushGray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
