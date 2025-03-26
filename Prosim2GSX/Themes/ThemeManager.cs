using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using Prosim2GSX.Models;

namespace Prosim2GSX.Themes
{
    public class ThemeManager
    {
        private static ThemeManager _instance;
        public static ThemeManager Instance => _instance ??= new ThemeManager();
        
        private Dictionary<string, Theme> _themes = new Dictionary<string, Theme>();
        private Theme _currentTheme;
        private ServiceModel _serviceModel;
        
        public Theme CurrentTheme => _currentTheme;
        public IEnumerable<string> AvailableThemes => _themes.Keys;
        
        public void SetServiceModel(ServiceModel serviceModel)
        {
            _serviceModel = serviceModel;
        }
        
        public void Initialize()
        {
            if (_serviceModel == null)
            {
                Logger.Log(LogLevel.Warning, "ThemeManager", "ServiceModel not set. Theme functionality will be limited.");
                return;
            }
            
            // Create themes directory if it doesn't exist
            string themesDir = Path.Combine(App.AppDir, "Themes");
            if (!Directory.Exists(themesDir))
            {
                Directory.CreateDirectory(themesDir);
            }
            
            // Load all themes from the themes directory
            LoadThemesFromDirectory(themesDir);
            
            // Set current theme from settings
            string themeName = _serviceModel.GetSetting("currentTheme", "Light");
            if (_themes.ContainsKey(themeName))
            {
                _currentTheme = _themes[themeName];
            }
            else if (_themes.Count > 0)
            {
                // Use the first available theme if the saved theme doesn't exist
                _currentTheme = _themes.Values.First();
            }
            else
            {
                // If no themes are available, log a warning
                Logger.Log(LogLevel.Warning, "ThemeManager", "No theme files found in the Themes directory.");
                return;
            }
            
            // Apply the current theme
            ApplyTheme(_currentTheme.Name);
        }
        
        private void LoadThemesFromDirectory(string directory)
        {
            _themes.Clear();
            
            foreach (string file in Directory.GetFiles(directory, "*.json"))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    
                    // Remove comments from JSON before parsing
                    json = RemoveJsonComments(json);
                    
                    Theme theme = JsonSerializer.Deserialize<Theme>(json, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (theme != null && !string.IsNullOrEmpty(theme.Name))
                    {
                        _themes[theme.Name] = theme;
                        Logger.Log(LogLevel.Debug, "ThemeManager", $"Loaded theme: {theme.Name}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Warning, "ThemeManager", $"Failed to load theme file {file}: {ex.Message}");
                }
            }
        }
        
        private string RemoveJsonComments(string json)
        {
            // Simple method to remove comments from JSON
            var lines = json.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                int commentIndex = lines[i].IndexOf("//");
                if (commentIndex >= 0)
                {
                    lines[i] = lines[i].Substring(0, commentIndex);
                }
            }
            return string.Join("\n", lines);
        }
        
        public void ApplyTheme(string themeName)
        {
            if (_serviceModel == null)
            {
                Logger.Log(LogLevel.Warning, "ThemeManager", "ServiceModel not set. Cannot apply theme.");
                return;
            }
            
            if (_themes.ContainsKey(themeName))
            {
                _currentTheme = _themes[themeName];
                _serviceModel.SetSetting("currentTheme", themeName);
                ApplyThemeToResources();
            }
        }
        
        private void ApplyThemeToResources()
        {
            // Update application resources with current theme colors
            var resources = Application.Current.Resources;
            
            // Update colors in resource dictionary
            resources["PrimaryColor"] = new SolidColorBrush(_currentTheme.Colors.GetPrimaryColor());
            resources["SecondaryColor"] = new SolidColorBrush(_currentTheme.Colors.GetSecondaryColor());
            resources["AccentColor"] = new SolidColorBrush(_currentTheme.Colors.GetAccentColor());
            
            resources["HeaderBackground"] = new SolidColorBrush(_currentTheme.Colors.GetHeaderBackground());
            resources["ContentBackground"] = new SolidColorBrush(_currentTheme.Colors.GetContentBackground());
            resources["SectionBackground"] = new SolidColorBrush(_currentTheme.Colors.GetSectionBackground());
            
            resources["HeaderText"] = new SolidColorBrush(_currentTheme.Colors.GetHeaderText());
            resources["ContentText"] = new SolidColorBrush(_currentTheme.Colors.GetContentText());
            resources["CategoryText"] = new SolidColorBrush(_currentTheme.Colors.GetCategoryText());
            
            resources["AtGatePhase"] = new SolidColorBrush(_currentTheme.Colors.FlightPhaseColors.GetAtGateColor());
            resources["TaxiOutPhase"] = new SolidColorBrush(_currentTheme.Colors.FlightPhaseColors.GetTaxiOutColor());
            resources["InFlightPhase"] = new SolidColorBrush(_currentTheme.Colors.FlightPhaseColors.GetInFlightColor());
            resources["ApproachPhase"] = new SolidColorBrush(_currentTheme.Colors.FlightPhaseColors.GetApproachColor());
            resources["ArrivedPhase"] = new SolidColorBrush(_currentTheme.Colors.FlightPhaseColors.GetArrivedColor());
            
            // Status colors remain consistent across themes for clarity
            resources["ActiveStatus"] = new SolidColorBrush(Colors.Green);
            resources["CompletedStatus"] = new SolidColorBrush(Colors.Gold);
            resources["WaitingStatus"] = new SolidColorBrush(Colors.Blue);
            resources["DisconnectedStatus"] = new SolidColorBrush(Colors.Red);
            resources["InactiveStatus"] = new SolidColorBrush(Colors.LightGray);
        }
        
        public void RefreshThemes()
        {
            // Reload themes from the themes directory
            string themesDir = Path.Combine(App.AppDir, "Themes");
            LoadThemesFromDirectory(themesDir);
            
            // Reapply current theme or switch to a default if current theme is no longer available
            if (_currentTheme != null && _themes.ContainsKey(_currentTheme.Name))
            {
                ApplyTheme(_currentTheme.Name);
            }
            else if (_themes.Count > 0)
            {
                ApplyTheme(_themes.Keys.First());
            }
        }
    }
}
