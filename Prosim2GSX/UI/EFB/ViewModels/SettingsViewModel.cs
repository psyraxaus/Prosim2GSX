using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Prosim2GSX.Models;
using Prosim2GSX.Services;
using Prosim2GSX.UI.EFB.Navigation;
using Prosim2GSX.UI.EFB.Themes;
using Prosim2GSX.UI.EFB.ViewModels.Settings;

namespace Prosim2GSX.UI.EFB.ViewModels
{
    /// <summary>
    /// View model for the Settings page.
    /// </summary>
    public class SettingsViewModel : BaseViewModel
    {
        private readonly ServiceModel _serviceModel;
        private readonly EFBThemeManager _themeManager;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
        /// </summary>
        /// <param name="serviceModel">The service model.</param>
        /// <param name="navigationService">The navigation service.</param>
        /// <param name="logger">The logger.</param>
        public SettingsViewModel(ServiceModel serviceModel, EFBNavigationService navigationService = null, ILogger logger = null)
        {
            _serviceModel = serviceModel ?? throw new ArgumentNullException(nameof(serviceModel));
            _themeManager = null; // We'll get it from the service model or application resources later
            _logger = logger;

            // Initialize commands
            SaveSettingsCommand = new RelayCommand(SaveSettings);
            ResetToDefaultsCommand = new RelayCommand(ResetToDefaults);
            IncrementCommand = new RelayCommand<SettingViewModel>(IncrementNumericValue);
            DecrementCommand = new RelayCommand<SettingViewModel>(DecrementNumericValue);

            // Initialize categories
            InitializeCategories();

            // Load settings from service model
            LoadSettings();
        }

        #region Properties

        /// <summary>
        /// Gets the settings categories.
        /// </summary>
        public ObservableCollection<SettingsCategoryViewModelBase> SettingsCategories { get; } = new ObservableCollection<SettingsCategoryViewModelBase>();

        #endregion

        #region Commands

        /// <summary>
        /// Gets the command to save settings.
        /// </summary>
        public ICommand SaveSettingsCommand { get; }

        /// <summary>
        /// Gets the command to reset settings to defaults.
        /// </summary>
        public ICommand ResetToDefaultsCommand { get; }

        /// <summary>
        /// Gets the command to increment a numeric value.
        /// </summary>
        public ICommand IncrementCommand { get; }

        /// <summary>
        /// Gets the command to decrement a numeric value.
        /// </summary>
        public ICommand DecrementCommand { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes the settings categories.
        /// </summary>
        private void InitializeCategories()
        {
            try
            {
                _logger?.Log(LogLevel.Debug, "SettingsViewModel", "Initializing settings categories");

                // Flight Planning
                SettingsCategories.Add(new FlightPlanningSettingsViewModel(_serviceModel, _logger));

                // Automation
                SettingsCategories.Add(new AutomationSettingsViewModel(_serviceModel, _logger));

                // Aircraft Configuration
                SettingsCategories.Add(new AircraftConfigSettingsViewModel(_serviceModel, _logger));

                // Fuel Management
                SettingsCategories.Add(new FuelManagementSettingsViewModel(_serviceModel, _logger));

                // Audio Control
                SettingsCategories.Add(new AudioControlSettingsViewModel(_serviceModel, _logger));

                // System Settings
                // Try to get the theme manager from the application resources
                var themeManager = System.Windows.Application.Current.Resources["ThemeManager"] as EFBThemeManager;
                SettingsCategories.Add(new SystemSettingsViewModel(_serviceModel, themeManager, _logger));

                _logger?.Log(LogLevel.Debug, "SettingsViewModel", "Settings categories initialized successfully");
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, "SettingsViewModel", ex, "Error initializing settings categories");
            }
        }

        /// <summary>
        /// Loads settings from the service model.
        /// </summary>
        public void LoadSettings()
        {
            try
            {
                _logger?.Log(LogLevel.Debug, "SettingsViewModel", "Loading settings");

                // Load settings for each category
                foreach (var category in SettingsCategories)
                {
                    category.LoadSettings();
                }

                _logger?.Log(LogLevel.Information, "SettingsViewModel", "Settings loaded successfully");
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, "SettingsViewModel", ex, "Error loading settings");
            }
        }

        /// <summary>
        /// Saves settings to the service model.
        /// </summary>
        private void SaveSettings()
        {
            try
            {
                _logger?.Log(LogLevel.Debug, "SettingsViewModel", "Saving settings");

                // Settings are saved automatically when properties are changed
                // This method is provided for explicit save functionality if needed

                _logger?.Log(LogLevel.Information, "SettingsViewModel", "Settings saved successfully");
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, "SettingsViewModel", ex, "Error saving settings");
            }
        }

        /// <summary>
        /// Resets settings to default values.
        /// </summary>
        private void ResetToDefaults()
        {
            try
            {
                _logger?.Log(LogLevel.Debug, "SettingsViewModel", "Resetting settings to defaults");

                // Reset settings for each category
                foreach (var category in SettingsCategories)
                {
                    category.ResetToDefaults();
                }

                _logger?.Log(LogLevel.Information, "SettingsViewModel", "Settings reset to defaults");
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, "SettingsViewModel", ex, "Error resetting settings to defaults");
            }
        }

        /// <summary>
        /// Increments a numeric value.
        /// </summary>
        /// <param name="setting">The setting to increment.</param>
        private void IncrementNumericValue(SettingViewModel setting)
        {
            if (setting == null || setting.Type != SettingType.Numeric)
            {
                return;
            }

            setting.NumericValue += 1;
        }

        /// <summary>
        /// Decrements a numeric value.
        /// </summary>
        /// <param name="setting">The setting to decrement.</param>
        private void DecrementNumericValue(SettingViewModel setting)
        {
            if (setting == null || setting.Type != SettingType.Numeric)
            {
                return;
            }

            setting.NumericValue -= 1;
        }

        #endregion
    }
}
