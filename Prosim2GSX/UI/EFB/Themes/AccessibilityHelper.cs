using System;
using System.Windows.Media;

namespace Prosim2GSX.UI.EFB.Themes
{
    /// <summary>
    /// Provides utilities for accessibility calculations and contrast enforcement.
    /// </summary>
    public static class AccessibilityHelper
    {
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
                    adjustedColor = ColorUtilities.LightenColor(adjustedColor, step);
                }
                else
                {
                    adjustedColor = ColorUtilities.DarkenColor(adjustedColor, step);
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
