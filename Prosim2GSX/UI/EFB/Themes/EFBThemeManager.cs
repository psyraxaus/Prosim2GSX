using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Prosim2GSX.Models;
using Prosim2GSX.Services;

namespace Prosim2GSX.UI.EFB.Themes
{
    /// <summary>
    /// Manages themes for the EFB UI.
    /// </summary>
    public class EFBThemeManager
    {
        private readonly Dictionary<string, EFBThemeDefinition> _themes = new Dictionary<string, EFBThemeDefinition>();
        private EFBThemeDefinition _currentTheme;
        private EFBThemeDefinition _defaultTheme;
        private readonly ILogger _logger;
        private readonly ServiceModel _serviceModel;

        /// <summary>
        /// Event raised when the theme changes.
        /// </summary>
        public event EventHandler<ThemeChangedEventArgs> ThemeChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="EFBThemeManager"/> class.
        /// </summary>
        /// <param name="serviceModel">The service model.</param>
        /// <param name="logger">Optional logger instance.</param>
        public EFBThemeManager(ServiceModel serviceModel, ILogger logger = null)
        {
            _serviceModel = serviceModel ?? throw new ArgumentNullException(nameof(serviceModel));
            _logger = logger;
            
            // Create a default theme
            _defaultTheme = new EFBThemeDefinition("Default")
            {
                Description = "Default EFB theme",
                ResourceDictionaryPath = "/Prosim2GSX;component/UI/EFB/Styles/EFBStyles.xaml"
            };

            // Add the default theme to the themes dictionary
            _themes.Add(_defaultTheme.Name, _defaultTheme);
            
            _logger?.Log(LogLevel.Debug, "EFBThemeManager:Constructor", 
                "EFBThemeManager initialized with default theme");
        }

        /// <summary>
        /// Gets the current theme.
        /// </summary>
        public EFBThemeDefinition CurrentTheme => _currentTheme ?? _defaultTheme;

        /// <summary>
        /// Gets the default theme.
        /// </summary>
        public EFBThemeDefinition DefaultTheme => _defaultTheme;

        /// <summary>
        /// Gets all themes.
        /// </summary>
        public IReadOnlyDictionary<string, EFBThemeDefinition> Themes => _themes;

        /// <summary>
        /// Loads themes from a directory.
        /// </summary>
        /// <param name="themesDirectory">The themes directory.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task LoadThemesAsync(string themesDirectory)
        {
            if (string.IsNullOrEmpty(themesDirectory))
            {
                throw new ArgumentException("Themes directory cannot be null or empty.", nameof(themesDirectory));
            }

            if (!Directory.Exists(themesDirectory))
            {
                throw new DirectoryNotFoundException($"Themes directory not found: {themesDirectory}");
            }

            // Find all JSON theme files
            var themeFiles = Directory.GetFiles(themesDirectory, "*.json");
            
            foreach (var themeFile in themeFiles)
            {
                try
                {
                    // Read the theme file
                    var json = await File.ReadAllTextAsync(themeFile);
                    
                    // Deserialize the theme
                    var themeJson = JsonConvert.DeserializeObject<ThemeJson>(json);
                    
                    // Validate the theme
                    if (!ValidateTheme(themeJson))
                    {
                    _logger?.Log(LogLevel.Warning, "EFBThemeManager:LoadThemesAsync", 
                        $"Theme validation failed for {themeFile}");
                        continue;
                    }
                    
                    // Convert to EFBThemeDefinition
                    var themeDefinition = ConvertJsonToThemeDefinition(themeJson);
                    
                    // Add to themes dictionary
                    AddTheme(themeDefinition);
                    
                    _logger?.Log(LogLevel.Debug, "EFBThemeManager:LoadThemesAsync", 
                        $"Loaded theme: {themeDefinition.Name}");
                }
                catch (Exception ex)
                {
                    // Log the error but continue processing other themes
                    _logger?.Log(LogLevel.Error, "EFBThemeManager:LoadThemesAsync", ex,
                        $"Error loading theme from {themeFile}");
                }
            }
        }

        /// <summary>
        /// Validates a theme JSON object.
        /// </summary>
        /// <param name="theme">The theme to validate.</param>
        /// <returns>True if the theme is valid, false otherwise.</returns>
        private bool ValidateTheme(ThemeJson theme)
        {
            if (theme == null)
            {
                _logger?.Log(LogLevel.Warning, "EFBThemeManager:ValidateTheme", 
                    "Theme validation failed: Theme is null");
                return false;
            }
            
            // Check required properties
            if (string.IsNullOrEmpty(theme.Name))
            {
                _logger?.Log(LogLevel.Warning, "EFBThemeManager:ValidateTheme", 
                    "Theme validation failed: Name is required");
                return false;
            }
            
            if (theme.Colors == null || theme.Colors.Count == 0)
            {
                _logger?.Log(LogLevel.Warning, "EFBThemeManager:ValidateTheme", 
                    "Theme validation failed: Colors are required");
                return false;
            }
            
            // Check for simplified theme format (only requires core colors)
            var coreColors = new[] 
            { 
                "PrimaryColor", 
                "SecondaryColor", 
                "AccentColor", 
                "BackgroundColor", 
                "TextColor" 
            };
            
            // Check if we have all core colors for simplified format
            bool hasAllCoreColors = true;
            foreach (var color in coreColors)
            {
                if (!theme.Colors.ContainsKey(color))
                {
                    hasAllCoreColors = false;
                    break;
                }
            }
            
            // If we have all core colors, validate them
            if (hasAllCoreColors)
            {
                foreach (var color in coreColors)
                {
                    // Validate color format
                    if (!ColorUtilities.IsValidColor(theme.Colors[color]))
                    {
                        _logger?.Log(LogLevel.Warning, "EFBThemeManager:ValidateTheme", 
                            $"Theme validation failed: Color {color} has invalid format: {theme.Colors[color]}");
                        return false;
                    }
                }
                
                return true;
            }
            
            // If not using simplified format, check legacy required colors
            var legacyRequiredColors = new[] 
            { 
                "PrimaryColor", 
                "SecondaryColor", 
                "AccentColor", 
                "BackgroundColor", 
                "ForegroundColor" 
            };
            
            foreach (var color in legacyRequiredColors)
            {
                if (!theme.Colors.ContainsKey(color))
                {
                    _logger?.Log(LogLevel.Warning, "EFBThemeManager:ValidateTheme", 
                        $"Theme validation failed: Required color {color} is missing");
                    return false;
                }
                
                // Validate color format
                if (!ColorUtilities.IsValidColor(theme.Colors[color]))
                {
                    _logger?.Log(LogLevel.Warning, "EFBThemeManager:ValidateTheme", 
                        $"Theme validation failed: Color {color} has invalid format: {theme.Colors[color]}");
                    return false;
                }
            }
            
            return true;
        }

        /// <summary>
        /// Converts a ThemeJson object to an EFBThemeDefinition.
        /// </summary>
        /// <param name="themeJson">The ThemeJson object to convert.</param>
        /// <returns>An EFBThemeDefinition.</returns>
        private EFBThemeDefinition ConvertJsonToThemeDefinition(ThemeJson themeJson)
        {
            var theme = new EFBThemeDefinition(themeJson.Name)
            {
                Description = themeJson.Description,
                // Use a default resource dictionary path based on the theme name
                ResourceDictionaryPath = $"/Prosim2GSX;component/UI/EFB/Styles/EFBStyles.xaml"
            };
            
            // Store airline code if available
            if (!string.IsNullOrEmpty(themeJson.AirlineCode))
            {
                theme.SetResource("AirlineCode", themeJson.AirlineCode);
            }
            
            // Define mapping from theme JSON keys to EFB resource keys
            var colorKeyMapping = new Dictionary<string, string>
            {
                { "PrimaryColor", "EFBPrimaryColor" },
                { "SecondaryColor", "EFBSecondaryColor" },
                { "AccentColor", "EFBAccentColor" },
                { "BackgroundColor", "EFBBackgroundColor" },
                { "ForegroundColor", "EFBForegroundColor" },
                { "TextColor", "EFBForegroundColor" }, // Map TextColor to ForegroundColor for simplified themes
                { "BorderColor", "EFBBorderColor" },
                { "SuccessColor", "EFBSuccessColor" },
                { "WarningColor", "EFBWarningColor" },
                { "ErrorColor", "EFBErrorColor" },
                { "InfoColor", "EFBInfoColor" },
                { "TabSelectedColor", "TabSelectedColor" },
                // Additional mappings for header and button colors
                { "HeaderBackgroundColor", "HeaderBackgroundColor" },
                { "HeaderForegroundColor", "HeaderForegroundColor" },
                { "ButtonBackgroundColor", "ButtonBackgroundColor" },
                { "ButtonForegroundColor", "ButtonForegroundColor" },
                { "ButtonHoverBackgroundColor", "ButtonHoverBackgroundColor" },
                { "ButtonPressedBackgroundColor", "ButtonPressedBackgroundColor" },
                { "InputBackgroundColor", "InputBackgroundColor" },
                { "InputForegroundColor", "InputForegroundColor" },
                { "InputBorderColor", "InputBorderColor" },
                { "InputFocusBorderColor", "InputFocusBorderColor" },
                // Legacy mappings for backward compatibility
                { "HeaderColor", "HeaderBackgroundColor" },
                { "ButtonColor", "ButtonBackgroundColor" },
                { "ButtonHoverColor", "ButtonHoverBackgroundColor" },
                { "ButtonPressedColor", "ButtonPressedBackgroundColor" },
                { "TextBoxColor", "InputBackgroundColor" },
                { "TextBoxBorderColor", "InputBorderColor" },
                { "TextBoxFocusedColor", "InputFocusBorderColor" }
            };
            
            // Check if we're using the simplified theme format
            bool isSimplifiedTheme = themeJson.Colors.ContainsKey("PrimaryColor") &&
                                    themeJson.Colors.ContainsKey("SecondaryColor") &&
                                    themeJson.Colors.ContainsKey("AccentColor") &&
                                    themeJson.Colors.ContainsKey("BackgroundColor") &&
                                    themeJson.Colors.ContainsKey("TextColor");
            
            // If using simplified theme, derive missing colors
            if (isSimplifiedTheme)
            {
                DeriveColorsFromSimplifiedTheme(themeJson.Colors);
            }
            
            // Add colors with mapped keys
            foreach (var color in themeJson.Colors)
            {
                // Get the mapped key if it exists, otherwise use the original key
                string resourceKey = color.Key;
                if (colorKeyMapping.TryGetValue(color.Key, out string mappedKey))
                {
                    resourceKey = mappedKey;
                }
                
                // Convert the color value to a resource and add it to the theme
                theme.SetResource(resourceKey, ResourceConverter.ConvertToResource(resourceKey, color.Value));
                
                // Also add brush resources for each color
                if (resourceKey.EndsWith("Color"))
                {
                    string brushKey = resourceKey.Replace("Color", "Brush");
                    theme.SetResource(brushKey, ResourceConverter.ConvertToResource(brushKey, color.Value));
                }
            }
            
            // Add fonts with fallbacks
            if (themeJson.Fonts != null)
            {
                foreach (var font in themeJson.Fonts)
                {
                    if (font.Key.EndsWith("FontFamily", StringComparison.OrdinalIgnoreCase))
                    {
                        // Convert font family strings to FontFamily objects with fallbacks
                        theme.SetResource(font.Key, FontUtilities.ConvertToFontFamily(font.Value));
                    }
                    else if (font.Key.EndsWith("FontWeight", StringComparison.OrdinalIgnoreCase))
                    {
                        // Convert font weight strings to FontWeight objects
                        theme.SetResource(font.Key, ResourceConverter.ConvertToFontWeight(font.Value));
                        _logger?.Log(LogLevel.Debug, "EFBThemeManager:ConvertJsonToThemeDefinition", 
                            $"Converted font weight '{font.Value}' for key '{font.Key}'");
                    }
                    else
                    {
                        // For other font properties (size, etc.), use as is
                        theme.SetResource(font.Key, font.Value);
                    }
                }
            }
            
            // Add other resources
            if (themeJson.Resources != null)
            {
                foreach (var resource in themeJson.Resources)
                {
                    theme.SetResource(resource.Key, ConvertResourceValue(resource.Key, resource.Value));
                }
            }
            
            // Store IsDefault property
            theme.SetResource("IsDefault", themeJson.IsDefault);
            
            return theme;
        }
        
        /// <summary>
        /// Derives missing colors from a simplified theme.
        /// </summary>
        /// <param name="colors">The colors dictionary to update.</param>
        private void DeriveColorsFromSimplifiedTheme(Dictionary<string, string> colors)
        {
            try
            {
                // Get base colors
                var primaryColor = ColorUtilities.ConvertToColor(colors["PrimaryColor"]);
                var secondaryColor = ColorUtilities.ConvertToColor(colors["SecondaryColor"]);
                var accentColor = ColorUtilities.ConvertToColor(colors["AccentColor"]);
                var backgroundColor = ColorUtilities.ConvertToColor(colors["BackgroundColor"]);
                var textColor = ColorUtilities.ConvertToColor(colors["TextColor"]);
                
                // Derive UI element colors if not explicitly defined
                SetIfMissing(colors, "ForegroundColor", textColor);
                SetIfMissing(colors, "BorderColor", secondaryColor);
                
                // Header colors
                SetIfMissing(colors, "HeaderBackgroundColor", secondaryColor);
                
                // Ensure header foreground has good contrast with header background
                var headerBackground = ColorUtilities.ConvertToColor(colors["HeaderBackgroundColor"]);
                var headerForeground = AccessibilityHelper.GetContrastColor(headerBackground);
                SetIfMissing(colors, "HeaderForegroundColor", headerForeground);
                
                // Button colors
                SetIfMissing(colors, "ButtonBackgroundColor", secondaryColor);
                SetIfMissing(colors, "ButtonForegroundColor", textColor);
                SetIfMissing(colors, "ButtonHoverBackgroundColor", ColorUtilities.LightenColor(secondaryColor, 0.15));
                SetIfMissing(colors, "ButtonPressedBackgroundColor", accentColor);
                SetIfMissing(colors, "ButtonPressedForegroundColor", AccessibilityHelper.GetContrastColor(accentColor));
                
                // Input colors
                SetIfMissing(colors, "InputBackgroundColor", ColorUtilities.DarkenColor(backgroundColor, 0.1));
                SetIfMissing(colors, "InputForegroundColor", textColor);
                SetIfMissing(colors, "InputBorderColor", secondaryColor);
                SetIfMissing(colors, "InputFocusBorderColor", accentColor);
                
                // Tab colors
                SetIfMissing(colors, "TabSelectedColor", accentColor);
                
                // Text colors
                SetIfMissing(colors, "EFBTextPrimaryColor", textColor);
                SetIfMissing(colors, "EFBTextSecondaryColor", ColorToHex(ColorUtilities.SetOpacity(textColor, 0.7)));
                SetIfMissing(colors, "EFBTextAccentColor", accentColor);
                SetIfMissing(colors, "EFBTextContrastColor", AccessibilityHelper.GetContrastColor(accentColor));
                
                // Status colors (defaults if not specified)
                SetIfMissing(colors, "SuccessColor", "#33CC33");
                SetIfMissing(colors, "WarningColor", "#FFCC00");
                SetIfMissing(colors, "ErrorColor", "#FF3333");
                SetIfMissing(colors, "InfoColor", "#3366FF");
                
                // Status text colors
                SetIfMissing(colors, "EFBStatusSuccessTextColor", colors["SuccessColor"]);
                SetIfMissing(colors, "EFBStatusWarningTextColor", colors["WarningColor"]);
                SetIfMissing(colors, "EFBStatusErrorTextColor", colors["ErrorColor"]);
                SetIfMissing(colors, "EFBStatusInfoTextColor", colors["InfoColor"]);
                SetIfMissing(colors, "EFBStatusInactiveTextColor", "#AAAAAA");
                
                _logger?.Log(LogLevel.Debug, "EFBThemeManager:DeriveColorsFromSimplifiedTheme", 
                    "Successfully derived colors from simplified theme");
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, "EFBThemeManager:DeriveColorsFromSimplifiedTheme", ex,
                    "Error deriving colors from simplified theme");
            }
        }
        
        /// <summary>
        /// Sets a color in the dictionary if it doesn't already exist.
        /// </summary>
        /// <param name="colors">The colors dictionary.</param>
        /// <param name="key">The color key.</param>
        /// <param name="value">The color value.</param>
        private void SetIfMissing(Dictionary<string, string> colors, string key, System.Windows.Media.Color value)
        {
            if (!colors.ContainsKey(key))
            {
                colors[key] = ColorToHex(value);
            }
        }
        
        /// <summary>
        /// Sets a color in the dictionary if it doesn't already exist.
        /// </summary>
        /// <param name="colors">The colors dictionary.</param>
        /// <param name="key">The color key.</param>
        /// <param name="value">The color value as a hex string.</param>
        private void SetIfMissing(Dictionary<string, string> colors, string key, string value)
        {
            if (!colors.ContainsKey(key))
            {
                colors[key] = value;
            }
        }
        
        /// <summary>
        /// Converts a Color to a hex string.
        /// </summary>
        /// <param name="color">The color to convert.</param>
        /// <returns>The hex string representation of the color.</returns>
        private string ColorToHex(System.Windows.Media.Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        /// <summary>
        /// Converts a resource value to the appropriate type based on the resource key.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <param name="value">The resource value.</param>
        /// <returns>The converted value.</returns>
        private object ConvertResourceValue(string key, object value)
        {
            // If the value is null, return null
            if (value == null)
            {
                return null;
            }
            
            try
            {
                // Convert string values to the appropriate types based on the key
                if (value is string stringValue)
                {
                    // Convert font sizes to doubles
                    if (key.EndsWith("FontSize", StringComparison.OrdinalIgnoreCase))
                    {
                        if (double.TryParse(stringValue, out double fontSize))
                        {
                            return fontSize;
                        }
                        else
                        {
                            _logger?.Log(LogLevel.Warning, "EFBThemeManager:ConvertResourceValue", 
                                $"Failed to parse font size value '{stringValue}' for key '{key}'");
                            return 12.0; // Default font size
                        }
                    }
                    
                    // Convert corner radii to CornerRadius objects
                    if (key.EndsWith("CornerRadius", StringComparison.OrdinalIgnoreCase))
                    {
                        if (double.TryParse(stringValue, out double radius))
                        {
                            return new System.Windows.CornerRadius(radius);
                        }
                        else
                        {
                            _logger?.Log(LogLevel.Warning, "EFBThemeManager:ConvertResourceValue", 
                                $"Failed to parse corner radius value '{stringValue}' for key '{key}'");
                            return new System.Windows.CornerRadius(0);
                        }
                    }
                    
                    // Convert regular radius values to doubles (not corner radius)
                    if (key.EndsWith("Radius", StringComparison.OrdinalIgnoreCase))
                    {
                        if (double.TryParse(stringValue, out double radius))
                        {
                            return radius;
                        }
                        else
                        {
                            _logger?.Log(LogLevel.Warning, "EFBThemeManager:ConvertResourceValue", 
                                $"Failed to parse radius value '{stringValue}' for key '{key}'");
                            return 0.0; // Default radius
                        }
                    }
                    
                    // Convert thickness values to Thickness objects
                    if (key.EndsWith("Thickness", StringComparison.OrdinalIgnoreCase))
                    {
                        if (double.TryParse(stringValue, out double thickness))
                        {
                            return new System.Windows.Thickness(thickness);
                        }
                        else
                        {
                            _logger?.Log(LogLevel.Warning, "EFBThemeManager:ConvertResourceValue", 
                                $"Failed to parse thickness value '{stringValue}' for key '{key}'");
                            return new System.Windows.Thickness(1);
                        }
                    }
                    
                    // Convert margin and padding values to Thickness objects
                    if (key.EndsWith("Margin", StringComparison.OrdinalIgnoreCase) ||
                        key.EndsWith("Padding", StringComparison.OrdinalIgnoreCase))
                    {
                        if (double.TryParse(stringValue, out double spacing))
                        {
                            return new System.Windows.Thickness(spacing);
                        }
                        else
                        {
                            _logger?.Log(LogLevel.Warning, "EFBThemeManager:ConvertResourceValue", 
                                $"Failed to parse margin/padding value '{stringValue}' for key '{key}'");
                            return new System.Windows.Thickness(8);
                        }
                    }
                    
                    // Convert spacing values to doubles
                    if (key.EndsWith("Spacing", StringComparison.OrdinalIgnoreCase))
                    {
                        if (double.TryParse(stringValue, out double spacing))
                        {
                            return spacing;
                        }
                        else
                        {
                            _logger?.Log(LogLevel.Warning, "EFBThemeManager:ConvertResourceValue", 
                                $"Failed to parse spacing value '{stringValue}' for key '{key}'");
                            return 8.0; // Default spacing
                        }
                    }
                    
                    // Convert height and width values to doubles
                    if (key.EndsWith("Height", StringComparison.OrdinalIgnoreCase) ||
                        key.EndsWith("Width", StringComparison.OrdinalIgnoreCase))
                    {
                        if (double.TryParse(stringValue, out double size))
                        {
                            return size;
                        }
                        else
                        {
                            _logger?.Log(LogLevel.Warning, "EFBThemeManager:ConvertResourceValue", 
                                $"Failed to parse size value '{stringValue}' for key '{key}'");
                            return 0.0; // Default size
                        }
                    }
                    
                    // Convert boolean values
                    if (stringValue.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                        stringValue.Equals("false", StringComparison.OrdinalIgnoreCase))
                    {
                        return bool.Parse(stringValue);
                    }
                }
                
                // Return the original value if no conversion was applied
                return value;
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, "EFBThemeManager:ConvertResourceValue", ex,
                    $"Error converting resource value '{value}' for key '{key}'");
                return value; // Return the original value on error
            }
        }
        
        /// <summary>
        /// Applies the default theme.
        /// </summary>
        public void ApplyDefaultTheme()
        {
            ApplyTheme(_defaultTheme);
        }

        /// <summary>
        /// Applies a theme by name.
        /// </summary>
        /// <param name="themeName">The theme name.</param>
        /// <returns>True if the theme was applied, false otherwise.</returns>
        public bool ApplyTheme(string themeName)
        {
            if (string.IsNullOrEmpty(themeName))
            {
                throw new ArgumentException("Theme name cannot be null or empty.", nameof(themeName));
            }

            if (!_themes.TryGetValue(themeName, out var theme))
            {
                return false;
            }

            ApplyTheme(theme);
            return true;
        }

        /// <summary>
        /// Applies a theme.
        /// </summary>
        /// <param name="theme">The theme to apply.</param>
        public void ApplyTheme(EFBThemeDefinition theme)
        {
            if (theme == null)
            {
                throw new ArgumentNullException(nameof(theme));
            }

            var oldTheme = _currentTheme;
            _currentTheme = theme;
            
            // Save the theme name to configuration
            _serviceModel.SetSetting("efbThemeName", theme.Name, true);

            // Begin a transition animation if we're switching themes
            var transition = oldTheme != null && oldTheme != theme;
            var mainWindow = Application.Current.MainWindow;
            
            if (transition && mainWindow != null)
            {
                ThemeTransitionManager.BeginTransition(mainWindow);
            }

            // Load the theme's resource dictionary if it's not already loaded
            if (theme.ResourceDictionary == null && !string.IsNullOrEmpty(theme.ResourceDictionaryPath))
            {
                try
                {
                    theme.ResourceDictionary = new ResourceDictionary
                    {
                        Source = new Uri(theme.ResourceDictionaryPath, UriKind.RelativeOrAbsolute)
                    };
                }
                catch (Exception ex)
                {
                    _logger?.Log(LogLevel.Error, "EFBThemeManager:ApplyTheme", ex,
                        $"Error loading theme resource dictionary for theme '{theme.Name}'");
                }
            }

            // Apply the theme's resource dictionary
            if (theme.ResourceDictionary != null)
            {
                // Remove the old theme's resource dictionary if it exists
                if (oldTheme?.ResourceDictionary != null)
                {
                    Application.Current.Resources.MergedDictionaries.Remove(oldTheme.ResourceDictionary);
                }

                // Add the new theme's resource dictionary
                Application.Current.Resources.MergedDictionaries.Add(theme.ResourceDictionary);
            }

            // Apply all theme resources to the application resources
            foreach (var resource in theme.GetResources())
            {
                try
                {
                    Application.Current.Resources[resource.Key] = resource.Value;
                    _logger?.Log(LogLevel.Debug, "EFBThemeManager:ApplyTheme", 
                        $"Applied resource '{resource.Key}' for theme '{theme.Name}'");
                }
                catch (Exception ex)
                {
                    _logger?.Log(LogLevel.Error, "EFBThemeManager:ApplyTheme", ex,
                        $"Error applying resource '{resource.Key}' for theme '{theme.Name}'");
                }
            }
            
            // Ensure all required resources are set
            EnsureRequiredResources();

            // Complete the transition if we're switching themes
            if (transition && mainWindow != null)
            {
                ThemeTransitionManager.CompleteTransition(mainWindow);
            }

            // Raise the ThemeChanged event
            ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(oldTheme, theme));
        }

        /// <summary>
        /// Adds a theme.
        /// </summary>
        /// <param name="theme">The theme to add.</param>
        public void AddTheme(EFBThemeDefinition theme)
        {
            if (theme == null)
            {
                throw new ArgumentNullException(nameof(theme));
            }

            _themes[theme.Name] = theme;
        }

        /// <summary>
        /// Removes a theme.
        /// </summary>
        /// <param name="themeName">The name of the theme to remove.</param>
        /// <returns>True if the theme was removed, false otherwise.</returns>
        public bool RemoveTheme(string themeName)
        {
            if (string.IsNullOrEmpty(themeName))
            {
                throw new ArgumentException("Theme name cannot be null or empty.", nameof(themeName));
            }

            // Don't allow removing the default theme
            if (themeName == _defaultTheme.Name)
            {
                return false;
            }

            // Don't allow removing the current theme
            if (_currentTheme != null && themeName == _currentTheme.Name)
            {
                return false;
            }

            return _themes.Remove(themeName);
        }

        /// <summary>
        /// Gets a theme by name.
        /// </summary>
        /// <param name="themeName">The theme name.</param>
        /// <returns>The theme, or null if not found.</returns>
        public EFBThemeDefinition GetTheme(string themeName)
        {
            if (string.IsNullOrEmpty(themeName))
            {
                throw new ArgumentException("Theme name cannot be null or empty.", nameof(themeName));
            }

            return _themes.TryGetValue(themeName, out var theme) ? theme : null;
        }
        
        /// <summary>
        /// Gets all themes that have an airline code.
        /// </summary>
        /// <returns>A collection of airline themes.</returns>
        public IEnumerable<EFBThemeDefinition> GetAirlineThemes()
        {
            return _themes.Values.Where(t => 
                t.GetResource("AirlineCode") != null && 
                !string.IsNullOrEmpty(t.GetResource("AirlineCode").ToString()));
        }

        /// <summary>
        /// Gets a theme by airline code.
        /// </summary>
        /// <param name="airlineCode">The airline code.</param>
        /// <returns>The theme, or null if not found.</returns>
        public EFBThemeDefinition GetThemeByAirlineCode(string airlineCode)
        {
            if (string.IsNullOrEmpty(airlineCode))
            {
                throw new ArgumentException("Airline code cannot be null or empty.", nameof(airlineCode));
            }

            return _themes.Values.FirstOrDefault(t => 
                t.GetResource("AirlineCode") != null && 
                t.GetResource("AirlineCode").ToString().Equals(airlineCode, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Determines whether a theme with the specified name exists.
        /// </summary>
        /// <param name="themeName">The theme name.</param>
        /// <returns>True if a theme with the specified name exists, false otherwise.</returns>
        public bool ThemeExists(string themeName)
        {
            if (string.IsNullOrEmpty(themeName))
            {
                throw new ArgumentException("Theme name cannot be null or empty.", nameof(themeName));
            }

            return _themes.ContainsKey(themeName);
        }
        
        /// <summary>
        /// Gets all available airline codes from the loaded themes.
        /// </summary>
        /// <returns>A collection of airline codes.</returns>
        public IEnumerable<string> GetAvailableAirlineCodes()
        {
            return _themes.Values
                .Where(t => t.GetResource("AirlineCode") != null)
                .Select(t => t.GetResource("AirlineCode").ToString())
                .Where(code => !string.IsNullOrEmpty(code))
                .Distinct();
        }

        /// <summary>
        /// Cycles through available themes.
        /// </summary>
        public void CycleThemes()
        {
            // Get all themes
            var themes = _themes.Values.ToList();
            
            // If there are no themes, return
            if (themes.Count == 0)
            {
                return;
            }
            
            // Find the index of the current theme
            var currentIndex = themes.IndexOf(CurrentTheme);
            
            // Calculate the index of the next theme
            var nextIndex = (currentIndex + 1) % themes.Count;
            
            // Apply the next theme
            ApplyTheme(themes[nextIndex]);
        }

        /// <summary>
        /// Shows the theme selector.
        /// </summary>
        public void ShowThemeSelector()
        {
            // This would be implemented to show a theme selector dialog
            // For now, just cycle themes
            CycleThemes();
        }
        
        /// <summary>
        /// Ensures that all required resources are set in the application resources.
        /// </summary>
        private void EnsureRequiredResources()
        {
            // Define default values for required resources
            var defaultResources = new Dictionary<string, object>
            {
                // Colors
                { "EFBPrimaryColor", System.Windows.Media.Colors.DodgerBlue },
                { "EFBSecondaryColor", System.Windows.Media.Colors.DarkSlateGray },
                { "EFBAccentColor", System.Windows.Media.Colors.Orange },
                { "EFBBackgroundColor", System.Windows.Media.Colors.White },
                { "EFBForegroundColor", System.Windows.Media.Colors.Black },
                { "EFBBorderColor", System.Windows.Media.Colors.LightGray },
                { "EFBSuccessColor", System.Windows.Media.Colors.Green },
                { "EFBWarningColor", System.Windows.Media.Colors.Orange },
                { "EFBErrorColor", System.Windows.Media.Colors.Red },
                { "EFBInfoColor", System.Windows.Media.Colors.Blue },
                
                // Brushes
                { "EFBPrimaryBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DodgerBlue) },
                { "EFBSecondaryBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DarkSlateGray) },
                { "EFBAccentBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange) },
                { "EFBBackgroundBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White) },
                { "EFBForegroundBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black) },
                { "EFBBorderBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray) },
                { "EFBSuccessBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green) },
                { "EFBWarningBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange) },
                { "EFBErrorBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red) },
                { "EFBInfoBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue) },
                
                // Toggle colors
                { "EFBToggleBackgroundColor", System.Windows.Media.Colors.Gray },
                { "EFBToggleBorderColor", System.Windows.Media.Colors.DarkGray },
                { "EFBToggleThumbColor", System.Windows.Media.Colors.LightGray },
                { "EFBToggleThumbBorderColor", System.Windows.Media.Colors.Silver },
                { "EFBToggleCheckedBackgroundColor", System.Windows.Media.Colors.DodgerBlue },
                { "EFBToggleCheckedThumbColor", System.Windows.Media.Colors.White },
                
                // Button colors
                { "ButtonBackgroundColor", System.Windows.Media.Colors.DodgerBlue },
                { "ButtonForegroundColor", System.Windows.Media.Colors.White },
                { "ButtonHoverBackgroundColor", System.Windows.Media.Colors.RoyalBlue },
                { "ButtonPressedBackgroundColor", System.Windows.Media.Colors.DarkBlue },
                
                // Toggle brushes
                { "EFBToggleBackgroundBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray) },
                { "EFBToggleBorderBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DarkGray) },
                { "EFBToggleThumbBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray) },
                { "EFBToggleThumbBorderBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Silver) },
                { "EFBToggleCheckedBackgroundBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DodgerBlue) },
                { "EFBToggleCheckedThumbBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White) },
                
                // Button brushes
                { "ButtonBackgroundBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DodgerBlue) },
                { "ButtonForegroundBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White) },
                { "ButtonHoverBackgroundBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.RoyalBlue) },
                { "ButtonPressedBackgroundBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DarkBlue) },
                
                // Header colors
                { "HeaderBackgroundColor", System.Windows.Media.Colors.DarkSlateGray },
                { "HeaderForegroundColor", System.Windows.Media.Colors.White },
                
                // Header brushes
                { "HeaderBackgroundBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DarkSlateGray) },
                { "HeaderForegroundBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White) },
                
                // Input colors
                { "InputBackgroundColor", System.Windows.Media.Colors.White },
                { "InputForegroundColor", System.Windows.Media.Colors.Black },
                { "InputBorderColor", System.Windows.Media.Colors.LightGray },
                { "InputFocusBorderColor", System.Windows.Media.Colors.DodgerBlue },
                
                // Input brushes
                { "InputBackgroundBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White) },
                { "InputForegroundBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black) },
                { "InputBorderBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray) },
                { "InputFocusBorderBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DodgerBlue) },
                
                // Tab colors
                { "TabSelectedColor", System.Windows.Media.Colors.DodgerBlue },
                
                // Tab brushes
                { "TabSelectedBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DodgerBlue) },
                
                // Text-specific colors for better readability
                { "EFBTextPrimaryColor", System.Windows.Media.Colors.Black },
                { "EFBTextSecondaryColor", System.Windows.Media.Colors.DarkGray },
                { "EFBTextAccentColor", System.Windows.Media.Colors.DodgerBlue },
                { "EFBTextContrastColor", System.Windows.Media.Colors.White },
                
                // Text-specific brushes
                { "EFBTextPrimaryBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black) },
                { "EFBTextSecondaryBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DarkGray) },
                { "EFBTextAccentBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DodgerBlue) },
                { "EFBTextContrastBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White) },
                
                // Status text colors
                { "EFBStatusSuccessTextColor", System.Windows.Media.Colors.Green },
                { "EFBStatusWarningTextColor", System.Windows.Media.Colors.Orange },
                { "EFBStatusErrorTextColor", System.Windows.Media.Colors.Red },
                { "EFBStatusInfoTextColor", System.Windows.Media.Colors.Blue },
                { "EFBStatusInactiveTextColor", System.Windows.Media.Colors.Gray },
                
                // Status text brushes
                { "EFBStatusSuccessTextBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green) },
                { "EFBStatusWarningTextBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange) },
                { "EFBStatusErrorTextBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red) },
                { "EFBStatusInfoTextBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue) },
                { "EFBStatusInactiveTextBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray) },
                
                // Flight Phase Indicator Resources
                { "PhaseDetailsForeground", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black) },
                { "PhaseItemForeground", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black) },
                { "ActivePhaseItemForeground", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White) },
                { "PredictedPhaseItemForeground", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DarkGray) },
                
                { "PhaseDetailsBackground", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray) },
                { "PhaseDetailsBorderBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray) },
                { "PhaseItemBackground", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray) },
                { "PhaseItemBorderBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray) },
                { "ActivePhaseItemBackground", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DodgerBlue) },
                { "ActivePhaseItemBorderBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DodgerBlue) },
                { "PredictedPhaseItemBackground", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.WhiteSmoke) },
                { "PredictedPhaseItemBorderBrush", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray) },
                { "PhaseConnectorStroke", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray) },
                { "ActivePhaseConnectorStroke", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DodgerBlue) },
                { "PredictedPhaseConnectorStroke", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray) }
            };
            
            // Check if each required resource exists in the application resources
            foreach (var resource in defaultResources)
            {
                if (!Application.Current.Resources.Contains(resource.Key))
                {
                    // If not, add the default value
                    Application.Current.Resources[resource.Key] = resource.Value;
                    _logger?.Log(LogLevel.Debug, "EFBThemeManager:EnsureRequiredResources", 
                        $"Added missing resource '{resource.Key}' with default value");
                }
            }
            
            // Ensure text colors have appropriate contrast with background
            EnsureTextContrast();
        }
        
        /// <summary>
        /// Defines a relationship between a foreground color and a background color.
        /// </summary>
        private class ColorRelationship
        {
            /// <summary>
            /// Gets the background color key.
            /// </summary>
            public string BackgroundKey { get; }
            
            /// <summary>
            /// Gets the minimum contrast ratio required.
            /// </summary>
            public double MinimumContrast { get; }
            
            /// <summary>
            /// Initializes a new instance of the <see cref="ColorRelationship"/> class.
            /// </summary>
            /// <param name="backgroundKey">The background color key.</param>
            /// <param name="minimumContrast">The minimum contrast ratio required.</param>
            public ColorRelationship(string backgroundKey, double minimumContrast)
            {
                BackgroundKey = backgroundKey;
                MinimumContrast = minimumContrast;
            }
        }
        
        /// <summary>
        /// Defines color relationships for contrast checking.
        /// </summary>
        private readonly Dictionary<string, ColorRelationship> _colorRelationships = new Dictionary<string, ColorRelationship>
        {
            // Main text on backgrounds
            { "EFBForegroundColor", new ColorRelationship("EFBBackgroundColor", 4.5) },
            { "EFBTextPrimaryColor", new ColorRelationship("EFBBackgroundColor", 4.5) },
            { "EFBTextSecondaryColor", new ColorRelationship("EFBBackgroundColor", 4.5) },
            { "EFBTextContrastColor", new ColorRelationship("EFBPrimaryColor", 4.5) },
            
            // Header text on header backgrounds
            { "HeaderForegroundColor", new ColorRelationship("HeaderBackgroundColor", 4.5) },
            
            // Button text on button backgrounds - using unique keys for each relationship
            { "ButtonForegroundColor", new ColorRelationship("ButtonBackgroundColor", 4.5) },
            { "ButtonForegroundColor_Hover", new ColorRelationship("ButtonHoverBackgroundColor", 4.5) },
            { "ButtonForegroundColor_Pressed", new ColorRelationship("ButtonPressedBackgroundColor", 4.5) },
            
            // Input text on input backgrounds
            { "InputForegroundColor", new ColorRelationship("InputBackgroundColor", 4.5) },
            
            // Status text on backgrounds
            { "EFBStatusSuccessTextColor", new ColorRelationship("EFBBackgroundColor", 3.0) },
            { "EFBStatusWarningTextColor", new ColorRelationship("EFBBackgroundColor", 3.0) },
            { "EFBStatusErrorTextColor", new ColorRelationship("EFBBackgroundColor", 3.0) },
            { "EFBStatusInfoTextColor", new ColorRelationship("EFBBackgroundColor", 3.0) },
            { "EFBStatusInactiveTextColor", new ColorRelationship("EFBBackgroundColor", 3.0) }
        };
        
        /// <summary>
        /// Ensures text colors have appropriate contrast with background colors.
        /// </summary>
        private void EnsureTextContrast()
        {
            try
            {
                // Get background colors for different contexts
                var backgroundColor = GetResourceColor("EFBBackgroundColor", System.Windows.Media.Colors.White);
                var primaryColor = GetResourceColor("EFBPrimaryColor", System.Windows.Media.Colors.DodgerBlue);
                var secondaryColor = GetResourceColor("EFBSecondaryColor", System.Windows.Media.Colors.DarkSlateGray);
                var headerBackgroundColor = GetResourceColor("HeaderBackgroundColor", System.Windows.Media.Colors.DarkSlateGray);
                var buttonBackgroundColor = GetResourceColor("ButtonBackgroundColor", System.Windows.Media.Colors.DodgerBlue);
                var buttonHoverBackgroundColor = GetResourceColor("ButtonHoverBackgroundColor", System.Windows.Media.Colors.RoyalBlue);
                var buttonPressedBackgroundColor = GetResourceColor("ButtonPressedBackgroundColor", System.Windows.Media.Colors.DarkBlue);
                var inputBackgroundColor = GetResourceColor("InputBackgroundColor", System.Windows.Media.Colors.White);
                
                // Calculate background luminance
                double backgroundLuminance = CalculateLuminance(backgroundColor);
                
                // Set appropriate text contrast colors based on background luminance
                if (backgroundLuminance < 0.5)
                {
                    // Dark background - use light text
                    SetResourceColorIfNeeded("EFBForegroundColor", System.Windows.Media.Colors.White);
                    SetResourceColorIfNeeded("EFBTextPrimaryColor", System.Windows.Media.Colors.White);
                    SetResourceColorIfNeeded("EFBTextSecondaryColor", System.Windows.Media.Colors.LightGray);
                    SetResourceColorIfNeeded("EFBTextContrastColor", System.Windows.Media.Colors.Black);
                }
                else
                {
                    // Light background - use dark text
                    SetResourceColorIfNeeded("EFBForegroundColor", System.Windows.Media.Colors.Black);
                    SetResourceColorIfNeeded("EFBTextPrimaryColor", System.Windows.Media.Colors.Black);
                    SetResourceColorIfNeeded("EFBTextSecondaryColor", System.Windows.Media.Colors.DarkGray);
                    SetResourceColorIfNeeded("EFBTextContrastColor", System.Windows.Media.Colors.White);
                }
                
                // Check and adjust all color relationships
                foreach (var relationship in _colorRelationships)
                {
                    string foregroundKey = relationship.Key;
                    string backgroundKey = relationship.Value.BackgroundKey;
                    double minContrast = relationship.Value.MinimumContrast;
                    
                    EnsureContrastForContext(foregroundKey, backgroundKey, minContrast);
                }
                
                // Update brushes for all adjusted colors
                UpdateBrushesFromColors();
                
                _logger?.Log(LogLevel.Debug, "EFBThemeManager:EnsureTextContrast", 
                    "Completed text contrast adjustments");
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, "EFBThemeManager:EnsureTextContrast", ex,
                    "Error ensuring text contrast");
            }
        }
        
        /// <summary>
        /// Gets a color resource with a fallback.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <param name="fallback">The fallback color.</param>
        /// <returns>The color resource or fallback.</returns>
        private System.Windows.Media.Color GetResourceColor(string key, System.Windows.Media.Color fallback)
        {
            return Application.Current.Resources[key] as System.Windows.Media.Color? ?? fallback;
        }
        
        /// <summary>
        /// Sets a color resource if it doesn't already have sufficient contrast.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <param name="color">The color to set.</param>
        private void SetResourceColorIfNeeded(string key, System.Windows.Media.Color color)
        {
            Application.Current.Resources[key] = color;
        }
        
        /// <summary>
        /// Updates brushes for all color resources.
        /// </summary>
        private void UpdateBrushesFromColors()
        {
            foreach (var key in Application.Current.Resources.Keys.OfType<string>().ToList())
            {
                if (key.EndsWith("Color") && Application.Current.Resources[key] is System.Windows.Media.Color color)
                {
                    string brushKey = key.Replace("Color", "Brush");
                    Application.Current.Resources[brushKey] = new System.Windows.Media.SolidColorBrush(color);
                }
            }
        }
        
        /// <summary>
        /// Ensures contrast for a specific color context.
        /// </summary>
        /// <param name="foregroundKey">The foreground color key.</param>
        /// <param name="backgroundKey">The background color key.</param>
        /// <param name="minContrast">The minimum contrast ratio required.</param>
        private void EnsureContrastForContext(string foregroundKey, string backgroundKey, double minContrast)
        {
            try
            {
                // Handle special case for button foreground colors with suffixes
                string actualForegroundKey = foregroundKey;
                if (foregroundKey.StartsWith("ButtonForegroundColor_"))
                {
                    // Use the base ButtonForegroundColor as the actual resource key
                    actualForegroundKey = "ButtonForegroundColor";
                }
                
                if (!Application.Current.Resources.Contains(actualForegroundKey) || 
                    !Application.Current.Resources.Contains(backgroundKey))
                {
                    return;
                }
                
                var foregroundColor = Application.Current.Resources[actualForegroundKey] as System.Windows.Media.Color?;
                var backgroundColor = Application.Current.Resources[backgroundKey] as System.Windows.Media.Color?;
                
                if (foregroundColor == null || backgroundColor == null)
                {
                    return;
                }
                
                double contrast = CalculateContrast(
                    CalculateLuminance(foregroundColor.Value), 
                    CalculateLuminance(backgroundColor.Value));
                
                if (contrast < minContrast)
                {
                    // Adjust the foreground color to meet contrast requirements
                    System.Windows.Media.Color adjustedColor = AdjustColorForContrast(
                        foregroundColor.Value, 
                        backgroundColor.Value, 
                        minContrast);
                    
                    // Update the actual resource key
                    Application.Current.Resources[actualForegroundKey] = adjustedColor;
                    
                    _logger?.Log(LogLevel.Debug, "EFBThemeManager:EnsureContrastForContext", 
                        $"Adjusted {actualForegroundKey} for better contrast with {backgroundKey} (from {contrast:F2} to {CalculateContrast(CalculateLuminance(adjustedColor), CalculateLuminance(backgroundColor.Value)):F2})");
                }
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, "EFBThemeManager:EnsureContrastForContext", ex,
                    $"Error ensuring contrast for {foregroundKey} against {backgroundKey}");
            }
        }
        
        /// <summary>
        /// Adjusts a color to meet a target contrast ratio with a background color.
        /// </summary>
        /// <param name="foreground">The foreground color to adjust.</param>
        /// <param name="background">The background color to contrast against.</param>
        /// <param name="targetContrast">The target contrast ratio.</param>
        /// <returns>The adjusted color.</returns>
        private System.Windows.Media.Color AdjustColorForContrast(
            System.Windows.Media.Color foreground, 
            System.Windows.Media.Color background, 
            double targetContrast)
        {
            // Convert to HSL for more natural adjustments
            var hsl = RgbToHsl(foreground);
            double backgroundLuminance = CalculateLuminance(background);
            
            // Start with small adjustments and increase until we meet contrast requirements
            double step = 0.05;
            double maxAdjustment = 0.5;
            double adjustment = 0;
            
            System.Windows.Media.Color adjustedColor = foreground;
            double contrast = CalculateContrast(
                CalculateLuminance(adjustedColor), 
                backgroundLuminance);
            
            while (contrast < targetContrast && adjustment < maxAdjustment)
            {
                // Determine if we should lighten or darken based on background
                bool shouldLighten = backgroundLuminance < 0.5;
                
                if (shouldLighten)
                {
                    hsl.L = Math.Min(1.0, hsl.L + step);
                }
                else
                {
                    hsl.L = Math.Max(0.0, hsl.L - step);
                }
                
                adjustedColor = HslToRgb(hsl);
                contrast = CalculateContrast(
                    CalculateLuminance(adjustedColor), 
                    backgroundLuminance);
                adjustment += step;
            }
            
            // If we still don't have enough contrast, try more drastic measures
            if (contrast < targetContrast)
            {
                // Use white or black for maximum contrast
                if (backgroundLuminance < 0.5)
                {
                    adjustedColor = System.Windows.Media.Colors.White;
                }
                else
                {
                    adjustedColor = System.Windows.Media.Colors.Black;
                }
            }
            
            return adjustedColor;
        }
        
        /// <summary>
        /// Represents a color in HSL (Hue, Saturation, Lightness) format.
        /// </summary>
        private struct HslColor
        {
            public double H; // Hue (0-360)
            public double S; // Saturation (0-1)
            public double L; // Lightness (0-1)
        }
        
        /// <summary>
        /// Converts an RGB color to HSL.
        /// </summary>
        /// <param name="rgb">The RGB color to convert.</param>
        /// <returns>The HSL representation.</returns>
        private HslColor RgbToHsl(System.Windows.Media.Color rgb)
        {
            double r = rgb.R / 255.0;
            double g = rgb.G / 255.0;
            double b = rgb.B / 255.0;
            
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;
            
            HslColor hsl = new HslColor();
            
            // Lightness
            hsl.L = (max + min) / 2.0;
            
            if (delta == 0)
            {
                // Achromatic (gray)
                hsl.H = 0;
                hsl.S = 0;
            }
            else
            {
                // Saturation
                hsl.S = hsl.L < 0.5 ? delta / (max + min) : delta / (2.0 - max - min);
                
                // Hue
                if (r == max)
                {
                    hsl.H = (g - b) / delta + (g < b ? 6 : 0);
                }
                else if (g == max)
                {
                    hsl.H = (b - r) / delta + 2;
                }
                else
                {
                    hsl.H = (r - g) / delta + 4;
                }
                
                hsl.H *= 60; // Convert to degrees
            }
            
            return hsl;
        }
        
        /// <summary>
        /// Converts an HSL color to RGB.
        /// </summary>
        /// <param name="hsl">The HSL color to convert.</param>
        /// <returns>The RGB representation.</returns>
        private System.Windows.Media.Color HslToRgb(HslColor hsl)
        {
            double r, g, b;
            
            if (hsl.S == 0)
            {
                // Achromatic (gray)
                r = g = b = hsl.L;
            }
            else
            {
                double q = hsl.L < 0.5 ? hsl.L * (1 + hsl.S) : hsl.L + hsl.S - hsl.L * hsl.S;
                double p = 2 * hsl.L - q;
                
                r = HueToRgb(p, q, hsl.H / 360.0 + 1.0/3.0);
                g = HueToRgb(p, q, hsl.H / 360.0);
                b = HueToRgb(p, q, hsl.H / 360.0 - 1.0/3.0);
            }
            
            return System.Windows.Media.Color.FromArgb(
                255,
                (byte)(r * 255),
                (byte)(g * 255),
                (byte)(b * 255)
            );
        }
        
        /// <summary>
        /// Helper function for HSL to RGB conversion.
        /// </summary>
        private double HueToRgb(double p, double q, double t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            
            if (t < 1.0/6.0) return p + (q - p) * 6 * t;
            if (t < 1.0/2.0) return q;
            if (t < 2.0/3.0) return p + (q - p) * (2.0/3.0 - t) * 6;
            
            return p;
        }
        
        /// <summary>
        /// Validates the contrast of a theme.
        /// </summary>
        /// <param name="theme">The theme to validate.</param>
        /// <returns>A dictionary of contrast issues.</returns>
        public Dictionary<string, double> ValidateThemeContrast(EFBThemeDefinition theme)
        {
            var issues = new Dictionary<string, double>();
            
            foreach (var relationship in _colorRelationships)
            {
                string foregroundKey = relationship.Key;
                string backgroundKey = relationship.Value.BackgroundKey;
                double minContrast = relationship.Value.MinimumContrast;
                
                // Handle special case for button foreground colors with suffixes
                string actualForegroundKey = foregroundKey;
                if (foregroundKey.StartsWith("ButtonForegroundColor_"))
                {
                    // Use the base ButtonForegroundColor as the actual resource key
                    actualForegroundKey = "ButtonForegroundColor";
                }
                
                if (theme.ContainsResource(actualForegroundKey) && theme.ContainsResource(backgroundKey))
                {
                    var foreground = theme.GetResource(actualForegroundKey) as System.Windows.Media.Color?;
                    var background = theme.GetResource(backgroundKey) as System.Windows.Media.Color?;
                    
                    if (foreground != null && background != null)
                    {
                        double contrast = CalculateContrast(
                            CalculateLuminance(foreground.Value), 
                            CalculateLuminance(background.Value));
                        
                        if (contrast < minContrast)
                        {
                            issues.Add($"{foregroundKey} on {backgroundKey}", contrast);
                        }
                    }
                }
            }
            
            return issues;
        }
        
        /// <summary>
        /// Calculates the relative luminance of a color.
        /// </summary>
        /// <param name="color">The color to calculate luminance for.</param>
        /// <returns>The relative luminance value between 0 and 1.</returns>
        private double CalculateLuminance(System.Windows.Media.Color color)
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
        /// Calculates the contrast ratio between two luminance values.
        /// </summary>
        /// <param name="luminance1">The first luminance value.</param>
        /// <param name="luminance2">The second luminance value.</param>
        /// <returns>The contrast ratio between the two luminance values.</returns>
        private double CalculateContrast(double luminance1, double luminance2)
        {
            // Calculate contrast ratio using the formula from WCAG 2.0
            double lighter = Math.Max(luminance1, luminance2);
            double darker = Math.Min(luminance1, luminance2);
            
            return (lighter + 0.05) / (darker + 0.05);
        }
        
        /// <summary>
        /// Lightens a color by the specified amount.
        /// </summary>
        /// <param name="color">The color to lighten.</param>
        /// <param name="amount">The amount to lighten by (0-1).</param>
        /// <returns>The lightened color.</returns>
        private System.Windows.Media.Color LightenColor(System.Windows.Media.Color color, double amount)
        {
            return System.Windows.Media.Color.FromArgb(
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
        private System.Windows.Media.Color DarkenColor(System.Windows.Media.Color color, double amount)
        {
            return System.Windows.Media.Color.FromArgb(
                color.A,
                (byte)Math.Max(0, color.R - color.R * amount),
                (byte)Math.Max(0, color.G - color.G * amount),
                (byte)Math.Max(0, color.B - color.B * amount)
            );
        }
    }
}
