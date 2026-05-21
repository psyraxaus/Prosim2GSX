using System;
using System.Collections;
using System.Globalization;
using System.Windows.Data;

namespace Prosim2GSX.Audio
{
    // Multi-value converter for the VOICEMEETER MAPPINGS DataGrid cell.
    // Inputs (in order): StripIndex (int), IsBus (bool), AvailableStrips (IEnumerable<VoiceMeeterStrip>).
    // Output: the matching strip's DisplayName ("Strip 1 — Hardware Input 1"),
    // falling back to "Strip N+1" / "Bus N+1" when the strips list isn't
    // populated yet (e.g. VoiceMeeter not running, DLL path not set).
    public class VoiceMeeterStripDisplayConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3) return string.Empty;

            int index = values[0] is int i ? i : 0;
            bool isBus = values[1] is bool b && b;
            var strips = values[2] as IEnumerable;

            if (strips != null)
            {
                foreach (var item in strips)
                {
                    if (item is VoiceMeeterStrip s && s.Index == index && s.IsBus == isBus)
                        return s.DisplayName;
                }
            }
            return $"{(isBus ? "Bus" : "Strip")} {index + 1}";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
