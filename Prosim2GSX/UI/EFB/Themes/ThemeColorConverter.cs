using System;
using System.Windows;
using System.Windows.Media;
using Prosim2GSX.Services;

namespace Prosim2GSX.UI.EFB.Themes
{
    /// <summary>
    /// Provides utilities for converting color strings and other theme values to WPF resources.
    /// Also includes methods for color manipulation and contrast calculation.
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
                Logger.Log(LogLevel.Warning, "ThemeColorConverter:ConvertToColor", 
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
        
        /// <summary>
        /// Calculates the relative luminance of a color.
        /// </summary>
        /// <param name="color">The color to calculate luminance for.</param>
        /// <returns>The relative luminance value between 0 and 1.</returns>
        public static double CalculateLuminance(Color color)
        {
            // Convert RGB to relative luminance using the formula from WCAG 2.0
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;
            
            r = r <= 0.03928 ? r / 12.92 : Math.Pow((r + 0.055) / 1.055, 2.4);
            g = g <= 0.03928 ? g / 12.92 : Math.Pow((g + 0.055) / 1.055, 2.4);
            b = b <= 0.03928 ? b / 12.92 : Math.Pow((b + 0.055) / 1.055, 2.4);
            
            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }
        
        /// <summary>
        /// Calculates the contrast ratio between two colors.
        /// </summary>
        /// <param name="color1">The first color.</param>
        /// <param name="color2">The second color.</param>
        /// <returns>The contrast ratio between the two colors.</returns>
        public static double CalculateContrast(Color color1, Color color2)
        {
            double luminance1 = CalculateLuminance(color1);
            double luminance2 = CalculateLuminance(color2);
            
            // Calculate contrast ratio using the formula from WCAG 2.0
            double lighter = Math.Max(luminance1, luminance2);
            double darker = Math.Min(luminance1, luminance2);
            
            return (lighter + 0.05) / (darker + 0.05);
        }
        
        /// <summary>
        /// Gets a contrasting color (black or white) based on the background color.
        /// </summary>
        /// <param name="backgroundColor">The background color.</param>
        /// <returns>Black or white, depending on which provides better contrast.</returns>
        public static Color GetContrastColor(Color backgroundColor)
        {
            double luminance = CalculateLuminance(backgroundColor);
            return luminance > 0.5 ? Colors.Black : Colors.White;
        }
        
        /// <summary>
        /// Adjusts a color to ensure it has sufficient contrast with a background color.
        /// </summary>
        /// <param name="foreground">The foreground color.</param>
        /// <param name="background">The background color.</param>
        /// <param name="minContrast">The minimum contrast ratio required.</param>
        /// <returns>The adjusted foreground color.</returns>
        public static Color EnsureContrast(Color foreground, Color background, double minContrast = 4.5)
        {
            double contrast = CalculateContrast(foreground, background);
            
            if (contrast >= minContrast)
            {
                return foreground;
            }
            
            // Determine if we should lighten or darken based on background
            double backgroundLuminance = CalculateLuminance(background);
            bool shouldLighten = backgroundLuminance < 0.5;
            
            // Start with small adjustments and increase until we meet contrast requirements
            double step = 0.05;
            double maxAdjustment = 0.5;
            double adjustment = 0;
            
            Color adjustedColor = foreground;
            
            while (contrast < minContrast && adjustment < maxAdjustment)
            {
                if (shouldLighten)
                {
                    adjustedColor = LightenColor(adjustedColor, step);
                }
                else
                {
                    adjustedColor = DarkenColor(adjustedColor, step);
                }
                
                contrast = CalculateContrast(adjustedColor, background);
                adjustment += step;
            }
            
            // If we still don't have enough contrast, use black or white
            if (contrast < minContrast)
            {
                return shouldLighten ? Colors.White : Colors.Black;
            }
            
            return adjustedColor;
        }
    }
}
