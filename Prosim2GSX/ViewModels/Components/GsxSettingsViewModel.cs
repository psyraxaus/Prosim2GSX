using Prosim2GSX.Models;
using Prosim2GSX.ViewModels.Base;
using Prosim2GSX.ViewModels.Commands;
using System;
using System.Globalization;
using System.Windows.Input;

namespace Prosim2GSX.ViewModels.Components
{
    /// <summary>
    /// ViewModel for GSX-specific settings
    /// </summary>
    public class GsxSettingsViewModel : ViewModelBase
    {
        private readonly ServiceModel _serviceModel;
        private bool _isLoadingSettings;

        // Boarding settings
        private bool _autoBoarding;
        private bool _autoDeboarding;
        private bool _disableCrew;

        // Refueling settings
        private bool _autoRefuel;
        private bool _saveFuel;
        private bool _zeroFuel;
        private string _refuelRate;
        private bool _isKgs; // true for KGS, false for LBS

        // Repositioning settings
        private bool _autoReposition;
        private string _repositionDelay;

        // Door settings
        private bool _openCargoDoors;
        private bool _openDoorCatering;

        // Jetway settings
        private bool _jetwayOnly;
        private bool _pcaOnlyJetway;

        // Other GSX settings
        private bool _callCatering;
        private bool _connectPCA;
        private bool _synchBypass;
        private bool _useActualPaxValue;
        private bool _saveHydraulicFluids;

        // Commands
        public ICommand ResetToDefaultsCommand { get; }

        /// <summary>
        /// Initialize a new instance of the GsxSettingsViewModel
        /// </summary>
        /// <param name="serviceModel">Service model for application state</param>
        public GsxSettingsViewModel(ServiceModel serviceModel)
        {
            _serviceModel = serviceModel ?? throw new ArgumentNullException(nameof(serviceModel));

            // Initialize commands
            ResetToDefaultsCommand = new RelayCommand(_ => ResetToDefaults());

            // Load settings
            LoadSettings();
        }

        #region Properties

        /// <summary>
        /// Gets or sets whether auto boarding is enabled
        /// </summary>
        public bool AutoBoarding
        {
            get => _autoBoarding;
            set
            {
                if (SetProperty(ref _autoBoarding, value) && !_isLoadingSettings)
                {
                    _serviceModel.SetSetting("autoBoarding", value.ToString().ToLower());
                }
            }
        }

        /// <summary>
        /// Gets or sets whether auto deboarding is enabled
        /// </summary>
        public bool AutoDeboarding
        {
            get => _autoDeboarding;
            set
            {
                if (SetProperty(ref _autoDeboarding, value) && !_isLoadingSettings)
                {
                    _serviceModel.SetSetting("autoDeboarding", value.ToString().ToLower());
                }
            }
        }

        /// <summary>
        /// Gets or sets whether crew boarding is disabled
        /// </summary>
        public bool DisableCrew
        {
            get => _disableCrew;
            set
            {
                if (SetProperty(ref _disableCrew, value) && !_isLoadingSettings)
                {
                    _serviceModel.SetSetting("disableCrew", value.ToString().ToLower());
                }
            }
        }

        /// <summary>
        /// Gets or sets whether auto refueling is enabled
        /// </summary>
        public bool AutoRefuel
        {
            get => _autoRefuel;
            set
            {
                if (SetProperty(ref _autoRefuel, value) && !_isLoadingSettings)
                {
                    _serviceModel.SetSetting("autoRefuel", value.ToString().ToLower());
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to save fuel state
        /// </summary>
        public bool SaveFuel
        {
            get => _saveFuel;
            set
            {
                if (SetProperty(ref _saveFuel, value) && !_isLoadingSettings)
                {
                    _serviceModel.SetSetting("setSaveFuel", value.ToString().ToLower());

                    // Zero fuel is exclusive with save fuel
                    if (value)
                    {
                        _zeroFuel = false;
                        OnPropertyChanged(nameof(ZeroFuel));
                        _serviceModel.SetSetting("setZeroFuel", "false");
                    }

                    // Update the enabled state of the zero fuel option
                    OnPropertyChanged(nameof(IsZeroFuelEnabled));
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to zero the fuel
        /// </summary>
        public bool ZeroFuel
        {
            get => _zeroFuel;
            set
            {
                if (SetProperty(ref _zeroFuel, value) && !_isLoadingSettings)
                {
                    _serviceModel.SetSetting("setZeroFuel", value.ToString().ToLower());
                }
            }
        }

        /// <summary>
        /// Gets whether the zero fuel option is enabled
        /// </summary>
        public bool IsZeroFuelEnabled => !SaveFuel;

        /// <summary>
        /// Gets or sets the refuel rate
        /// </summary>
        public string RefuelRate
        {
            get => _refuelRate;
            set
            {
                if (SetProperty(ref _refuelRate, value) && !_isLoadingSettings)
                {
                    if (float.TryParse(value, CultureInfo.InvariantCulture, out _))
                    {
                        _serviceModel.SetSetting("refuelRate", value);
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets whether refuel unit is KGS (true) or LBS (false)
        /// </summary>
        public bool IsKgs
        {
            get => _isKgs;
            set
            {
                if (SetProperty(ref _isKgs, value) && !_isLoadingSettings)
                {
                    // Convert the refuel rate if needed
                    if (float.TryParse(_refuelRate, CultureInfo.InvariantCulture, out float fuelRate))
                    {
                        if (value) // Converting to KGS
                        {
                            fuelRate /= Services.ServiceLocator.WeightConversion;
                            _serviceModel.SetSetting("refuelUnit", "KGS");
                        }
                        else // Converting to LBS
                        {
                            fuelRate *= Services.ServiceLocator.WeightConversion;
                            _serviceModel.SetSetting("refuelUnit", "LBS");
                        }

                        // Update the refuel rate
                        _refuelRate = fuelRate.ToString(CultureInfo.InvariantCulture);
                        _serviceModel.SetSetting("refuelRate", _refuelRate);
                        OnPropertyChanged(nameof(RefuelRate));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets whether auto repositioning is enabled
        /// </summary>
        public bool AutoReposition
        {
            get => _autoReposition;
            set
            {
                if (SetProperty(ref _autoReposition, value) && !_isLoadingSettings)
                {
                    _serviceModel.SetSetting("repositionPlane", value.ToString().ToLower());
                }
            }
        }

        /// <summary>
        /// Gets or sets the reposition delay in seconds
        /// </summary>
        public string RepositionDelay
        {
            get => _repositionDelay;
            set
            {
                if (SetProperty(ref _repositionDelay, value) && !_isLoadingSettings)
                {
                    if (float.TryParse(value, CultureInfo.InvariantCulture, out _))
                    {
                        _serviceModel.SetSetting("repositionDelay", value);
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to open cargo doors
        /// </summary>
        public bool OpenCargoDoors
        {
            get => _openCargoDoors;
            set
            {
                if (SetProperty(ref _openCargoDoors, value) && !_isLoadingSettings)
                {
                    _serviceModel.SetSetting("setOpenCargoDoors", value.ToString().ToLower());
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to open catering door
        /// </summary>
        public bool OpenDoorCatering
        {
            get => _openDoorCatering;
            set
            {
                if (SetProperty(ref _openDoorCatering, value) && !_isLoadingSettings)
                {
                    _serviceModel.SetSetting("setOpenAftDoorCatering", value.ToString().ToLower());
                }
            }
        }

        /// <summary>
        /// Gets or sets whether jetway only mode is enabled
        /// </summary>
        public bool JetwayOnly
        {
            get => _jetwayOnly;
            set
            {
                if (SetProperty(ref _jetwayOnly, value) && !_isLoadingSettings)
                {
                    _serviceModel.SetSetting("jetwayOnly", value.ToString().ToLower());
                }
            }
        }

        /// <summary>
        /// Gets or sets whether PCA is only available for jetways
        /// </summary>
        public bool PcaOnlyJetway
        {
            get => _pcaOnlyJetway;
            set
            {
                if (SetProperty(ref _pcaOnlyJetway, value) && !_isLoadingSettings)
                {
                    _serviceModel.SetSetting("pcaOnlyJetway", value.ToString().ToLower());
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to call catering
        /// </summary>
        public bool CallCatering
        {
            get => _callCatering;
            set
            {
                if (SetProperty(ref _callCatering, value) && !_isLoadingSettings)
                {
                    _serviceModel.SetSetting("callCatering", value.ToString().ToLower());
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to connect PCA
        /// </summary>
        public bool ConnectPCA
        {
            get => _connectPCA;
            set
            {
                if (SetProperty(ref _connectPCA, value) && !_isLoadingSettings)
                {
                    _serviceModel.SetSetting("connectPCA", value.ToString().ToLower());
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to use synchronization bypass
        /// </summary>
        public bool SynchBypass
        {
            get => _synchBypass;
            set
            {
                if (SetProperty(ref _synchBypass, value) && !_isLoadingSettings)
                {
                    _serviceModel.SetSetting("synchBypass", value.ToString().ToLower());
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to use actual passenger values
        /// </summary>
        public bool UseActualPaxValue
        {
            get => _useActualPaxValue;
            set
            {
                if (SetProperty(ref _useActualPaxValue, value) && !_isLoadingSettings)
                {
                    _serviceModel.SetSetting("useActualValue", value.ToString().ToLower());
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to save hydraulic fluids on arrival
        /// </summary>
        public bool SaveHydraulicFluids
        {
            get => _saveHydraulicFluids;
            set
            {
                if (SetProperty(ref _saveHydraulicFluids, value) && !_isLoadingSettings)
                {
                    _serviceModel.SetSetting("saveHydraulicFluids", value.ToString().ToLower());
                }
            }
        }

        #endregion

        /// <summary>
        /// Load settings from the service model
        /// </summary>
        private void LoadSettings()
        {
            _isLoadingSettings = true;

            try
            {
                // Load boarding settings
                _autoBoarding = _serviceModel.AutoBoarding;
                _autoDeboarding = _serviceModel.AutoDeboarding;
                _disableCrew = _serviceModel.DisableCrew;

                // Load refueling settings
                _autoRefuel = _serviceModel.AutoRefuel;
                _saveFuel = _serviceModel.SetSaveFuel;
                _zeroFuel = _serviceModel.SetZeroFuel;
                _refuelRate = _serviceModel.RefuelRate.ToString(CultureInfo.InvariantCulture);
                _isKgs = _serviceModel.RefuelUnit == "KGS";

                // Load repositioning settings
                _autoReposition = _serviceModel.RepositionPlane;
                _repositionDelay = _serviceModel.RepositionDelay.ToString(CultureInfo.InvariantCulture);

                // Load door settings
                _openCargoDoors = _serviceModel.SetOpenCargoDoors;
                _openDoorCatering = _serviceModel.SetOpenCateringDoor;

                // Load jetway settings
                _jetwayOnly = _serviceModel.JetwayOnly;
                _pcaOnlyJetway = _serviceModel.PcaOnlyJetways;

                // Load other GSX settings
                _callCatering = _serviceModel.CallCatering;
                _connectPCA = _serviceModel.ConnectPCA;
                _synchBypass = _serviceModel.SynchBypass;
                _useActualPaxValue = _serviceModel.UseActualPaxValue;
                _saveHydraulicFluids = _serviceModel.GetSettingBool("saveHydraulicFluids", false);

                // Notify all properties have changed
                OnAllPropertiesChanged();
            }
            finally
            {
                _isLoadingSettings = false;
            }
        }

        /// <summary>
        /// Reset settings to default values
        /// </summary>
        private void ResetToDefaults()
        {
            _isLoadingSettings = true;

            try
            {
                // Reset to default values
                AutoBoarding = false;
                AutoDeboarding = false;
                DisableCrew = false;
                AutoRefuel = false;
                SaveFuel = false;
                ZeroFuel = false;
                RefuelRate = "1000";
                IsKgs = true;
                AutoReposition = false;
                RepositionDelay = "120";
                OpenCargoDoors = false;
                OpenDoorCatering = false;
                JetwayOnly = false;
                PcaOnlyJetway = false;
                CallCatering = false;
                ConnectPCA = false;
                SynchBypass = false;
                UseActualPaxValue = false;
                SaveHydraulicFluids = false;
            }
            finally
            {
                _isLoadingSettings = false;
            }
        }

        /// <summary>
        /// Notify that all properties have changed
        /// </summary>
        private void OnAllPropertiesChanged()
        {
            OnPropertyChanged(nameof(AutoBoarding));
            OnPropertyChanged(nameof(AutoDeboarding));
            OnPropertyChanged(nameof(DisableCrew));
            OnPropertyChanged(nameof(AutoRefuel));
            OnPropertyChanged(nameof(SaveFuel));
            OnPropertyChanged(nameof(ZeroFuel));
            OnPropertyChanged(nameof(IsZeroFuelEnabled));
            OnPropertyChanged(nameof(RefuelRate));
            OnPropertyChanged(nameof(IsKgs));
            OnPropertyChanged(nameof(AutoReposition));
            OnPropertyChanged(nameof(RepositionDelay));
            OnPropertyChanged(nameof(OpenCargoDoors));
            OnPropertyChanged(nameof(OpenDoorCatering));
            OnPropertyChanged(nameof(JetwayOnly));
            OnPropertyChanged(nameof(PcaOnlyJetway));
            OnPropertyChanged(nameof(CallCatering));
            OnPropertyChanged(nameof(ConnectPCA));
            OnPropertyChanged(nameof(SynchBypass));
            OnPropertyChanged(nameof(UseActualPaxValue));
            OnPropertyChanged(nameof(SaveHydraulicFluids));
        }
    }
}