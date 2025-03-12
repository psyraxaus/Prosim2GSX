using System;
using System.Windows;
using System.Windows.Media;

namespace Prosim2GSX.UI.EFB.Themes
{
    /// <summary>
    /// Provides utilities for converting color strings to WPF resources.
    /// </summary>
    public static class ThemeColorConverter
    {
        /// <summary>
        /// Converts a color string to a WPF resource.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <param name="colorString">The color string.</param>
        /// <returns>The WPF resource.</returns>
        public static object ConvertToResource(string key, string colorString)
        {
            if (string.IsNullOrEmpty(colorString))
            {
                return null;
            }
            
            try
            {
                // Convert the color string to a Color
                var color = (Color)ColorConverter.ConvertFromString(colorString);
                
                // For brush resources, create a SolidColorBrush
                if (key.EndsWith("Color") || key.EndsWith("Brush"))
                {
                    return new SolidColorBrush(color);
                }
                
                // For other resources, return the color itself
                return color;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error converting color {colorString}: {ex.Message}");
                return null;
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
    }
}
