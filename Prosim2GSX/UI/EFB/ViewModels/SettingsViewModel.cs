using CommunityToolkit.Mvvm.Input;
using Prosim2GSX.Models;
using Prosim2GSX.Services;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace Prosim2GSX.UI.EFB.ViewModels
{
    /// <summary>
    /// ViewModel for the Settings page.
    /// </summary>
    public class SettingsViewModel : BaseViewModel
    {
        private readonly ServiceModel _serviceModel;
        private readonly ILogger _logger;

        #region Properties

        // Flight Plan Settings
        public bool UseEfbFlightPlan
        {
            get => GetProperty<bool>(nameof(UseEfbFlightPlan));
            set
            {
                if (SetProperty(value, nameof(UseEfbFlightPlan)))
                {
                    if (value)
                    {
                        _serviceModel.SetSetting("flightPlanType", "EFB");
                    }
                    _logger?.Log(LogLevel.Debug, "SettingsViewModel", $"Flight plan type set to EFB: {value}");
                }
            }
        }

        public bool UseMcduFlightPlan
        {
            get => GetProperty<bool>(nameof(UseMcduFlightPlan));
            set
            {
                if (SetProperty(value, nameof(UseMcduFlightPlan)))
                {
                    if (value)
                    {
                        _serviceModel.SetSetting("flightPlanType", "MCDU");
                    }
                    _logger?.Log(LogLevel.Debug, "SettingsViewModel", $"Flight plan type set to MCDU: {value}");
                }
            }
        }

        public bool UseActualPaxValue
        {
            get => GetProperty<bool>(nameof(UseActualPaxValue));
            set
            {
                if (SetProperty(value, nameof(UseActualPaxValue)))
                {
                    _serviceModel.SetSetting("useActualValue", value.ToString().ToLower());
                    _logger?.Log(LogLevel.Debug, "SettingsViewModel", $"Use actual pax value: {value}");
                }
            }
        }

        public bool UseAcars
        {
            get => GetProperty<bool>(nameof(UseAcars));
            set
            {
                if (SetProperty(value, nameof(UseAcars)))
                {
                    _serviceModel.SetSetting("useAcars", value.ToString().ToLower());
                    _logger?.Log(LogLevel.Debug, "SettingsViewModel", $"Use ACARS: {value}");
                    
                    // Update UI state
                    OnPropertyChanged(nameof(IsAcarsNetworkSelectionEnabled));
                }
            }
        }

        public bool UseHoppieAcars
        {
            get => GetProperty<bool>(nameof(UseHoppieAcars));
            set
            {
                if (SetProperty(value, nameof(UseHoppieAcars)))
                {
                    if (value)
                    {
                        _serviceModel.SetSetting("acarsNetwork", "Hoppie");
                    }
                    _logger?.Log(LogLevel.Debug, "SettingsViewModel", $"ACARS network set to Hoppie: {value}");
                }
            }
        }

        public bool UseSayIntentionsAcars
        {
            get => GetProperty<bool>(nameof(UseSayIntentionsAcars));
            set
            {
                if (SetProperty(value, nameof(UseSayIntentionsAcars)))
                {
                    if (value)
                    {
                        _serviceModel.SetSetting("acarsNetwork", "SayIntentions");
                    }
                    _logger?.Log(LogLevel.Debug, "SettingsViewModel", $"ACARS network set to SayIntentions: {value}");
                }
            }
        }

        public bool IsAcarsNetworkSelectionEnabled => UseAcars;

        // Prosim Settings
        public bool SaveHydraulicFluids
        {
            get => GetProperty<bool>(nameof(SaveHydraulicFluids));
            set
            {
                if (SetProperty(value, nameof(SaveHydraulicFluids)))
                {
                    _serviceModel.SetSetting("saveHydraulicFluids", value.ToString().ToLower());
                    _logger?.Log(LogLevel.Debug, "SettingsViewModel", $"Save hydraulic fluids: {value}");
                }
            }
        }

        // Service Calls
        public bool RepositionPlane
        {
            get => GetProperty<bool>(nameof(RepositionPlane));
            set
            {
                if (SetProperty(value, nameof(RepositionPlane)))
                {
                    _serviceModel.SetSetting("repositionPlane", value.ToString().ToLower());
                    _logger?.Log(LogLevel.Debug, "SettingsViewModel", $"Reposition plane: {value}");
                }
            }
        }

        public string RepositionDelay
        {
            get => GetProperty<string>(nameof(RepositionDelay));
            set
            {
                if (SetProperty(value, nameof(RepositionDelay)))
                {
                    if (float.TryParse(value, CultureInfo.InvariantCulture, out float delay))
                    {
                        _serviceModel.SetSetting("repositionDelay", delay.ToString(CultureInfo.InvariantCulture));
                        _logger?.Log(LogLevel.Debug, "SettingsViewModel", $"Reposition delay: {delay}");
                    }
                }
            }
        }

        public bool AutoRefuel
        {
            get => GetProperty<bool>(nameof(AutoRefuel));
            set
            {
                if (SetProperty(value, nameof(AutoRefuel)))
                {
                    _serviceModel.SetSetting("autoRefuel", value.ToString().ToLower());
                    _logger?.Log(LogLevel.Debug, "SettingsViewModel", $"Auto refuel: {value}");
                }
            }
        }

        public bool ZeroFuel
        {
            get => GetProperty<bool>(nameof(ZeroFuel));
            set
            {
                if (SetProperty(value, nameof(ZeroFuel)))
                {
                    _serviceModel.SetSetting("setZeroFuel", value.ToString().ToLower());
                    _logger?.Log(LogLevel.Debug, "SettingsViewModel", $"Zero fuel: {value}");
                }
            }
        }

        public bool SaveFuel
        {
            get => GetProperty<bool>(nameof(SaveFuel));
            set
            {
                if (SetProperty(value, nameof(SaveFuel)))
                {
                    _serviceModel.SetSetting("setSaveFuel", value.ToString().ToLower());
                    _logger?.Log(LogLevel.Debug, "SettingsViewModel", $"Save fuel: {value}");
                    
                    // If save fuel is enabled, disable zero fuel
                    if (value)
                    {
                        ZeroFuel = false;
                    }
                    
                    // Update UI state
                    OnPropertyChanged(nameof(IsZeroFuelEnabled));
                }
            }
        }

        public bool IsZeroFuelEnabled => !SaveFuel;

        public bool CallCatering
        {
            get => GetProperty<bool>(nameof(CallCatering));
            set
            {
                if (SetProperty(value, nameof(CallCatering)))
                {
                    _serviceModel.SetSetting("callCatering", value.ToString().ToLower());
                    _logger?.Log(LogLevel.Debug, "SettingsViewModel", $"Call catering: {value}");
                }
            }
        }

        public bool OpenDoorCatering
        {
            get => GetProperty<bool>(nameof(OpenDoorCatering));
            set
            {
                if (SetProperty(value, nameof(OpenDoorCatering)))
                {
                    _serviceModel.SetSetting("setOpenAftDoorCatering", value.ToString().ToLower());
                    _logger?.Log(LogLevel.Debug, "SettingsViewModel", $"Open door catering: {value}");
                }
            }
        }

        public bool OpenCargoDoors
        {
            get => GetProperty<bool>(nameof(OpenCargoDoors));
            set
            {
                if (SetProperty(value, nameof(OpenCargoDoors)))
                {
                    _serviceModel.SetSetting("setOpenCargoDoors", value.ToString().ToLower());
                    _logger?.Log(LogLevel.Debug, "SettingsViewModel", $"Open cargo doors: {value}");
                }
            }
        }

        public bool CargoLoadingBeforeBoarding
        {
            get => GetProperty<bool>(nameof(CargoLoadingBeforeBoarding));
            set
            {
                if (SetProperty(value, nameof(CargoLoadingBeforeBoarding)))
                {
                    _serviceModel.SetSetting("cargoLoadingBeforeBoarding", value.ToString().ToLower());
                    _logger?.Log(LogLevel.Debug, "SettingsViewModel", $"Cargo loading before boarding: {value}");
                }
            }
        }

        public bool AutoBoard
        {
            get => GetProperty<bool>(nameof(AutoBoard));
            set
            {
                if (SetProperty(value, nameof(AutoBoard)))
                {
                    _serviceModel.SetSetting("autoBoarding", value.ToString().ToLower());
                    _logger?.Log(LogLevel.Debug, "SettingsViewModel", $"Auto board: {value}");
                }
            }
        }

        public bool AutoDeboard
        {
            get => GetProperty<bool>(nameof(AutoDeboard));
            set
            {
                if (SetProperty(value, nameof(AutoDeboard)))
                {
                    _serviceModel.SetSetting("autoDeboarding", value.ToString().ToLower());
                    _logger?.Log(LogLevel.Debug, "SettingsViewModel", $"Auto deboard: {value}");
                }
            }
        }

        public bool DisableCrewBoarding
        {
            get => GetProperty<bool>(nameof(DisableCrewBoarding));
            set
            {
                if (SetProperty(value, nameof(DisableCrewBoarding)))
                {
                    _serviceModel.SetSetting("disableCrew", value.ToString().ToLower());
                    _logger?.Log(LogLevel.Debug, "SettingsViewModel", $"Disable crew boarding: {value}");
                }
            }
        }

        public string RefuelRate
        {
            get => GetProperty<string>(nameof(RefuelRate));
            set
            {
                if (SetProperty(value, nameof(RefuelRate)))
                {
                    if (float.TryParse(value, CultureInfo.InvariantCulture, out float rate))
                    {
                        _serviceModel.SetSetting("refuelRate", rate.ToString(CultureInfo.InvariantCulture));
                        _logger?.Log(LogLevel.Debug, "SettingsViewModel", $"Refuel rate: {rate}");
                    }
                }
            }
        }

        public bool UseKgsUnit
        {
            get => GetProperty<bool>(nameof(UseKgsUnit));
            set
            {
                if (SetProperty(value, nameof(UseKgsUnit)))
                {
                    if (value)
                    {
                        // Convert from LBS to KGS if needed
                        if (_serviceModel.RefuelUnit == "LBS" && float.TryParse(RefuelRate, CultureInfo.InvariantCulture, out float rate))
                        {
                            rate /= ProsimController.weightConversion;
                            RefuelRate = rate.ToString(CultureInfo.InvariantCulture);
                        }
                        
                        _serviceModel.SetSetting("refuelUnit", "KGS");
                        _logger?.Log(LogLevel.Debug, "SettingsViewModel", "Refuel unit set to KGS");
                    }
                }
            }
        }

        public bool UseLbsUnit
        {
            get => GetProperty<bool>(nameof(UseLbsUnit));
            set
            {
                if (SetProperty(value, nameof(UseLbsUnit)))
                {
                    if (value)
                    {
                        // Convert from KGS to LBS if needed
                        if (_serviceModel.RefuelUnit == "KGS" && float.TryParse(RefuelRate, CultureInfo.InvariantCulture, out float rate))
                        {
                            rate *= ProsimController.weightConversion;
                            RefuelRate = rate.ToString(CultureInfo.InvariantCulture);
                        }
                        
                        _serviceModel.SetSetting("refuelUnit", "LBS");
                        _logger?.Log(LogLevel.Debug, "SettingsViewModel", "Refuel unit set to LBS");
                    }
                }
            }
        }

        // Ground Handling
        public bool AutoConnect
        {
            get => GetProperty<bool>(nameof(AutoConnect));
            set
            {
                if (SetProperty(value, nameof(AutoConnect)))
                {
                    _serviceModel.SetSetting("autoConnect", value.ToString().ToLower());
                    _logger?.Log(LogLevel.Debug, "SettingsViewModel", $"Auto connect: {value}");
                }
            }
        }

        public bool JetwayOnly
        {
            get => GetProperty<bool>(nameof(JetwayOnly));
            set
            {
                if (SetProperty(value, nameof(JetwayOnly)))
                {
                    _serviceModel.SetSetting("jetwayOnly", value.ToString().ToLower());
                    _logger?.Log(LogLevel.Debug, "SettingsViewModel", $"Jetway only: {value}");
                }
            }
        }

        public bool ConnectPCA
        {
            get => GetProperty<bool>(nameof(ConnectPCA));
            set
            {
                if (SetProperty(value, nameof(ConnectPCA)))
                {
                    _serviceModel.SetSetting("connectPCA", value.ToString().ToLower());
                    _logger?.Log(LogLevel.Debug, "SettingsViewModel", $"Connect PCA: {value}");
                }
            }
        }

        public bool PcaOnlyJetway
        {
            get => GetProperty<bool>(nameof(PcaOnlyJetway));
            set
            {
                if (SetProperty(value, nameof(PcaOnlyJetway)))
                {
                    _serviceModel.SetSetting("pcaOnlyJetway", value.ToString().ToLower());
                    _logger?.Log(LogLevel.Debug, "SettingsViewModel", $"PCA only jetway: {value}");
                }
            }
        }

        public bool SynchBypass
        {
            get => GetProperty<bool>(nameof(SynchBypass));
            set
            {
                if (SetProperty(value, nameof(SynchBypass)))
                {
                    _serviceModel.SetSetting("synchBypass", value.ToString().ToLower());
                    _logger?.Log(LogLevel.Debug, "SettingsViewModel", $"Synch bypass: {value}");
                }
            }
        }

        // Volume Control and UI Settings
        public bool UseEfbUi
        {
            get => GetProperty<bool>(nameof(UseEfbUi));
            set
            {
                if (SetProperty(value, nameof(UseEfbUi)))
                {
                    _serviceModel.SetSetting("useEfbUi", value.ToString().ToLower());
                    _logger?.Log(LogLevel.Debug, "SettingsViewModel", $"Use EFB UI: {value}");
                    
                    // Show a message to inform the user that the change will take effect after restart
                    MessageBox.Show(
                        "The UI change will take effect after restarting the application.",
                        "UI Change",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }

        public bool GsxVolumeControl
        {
            get => GetProperty<bool>(nameof(GsxVolumeControl));
            set
            {
                if (SetProperty(value, nameof(GsxVolumeControl)))
                {
                    _serviceModel.SetSetting("gsxVolumeControl", value.ToString().ToLower());
                    _logger?.Log(LogLevel.Debug, "SettingsViewModel", $"GSX volume control: {value}");
                }
            }
        }

        public bool Vhf1VolumeControl
        {
            get => GetProperty<bool>(nameof(Vhf1VolumeControl));
            set
            {
                if (SetProperty(value, nameof(Vhf1VolumeControl)))
                {
                    _serviceModel.SetSetting("vhf1VolumeControl", value.ToString().ToLower());
                    _logger?.Log(LogLevel.Debug, "SettingsViewModel", $"VHF1 volume control: {value}");
                    
                    // Update UI state
                    OnPropertyChanged(nameof(IsVhf1AppEnabled));
                }
            }
        }

        public bool Vhf1LatchMute
        {
            get => GetProperty<bool>(nameof(Vhf1LatchMute));
            set
            {
                if (SetProperty(value, nameof(Vhf1LatchMute)))
                {
                    _serviceModel.SetSetting("vhf1LatchMute", value.ToString().ToLower());
                    _logger?.Log(LogLevel.Debug, "SettingsViewModel", $"VHF1 latch mute: {value}");
                }
            }
        }

        public string Vhf1VolumeApp
        {
            get => GetProperty<string>(nameof(Vhf1VolumeApp));
            set
            {
                if (SetProperty(value, nameof(Vhf1VolumeApp)))
                {
                    _serviceModel.SetSetting("vhf1VolumeApp", value);
                    _logger?.Log(LogLevel.Debug, "SettingsViewModel", $"VHF1 volume app: {value}");
                }
            }
        }

        public bool IsVhf1AppEnabled => Vhf1VolumeControl;

        #endregion

        #region Commands

        public ICommand NavigateBackCommand { get; }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
        /// </summary>
        /// <param name="serviceModel">The service model.</param>
        /// <param name="navigationService">The navigation service.</param>
        /// <param name="logger">The logger.</param>
        public SettingsViewModel(ServiceModel serviceModel, Navigation.EFBNavigationService navigationService, ILogger logger = null)
        {
            _serviceModel = serviceModel ?? throw new ArgumentNullException(nameof(serviceModel));
            _logger = logger;

            // Initialize commands
            NavigateBackCommand = new RelayCommand(() => navigationService.GoBack());

            // Load settings from service model
            LoadSettings();
        }

        /// <summary>
        /// Loads settings from the service model.
        /// </summary>
        private void LoadSettings()
        {
            // Flight Plan Settings
            SetProperty(_serviceModel.FlightPlanType == "EFB", nameof(UseEfbFlightPlan));
            SetProperty(_serviceModel.FlightPlanType == "MCDU", nameof(UseMcduFlightPlan));
            SetProperty(_serviceModel.UseActualPaxValue, nameof(UseActualPaxValue));
            SetProperty(_serviceModel.UseAcars, nameof(UseAcars));
            SetProperty(_serviceModel.AcarsNetwork == "Hoppie", nameof(UseHoppieAcars));
            SetProperty(_serviceModel.AcarsNetwork == "SayIntentions", nameof(UseSayIntentionsAcars));

            // Prosim Settings
            SetProperty(_serviceModel.SetSaveHydraulicFluids, nameof(SaveHydraulicFluids));

            // Service Calls
            SetProperty(_serviceModel.RepositionPlane, nameof(RepositionPlane));
            SetProperty(_serviceModel.RepositionDelay.ToString(CultureInfo.InvariantCulture), nameof(RepositionDelay));
            SetProperty(_serviceModel.AutoRefuel, nameof(AutoRefuel));
            SetProperty(_serviceModel.SetZeroFuel, nameof(ZeroFuel));
            SetProperty(_serviceModel.SetSaveFuel, nameof(SaveFuel));
            SetProperty(_serviceModel.CallCatering, nameof(CallCatering));
            SetProperty(_serviceModel.SetOpenCateringDoor, nameof(OpenDoorCatering));
            SetProperty(_serviceModel.SetOpenCargoDoors, nameof(OpenCargoDoors));
            SetProperty(_serviceModel.CargoLoadingBeforeBoarding, nameof(CargoLoadingBeforeBoarding));
            SetProperty(_serviceModel.AutoBoarding, nameof(AutoBoard));
            SetProperty(_serviceModel.AutoDeboarding, nameof(AutoDeboard));
            SetProperty(_serviceModel.DisableCrew, nameof(DisableCrewBoarding));
            SetProperty(_serviceModel.RefuelRate.ToString(CultureInfo.InvariantCulture), nameof(RefuelRate));
            SetProperty(_serviceModel.RefuelUnit == "KGS", nameof(UseKgsUnit));
            SetProperty(_serviceModel.RefuelUnit == "LBS", nameof(UseLbsUnit));

            // Ground Handling
            SetProperty(_serviceModel.AutoConnect, nameof(AutoConnect));
            SetProperty(_serviceModel.JetwayOnly, nameof(JetwayOnly));
            SetProperty(_serviceModel.ConnectPCA, nameof(ConnectPCA));
            SetProperty(_serviceModel.PcaOnlyJetways, nameof(PcaOnlyJetway));
            SetProperty(_serviceModel.SynchBypass, nameof(SynchBypass));

            // Volume Control and UI Settings
            SetProperty(_serviceModel.UseEfbUi, nameof(UseEfbUi));
            SetProperty(_serviceModel.GsxVolumeControl, nameof(GsxVolumeControl));
            SetProperty(_serviceModel.Vhf1VolumeControl, nameof(Vhf1VolumeControl));
            SetProperty(_serviceModel.Vhf1LatchMute, nameof(Vhf1LatchMute));
            SetProperty(_serviceModel.Vhf1VolumeApp, nameof(Vhf1VolumeApp));
        }
    }
}
