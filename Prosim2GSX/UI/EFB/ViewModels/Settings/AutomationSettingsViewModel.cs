using System;
using System.Collections.ObjectModel;
using Prosim2GSX.Models;
using Prosim2GSX.Services;

namespace Prosim2GSX.UI.EFB.ViewModels.Settings
{
    /// <summary>
    /// View model for automation settings.
    /// </summary>
    public class AutomationSettingsViewModel : SettingsCategoryViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AutomationSettingsViewModel"/> class.
        /// </summary>
        /// <param name="serviceModel">The service model.</param>
        /// <param name="logger">The logger.</param>
        public AutomationSettingsViewModel(ServiceModel serviceModel, ILogger logger = null)
            : base(serviceModel, logger)
        {
            InitializeSettings();
        }

        /// <summary>
        /// Gets the title of the category.
        /// </summary>
        public override string Title => "Automation";

        /// <summary>
        /// Gets the icon of the category.
        /// </summary>
        public override string Icon => "\uE8F1";

        #region Properties

        private bool _autoBoarding;
        /// <summary>
        /// Gets or sets a value indicating whether to automatically call boarding service.
        /// </summary>
        public bool AutoBoarding
        {
            get => _autoBoarding;
            set
            {
                if (SetProperty(ref _autoBoarding, value))
                {
                    ServiceModel.SetSetting("autoBoarding", value.ToString().ToLower());
                }
            }
        }

        private bool _autoDeboarding;
        /// <summary>
        /// Gets or sets a value indicating whether to automatically call deboarding service.
        /// </summary>
        public bool AutoDeboarding
        {
            get => _autoDeboarding;
            set
            {
                if (SetProperty(ref _autoDeboarding, value))
                {
                    ServiceModel.SetSetting("autoDeboarding", value.ToString().ToLower());
                }
            }
        }

        private bool _autoRefuel;
        /// <summary>
        /// Gets or sets a value indicating whether to automatically call refueling service.
        /// </summary>
        public bool AutoRefuel
        {
            get => _autoRefuel;
            set
            {
                if (SetProperty(ref _autoRefuel, value))
                {
                    ServiceModel.SetSetting("autoRefuel", value.ToString().ToLower());
                }
            }
        }

        private bool _autoReposition;
        /// <summary>
        /// Gets or sets a value indicating whether to automatically position aircraft at gate.
        /// </summary>
        public bool AutoReposition
        {
            get => _autoReposition;
            set
            {
                if (SetProperty(ref _autoReposition, value))
                {
                    ServiceModel.SetSetting("repositionPlane", value.ToString().ToLower());
                }
            }
        }

        private float _repositionDelay;
        /// <summary>
        /// Gets or sets the delay in seconds before repositioning aircraft.
        /// </summary>
        public float RepositionDelay
        {
            get => _repositionDelay;
            set
            {
                if (SetProperty(ref _repositionDelay, value))
                {
                    ServiceModel.SetSetting("repositionDelay", value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
            }
        }

        private bool _callCatering;
        /// <summary>
        /// Gets or sets a value indicating whether to automatically call catering service.
        /// </summary>
        public bool CallCatering
        {
            get => _callCatering;
            set
            {
                if (SetProperty(ref _callCatering, value))
                {
                    ServiceModel.SetSetting("callCatering", value.ToString().ToLower());
                }
            }
        }

        private bool _cargoLoadingBeforeBoarding;
        /// <summary>
        /// Gets or sets a value indicating whether to load cargo before passenger boarding.
        /// </summary>
        public bool CargoLoadingBeforeBoarding
        {
            get => _cargoLoadingBeforeBoarding;
            set
            {
                if (SetProperty(ref _cargoLoadingBeforeBoarding, value))
                {
                    ServiceModel.SetSetting("cargoLoadingBeforeBoarding", value.ToString().ToLower());
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes the settings.
        /// </summary>
        private void InitializeSettings()
        {
            Settings.Add(new SettingViewModel
            {
                Name = "Auto Boarding",
                Description = "Automatically call boarding service when ready",
                Type = SettingType.Toggle,
                IsToggled = AutoBoarding,
                ToggledChanged = value => AutoBoarding = value
            });

            Settings.Add(new SettingViewModel
            {
                Name = "Auto Deboarding",
                Description = "Automatically call deboarding service on arrival",
                Type = SettingType.Toggle,
                IsToggled = AutoDeboarding,
                ToggledChanged = value => AutoDeboarding = value
            });

            Settings.Add(new SettingViewModel
            {
                Name = "Auto Refuel",
                Description = "Automatically call refueling service based on flight plan",
                Type = SettingType.Toggle,
                IsToggled = AutoRefuel,
                ToggledChanged = value => AutoRefuel = value
            });

            Settings.Add(new SettingViewModel
            {
                Name = "Auto Reposition",
                Description = "Automatically position aircraft at gate on startup",
                Type = SettingType.Toggle,
                IsToggled = AutoReposition,
                ToggledChanged = value => AutoReposition = value
            });

            Settings.Add(new SettingViewModel
            {
                Name = "Reposition Delay",
                Description = "Delay in seconds before repositioning aircraft",
                Type = SettingType.Numeric,
                NumericValue = RepositionDelay,
                NumericChanged = value => RepositionDelay = value
            });

            Settings.Add(new SettingViewModel
            {
                Name = "Call Catering",
                Description = "Automatically call catering service",
                Type = SettingType.Toggle,
                IsToggled = CallCatering,
                ToggledChanged = value => CallCatering = value
            });

            Settings.Add(new SettingViewModel
            {
                Name = "Cargo Loading Before Boarding",
                Description = "Load cargo before passenger boarding begins",
                Type = SettingType.Toggle,
                IsToggled = CargoLoadingBeforeBoarding,
                ToggledChanged = value => CargoLoadingBeforeBoarding = value
            });
        }

        /// <summary>
        /// Loads the settings from the service model.
        /// </summary>
        public override void LoadSettings()
        {
            try
            {
                AutoBoarding = ServiceModel.AutoBoarding;
                AutoDeboarding = ServiceModel.AutoDeboarding;
                AutoRefuel = ServiceModel.AutoRefuel;
                AutoReposition = ServiceModel.RepositionPlane;
                RepositionDelay = ServiceModel.RepositionDelay;
                CallCatering = ServiceModel.CallCatering;
                CargoLoadingBeforeBoarding = ServiceModel.CargoLoadingBeforeBoarding;

                UpdateSettings();
            }
            catch (Exception ex)
            {
                Logger?.Log(LogLevel.Error, "AutomationSettingsViewModel", ex, "Error loading automation settings");
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
                    case "Auto Boarding":
                        setting.IsToggled = AutoBoarding;
                        break;
                    case "Auto Deboarding":
                        setting.IsToggled = AutoDeboarding;
                        break;
                    case "Auto Refuel":
                        setting.IsToggled = AutoRefuel;
                        break;
                    case "Auto Reposition":
                        setting.IsToggled = AutoReposition;
                        break;
                    case "Reposition Delay":
                        setting.NumericValue = RepositionDelay;
                        break;
                    case "Call Catering":
                        setting.IsToggled = CallCatering;
                        break;
                    case "Cargo Loading Before Boarding":
                        setting.IsToggled = CargoLoadingBeforeBoarding;
                        break;
                }
            }
        }

        /// <summary>
        /// Resets the settings to their default values.
        /// </summary>
        public override void ResetToDefaults()
        {
            AutoBoarding = true;
            AutoDeboarding = true;
            AutoRefuel = true;
            AutoReposition = true;
            RepositionDelay = 3;
            CallCatering = true;
            CargoLoadingBeforeBoarding = true;

            UpdateSettings();
        }

        #endregion
    }
}
