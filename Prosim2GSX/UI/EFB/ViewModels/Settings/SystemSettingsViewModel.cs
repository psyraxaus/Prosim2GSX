using System;
using System.Collections.ObjectModel;
using Prosim2GSX.Models;
using Prosim2GSX.Services;
using Prosim2GSX.UI.EFB.Themes;

namespace Prosim2GSX.UI.EFB.ViewModels.Settings
{
    /// <summary>
    /// View model for system settings.
    /// </summary>
    public class SystemSettingsViewModel : SettingsCategoryViewModelBase
    {
        private readonly EFBThemeManager _themeManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemSettingsViewModel"/> class.
        /// </summary>
        /// <param name="serviceModel">The service model.</param>
        /// <param name="themeManager">The theme manager.</param>
        /// <param name="logger">The logger.</param>
        public SystemSettingsViewModel(ServiceModel serviceModel, EFBThemeManager themeManager = null, ILogger logger = null)
            : base(serviceModel, logger)
        {
            _themeManager = themeManager;
            
            // Load available themes
            LoadAvailableThemes();
            
            InitializeSettings();
        }

        /// <summary>
        /// Gets the title of the category.
        /// </summary>
        public override string Title => "System Settings";

        /// <summary>
        /// Gets the icon of the category.
        /// </summary>
        public override string Icon => "\uE713";

        #region Properties

        private bool _autoConnect;
        /// <summary>
        /// Gets or sets a value indicating whether to automatically connect to ProsimA320 and MSFS2020.
        /// </summary>
        public bool AutoConnect
        {
            get => _autoConnect;
            set
            {
                if (SetProperty(ref _autoConnect, value))
                {
                    ServiceModel.SetSetting("autoConnect", value.ToString().ToLower());
                }
            }
        }

        private bool _synchBypass;
        /// <summary>
        /// Gets or sets a value indicating whether to bypass synchronization checks.
        /// </summary>
        public bool SynchBypass
        {
            get => _synchBypass;
            set
            {
                if (SetProperty(ref _synchBypass, value))
                {
                    ServiceModel.SetSetting("synchBypass", value.ToString().ToLower());
                }
            }
        }

        private bool _useEfbUi;
        /// <summary>
        /// Gets or sets a value indicating whether to use the EFB-style UI.
        /// </summary>
        public bool UseEfbUi
        {
            get => _useEfbUi;
            set
            {
                if (SetProperty(ref _useEfbUi, value))
                {
                    ServiceModel.SetSetting("useEfbUi", value.ToString().ToLower());

                    // Show a message to inform the user that the change will take effect after restart
                    System.Windows.MessageBox.Show(
                        "The UI change will take effect after restarting the application.",
                        "UI Change",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
            }
        }

        private string _efbThemeName;
        /// <summary>
        /// Gets or sets the EFB theme name.
        /// </summary>
        public string EfbThemeName
        {
            get => _efbThemeName;
            set
            {
                if (SetProperty(ref _efbThemeName, value))
                {
                    ServiceModel.SetSetting("efbThemeName", value);

                    // Apply the theme if theme manager is available
                    _themeManager?.ApplyTheme(value);
                }
            }
        }

        /// <summary>
        /// Gets the available themes.
        /// </summary>
        public ObservableCollection<string> AvailableThemes { get; } = new ObservableCollection<string>();

        #endregion

        #region Methods

        /// <summary>
        /// Initializes the settings.
        /// </summary>
        private void InitializeSettings()
        {
            Settings.Add(new SettingViewModel
            {
                Name = "Auto Connect",
                Description = "Automatically connect to ProsimA320 and MSFS2020",
                Type = SettingType.Toggle,
                IsToggled = AutoConnect,
                ToggledChanged = value => AutoConnect = value
            });

            Settings.Add(new SettingViewModel
            {
                Name = "Synch Bypass",
                Description = "Bypass synchronization checks",
                Type = SettingType.Toggle,
                IsToggled = SynchBypass,
                ToggledChanged = value => SynchBypass = value
            });

            Settings.Add(new SettingViewModel
            {
                Name = "Use EFB UI",
                Description = "Use the EFB-style UI (requires restart)",
                Type = SettingType.Toggle,
                IsToggled = UseEfbUi,
                ToggledChanged = value => UseEfbUi = value
            });

            Settings.Add(new SettingViewModel
            {
                Name = "EFB Theme",
                Description = "Select the theme for the EFB UI",
                Type = SettingType.Options,
                Options = AvailableThemes,
                SelectedOption = EfbThemeName,
                SelectedOptionChanged = value => EfbThemeName = value
            });
        }

        /// <summary>
        /// Loads available themes from the theme manager.
        /// </summary>
        private void LoadAvailableThemes()
        {
            AvailableThemes.Clear();

            if (_themeManager != null && _themeManager.Themes != null)
            {
                foreach (var theme in _themeManager.Themes.Values)
                {
                    AvailableThemes.Add(theme.Name);
                }
            }
            else
            {
                // Add default theme if theme manager is not available
                AvailableThemes.Add("Default");
            }
        }

        /// <summary>
        /// Loads the settings from the service model.
        /// </summary>
        public override void LoadSettings()
        {
            try
            {
                AutoConnect = ServiceModel.AutoConnect;
                SynchBypass = ServiceModel.SynchBypass;
                UseEfbUi = ServiceModel.UseEfbUi;
                EfbThemeName = ServiceModel.EfbThemeName;

                // Reload available themes
                LoadAvailableThemes();

                UpdateSettings();
            }
            catch (Exception ex)
            {
                Logger?.Log(LogLevel.Error, "SystemSettingsViewModel", ex, "Error loading system settings");
            }
        }

        /// <summary>
        /// Updates the settings in the category.
        /// </summary>
        public override void UpdateSettings()
        {
            foreach (var setting in Settings)
            {
                switch (setting.Name)
                {
                    case "Auto Connect":
                        setting.IsToggled = AutoConnect;
                        break;
                    case "Synch Bypass":
                        setting.IsToggled = SynchBypass;
                        break;
                    case "Use EFB UI":
                        setting.IsToggled = UseEfbUi;
                        break;
                    case "EFB Theme":
                        setting.SelectedOption = EfbThemeName;
                        break;
                }
            }
        }

        /// <summary>
        /// Resets the settings to their default values.
        /// </summary>
        public override void ResetToDefaults()
        {
            AutoConnect = true;
            SynchBypass = true;
            UseEfbUi = true;
            EfbThemeName = "Default";

            UpdateSettings();
        }

        #endregion
    }
}
