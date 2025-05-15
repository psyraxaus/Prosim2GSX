using Prosim2GSX.Models;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.IO;
using System.Diagnostics;
using Prosim2GSX.Services.Logger.Implementation;
using Prosim2GSX.Services.Logger.Enums;
using Prosim2GSX.Themes;
using Prosim2GSX.ViewModels.Base;
using Prosim2GSX.ViewModels.Commands;

namespace Prosim2GSX.ViewModels.Components
{
    /// <summary>
    /// ViewModel for application settings control
    /// </summary>
    public class AppSettingsViewModel : ViewModelBase
    {
        private readonly ServiceModel _serviceModel;

        // Theme properties
        private int _selectedThemeIndex;
        private string _selectedThemeName;
        private ObservableCollection<string> _themes = new ObservableCollection<string>();
        private string _themesPath;

        // Debug verbosity properties
        private string _debugVerbosity;
        private string _customVerbosity;
        private bool _showCustomVerbosityPanel;

        // Other settings
        private bool _alwaysOnTop;
        private bool _showDebugInfo;

        // Commands
        public ICommand OpenThemeFolderCommand { get; }
        public ICommand RefreshThemesCommand { get; }
        public ICommand ShowExternalDependenciesCommand { get; }

        /// <summary>
        /// Initialize a new instance of the AppSettingsViewModel
        /// </summary>
        /// <param name="serviceModel">Service model for application state</param>
        public AppSettingsViewModel(ServiceModel serviceModel)
        {
            _serviceModel = serviceModel ?? throw new ArgumentNullException(nameof(serviceModel));

            // Initialize commands
            OpenThemeFolderCommand = new RelayCommand(_ => OpenThemeFolder());
            RefreshThemesCommand = new RelayCommand(_ => RefreshThemes());
            ShowExternalDependenciesCommand = new RelayCommand(_ => ShowExternalDependencies());

            // Initialize theme path
            _themesPath = Path.Combine(
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                "Themes");

            // Load settings
            LoadSettings();
        }

        /// <summary>
        /// Gets the selected theme index
        /// </summary>
        public int SelectedThemeIndex
        {
            get => _selectedThemeIndex;
            set
            {
                if (SetProperty(ref _selectedThemeIndex, value) && value >= 0 && value < Themes.Count)
                {
                    SelectedThemeName = Themes[value];

                    // Apply theme immediately
                    ThemeManager.Instance.ApplyTheme(SelectedThemeName);

                    // Save the setting
                    _serviceModel.SetSetting("theme", SelectedThemeName);
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected theme name
        /// </summary>
        public string SelectedThemeName
        {
            get => _selectedThemeName;
            private set => SetProperty(ref _selectedThemeName, value);
        }

        /// <summary>
        /// Gets the available themes
        /// </summary>
        public ObservableCollection<string> Themes
        {
            get => _themes;
            private set => SetProperty(ref _themes, value);
        }

        /// <summary>
        /// Gets the themes folder path
        /// </summary>
        public string ThemesPath
        {
            get => _themesPath;
            private set => SetProperty(ref _themesPath, value);
        }

        /// <summary>
        /// Gets or sets the debug verbosity
        /// </summary>
        public string DebugVerbosity
        {
            get => _debugVerbosity;
            set
            {
                if (SetProperty(ref _debugVerbosity, value))
                {
                    // Show custom panel if "Custom" is selected
                    ShowCustomVerbosityPanel = value == "Custom";

                    // Only update settings if not custom
                    if (value != "Custom")
                    {
                        _serviceModel.SetSetting("debugLogVerbosity", value);
                        _serviceModel.DebugLogVerbosity = value;

                        // Update the log service
                        LogService.SetDebugVerbosity(value);
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the custom verbosity string
        /// </summary>
        public string CustomVerbosity
        {
            get => _customVerbosity;
            set
            {
                if (SetProperty(ref _customVerbosity, value))
                {
                    // Don't auto-apply custom verbosity - wait for command or lost focus
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to show the custom verbosity panel
        /// </summary>
        public bool ShowCustomVerbosityPanel
        {
            get => _showCustomVerbosityPanel;
            private set => SetProperty(ref _showCustomVerbosityPanel, value);
        }

        /// <summary>
        /// Gets or sets whether to always keep the window on top
        /// </summary>
        public bool AlwaysOnTop
        {
            get => _alwaysOnTop;
            set
            {
                if (SetProperty(ref _alwaysOnTop, value))
                {
                    _serviceModel.SetSetting("alwaysOnTop", value.ToString());
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to show debug information
        /// </summary>
        public bool ShowDebugInfo
        {
            get => _showDebugInfo;
            set
            {
                if (SetProperty(ref _showDebugInfo, value))
                {
                    _serviceModel.SetSetting("showDebugInfo", value.ToString());
                }
            }
        }

        /// <summary>
        /// Load settings from the service model
        /// </summary>
        private void LoadSettings()
        {
            // Load themes
            LoadThemes();

            // Load debug verbosity
            _debugVerbosity = _serviceModel.DebugLogVerbosity;

            // Check if it's a custom value
            ShowCustomVerbosityPanel = !IsStandardVerbosity(_debugVerbosity);

            if (ShowCustomVerbosityPanel)
            {
                DebugVerbosity = "Custom";
                CustomVerbosity = _debugVerbosity;
            }
            else
            {
                DebugVerbosity = _debugVerbosity;
            }

            // Load other settings
            AlwaysOnTop = _serviceModel.GetSettingBool("alwaysOnTop", false);
            ShowDebugInfo = _serviceModel.GetSettingBool("showDebugInfo", false);
        }

        /// <summary>
        /// Load available themes
        /// </summary>
        private void LoadThemes()
        {
            Themes.Clear();

            foreach (string themeName in Prosim2GSX.Themes.ThemeManager.Instance.AvailableThemes)
            {
                Themes.Add(themeName);
            }

            // Select current theme
            string currentTheme = _serviceModel.GetSetting("theme", "Light");
            int index = Themes.IndexOf(currentTheme);

            if (index >= 0)
            {
                SelectedThemeIndex = index;
                SelectedThemeName = currentTheme;
            }
            else if (Themes.Count > 0)
            {
                SelectedThemeIndex = 0;
                SelectedThemeName = Themes[0];
            }
        }

        /// <summary>
        /// Check if a verbosity value is one of the standard options
        /// </summary>
        private bool IsStandardVerbosity(string verbosity)
        {
            return verbosity == "All" ||
                   verbosity == "None" ||
                   verbosity == "GSX" ||
                   verbosity == "Prosim";
        }

        /// <summary>
        /// Apply and save custom verbosity
        /// </summary>
        public void ApplyCustomVerbosity()
        {
            if (VerifyCustomCategories(CustomVerbosity))
            {
                // Update settings
                _serviceModel.SetSetting("debugLogVerbosity", CustomVerbosity);
                _serviceModel.DebugLogVerbosity = CustomVerbosity;

                // Update the log service
                LogService.SetDebugVerbosity(CustomVerbosity);
            }
        }

        /// <summary>
        /// Verify custom verbosity categories
        /// </summary>
        private bool VerifyCustomCategories(string categoriesString)
        {
            // Same implementation as in MainWindow
            if (string.IsNullOrWhiteSpace(categoriesString) ||
                categoriesString.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string[] categories = categoriesString.Split(',', StringSplitOptions.RemoveEmptyEntries);

            foreach (string category in categories)
            {
                string trimmed = category.Trim();

                // Try parse directly
                bool isValid = Enum.TryParse<Services.Logger.Enums.LogCategory>(trimmed, true, out _);

                // Check known friendly names if direct parse fails
                if (!isValid)
                {
                    isValid = IsKnownFriendlyName(trimmed);
                }

                if (!isValid)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Check if a name is a known friendly name for a log category
        /// </summary>
        private bool IsKnownFriendlyName(string name)
        {
            // Same implementation as in MainWindow
            string lowered = name.ToLowerInvariant();

            return lowered switch
            {
                "all" or "all categories" or "gsx" or "gsxcontroller" or "refuel" or "refueling" or
                "board" or "boarding" or "cater" or "catering" or "ground" or "groundservices" or
                "ground services" or "sim" or "simconnect" or "ps" or "prosim" or "event" or
                "events" or "menu" or "menus" or "audio" or "sound" or "config" or "configuration" or
                "door" or "doors" or "cargo" or "load" or "loadsheet" => true,
                _ => false
            };
        }

        /// <summary>
        /// Refresh themes
        /// </summary>
        private void RefreshThemes()
        {
            Prosim2GSX.Themes.ThemeManager.Instance.ApplyTheme(SelectedThemeName);
            LoadThemes();
        }

        /// <summary>
        /// Open themes folder
        /// </summary>
        private void OpenThemeFolder()
        {
            if (Directory.Exists(ThemesPath))
            {
                Process.Start("explorer.exe", ThemesPath);
            }
        }

        /// <summary>
        /// Show external dependencies dialog
        /// </summary>
        private void ShowExternalDependencies()
        {
            var dialog = new ExternalDependenciesDialog(_serviceModel);
            dialog.ShowDialog();
        }
    }
}
