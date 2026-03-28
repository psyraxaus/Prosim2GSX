using System;
using System.Windows.Media;

namespace Prosim2GSX.Themes
{
    public class Theme
    {
        public string Name { get; set; } = "Light";
        public string Description { get; set; } = "";
        public ThemeColors Colors { get; set; } = new();
    }

    public class ThemeColors
    {
        public string PrimaryColor { get; set; } = "#1E90FF";
        public string SecondaryColor { get; set; } = "#0078D7";
        public string AccentColor { get; set; } = "#FF4500";
        public string HeaderBackground { get; set; } = "#1E90FF";
        public string ContentBackground { get; set; } = "#F8F8F8";
        public string SectionBackground { get; set; } = "#FFFFFF";
        public string HeaderText { get; set; } = "#FFFFFF";
        public string ContentText { get; set; } = "#333333";
        public string CategoryText { get; set; } = "#1E90FF";
        public string TabBarBackground { get; set; } = "#0D1A2A";
        public FlightPhaseColors FlightPhaseColors { get; set; } = new();

        private static Color HexToColor(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                return Colors.Transparent;
            try
            {
                hex = hex.TrimStart('#');
                if (hex.Length == 6)
                    hex = "FF" + hex;
                if (hex.Length != 8)
                    return Colors.Transparent;
                byte a = Convert.ToByte(hex.Substring(0, 2), 16);
                byte r = Convert.ToByte(hex.Substring(2, 2), 16);
                byte g = Convert.ToByte(hex.Substring(4, 2), 16);
                byte b = Convert.ToByte(hex.Substring(6, 2), 16);
                return Color.FromArgb(a, r, g, b);
            }
            catch
            {
                return Colors.Transparent;
            }
        }

        public Color GetPrimaryColor() => HexToColor(PrimaryColor);
        public Color GetSecondaryColor() => HexToColor(SecondaryColor);
        public Color GetAccentColor() => HexToColor(AccentColor);
        public Color GetHeaderBackground() => HexToColor(HeaderBackground);
        public Color GetContentBackground() => HexToColor(ContentBackground);
        public Color GetSectionBackground() => HexToColor(SectionBackground);
        public Color GetHeaderText() => HexToColor(HeaderText);
        public Color GetContentText() => HexToColor(ContentText);
        public Color GetCategoryText() => HexToColor(CategoryText);
        public Color GetTabBarBackground() => HexToColor(TabBarBackground);
    }

    public class FlightPhaseColors
    {
        public string AtGate { get; set; } = "#1E90FF";
        public string TaxiOut { get; set; } = "#FF9800";
        public string InFlight { get; set; } = "#4CAF50";
        public string Approach { get; set; } = "#9C27B0";
        public string Arrived { get; set; } = "#009688";

        private static Color HexToColor(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                return Colors.Transparent;
            try
            {
                hex = hex.TrimStart('#');
                if (hex.Length == 6)
                    hex = "FF" + hex;
                if (hex.Length != 8)
                    return Colors.Transparent;
                byte a = Convert.ToByte(hex.Substring(0, 2), 16);
                byte r = Convert.ToByte(hex.Substring(2, 2), 16);
                byte g = Convert.ToByte(hex.Substring(4, 2), 16);
                byte b = Convert.ToByte(hex.Substring(6, 2), 16);
                return Color.FromArgb(a, r, g, b);
            }
            catch
            {
                return Colors.Transparent;
            }
        }

        public Color GetAtGateColor() => HexToColor(AtGate);
        public Color GetTaxiOutColor() => HexToColor(TaxiOut);
        public Color GetInFlightColor() => HexToColor(InFlight);
        public Color GetApproachColor() => HexToColor(Approach);
        public Color GetArrivedColor() => HexToColor(Arrived);
    }
}
