using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Globalization;
using Prosim2GSX.Services;

namespace Prosim2GSX.UI.EFB.Themes
{
    /// <summary>
    /// Provides utilities for converting resource strings to WPF resources.
    /// </summary>
    public static class ResourceConverter
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
                // Handle font weight resources
                if (key.EndsWith("FontWeight", StringComparison.OrdinalIgnoreCase))
                {
                    return ConvertToFontWeight(resourceString);
                }
                
                // Handle font family resources
                if (key.EndsWith("FontFamily", StringComparison.OrdinalIgnoreCase))
                {
                    return FontUtilities.ConvertToFontFamily(resourceString);
                }
                
                // Handle corner radius resources
                if (key.EndsWith("CornerRadius", StringComparison.OrdinalIgnoreCase) ||
                    key.EndsWith("Radius", StringComparison.OrdinalIgnoreCase))
                {
                    return ConvertToCornerRadius(resourceString);
                }
                
                // Handle thickness resources
                if (key.EndsWith("Thickness", StringComparison.OrdinalIgnoreCase) ||
                    key.EndsWith("Margin", StringComparison.OrdinalIgnoreCase) ||
                    key.EndsWith("Padding", StringComparison.OrdinalIgnoreCase) ||
                    key.EndsWith("BorderThickness", StringComparison.OrdinalIgnoreCase))
                {
                    return ConvertToThickness(resourceString);
                }
                
                // Handle font style resources
                if (key.EndsWith("FontStyle", StringComparison.OrdinalIgnoreCase))
                {
                    return ConvertToFontStyle(resourceString);
                }
                
                // Handle font stretch resources
                if (key.EndsWith("FontStretch", StringComparison.OrdinalIgnoreCase))
                {
                    return ConvertToFontStretch(resourceString);
                }
                
                // Handle text alignment resources
                if (key.EndsWith("TextAlignment", StringComparison.OrdinalIgnoreCase))
                {
                    return ConvertToTextAlignment(resourceString);
                }
                
                // Handle horizontal alignment resources
                if (key.EndsWith("HorizontalAlignment", StringComparison.OrdinalIgnoreCase))
                {
                    return ConvertToHorizontalAlignment(resourceString);
                }
                
                // Handle vertical alignment resources
                if (key.EndsWith("VerticalAlignment", StringComparison.OrdinalIgnoreCase))
                {
                    return ConvertToVerticalAlignment(resourceString);
                }
                
                // Handle visibility resources
                if (key.EndsWith("Visibility", StringComparison.OrdinalIgnoreCase))
                {
                    return ConvertToVisibility(resourceString);
                }
                
                // Handle color resources
                if (key.EndsWith("Color") || key.EndsWith("Brush"))
                {
                    // Convert the color string to a Color
                    var color = ColorUtilities.ConvertToColor(resourceString);
                    
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
                Logger.Log(LogLevel.Warning, "ResourceConverter:ConvertToResource", 
                    $"Error converting resource '{resourceString}' for key '{key}': {ex.Message}");
                
                // Return appropriate fallbacks based on resource type
                if (key.EndsWith("FontWeight", StringComparison.OrdinalIgnoreCase))
                {
                    return FontWeights.Normal;
                }
                else if (key.EndsWith("FontFamily", StringComparison.OrdinalIgnoreCase))
                {
                    return new FontFamily("Arial, sans-serif");
                }
                else if (key.EndsWith("CornerRadius", StringComparison.OrdinalIgnoreCase) ||
                         key.EndsWith("Radius", StringComparison.OrdinalIgnoreCase))
                {
                    return new CornerRadius(0);
                }
                else if (key.EndsWith("Thickness", StringComparison.OrdinalIgnoreCase) ||
                         key.EndsWith("Margin", StringComparison.OrdinalIgnoreCase) ||
                         key.EndsWith("Padding", StringComparison.OrdinalIgnoreCase) ||
                         key.EndsWith("BorderThickness", StringComparison.OrdinalIgnoreCase))
                {
                    return new Thickness(0);
                }
                else if (key.EndsWith("FontStyle", StringComparison.OrdinalIgnoreCase))
                {
                    return FontStyles.Normal;
                }
                else if (key.EndsWith("FontStretch", StringComparison.OrdinalIgnoreCase))
                {
                    return FontStretches.Normal;
                }
                else if (key.EndsWith("TextAlignment", StringComparison.OrdinalIgnoreCase))
                {
                    return TextAlignment.Left;
                }
                else if (key.EndsWith("HorizontalAlignment", StringComparison.OrdinalIgnoreCase))
                {
                    return HorizontalAlignment.Left;
                }
                else if (key.EndsWith("VerticalAlignment", StringComparison.OrdinalIgnoreCase))
                {
                    return VerticalAlignment.Top;
                }
                else if (key.EndsWith("Visibility", StringComparison.OrdinalIgnoreCase))
                {
                    return Visibility.Visible;
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
        /// Converts a string to a FontWeight.
        /// </summary>
        /// <param name="fontWeightString">The font weight string.</param>
        /// <returns>The FontWeight object.</returns>
        public static FontWeight ConvertToFontWeight(string fontWeightString)
        {
            if (string.IsNullOrEmpty(fontWeightString))
            {
                return FontWeights.Normal;
            }
            
            try
            {
                // Handle common font weight names
                switch (fontWeightString.Trim().ToLowerInvariant())
                {
                    case "thin":
                        return FontWeights.Thin;
                    case "extralight":
                    case "extra light":
                    case "ultralight":
                    case "ultra light":
                        return FontWeights.ExtraLight;
                    case "light":
                        return FontWeights.Light;
                    case "regular":
                    case "normal":
                        return FontWeights.Normal;
                    case "medium":
                        return FontWeights.Medium;
                    case "semibold":
                    case "semi bold":
                    case "demibold":
                    case "demi bold":
                        return FontWeights.SemiBold;
                    case "bold":
                        return FontWeights.Bold;
                    case "extrabold":
                    case "extra bold":
                    case "ultrabold":
                    case "ultra bold":
                        return FontWeights.ExtraBold;
                    case "black":
                    case "heavy":
                        return FontWeights.Black;
                    case "extrablack":
                    case "extra black":
                    case "ultrablack":
                    case "ultra black":
                        return FontWeights.ExtraBlack;
                }
                
                // Try to parse as a numeric value (100-900)
                if (int.TryParse(fontWeightString, out int weight))
                {
                    // Ensure the weight is within valid range
                    weight = Math.Max(1, Math.Min(999, weight));
                    
                    // Convert to FontWeight
                    return FontWeight.FromOpenTypeWeight(weight);
                }
                
                // Try to parse using TypeConverter
                var converter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(FontWeight));
                if (converter != null && converter.CanConvertFrom(typeof(string)))
                {
                    return (FontWeight)converter.ConvertFromString(null, CultureInfo.InvariantCulture, fontWeightString);
                }
                
                // Default to Normal if we couldn't parse it
                Logger.Log(LogLevel.Warning, "ResourceConverter:ConvertToFontWeight", 
                    $"Could not parse font weight '{fontWeightString}', using Normal as fallback");
                return FontWeights.Normal;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, "ResourceConverter:ConvertToFontWeight", 
                    $"Error converting font weight '{fontWeightString}': {ex.Message}");
                
                // Return a safe fallback
                return FontWeights.Normal;
            }
        }
        
        /// <summary>
        /// Converts a string to a CornerRadius.
        /// </summary>
        /// <param name="cornerRadiusString">The corner radius string.</param>
        /// <returns>The CornerRadius object.</returns>
        public static CornerRadius ConvertToCornerRadius(string cornerRadiusString)
        {
            if (string.IsNullOrEmpty(cornerRadiusString))
            {
                return new CornerRadius(0);
            }
            
            try
            {
                // Handle uniform radius (single value)
                if (double.TryParse(cornerRadiusString, out double radius))
                {
                    return new CornerRadius(radius);
                }
                
                // Handle complex radius (comma-separated values)
                var parts = cornerRadiusString.Split(',');
                if (parts.Length == 4 &&
                    double.TryParse(parts[0].Trim(), out double topLeft) &&
                    double.TryParse(parts[1].Trim(), out double topRight) &&
                    double.TryParse(parts[2].Trim(), out double bottomRight) &&
                    double.TryParse(parts[3].Trim(), out double bottomLeft))
                {
                    return new CornerRadius(topLeft, topRight, bottomRight, bottomLeft);
                }
                
                // Try to parse using TypeConverter
                var converter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(CornerRadius));
                if (converter != null && converter.CanConvertFrom(typeof(string)))
                {
                    return (CornerRadius)converter.ConvertFromString(null, CultureInfo.InvariantCulture, cornerRadiusString);
                }
                
                // Default to 0 if we couldn't parse it
                Logger.Log(LogLevel.Warning, "ResourceConverter:ConvertToCornerRadius", 
                    $"Could not parse corner radius '{cornerRadiusString}', using 0 as fallback");
                return new CornerRadius(0);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, "ResourceConverter:ConvertToCornerRadius", 
                    $"Error converting corner radius '{cornerRadiusString}': {ex.Message}");
                
                // Return a safe fallback
                return new CornerRadius(0);
            }
        }
        
        /// <summary>
        /// Converts a string to a Thickness.
        /// </summary>
        /// <param name="thicknessString">The thickness string.</param>
        /// <returns>The Thickness object.</returns>
        public static Thickness ConvertToThickness(string thicknessString)
        {
            if (string.IsNullOrEmpty(thicknessString))
            {
                return new Thickness(0);
            }
            
            try
            {
                // Handle uniform thickness (single value)
                if (double.TryParse(thicknessString, out double thickness))
                {
                    return new Thickness(thickness);
                }
                
                // Handle complex thickness (comma-separated values)
                var parts = thicknessString.Split(',');
                if (parts.Length == 4 &&
                    double.TryParse(parts[0].Trim(), out double left) &&
                    double.TryParse(parts[1].Trim(), out double top) &&
                    double.TryParse(parts[2].Trim(), out double right) &&
                    double.TryParse(parts[3].Trim(), out double bottom))
                {
                    return new Thickness(left, top, right, bottom);
                }
                else if (parts.Length == 2 &&
                         double.TryParse(parts[0].Trim(), out double horizontal) &&
                         double.TryParse(parts[1].Trim(), out double vertical))
                {
                    return new Thickness(horizontal, vertical, horizontal, vertical);
                }
                
                // Try to parse using TypeConverter
                var converter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(Thickness));
                if (converter != null && converter.CanConvertFrom(typeof(string)))
                {
                    return (Thickness)converter.ConvertFromString(null, CultureInfo.InvariantCulture, thicknessString);
                }
                
                // Default to 0 if we couldn't parse it
                Logger.Log(LogLevel.Warning, "ResourceConverter:ConvertToThickness", 
                    $"Could not parse thickness '{thicknessString}', using 0 as fallback");
                return new Thickness(0);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, "ResourceConverter:ConvertToThickness", 
                    $"Error converting thickness '{thicknessString}': {ex.Message}");
                
                // Return a safe fallback
                return new Thickness(0);
            }
        }
        
        /// <summary>
        /// Converts a string to a FontStyle.
        /// </summary>
        /// <param name="fontStyleString">The font style string.</param>
        /// <returns>The FontStyle object.</returns>
        public static FontStyle ConvertToFontStyle(string fontStyleString)
        {
            if (string.IsNullOrEmpty(fontStyleString))
            {
                return FontStyles.Normal;
            }
            
            try
            {
                // Handle common font style names
                switch (fontStyleString.Trim().ToLowerInvariant())
                {
                    case "normal":
                    case "regular":
                        return FontStyles.Normal;
                    case "italic":
                    case "italics":
                        return FontStyles.Italic;
                    case "oblique":
                        return FontStyles.Oblique;
                }
                
                // Try to parse using TypeConverter
                var converter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(FontStyle));
                if (converter != null && converter.CanConvertFrom(typeof(string)))
                {
                    return (FontStyle)converter.ConvertFromString(null, CultureInfo.InvariantCulture, fontStyleString);
                }
                
                // Default to Normal if we couldn't parse it
                Logger.Log(LogLevel.Warning, "ResourceConverter:ConvertToFontStyle", 
                    $"Could not parse font style '{fontStyleString}', using Normal as fallback");
                return FontStyles.Normal;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, "ResourceConverter:ConvertToFontStyle", 
                    $"Error converting font style '{fontStyleString}': {ex.Message}");
                
                // Return a safe fallback
                return FontStyles.Normal;
            }
        }
        
        /// <summary>
        /// Converts a string to a FontStretch.
        /// </summary>
        /// <param name="fontStretchString">The font stretch string.</param>
        /// <returns>The FontStretch object.</returns>
        public static FontStretch ConvertToFontStretch(string fontStretchString)
        {
            if (string.IsNullOrEmpty(fontStretchString))
            {
                return FontStretches.Normal;
            }
            
            try
            {
                // Handle common font stretch names
                switch (fontStretchString.Trim().ToLowerInvariant())
                {
                    case "ultracondensed":
                    case "ultra condensed":
                        return FontStretches.UltraCondensed;
                    case "extracondensed":
                    case "extra condensed":
                        return FontStretches.ExtraCondensed;
                    case "condensed":
                        return FontStretches.Condensed;
                    case "semicondensed":
                    case "semi condensed":
                        return FontStretches.SemiCondensed;
                    case "normal":
                    case "regular":
                        return FontStretches.Normal;
                    case "semiexpanded":
                    case "semi expanded":
                        return FontStretches.SemiExpanded;
                    case "expanded":
                        return FontStretches.Expanded;
                    case "extraexpanded":
                    case "extra expanded":
                        return FontStretches.ExtraExpanded;
                    case "ultraexpanded":
                    case "ultra expanded":
                        return FontStretches.UltraExpanded;
                }
                
                // Try to parse using TypeConverter
                var converter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(FontStretch));
                if (converter != null && converter.CanConvertFrom(typeof(string)))
                {
                    return (FontStretch)converter.ConvertFromString(null, CultureInfo.InvariantCulture, fontStretchString);
                }
                
                // Default to Normal if we couldn't parse it
                Logger.Log(LogLevel.Warning, "ResourceConverter:ConvertToFontStretch", 
                    $"Could not parse font stretch '{fontStretchString}', using Normal as fallback");
                return FontStretches.Normal;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, "ResourceConverter:ConvertToFontStretch", 
                    $"Error converting font stretch '{fontStretchString}': {ex.Message}");
                
                // Return a safe fallback
                return FontStretches.Normal;
            }
        }
        
        /// <summary>
        /// Converts a string to a TextAlignment.
        /// </summary>
        /// <param name="textAlignmentString">The text alignment string.</param>
        /// <returns>The TextAlignment value.</returns>
        public static TextAlignment ConvertToTextAlignment(string textAlignmentString)
        {
            if (string.IsNullOrEmpty(textAlignmentString))
            {
                return TextAlignment.Left;
            }
            
            try
            {
                // Handle common text alignment names
                switch (textAlignmentString.Trim().ToLowerInvariant())
                {
                    case "left":
                        return TextAlignment.Left;
                    case "center":
                    case "centre":
                        return TextAlignment.Center;
                    case "right":
                        return TextAlignment.Right;
                    case "justify":
                    case "justified":
                        return TextAlignment.Justify;
                }
                
                // Try to parse using Enum.Parse
                if (Enum.TryParse<TextAlignment>(textAlignmentString, true, out var result))
                {
                    return result;
                }
                
                // Default to Left if we couldn't parse it
                Logger.Log(LogLevel.Warning, "ResourceConverter:ConvertToTextAlignment", 
                    $"Could not parse text alignment '{textAlignmentString}', using Left as fallback");
                return TextAlignment.Left;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, "ResourceConverter:ConvertToTextAlignment", 
                    $"Error converting text alignment '{textAlignmentString}': {ex.Message}");
                
                // Return a safe fallback
                return TextAlignment.Left;
            }
        }
        
        /// <summary>
        /// Converts a string to a HorizontalAlignment.
        /// </summary>
        /// <param name="horizontalAlignmentString">The horizontal alignment string.</param>
        /// <returns>The HorizontalAlignment value.</returns>
        public static HorizontalAlignment ConvertToHorizontalAlignment(string horizontalAlignmentString)
        {
            if (string.IsNullOrEmpty(horizontalAlignmentString))
            {
                return HorizontalAlignment.Left;
            }
            
            try
            {
                // Handle common horizontal alignment names
                switch (horizontalAlignmentString.Trim().ToLowerInvariant())
                {
                    case "left":
                        return HorizontalAlignment.Left;
                    case "center":
                    case "centre":
                        return HorizontalAlignment.Center;
                    case "right":
                        return HorizontalAlignment.Right;
                    case "stretch":
                    case "fill":
                        return HorizontalAlignment.Stretch;
                }
                
                // Try to parse using Enum.Parse
                if (Enum.TryParse<HorizontalAlignment>(horizontalAlignmentString, true, out var result))
                {
                    return result;
                }
                
                // Default to Left if we couldn't parse it
                Logger.Log(LogLevel.Warning, "ResourceConverter:ConvertToHorizontalAlignment", 
                    $"Could not parse horizontal alignment '{horizontalAlignmentString}', using Left as fallback");
                return HorizontalAlignment.Left;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, "ResourceConverter:ConvertToHorizontalAlignment", 
                    $"Error converting horizontal alignment '{horizontalAlignmentString}': {ex.Message}");
                
                // Return a safe fallback
                return HorizontalAlignment.Left;
            }
        }
        
        /// <summary>
        /// Converts a string to a VerticalAlignment.
        /// </summary>
        /// <param name="verticalAlignmentString">The vertical alignment string.</param>
        /// <returns>The VerticalAlignment value.</returns>
        public static VerticalAlignment ConvertToVerticalAlignment(string verticalAlignmentString)
        {
            if (string.IsNullOrEmpty(verticalAlignmentString))
            {
                return VerticalAlignment.Top;
            }
            
            try
            {
                // Handle common vertical alignment names
                switch (verticalAlignmentString.Trim().ToLowerInvariant())
                {
                    case "top":
                        return VerticalAlignment.Top;
                    case "center":
                    case "centre":
                    case "middle":
                        return VerticalAlignment.Center;
                    case "bottom":
                        return VerticalAlignment.Bottom;
                    case "stretch":
                    case "fill":
                        return VerticalAlignment.Stretch;
                }
                
                // Try to parse using Enum.Parse
                if (Enum.TryParse<VerticalAlignment>(verticalAlignmentString, true, out var result))
                {
                    return result;
                }
                
                // Default to Top if we couldn't parse it
                Logger.Log(LogLevel.Warning, "ResourceConverter:ConvertToVerticalAlignment", 
                    $"Could not parse vertical alignment '{verticalAlignmentString}', using Top as fallback");
                return VerticalAlignment.Top;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, "ResourceConverter:ConvertToVerticalAlignment", 
                    $"Error converting vertical alignment '{verticalAlignmentString}': {ex.Message}");
                
                // Return a safe fallback
                return VerticalAlignment.Top;
            }
        }
        
        /// <summary>
        /// Converts a string to a Visibility.
        /// </summary>
        /// <param name="visibilityString">The visibility string.</param>
        /// <returns>The Visibility value.</returns>
        public static Visibility ConvertToVisibility(string visibilityString)
        {
            if (string.IsNullOrEmpty(visibilityString))
            {
                return Visibility.Visible;
            }
            
            try
            {
                // Handle common visibility names
                switch (visibilityString.Trim().ToLowerInvariant())
                {
                    case "visible":
                    case "show":
                    case "true":
                        return Visibility.Visible;
                    case "hidden":
                    case "hide":
                        return Visibility.Hidden;
                    case "collapsed":
                    case "collapse":
                    case "false":
                        return Visibility.Collapsed;
                }
                
                // Try to parse using Enum.Parse
                if (Enum.TryParse<Visibility>(visibilityString, true, out var result))
                {
                    return result;
                }
                
                // Try to parse as boolean (true = Visible, false = Collapsed)
                if (bool.TryParse(visibilityString, out bool boolValue))
                {
                    return boolValue ? Visibility.Visible : Visibility.Collapsed;
                }
                
                // Default to Visible if we couldn't parse it
                Logger.Log(LogLevel.Warning, "ResourceConverter:ConvertToVisibility", 
                    $"Could not parse visibility '{visibilityString}', using Visible as fallback");
                return Visibility.Visible;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, "ResourceConverter:ConvertToVisibility", 
                    $"Error converting visibility '{visibilityString}': {ex.Message}");
                
                // Return a safe fallback
                return Visibility.Visible;
            }
        }
    }
}
