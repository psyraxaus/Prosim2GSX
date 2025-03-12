using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

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

        /// <summary>
        /// Event raised when the theme changes.
        /// </summary>
        public event EventHandler<ThemeChangedEventArgs> ThemeChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="EFBThemeManager"/> class.
        /// </summary>
        public EFBThemeManager()
        {
            // Create a default theme
            _defaultTheme = new EFBThemeDefinition("Default")
            {
                Description = "Default EFB theme",
                ResourceDictionaryPath = "/Prosim2GSX;component/UI/EFB/Styles/EFBStyles.xaml"
            };

            // Add the default theme to the themes dictionary
            _themes.Add(_defaultTheme.Name, _defaultTheme);
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

            // Load themes from the directory
            // This is a placeholder implementation
            await Task.CompletedTask;
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
                    System.Diagnostics.Debug.WriteLine($"Error loading theme resource dictionary: {ex.Message}");
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
    }
}
