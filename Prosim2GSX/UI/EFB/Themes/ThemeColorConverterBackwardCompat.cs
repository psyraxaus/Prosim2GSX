using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Globalization;
using Prosim2GSX.Services;

namespace Prosim2GSX.UI.EFB.Themes
{
    /// <summary>
    /// Provides backward compatibility for the ThemeColorConverter class.
    /// This class forwards calls to the new utility classes while maintaining the existing API.
    /// </summary>
    public static class ThemeColorConverterBackwardCompat
    {
        /// <summary>
        /// Converts a resource string to a WPF resource.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <param name="resourceString">The resource string.</param>
        /// <returns>The WPF resource.</returns>
        public static object ConvertToResource(string key, string resourceString)
        {
            return ResourceConverter.ConvertToResource(key, resourceString);
        }

        /// <summary>
        /// Converts a string to a Color.
        /// </summary>
        /// <param name="colorString">The color string.</param>
        /// <returns>The Color object.</returns>
        public static Color ConvertToColor(string colorString)
        {
            return ColorUtilities.ConvertToColor(colorString);
        }

        /// <summary>
        /// Checks if a color string is valid.
        /// </summary>
        /// <param name="colorString">The color string to check.</param>
        /// <returns>True if the color is valid, false otherwise.</returns>
        public static bool IsValidColor(string colorString)
        {
            return ColorUtilities.IsValidColor(colorString);
        }

        /// <summary>
        /// Converts a string to a FontWeight.
        /// </summary>
        /// <param name="fontWeightString">The font weight string.</param>
        /// <returns>The FontWeight object.</returns>
        public static FontWeight ConvertToFontWeight(string fontWeightString)
        {
            return ResourceConverter.ConvertToFontWeight(fontWeightString);
        }

        /// <summary>
        /// Converts a font family string to a WPF FontFamily with fallbacks.
        /// </summary>
        /// <param name="fontFamilyString">The font family string.</param>
        /// <returns>A FontFamily object with appropriate fallbacks.</returns>
        public static FontFamily ConvertToFontFamily(string fontFamilyString)
        {
            return FontUtilities.ConvertToFontFamily(fontFamilyString);
        }

        /// <summary>
        /// Converts a string to a CornerRadius.
        /// </summary>
        /// <param name="cornerRadiusString">The corner radius string.</param>
        /// <returns>The CornerRadius object.</returns>
        public static CornerRadius ConvertToCornerRadius(string cornerRadiusString)
        {
            return ResourceConverter.ConvertToCornerRadius(cornerRadiusString);
        }
        
        /// <summary>
        /// Converts a string to a Thickness.
        /// </summary>
        /// <param name="thicknessString">The thickness string.</param>
        /// <returns>The Thickness object.</returns>
        public static Thickness ConvertToThickness(string thicknessString)
        {
            return ResourceConverter.ConvertToThickness(thicknessString);
        }
        
        /// <summary>
        /// Converts a string to a FontStyle.
        /// </summary>
        /// <param name="fontStyleString">The font style string.</param>
        /// <returns>The FontStyle object.</returns>
        public static FontStyle ConvertToFontStyle(string fontStyleString)
        {
            return ResourceConverter.ConvertToFontStyle(fontStyleString);
        }
        
        /// <summary>
        /// Converts a string to a FontStretch.
        /// </summary>
        /// <param name="fontStretchString">The font stretch string.</param>
        /// <returns>The FontStretch object.</returns>
        public static FontStretch ConvertToFontStretch(string fontStretchString)
        {
            return ResourceConverter.ConvertToFontStretch(fontStretchString);
        }
        
        /// <summary>
        /// Converts a string to a TextAlignment.
        /// </summary>
        /// <param name="textAlignmentString">The text alignment string.</param>
        /// <returns>The TextAlignment value.</returns>
        public static TextAlignment ConvertToTextAlignment(string textAlignmentString)
        {
            return ResourceConverter.ConvertToTextAlignment(textAlignmentString);
        }
        
        /// <summary>
        /// Converts a string to a HorizontalAlignment.
        /// </summary>
        /// <param name="horizontalAlignmentString">The horizontal alignment string.</param>
        /// <returns>The HorizontalAlignment value.</returns>
        public static HorizontalAlignment ConvertToHorizontalAlignment(string horizontalAlignmentString)
        {
            return ResourceConverter.ConvertToHorizontalAlignment(horizontalAlignmentString);
        }
        
        /// <summary>
        /// Converts a string to a VerticalAlignment.
        /// </summary>
        /// <param name="verticalAlignmentString">The vertical alignment string.</param>
        /// <returns>The VerticalAlignment value.</returns>
        public static VerticalAlignment ConvertToVerticalAlignment(string verticalAlignmentString)
        {
            return ResourceConverter.ConvertToVerticalAlignment(verticalAlignmentString);
        }
        
        /// <summary>
        /// Converts a string to a Visibility.
        /// </summary>
        /// <param name="visibilityString">The visibility string.</param>
        /// <returns>The Visibility value.</returns>
        public static Visibility ConvertToVisibility(string visibilityString)
        {
            return ResourceConverter.ConvertToVisibility(visibilityString);
        }

        /// <summary>
        /// Lightens a color by the specified amount.
        /// </summary>
        /// <param name="color">The color to lighten.</param>
        /// <param name="amount">The amount to lighten by (0-1).</param>
        /// <returns>The lightened color.</returns>
        public static Color LightenColor(Color color, double amount)
        {
            return ColorUtilities.LightenColor(color, amount);
        }
        
        /// <summary>
        /// Darkens a color by the specified amount.
        /// </summary>
        /// <param name="color">The color to darken.</param>
        /// <param name="amount">The amount to darken by (0-1).</param>
        /// <returns>The darkened color.</returns>
        public static Color DarkenColor(Color color, double amount)
        {
            return ColorUtilities.DarkenColor(color, amount);
        }
        
        /// <summary>
        /// Sets the opacity of a color.
        /// </summary>
        /// <param name="color">The color to modify.</param>
        /// <param name="opacity">The opacity value (0-1).</param>
        /// <returns>The color with the specified opacity.</returns>
        public static Color SetOpacity(Color color, double opacity)
        {
            return ColorUtilities.SetOpacity(color, opacity);
        }
        
        /// <summary>
        /// Calculates the relative luminance of a color.
        /// </summary>
        /// <param name="color">The color to calculate luminance for.</param>
        /// <returns>The relative luminance value between 0 and 1.</returns>
        public static double CalculateLuminance(Color color)
        {
            return AccessibilityHelper.CalculateLuminance(color);
        }
        
        /// <summary>
        /// Calculates the contrast ratio between two colors.
        /// </summary>
        /// <param name="color1">The first color.</param>
        /// <param name="color2">The second color.</param>
        /// <returns>The contrast ratio between the two colors.</returns>
        public static double CalculateContrast(Color color1, Color color2)
        {
            return AccessibilityHelper.CalculateContrast(color1, color2);
        }
        
        /// <summary>
        /// Gets a contrasting color (black or white) based on the background color.
        /// </summary>
        /// <param name="backgroundColor">The background color.</param>
        /// <returns>Black or white, depending on which provides better contrast.</returns>
        public static Color GetContrastColor(Color backgroundColor)
        {
            return AccessibilityHelper.GetContrastColor(backgroundColor);
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
            return AccessibilityHelper.EnsureContrast(foreground, background, minContrast);
        }
    }
}
