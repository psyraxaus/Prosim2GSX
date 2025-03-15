using System;
using System.Collections.ObjectModel;
using Prosim2GSX.Models;
using Prosim2GSX.Services;

namespace Prosim2GSX.UI.EFB.ViewModels.Settings
{
    /// <summary>
    /// View model for aircraft configuration settings.
    /// </summary>
    public class AircraftConfigSettingsViewModel : SettingsCategoryViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AircraftConfigSettingsViewModel"/> class.
        /// </summary>
        /// <param name="serviceModel">The service model.</param>
        /// <param name="logger">The logger.</param>
        public AircraftConfigSettingsViewModel(ServiceModel serviceModel, ILogger logger = null)
            : base(serviceModel, logger)
        {
            InitializeSettings();
        }

        /// <summary>
        /// Gets the title of the category.
        /// </summary>
        public override string Title => "Aircraft Configuration";

        /// <summary>
        /// Gets the icon of the category.
        /// </summary>
        public override string Icon => "\uE709";

        #region Properties

        private bool _openDoorCatering;
        /// <summary>
        /// Gets or sets a value indicating whether to open the catering door.
        /// </summary>
        public bool OpenDoorCatering
        {
            get => _openDoorCatering;
            set
            {
                if (SetProperty(ref _openDoorCatering, value))
                {
                    ServiceModel.SetSetting("setOpenAftDoorCatering", value.ToString().ToLower());
                }
            }
        }

        private bool _openCargoDoors;
        /// <summary>
        /// Gets or sets a value indicating whether to open cargo doors.
        /// </summary>
        public bool OpenCargoDoors
        {
            get => _openCargoDoors;
            set
            {
                if (SetProperty(ref _openCargoDoors, value))
                {
                    ServiceModel.SetSetting("setOpenCargoDoors", value.ToString().ToLower());
                }
            }
        }

        private bool _connectPCA;
        /// <summary>
        /// Gets or sets a value indicating whether to connect pre-conditioned air.
        /// </summary>
        public bool ConnectPCA
        {
            get => _connectPCA;
            set
            {
                if (SetProperty(ref _connectPCA, value))
                {
                    ServiceModel.SetSetting("connectPCA", value.ToString().ToLower());
                }
            }
        }

        private bool _pcaOnlyJetway;
        /// <summary>
        /// Gets or sets a value indicating whether to only connect PCA when jetway is used.
        /// </summary>
        public bool PcaOnlyJetway
        {
            get => _pcaOnlyJetway;
            set
            {
                if (SetProperty(ref _pcaOnlyJetway, value))
                {
                    ServiceModel.SetSetting("pcaOnlyJetway", value.ToString().ToLower());
                }
            }
        }

        private bool _jetwayOnly;
        /// <summary>
        /// Gets or sets a value indicating whether to always use jetway instead of stairs.
        /// </summary>
        public bool JetwayOnly
        {
            get => _jetwayOnly;
            set
            {
                if (SetProperty(ref _jetwayOnly, value))
                {
                    ServiceModel.SetSetting("jetwayOnly", value.ToString().ToLower());
                }
            }
        }

        private bool _disableCrew;
        /// <summary>
        /// Gets or sets a value indicating whether to disable automatic crew boarding.
        /// </summary>
        public bool DisableCrew
        {
            get => _disableCrew;
            set
            {
                if (SetProperty(ref _disableCrew, value))
                {
                    ServiceModel.SetSetting("disableCrew", value.ToString().ToLower());
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
                Name = "Open Door for Catering",
                Description = "Automatically open the catering door when catering service is called",
                Type = SettingType.Toggle,
                IsToggled = OpenDoorCatering,
                ToggledChanged = value => OpenDoorCatering = value
            });

            Settings.Add(new SettingViewModel
            {
                Name = "Open Cargo Doors",
                Description = "Automatically open cargo doors for loading/unloading",
                Type = SettingType.Toggle,
                IsToggled = OpenCargoDoors,
                ToggledChanged = value => OpenCargoDoors = value
            });

            Settings.Add(new SettingViewModel
            {
                Name = "Connect PCA",
                Description = "Automatically connect pre-conditioned air",
                Type = SettingType.Toggle,
                IsToggled = ConnectPCA,
                ToggledChanged = value => ConnectPCA = value
            });

            Settings.Add(new SettingViewModel
            {
                Name = "PCA Only with Jetway",
                Description = "Only connect PCA when jetway is used (not stairs)",
                Type = SettingType.Toggle,
                IsToggled = PcaOnlyJetway,
                ToggledChanged = value => PcaOnlyJetway = value
            });

            Settings.Add(new SettingViewModel
            {
                Name = "Jetway Only",
                Description = "Always use jetway instead of stairs",
                Type = SettingType.Toggle,
                IsToggled = JetwayOnly,
                ToggledChanged = value => JetwayOnly = value
            });

            Settings.Add(new SettingViewModel
            {
                Name = "Disable Crew Boarding",
                Description = "Disable automatic crew boarding",
                Type = SettingType.Toggle,
                IsToggled = DisableCrew,
                ToggledChanged = value => DisableCrew = value
            });
        }

        /// <summary>
        /// Loads the settings from the service model.
        /// </summary>
        public override void LoadSettings()
        {
            try
            {
                OpenDoorCatering = ServiceModel.SetOpenCateringDoor;
                OpenCargoDoors = ServiceModel.SetOpenCargoDoors;
                ConnectPCA = ServiceModel.ConnectPCA;
                PcaOnlyJetway = ServiceModel.PcaOnlyJetways;
                JetwayOnly = ServiceModel.JetwayOnly;
                DisableCrew = ServiceModel.DisableCrew;

                UpdateSettings();
            }
            catch (Exception ex)
            {
                Logger?.Log(LogLevel.Error, "AircraftConfigSettingsViewModel", ex, "Error loading aircraft configuration settings");
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
                    case "Open Door for Catering":
                        setting.IsToggled = OpenDoorCatering;
                        break;
                    case "Open Cargo Doors":
                        setting.IsToggled = OpenCargoDoors;
                        break;
                    case "Connect PCA":
                        setting.IsToggled = ConnectPCA;
                        break;
                    case "PCA Only with Jetway":
                        setting.IsToggled = PcaOnlyJetway;
                        break;
                    case "Jetway Only":
                        setting.IsToggled = JetwayOnly;
                        break;
                    case "Disable Crew Boarding":
                        setting.IsToggled = DisableCrew;
                        break;
                }
            }
        }

        /// <summary>
        /// Resets the settings to their default values.
        /// </summary>
        public override void ResetToDefaults()
        {
            OpenDoorCatering = false;
            OpenCargoDoors = true;
            ConnectPCA = true;
            PcaOnlyJetway = true;
            JetwayOnly = false;
            DisableCrew = true;

            UpdateSettings();
        }

        #endregion
    }
}
