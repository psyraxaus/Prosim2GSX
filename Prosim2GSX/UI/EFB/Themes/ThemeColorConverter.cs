using System;
using System.Windows;
using System.Windows.Media;
using Prosim2GSX.Services;

namespace Prosim2GSX.UI.EFB.Themes
{
    /// <summary>
    /// Provides utilities for converting color strings and other theme values to WPF resources.
    /// </summary>
    public static class ThemeColorConverter
    {
        /// <summary>
        /// Converts a resource string to a WPF resource.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <param name="resourceString">The resource string.</param>
        /// <returns>The WPF resource.</returns>
        public static object ConvertToResource(string key, string resourceString)
        {
            if (string.IsNullOrEmpty(resourceString))
            {
                return null;
            }
            
            try
            {
                // Handle font family resources
                if (key.EndsWith("FontFamily", StringComparison.OrdinalIgnoreCase))
                {
                    return ConvertToFontFamily(resourceString);
                }
                
                // Handle color resources
                if (key.EndsWith("Color") || key.EndsWith("Brush"))
                {
                    // Convert the color string to a Color
                    var color = (Color)ColorConverter.ConvertFromString(resourceString);
                    
                    // For brush resources, create a SolidColorBrush
                    if (key.EndsWith("Brush"))
                    {
                        return new SolidColorBrush(color);
                    }
                    
                    // For color resources, return the Color object directly
                    return color;
                }
                
                // For other resources, return the string itself
                return resourceString;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, "ThemeColorConverter:ConvertToResource", 
                    $"Error converting resource '{resourceString}' for key '{key}': {ex.Message}");
                
                // Return appropriate fallbacks based on resource type
                if (key.EndsWith("FontFamily", StringComparison.OrdinalIgnoreCase))
                {
                    return new FontFamily("Arial, sans-serif");
                }
                else if (key.EndsWith("Color"))
                {
                    return Colors.Black;
                }
                else if (key.EndsWith("Brush"))
                {
                    return Brushes.Black;
                }
                
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
        
        /// <summary>
        /// Converts a font family string to a WPF FontFamily with fallbacks.
        /// </summary>
        /// <param name="fontFamilyString">The font family string.</param>
        /// <returns>A FontFamily object with appropriate fallbacks.</returns>
        public static FontFamily ConvertToFontFamily(string fontFamilyString)
        {
            if (string.IsNullOrEmpty(fontFamilyString))
            {
                // Default fallback
                return new FontFamily("Arial, sans-serif");
            }
            
            try
            {
                // If the font family already includes fallbacks, use it as is
                if (fontFamilyString.Contains(","))
                {
                    return new FontFamily(fontFamilyString);
                }
                
                // Add fallbacks based on the font type
                string fontWithFallbacks;
                
                if (fontFamilyString.Contains("MDL2") || fontFamilyString.Contains("Segoe MDL"))
                {
                    // Icon font fallbacks
                    fontWithFallbacks = $"{fontFamilyString}, Arial, sans-serif";
                }
                else if (fontFamilyString.Contains("Consolas") || fontFamilyString.Contains("Courier"))
                {
                    // Monospace font fallbacks
                    fontWithFallbacks = $"{fontFamilyString}, Courier New, monospace";
                }
                else
                {
                    // Standard font fallbacks
                    fontWithFallbacks = $"{fontFamilyString}, Arial, sans-serif";
                }
                
                return new FontFamily(fontWithFallbacks);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, "ThemeColorConverter:ConvertToFontFamily", 
                    $"Error converting font family '{fontFamilyString}': {ex.Message}");
                
                // Return a safe fallback
                return new FontFamily("Arial, sans-serif");
            }
        }
    }
}
