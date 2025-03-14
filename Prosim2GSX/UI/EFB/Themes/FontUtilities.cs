using System;
using System.Windows.Media;
using Prosim2GSX.Services;

namespace Prosim2GSX.UI.EFB.Themes
{
    /// <summary>
    /// Provides utilities for font family conversion and management.
    /// </summary>
    public static class FontUtilities
    {
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
                Logger.Log(LogLevel.Warning, "FontUtilities:ConvertToFontFamily", 
                    $"Error converting font family '{fontFamilyString}': {ex.Message}");
                
                // Return a safe fallback
                return new FontFamily("Arial, sans-serif");
            }
        }
    }
}
