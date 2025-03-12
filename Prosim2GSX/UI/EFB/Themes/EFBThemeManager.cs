using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace Prosim2GSX.UI.EFB.Themes
{
    /// <summary>
    /// Event arguments for theme changed events.
    /// </summary>
    public class ThemeChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ThemeChangedEventArgs"/> class.
        /// </summary>
        /// <param name="oldTheme">The old theme.</param>
        /// <param name="newTheme">The new theme.</param>
        public ThemeChangedEventArgs(EFBThemeDefinition oldTheme, EFBThemeDefinition newTheme)
        {
            OldTheme = oldTheme;
            NewTheme = newTheme;
        }

        /// <summary>
        /// Gets the old theme.
        /// </summary>
        public EFBThemeDefinition OldTheme { get; }

        /// <summary>
        /// Gets the new theme.
        /// </summary>
        public EFBThemeDefinition NewTheme { get; }
    }

    /// <summary>
    /// Manager for EFB themes.
    /// </summary>
    public class EFBThemeManager
    {
        private readonly Dictionary<string, EFBThemeDefinition> _themes = new();
        private readonly Dictionary<string, ResourceDictionary> _themeDictionaries = new();
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
        private EFBThemeDefinition _currentTheme;
        private readonly string _defaultThemeName = "Default";

        /// <summary>
        /// Event raised when the theme changes.
        /// </summary>
        public event EventHandler<ThemeChangedEventArgs> ThemeChanged;

        /// <summary>
        /// Gets the current theme.
        /// </summary>
        public EFBThemeDefinition CurrentTheme => _currentTheme;

        /// <summary>
        /// Gets the available themes.
        /// </summary>
        public IEnumerable<EFBThemeDefinition> AvailableThemes => _themes.Values;

        /// <summary>
        /// Loads themes from the specified directory.
        /// </summary>
        /// <param name="themesDirectory">The directory containing theme files.</param>
        /// <returns>The number of themes loaded.</returns>
        public async Task<int> LoadThemesAsync(string themesDirectory)
        {
            if (!Directory.Exists(themesDirectory))
            {
                throw new DirectoryNotFoundException($"Themes directory not found: {themesDirectory}");
            }

            var themeFiles = Directory.GetFiles(themesDirectory, "*.json");
            var loadedCount = 0;

            foreach (var file in themeFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var theme = JsonSerializer.Deserialize<EFBThemeDefinition>(json, _jsonOptions);

                    if (theme == null)
                    {
                        continue;
                    }

                    theme.FilePath = file;
                    theme.Validate();

                    _themes[theme.Name] = theme;
                    loadedCount++;
                }
                catch (Exception ex)
                {
                    // Log the error but continue loading other themes
                    System.Diagnostics.Debug.WriteLine($"Error loading theme from {file}: {ex.Message}");
                }
            }

            // If no themes were loaded, create a default theme
            if (_themes.Count == 0)
            {
                var defaultTheme = CreateDefaultTheme();
                _themes[defaultTheme.Name] = defaultTheme;
                loadedCount++;
            }

            // If no default theme is specified, use the first theme as default
            if (!_themes.Values.Any(t => t.IsDefault))
            {
                var firstTheme = _themes.Values.First();
                firstTheme.IsDefault = true;
            }

            return loadedCount;
        }

        /// <summary>
        /// Gets a theme by name.
        /// </summary>
        /// <param name="themeName">The name of the theme.</param>
        /// <returns>The theme, or null if not found.</returns>
        public EFBThemeDefinition GetTheme(string themeName)
        {
            if (string.IsNullOrWhiteSpace(themeName))
            {
                throw new ArgumentException("Theme name cannot be null or empty.", nameof(themeName));
            }

            return _themes.TryGetValue(themeName, out var theme) ? theme : null;
        }

        /// <summary>
        /// Applies a theme by name.
        /// </summary>
        /// <param name="themeName">The name of the theme.</param>
        /// <returns>True if the theme was applied, false otherwise.</returns>
        public bool ApplyTheme(string themeName)
        {
            if (string.IsNullOrWhiteSpace(themeName))
            {
                throw new ArgumentException("Theme name cannot be null or empty.", nameof(themeName));
            }

            if (!_themes.TryGetValue(themeName, out var theme))
            {
                return false;
            }

            return ApplyTheme(theme);
        }

        /// <summary>
        /// Applies a theme.
        /// </summary>
        /// <param name="theme">The theme to apply.</param>
        /// <returns>True if the theme was applied, false otherwise.</returns>
        public bool ApplyTheme(EFBThemeDefinition theme)
        {
            if (theme == null)
            {
                throw new ArgumentNullException(nameof(theme));
            }

            if (theme.HasValidationErrors)
            {
                return false;
            }

            var oldTheme = _currentTheme;
            _currentTheme = theme;

            // Create or get the resource dictionary for this theme
            if (!_themeDictionaries.TryGetValue(theme.Name, out var resourceDictionary))
            {
                resourceDictionary = CreateResourceDictionary(theme);
                _themeDictionaries[theme.Name] = resourceDictionary;
            }

            // Apply the theme to the application
            var app = Application.Current;
            var existingThemeDictionaries = app.Resources.MergedDictionaries
                .Where(d => d.Source?.ToString().Contains("/Themes/") == true)
                .ToList();

            // Remove existing theme dictionaries
            foreach (var dictionary in existingThemeDictionaries)
            {
                app.Resources.MergedDictionaries.Remove(dictionary);
            }

            // Add the new theme dictionary
            app.Resources.MergedDictionaries.Add(resourceDictionary);

            // Raise the ThemeChanged event
            ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(oldTheme, theme));

            return true;
        }

        /// <summary>
        /// Applies the default theme.
        /// </summary>
        /// <returns>True if the default theme was applied, false otherwise.</returns>
        public bool ApplyDefaultTheme()
        {
            var defaultTheme = _themes.Values.FirstOrDefault(t => t.IsDefault) ?? _themes.Values.FirstOrDefault();

            if (defaultTheme == null)
            {
                return false;
            }

            return ApplyTheme(defaultTheme);
        }

        /// <summary>
        /// Creates a resource dictionary for a theme.
        /// </summary>
        /// <param name="theme">The theme.</param>
        /// <returns>The resource dictionary.</returns>
        private ResourceDictionary CreateResourceDictionary(EFBThemeDefinition theme)
        {
            var dictionary = new ResourceDictionary();

            // Add colors
            foreach (var color in theme.Colors)
            {
                dictionary[color.Key] = System.Windows.Media.ColorConverter.ConvertFromString(color.Value);
            }

            // Add fonts
            foreach (var font in theme.Fonts)
            {
                dictionary[font.Key] = new System.Windows.Media.FontFamily(font.Value);
            }

            // Add other resources
            foreach (var resource in theme.Resources)
            {
                dictionary[resource.Key] = resource.Value;
            }

            // Add theme properties
            dictionary["ThemeName"] = theme.Name;
            dictionary["ThemeDescription"] = theme.Description;
            dictionary["ThemeAuthor"] = theme.Author;
            dictionary["ThemeVersion"] = theme.Version;
            dictionary["ThemeAirlineCode"] = theme.AirlineCode;
            dictionary["ThemeIsDarkTheme"] = theme.IsDarkTheme;
            dictionary["ThemeLogoPath"] = theme.LogoPath;
            dictionary["ThemeBackgroundPath"] = theme.BackgroundPath;

            return dictionary;
        }

        /// <summary>
        /// Creates a default theme.
        /// </summary>
        /// <returns>The default theme.</returns>
        private EFBThemeDefinition CreateDefaultTheme()
        {
            var theme = new EFBThemeDefinition
            {
                Name = _defaultThemeName,
                Description = "Default EFB theme",
                Author = "Prosim2GSX",
                Version = "1.0.0",
                AirlineCode = "DEFAULT",
                IsDefault = true,
                IsDarkTheme = true,
                Colors = new Dictionary<string, string>
                {
                    { "PrimaryColor", "#1E1E1E" },
                    { "SecondaryColor", "#2D2D30" },
                    { "AccentColor", "#007ACC" },
                    { "BackgroundColor", "#1E1E1E" },
                    { "ForegroundColor", "#FFFFFF" },
                    { "BorderColor", "#3F3F46" },
                    { "HeaderColor", "#252526" },
                    { "TabColor", "#2D2D30" },
                    { "TabSelectedColor", "#007ACC" },
                    { "ButtonColor", "#3F3F46" },
                    { "ButtonHoverColor", "#505050" },
                    { "ButtonPressedColor", "#007ACC" },
                    { "TextBoxColor", "#333337" },
                    { "TextBoxBorderColor", "#3F3F46" },
                    { "TextBoxFocusedColor", "#007ACC" },
                    { "ErrorColor", "#FF3333" },
                    { "WarningColor", "#FFCC00" },
                    { "SuccessColor", "#33CC33" },
                    { "InfoColor", "#007ACC" }
                },
                Fonts = new Dictionary<string, string>
                {
                    { "PrimaryFont", "Segoe UI" },
                    { "SecondaryFont", "Consolas" },
                    { "HeaderFont", "Segoe UI Semibold" },
                    { "MonospaceFont", "Consolas" }
                },
                Resources = new Dictionary<string, object>
                {
                    { "CornerRadius", 4 },
                    { "ButtonHeight", 32 },
                    { "TabHeight", 40 },
                    { "HeaderHeight", 48 },
                    { "DefaultMargin", 8 },
                    { "DefaultPadding", 8 },
                    { "SmallFontSize", 11 },
                    { "DefaultFontSize", 12 },
                    { "LargeFontSize", 14 },
                    { "HeaderFontSize", 16 },
                    { "TitleFontSize", 20 }
                }
            };

            return theme;
        }

        /// <summary>
        /// Saves a theme to a file.
        /// </summary>
        /// <param name="theme">The theme to save.</param>
        /// <param name="filePath">The file path to save to.</param>
        /// <returns>True if the theme was saved, false otherwise.</returns>
        public async Task<bool> SaveThemeAsync(EFBThemeDefinition theme, string filePath)
        {
            if (theme == null)
            {
                throw new ArgumentNullException(nameof(theme));
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            }

            try
            {
                var json = JsonSerializer.Serialize(theme, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);
                theme.FilePath = filePath;
                theme.LastModifiedDate = DateTime.Now;
                return true;
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"Error saving theme to {filePath}: {ex.Message}");
                return false;
            }
        }
    }
}
