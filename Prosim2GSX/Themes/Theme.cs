using System;
using System.Globalization;
using System.Windows.Media;

namespace Prosim2GSX.Themes
{
    public class Theme
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ThemeColors Colors { get; set; }
    }

    public class ThemeColors
    {
        // Main colors
        public string PrimaryColor { get; set; }
        public string SecondaryColor { get; set; }
        public string AccentColor { get; set; }
        
        // Background colors
        public string HeaderBackground { get; set; }
        public string ContentBackground { get; set; }
        public string SectionBackground { get; set; }
        
        // Text colors
        public string HeaderText { get; set; }
        public string ContentText { get; set; }
        public string CategoryText { get; set; }
        
        // Flight phase colors
        public FlightPhaseColors FlightPhaseColors { get; set; }
        
        // Helper methods to convert hex strings to Color objects
        public Color GetPrimaryColor() => HexToColor(PrimaryColor);
        public Color GetSecondaryColor() => HexToColor(SecondaryColor);
        public Color GetAccentColor() => HexToColor(AccentColor);
        public Color GetHeaderBackground() => HexToColor(HeaderBackground);
        public Color GetContentBackground() => HexToColor(ContentBackground);
        public Color GetSectionBackground() => HexToColor(SectionBackground);
        public Color GetHeaderText() => HexToColor(HeaderText);
        public Color GetContentText() => HexToColor(ContentText);
        public Color GetCategoryText() => HexToColor(CategoryText);
        
        private static Color HexToColor(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                return Colors.Transparent;
                
            // Remove # if present
            if (hex.StartsWith("#"))
                hex = hex.Substring(1);
                
            // Parse the hex value
            if (hex.Length == 6) // Without alpha
            {
                return Color.FromRgb(
                    byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber),
                    byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber),
                    byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber)
                );
            }
            else if (hex.Length == 8) // With alpha
            {
                return Color.FromArgb(
                    byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber),
                    byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber),
                    byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber),
                    byte.Parse(hex.Substring(6, 2), NumberStyles.HexNumber)
                );
            }
            
            return Colors.Transparent;
        }
    }

    public class FlightPhaseColors
    {
        public string AtGate { get; set; }
        public string TaxiOut { get; set; }
        public string InFlight { get; set; }
        public string Approach { get; set; }
        public string Arrived { get; set; }
        
        // Helper methods to convert hex strings to Color objects
        public Color GetAtGateColor() => HexToColor(AtGate);
        public Color GetTaxiOutColor() => HexToColor(TaxiOut);
        public Color GetInFlightColor() => HexToColor(InFlight);
        public Color GetApproachColor() => HexToColor(Approach);
        public Color GetArrivedColor() => HexToColor(Arrived);
        
        private static Color HexToColor(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                return Colors.Transparent;
                
            // Remove # if present
            if (hex.StartsWith("#"))
                hex = hex.Substring(1);
                
            // Parse the hex value
            if (hex.Length == 6) // Without alpha
            {
                return Color.FromRgb(
                    byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber),
                    byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber),
                    byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber)
                );
            }
            else if (hex.Length == 8) // With alpha
            {
                return Color.FromArgb(
                    byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber),
                    byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber),
                    byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber),
                    byte.Parse(hex.Substring(6, 2), NumberStyles.HexNumber)
                );
            }
            
            return Colors.Transparent;
        }
    }
}
