using System;
using System.Collections.ObjectModel;
using Prosim2GSX.Models;
using Prosim2GSX.Services;

namespace Prosim2GSX.UI.EFB.ViewModels.Settings
{
    /// <summary>
    /// View model for flight planning settings.
    /// </summary>
    public class FlightPlanningSettingsViewModel : SettingsCategoryViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FlightPlanningSettingsViewModel"/> class.
        /// </summary>
        /// <param name="serviceModel">The service model.</param>
        /// <param name="logger">The logger.</param>
        public FlightPlanningSettingsViewModel(ServiceModel serviceModel, ILogger logger = null)
            : base(serviceModel, logger)
        {
            InitializeSettings();
        }

        /// <summary>
        /// Gets the title of the category.
        /// </summary>
        public override string Title => "Flight Planning";

        /// <summary>
        /// Gets the icon of the category.
        /// </summary>
        public override string Icon => "\uE8A5";

        #region Properties

        private string _flightPlanType;
        /// <summary>
        /// Gets or sets the flight plan type.
        /// </summary>
        public string FlightPlanType
        {
            get => _flightPlanType;
            set
            {
                if (SetProperty(ref _flightPlanType, value))
                {
                    ServiceModel.SetSetting("flightPlanType", value);
                }
            }
        }

        private bool _useAcars;
        /// <summary>
        /// Gets or sets a value indicating whether to use ACARS.
        /// </summary>
        public bool UseAcars
        {
            get => _useAcars;
            set
            {
                if (SetProperty(ref _useAcars, value))
                {
                    ServiceModel.SetSetting("useAcars", value.ToString().ToLower());
                    OnPropertyChanged(nameof(IsAcarsNetworkEnabled));
                    UpdateSettings();
                }
            }
        }

        private string _acarsNetwork;
        /// <summary>
        /// Gets or sets the ACARS network.
        /// </summary>
        public string AcarsNetwork
        {
            get => _acarsNetwork;
            set
            {
                if (SetProperty(ref _acarsNetwork, value))
                {
                    ServiceModel.SetSetting("acarsNetwork", value);
                }
            }
        }

        private string _simBriefID;
        /// <summary>
        /// Gets or sets the SimBrief ID.
        /// </summary>
        public string SimBriefID
        {
            get => _simBriefID;
            set
            {
                if (SetProperty(ref _simBriefID, value))
                {
                    ServiceModel.SetSetting("pilotID", value);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the ACARS network setting is enabled.
        /// </summary>
        public bool IsAcarsNetworkEnabled => UseAcars;

        #endregion

        #region Methods

        /// <summary>
        /// Initializes the settings.
        /// </summary>
        private void InitializeSettings()
        {
            Settings.Add(new SettingViewModel
            {
                Name = "Flight Plan Type",
                Description = "Select the source of flight plan data",
                Type = SettingType.Options,
                Options = new ObservableCollection<string> { "MCDU", "EFB" },
                SelectedOption = FlightPlanType,
                SelectedOptionChanged = value => FlightPlanType = value
            });

            Settings.Add(new SettingViewModel
            {
                Name = "Use ACARS",
                Description = "Enable ACARS integration for flight plans and loadsheets",
                Type = SettingType.Toggle,
                IsToggled = UseAcars,
                ToggledChanged = value => UseAcars = value
            });

            Settings.Add(new SettingViewModel
            {
                Name = "ACARS Network",
                Description = "Select the ACARS network to use",
                Type = SettingType.Options,
                Options = new ObservableCollection<string> { "Hoppie", "SayIntentions" },
                SelectedOption = AcarsNetwork,
                SelectedOptionChanged = value => AcarsNetwork = value,
                IsEnabled = IsAcarsNetworkEnabled
            });

            Settings.Add(new SettingViewModel
            {
                Name = "SimBrief ID",
                Description = "Your SimBrief pilot ID for flight plan retrieval",
                Type = SettingType.Text,
                TextValue = SimBriefID,
                TextChanged = value => SimBriefID = value
            });
        }

        /// <summary>
        /// Loads the settings from the service model.
        /// </summary>
        public override void LoadSettings()
        {
            try
            {
                FlightPlanType = ServiceModel.FlightPlanType;
                UseAcars = ServiceModel.UseAcars;
                AcarsNetwork = ServiceModel.AcarsNetwork;
                SimBriefID = ServiceModel.SimBriefID;

                UpdateSettings();
            }
            catch (Exception ex)
            {
                Logger?.Log(LogLevel.Error, "FlightPlanningSettingsViewModel", ex, "Error loading flight planning settings");
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
                    case "Flight Plan Type":
                        setting.SelectedOption = FlightPlanType;
                        break;
                    case "Use ACARS":
                        setting.IsToggled = UseAcars;
                        break;
                    case "ACARS Network":
                        setting.SelectedOption = AcarsNetwork;
                        setting.IsEnabled = IsAcarsNetworkEnabled;
                        break;
                    case "SimBrief ID":
                        setting.TextValue = SimBriefID;
                        break;
                }
            }
        }

        /// <summary>
        /// Resets the settings to their default values.
        /// </summary>
        public override void ResetToDefaults()
        {
            FlightPlanType = "MCDU";
            UseAcars = false;
            AcarsNetwork = "Hoppie";
            SimBriefID = "0";

            UpdateSettings();
        }

        #endregion
    }
}
