using System;
using System.Windows.Media;
using Prosim2GSX.Services;

namespace Prosim2GSX.UI.EFB.Themes
{
    /// <summary>
    /// Provides utilities for color manipulation and conversion.
    /// </summary>
    public static class ColorUtilities
    {
        /// <summary>
        /// Converts a string to a Color.
        /// </summary>
        /// <param name="colorString">The color string.</param>
        /// <returns>The Color object.</returns>
        public static Color ConvertToColor(string colorString)
        {
            try
            {
                return (Color)ColorConverter.ConvertFromString(colorString);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, "ColorUtilities:ConvertToColor", 
                    $"Error converting color '{colorString}': {ex.Message}");
                return Colors.Black;
            }
        }

        /// <summary>
        /// Checks if a color string is valid.
        /// </summary>
        /// <param name="colorString">The color string to check.</param>
        /// <returns>True if the color is valid, false otherwise.</returns>
        public static bool IsValidColor(string colorString)
        {
            if (string.IsNullOrEmpty(colorString))
            {
                return false;
            }
            
            // Check if it's a valid hex color
            if (colorString.StartsWith("#"))
            {
                // Check if it's a valid hex color with 3, 4, 6, or 8 digits
                return System.Text.RegularExpressions.Regex.IsMatch(colorString, "^#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{4}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$");
            }
            
            // Check if it's a valid named color
            try
            {
                var _ = ColorConverter.ConvertFromString(colorString);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Lightens a color by the specified amount.
        /// </summary>
        /// <param name="color">The color to lighten.</param>
        /// <param name="amount">The amount to lighten by (0-1).</param>
        /// <returns>The lightened color.</returns>
        public static Color LightenColor(Color color, double amount)
        {
            return Color.FromArgb(
                color.A,
                (byte)Math.Min(255, color.R + (255 - color.R) * amount),
                (byte)Math.Min(255, color.G + (255 - color.G) * amount),
                (byte)Math.Min(255, color.B + (255 - color.B) * amount)
            );
        }
        
        /// <summary>
        /// Darkens a color by the specified amount.
        /// </summary>
        /// <param name="color">The color to darken.</param>
        /// <param name="amount">The amount to darken by (0-1).</param>
        /// <returns>The darkened color.</returns>
        public static Color DarkenColor(Color color, double amount)
        {
            return Color.FromArgb(
                color.A,
                (byte)Math.Max(0, color.R - color.R * amount),
                (byte)Math.Max(0, color.G - color.G * amount),
                (byte)Math.Max(0, color.B - color.B * amount)
            );
        }
        
        /// <summary>
        /// Sets the opacity of a color.
        /// </summary>
        /// <param name="color">The color to modify.</param>
        /// <param name="opacity">The opacity value (0-1).</param>
        /// <returns>The color with the specified opacity.</returns>
        public static Color SetOpacity(Color color, double opacity)
        {
            return Color.FromArgb(
                (byte)(opacity * 255),
                color.R,
                color.G,
                color.B
            );
        }
    }
}
