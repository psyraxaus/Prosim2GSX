using System;
using System.Collections.ObjectModel;
using Prosim2GSX.Models;
using Prosim2GSX.Services;

namespace Prosim2GSX.UI.EFB.ViewModels.Settings
{
    /// <summary>
    /// View model for audio control settings.
    /// </summary>
    public class AudioControlSettingsViewModel : SettingsCategoryViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioControlSettingsViewModel"/> class.
        /// </summary>
        /// <param name="serviceModel">The service model.</param>
        /// <param name="logger">The logger.</param>
        public AudioControlSettingsViewModel(ServiceModel serviceModel, ILogger logger = null)
            : base(serviceModel, logger)
        {
            InitializeSettings();
        }

        /// <summary>
        /// Gets the title of the category.
        /// </summary>
        public override string Title => "Audio Control";

        /// <summary>
        /// Gets the icon of the category.
        /// </summary>
        public override string Icon => "\uE767";

        #region Properties

        private bool _gsxVolumeControl;
        /// <summary>
        /// Gets or sets a value indicating whether to control GSX volume with INT knob.
        /// </summary>
        public bool GsxVolumeControl
        {
            get => _gsxVolumeControl;
            set
            {
                if (SetProperty(ref _gsxVolumeControl, value))
                {
                    ServiceModel.SetSetting("gsxVolumeControl", value.ToString().ToLower());
                }
            }
        }

        private bool _vhf1VolumeControl;
        /// <summary>
        /// Gets or sets a value indicating whether to control VHF1 application volume with VHF1 knob.
        /// </summary>
        public bool Vhf1VolumeControl
        {
            get => _vhf1VolumeControl;
            set
            {
                if (SetProperty(ref _vhf1VolumeControl, value))
                {
                    ServiceModel.SetSetting("vhf1VolumeControl", value.ToString().ToLower());
                    OnPropertyChanged(nameof(IsVhf1VolumeAppEnabled));
                    UpdateSettings();
                }
            }
        }

        private string _vhf1VolumeApp;
        /// <summary>
        /// Gets or sets the application to control with VHF1 knob.
        /// </summary>
        public string Vhf1VolumeApp
        {
            get => _vhf1VolumeApp;
            set
            {
                if (SetProperty(ref _vhf1VolumeApp, value))
                {
                    ServiceModel.SetSetting("vhf1VolumeApp", value);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the VHF1 volume app setting is enabled.
        /// </summary>
        public bool IsVhf1VolumeAppEnabled => Vhf1VolumeControl;

        private bool _vhf1LatchMute;
        /// <summary>
        /// Gets or sets a value indicating whether to latch mute state for VHF1 application.
        /// </summary>
        public bool Vhf1LatchMute
        {
            get => _vhf1LatchMute;
            set
            {
                if (SetProperty(ref _vhf1LatchMute, value))
                {
                    ServiceModel.SetSetting("vhf1LatchMute", value.ToString().ToLower());
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
                Name = "GSX Volume Control",
                Description = "Control GSX volume with INT knob",
                Type = SettingType.Toggle,
                IsToggled = GsxVolumeControl,
                ToggledChanged = value => GsxVolumeControl = value
            });

            Settings.Add(new SettingViewModel
            {
                Name = "VHF1 Volume Control",
                Description = "Control VHF1 application volume with VHF1 knob",
                Type = SettingType.Toggle,
                IsToggled = Vhf1VolumeControl,
                ToggledChanged = value => Vhf1VolumeControl = value
            });

            Settings.Add(new SettingViewModel
            {
                Name = "VHF1 Volume App",
                Description = "Application to control with VHF1 knob",
                Type = SettingType.Text,
                TextValue = Vhf1VolumeApp,
                TextChanged = value => Vhf1VolumeApp = value,
                IsEnabled = IsVhf1VolumeAppEnabled
            });

            Settings.Add(new SettingViewModel
            {
                Name = "VHF1 Latch Mute",
                Description = "Latch mute state for VHF1 application",
                Type = SettingType.Toggle,
                IsToggled = Vhf1LatchMute,
                ToggledChanged = value => Vhf1LatchMute = value
            });
        }

        /// <summary>
        /// Loads the settings from the service model.
        /// </summary>
        public override void LoadSettings()
        {
            try
            {
                GsxVolumeControl = ServiceModel.GsxVolumeControl;
                Vhf1VolumeControl = ServiceModel.Vhf1VolumeControl;
                Vhf1VolumeApp = ServiceModel.Vhf1VolumeApp;
                Vhf1LatchMute = ServiceModel.Vhf1LatchMute;

                UpdateSettings();
            }
            catch (Exception ex)
            {
                Logger?.Log(LogLevel.Error, "AudioControlSettingsViewModel", ex, "Error loading audio control settings");
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
                    case "GSX Volume Control":
                        setting.IsToggled = GsxVolumeControl;
                        break;
                    case "VHF1 Volume Control":
                        setting.IsToggled = Vhf1VolumeControl;
                        break;
                    case "VHF1 Volume App":
                        setting.TextValue = Vhf1VolumeApp;
                        setting.IsEnabled = IsVhf1VolumeAppEnabled;
                        break;
                    case "VHF1 Latch Mute":
                        setting.IsToggled = Vhf1LatchMute;
                        break;
                }
            }
        }

        /// <summary>
        /// Resets the settings to their default values.
        /// </summary>
        public override void ResetToDefaults()
        {
            GsxVolumeControl = true;
            Vhf1VolumeControl = false;
            Vhf1VolumeApp = "vPilot";
            Vhf1LatchMute = true;

            UpdateSettings();
        }

        #endregion
    }
}
