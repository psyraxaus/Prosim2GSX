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
            
            // Check required colors
            var requiredColors = new[] 
            { 
                "PrimaryColor", 
                "SecondaryColor", 
                "AccentColor", 
                "BackgroundColor", 
                "ForegroundColor" 
            };
            
            foreach (var color in requiredColors)
            {
                if (!theme.Colors.ContainsKey(color))
                {
                    _logger?.Log(LogLevel.Warning, "EFBThemeManager:ValidateTheme", 
                        $"Theme validation failed: Required color {color} is missing");
                    return false;
                }
                
                // Validate color format
                if (!ThemeColorConverter.IsValidColor(theme.Colors[color]))
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
            
            // Define mapping from theme JSON keys to EFB resource keys
            var colorKeyMapping = new Dictionary<string, string>
            {
                { "PrimaryColor", "EFBPrimaryColor" },
                { "SecondaryColor", "EFBSecondaryColor" },
                { "AccentColor", "EFBAccentColor" },
                { "BackgroundColor", "EFBBackgroundColor" },
                { "ForegroundColor", "EFBForegroundColor" },
                { "BorderColor", "EFBBorderColor" },
                { "SuccessColor", "EFBSuccessColor" },
                { "WarningColor", "EFBWarningColor" },
                { "ErrorColor", "EFBErrorColor" },
                { "InfoColor", "EFBInfoColor" }
            };
            
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
                theme.SetResource(resourceKey, ThemeColorConverter.ConvertToResource(resourceKey, color.Value));
                
                // Also add brush resources for each color
                if (resourceKey.EndsWith("Color"))
                {
                    string brushKey = resourceKey.Replace("Color", "Brush");
                    theme.SetResource(brushKey, ThemeColorConverter.ConvertToResource(brushKey, color.Value));
                }
            }
            
            // Add fonts
            if (themeJson.Fonts != null)
            {
                foreach (var font in themeJson.Fonts)
                {
                    theme.SetResource(font.Key, font.Value);
                }
            }
            
            // Add other resources
            if (themeJson.Resources != null)
            {
                foreach (var resource in themeJson.Resources)
                {
                    theme.SetResource(resource.Key, resource.Value);
                }
            }
            
            // Store IsDefault property
            theme.SetResource("IsDefault", themeJson.IsDefault);
            
            return theme;
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
                Application.Current.Resources[resource.Key] = resource.Value;
            }

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
    }
}
