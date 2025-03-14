using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Globalization;
using Prosim2GSX.Services;

namespace Prosim2GSX.UI.EFB.Themes
{
    /// <summary>
    /// Provides utilities for converting color strings and other theme values to WPF resources.
    /// Also includes methods for color manipulation and contrast calculation.
    /// 
    /// This class is maintained for backward compatibility. For new code, use the specialized utility classes:
    /// - ResourceConverter: For converting resource strings to WPF resources
    /// - ColorUtilities: For color-specific operations
    /// - AccessibilityHelper: For accessibility-related calculations
    /// - FontUtilities: For font-related operations
    /// </summary>
    [Obsolete("This class is maintained for backward compatibility. Use the specialized utility classes instead: ResourceConverter, ColorUtilities, AccessibilityHelper, and FontUtilities.")]
    public static class ThemeColorConverter
    {
        /// <summary>
        /// Converts a resource string to a WPF resource.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <param name="resourceString">The resource string.</param>
        /// <returns>The WPF resource.</returns>
        [Obsolete("Use ResourceConverter.ConvertToResource() instead.")]
        public static object ConvertToResource(string key, string resourceString)
        {
            return ResourceConverter.ConvertToResource(key, resourceString);
        }

        /// <summary>
        /// Converts a string to a Color.
        /// </summary>
        /// <param name="colorString">The color string.</param>
        /// <returns>The Color object.</returns>
        [Obsolete("Use ColorUtilities.ConvertToColor() instead.")]
        public static Color ConvertToColor(string colorString)
        {
            return ColorUtilities.ConvertToColor(colorString);
        }

        /// <summary>
        /// Checks if a color string is valid.
        /// </summary>
        /// <param name="colorString">The color string to check.</param>
        /// <returns>True if the color is valid, false otherwise.</returns>
        [Obsolete("Use ColorUtilities.IsValidColor() instead.")]
        public static bool IsValidColor(string colorString)
        {
            return ColorUtilities.IsValidColor(colorString);
        }

        /// <summary>
        /// Converts a string to a FontWeight.
        /// </summary>
        /// <param name="fontWeightString">The font weight string.</param>
        /// <returns>The FontWeight object.</returns>
        [Obsolete("Use ResourceConverter.ConvertToFontWeight() instead.")]
        public static FontWeight ConvertToFontWeight(string fontWeightString)
        {
            return ResourceConverter.ConvertToFontWeight(fontWeightString);
        }

        /// <summary>
        /// Converts a font family string to a WPF FontFamily with fallbacks.
        /// </summary>
        /// <param name="fontFamilyString">The font family string.</param>
        /// <returns>A FontFamily object with appropriate fallbacks.</returns>
        [Obsolete("Use FontUtilities.ConvertToFontFamily() instead.")]
        public static FontFamily ConvertToFontFamily(string fontFamilyString)
        {
            return FontUtilities.ConvertToFontFamily(fontFamilyString);
        }

        /// <summary>
        /// Converts a string to a CornerRadius.
        /// </summary>
        /// <param name="cornerRadiusString">The corner radius string.</param>
        /// <returns>The CornerRadius object.</returns>
        [Obsolete("Use ResourceConverter.ConvertToCornerRadius() instead.")]
        public static CornerRadius ConvertToCornerRadius(string cornerRadiusString)
        {
            return ResourceConverter.ConvertToCornerRadius(cornerRadiusString);
        }
        
        /// <summary>
        /// Converts a string to a Thickness.
        /// </summary>
        /// <param name="thicknessString">The thickness string.</param>
        /// <returns>The Thickness object.</returns>
        [Obsolete("Use ResourceConverter.ConvertToThickness() instead.")]
        public static Thickness ConvertToThickness(string thicknessString)
        {
            return ResourceConverter.ConvertToThickness(thicknessString);
        }
        
        /// <summary>
        /// Converts a string to a FontStyle.
        /// </summary>
        /// <param name="fontStyleString">The font style string.</param>
        /// <returns>The FontStyle object.</returns>
        [Obsolete("Use ResourceConverter.ConvertToFontStyle() instead.")]
        public static FontStyle ConvertToFontStyle(string fontStyleString)
        {
            return ResourceConverter.ConvertToFontStyle(fontStyleString);
        }
        
        /// <summary>
        /// Converts a string to a FontStretch.
        /// </summary>
        /// <param name="fontStretchString">The font stretch string.</param>
        /// <returns>The FontStretch object.</returns>
        [Obsolete("Use ResourceConverter.ConvertToFontStretch() instead.")]
        public static FontStretch ConvertToFontStretch(string fontStretchString)
        {
            return ResourceConverter.ConvertToFontStretch(fontStretchString);
        }
        
        /// <summary>
        /// Converts a string to a TextAlignment.
        /// </summary>
        /// <param name="textAlignmentString">The text alignment string.</param>
        /// <returns>The TextAlignment value.</returns>
        [Obsolete("Use ResourceConverter.ConvertToTextAlignment() instead.")]
        public static TextAlignment ConvertToTextAlignment(string textAlignmentString)
        {
            return ResourceConverter.ConvertToTextAlignment(textAlignmentString);
        }
        
        /// <summary>
        /// Converts a string to a HorizontalAlignment.
        /// </summary>
        /// <param name="horizontalAlignmentString">The horizontal alignment string.</param>
        /// <returns>The HorizontalAlignment value.</returns>
        [Obsolete("Use ResourceConverter.ConvertToHorizontalAlignment() instead.")]
        public static HorizontalAlignment ConvertToHorizontalAlignment(string horizontalAlignmentString)
        {
            return ResourceConverter.ConvertToHorizontalAlignment(horizontalAlignmentString);
        }
        
        /// <summary>
        /// Converts a string to a VerticalAlignment.
        /// </summary>
        /// <param name="verticalAlignmentString">The vertical alignment string.</param>
        /// <returns>The VerticalAlignment value.</returns>
        [Obsolete("Use ResourceConverter.ConvertToVerticalAlignment() instead.")]
        public static VerticalAlignment ConvertToVerticalAlignment(string verticalAlignmentString)
        {
            return ResourceConverter.ConvertToVerticalAlignment(verticalAlignmentString);
        }
        
        /// <summary>
        /// Converts a string to a Visibility.
        /// </summary>
        /// <param name="visibilityString">The visibility string.</param>
        /// <returns>The Visibility value.</returns>
        [Obsolete("Use ResourceConverter.ConvertToVisibility() instead.")]
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
        [Obsolete("Use ColorUtilities.LightenColor() instead.")]
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
        [Obsolete("Use ColorUtilities.DarkenColor() instead.")]
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
        [Obsolete("Use ColorUtilities.SetOpacity() instead.")]
        public static Color SetOpacity(Color color, double opacity)
        {
            return ColorUtilities.SetOpacity(color, opacity);
        }
        
        /// <summary>
        /// Calculates the relative luminance of a color.
        /// </summary>
        /// <param name="color">The color to calculate luminance for.</param>
        /// <returns>The relative luminance value between 0 and 1.</returns>
        [Obsolete("Use AccessibilityHelper.CalculateLuminance() instead.")]
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
        [Obsolete("Use AccessibilityHelper.CalculateContrast() instead.")]
        public static double CalculateContrast(Color color1, Color color2)
        {
            return AccessibilityHelper.CalculateContrast(color1, color2);
        }
        
        /// <summary>
        /// Gets a contrasting color (black or white) based on the background color.
        /// </summary>
        /// <param name="backgroundColor">The background color.</param>
        /// <returns>Black or white, depending on which provides better contrast.</returns>
        [Obsolete("Use AccessibilityHelper.GetContrastColor() instead.")]
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
        [Obsolete("Use AccessibilityHelper.EnsureContrast() instead.")]
        public static Color EnsureContrast(Color foreground, Color background, double minContrast = 4.5)
        {
            return AccessibilityHelper.EnsureContrast(foreground, background, minContrast);
        }
    }
}
