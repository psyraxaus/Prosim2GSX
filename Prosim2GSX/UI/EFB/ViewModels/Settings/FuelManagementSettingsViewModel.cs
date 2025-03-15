using System;
using System.Collections.ObjectModel;
using Prosim2GSX.Models;
using Prosim2GSX.Services;

namespace Prosim2GSX.UI.EFB.ViewModels.Settings
{
    /// <summary>
    /// View model for fuel management settings.
    /// </summary>
    public class FuelManagementSettingsViewModel : SettingsCategoryViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FuelManagementSettingsViewModel"/> class.
        /// </summary>
        /// <param name="serviceModel">The service model.</param>
        /// <param name="logger">The logger.</param>
        public FuelManagementSettingsViewModel(ServiceModel serviceModel, ILogger logger = null)
            : base(serviceModel, logger)
        {
            InitializeSettings();
        }

        /// <summary>
        /// Gets the title of the category.
        /// </summary>
        public override string Title => "Fuel Management";

        /// <summary>
        /// Gets the icon of the category.
        /// </summary>
        public override string Icon => "\uE945";

        #region Properties

        private float _refuelRate;
        /// <summary>
        /// Gets or sets the refuel rate.
        /// </summary>
        public float RefuelRate
        {
            get => _refuelRate;
            set
            {
                if (SetProperty(ref _refuelRate, value))
                {
                    ServiceModel.SetSetting("refuelRate", value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
            }
        }

        private string _refuelUnit;
        /// <summary>
        /// Gets or sets the refuel unit.
        /// </summary>
        public string RefuelUnit
        {
            get => _refuelUnit;
            set
            {
                if (SetProperty(ref _refuelUnit, value))
                {
                    ServiceModel.SetSetting("refuelUnit", value);
                }
            }
        }

        private bool _saveFuel;
        /// <summary>
        /// Gets or sets a value indicating whether to save fuel state between sessions.
        /// </summary>
        public bool SaveFuel
        {
            get => _saveFuel;
            set
            {
                if (SetProperty(ref _saveFuel, value))
                {
                    ServiceModel.SetSetting("setSaveFuel", value.ToString().ToLower());
                    if (value)
                    {
                        ZeroFuel = false;
                    }
                    OnPropertyChanged(nameof(IsZeroFuelEnabled));
                    UpdateSettings();
                }
            }
        }

        private bool _zeroFuel;
        /// <summary>
        /// Gets or sets a value indicating whether to start with zero fuel.
        /// </summary>
        public bool ZeroFuel
        {
            get => _zeroFuel;
            set
            {
                if (SetProperty(ref _zeroFuel, value))
                {
                    ServiceModel.SetSetting("setZeroFuel", value.ToString().ToLower());
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the zero fuel setting is enabled.
        /// </summary>
        public bool IsZeroFuelEnabled => !SaveFuel;

        private bool _useActualPaxValue;
        /// <summary>
        /// Gets or sets a value indicating whether to use actual passenger count instead of percentage.
        /// </summary>
        public bool UseActualPaxValue
        {
            get => _useActualPaxValue;
            set
            {
                if (SetProperty(ref _useActualPaxValue, value))
                {
                    ServiceModel.SetSetting("useActualValue", value.ToString().ToLower());
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
                Name = "Refuel Rate",
                Description = "Rate of refueling in selected units per minute",
                Type = SettingType.Numeric,
                NumericValue = RefuelRate,
                NumericChanged = value => RefuelRate = value
            });

            Settings.Add(new SettingViewModel
            {
                Name = "Refuel Unit",
                Description = "Unit of measurement for fuel",
                Type = SettingType.Options,
                Options = new ObservableCollection<string> { "KGS", "LBS" },
                SelectedOption = RefuelUnit,
                SelectedOptionChanged = value => RefuelUnit = value
            });

            Settings.Add(new SettingViewModel
            {
                Name = "Save Fuel",
                Description = "Save fuel state between sessions",
                Type = SettingType.Toggle,
                IsToggled = SaveFuel,
                ToggledChanged = value => SaveFuel = value
            });

            Settings.Add(new SettingViewModel
            {
                Name = "Zero Fuel",
                Description = "Start with zero fuel",
                Type = SettingType.Toggle,
                IsToggled = ZeroFuel,
                ToggledChanged = value => ZeroFuel = value,
                IsEnabled = IsZeroFuelEnabled
            });

            Settings.Add(new SettingViewModel
            {
                Name = "Use Actual Pax Value",
                Description = "Use actual passenger count instead of percentage",
                Type = SettingType.Toggle,
                IsToggled = UseActualPaxValue,
                ToggledChanged = value => UseActualPaxValue = value
            });
        }

        /// <summary>
        /// Loads the settings from the service model.
        /// </summary>
        public override void LoadSettings()
        {
            try
            {
                RefuelRate = ServiceModel.RefuelRate;
                RefuelUnit = ServiceModel.RefuelUnit;
                SaveFuel = ServiceModel.SetSaveFuel;
                ZeroFuel = ServiceModel.SetZeroFuel;
                UseActualPaxValue = ServiceModel.UseActualPaxValue;

                UpdateSettings();
            }
            catch (Exception ex)
            {
                Logger?.Log(LogLevel.Error, "FuelManagementSettingsViewModel", ex, "Error loading fuel management settings");
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
                    case "Refuel Rate":
                        setting.NumericValue = RefuelRate;
                        break;
                    case "Refuel Unit":
                        setting.SelectedOption = RefuelUnit;
                        break;
                    case "Save Fuel":
                        setting.IsToggled = SaveFuel;
                        break;
                    case "Zero Fuel":
                        setting.IsToggled = ZeroFuel;
                        setting.IsEnabled = IsZeroFuelEnabled;
                        break;
                    case "Use Actual Pax Value":
                        setting.IsToggled = UseActualPaxValue;
                        break;
                }
            }
        }

        /// <summary>
        /// Resets the settings to their default values.
        /// </summary>
        public override void ResetToDefaults()
        {
            RefuelRate = 28;
            RefuelUnit = "KGS";
            SaveFuel = false;
            ZeroFuel = false;
            UseActualPaxValue = true;

            UpdateSettings();
        }

        #endregion
    }
}
