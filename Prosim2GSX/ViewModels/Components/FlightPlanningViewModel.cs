using Microsoft.Extensions.Logging;
using Prosim2GSX.Models;
using Prosim2GSX.ViewModels.Base;
using Prosim2GSX.ViewModels.Commands;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Prosim2GSX.ViewModels.Components
{
    /// <summary>
    /// ViewModel for flight planning settings
    /// </summary>
    public class FlightPlanningViewModel : ViewModelBase
    {
        private readonly ServiceModel _serviceModel;
        private ILoggerFactory _loggerFactory;
        private bool _isLoadingSettings;

        // SimBrief settings
        private string _simbriefId;
        private bool _useSimBrief;
        private bool _isTestingConnection;
        private string _testConnectionStatus;

        // Flight plan settings
        private bool _useEfbPlan;
        private bool _useMcduPlan;

        // ACARS settings
        private bool _useAcars;
        private bool _useHoppie;
        private bool _useSayIntentions;

        // Commands
        public ICommand TestSimbriefConnectionCommand { get; }

        /// <summary>
        /// Initialize a new instance of the FlightPlanningViewModel
        /// </summary>
        /// <param name="serviceModel">Service model for application state</param>
        public FlightPlanningViewModel(ServiceModel serviceModel)
        {
            _serviceModel = serviceModel ?? throw new ArgumentNullException(nameof(serviceModel));

            // Initialize commands
            TestSimbriefConnectionCommand = new AsyncRelayCommand(async _ => await TestSimbriefConnection(), _ => !IsTestingConnection && IsValidSimbriefId());

            // Load settings
            LoadSettings();
        }

        #region Properties

        /// <summary>
        /// Gets or sets the SimBrief pilot ID
        /// </summary>
        public string SimbriefId
        {
            get => _simbriefId;
            set
            {
                if (SetProperty(ref _simbriefId, value) && !_isLoadingSettings)
                {
                    // Don't auto-save - will be saved on lost focus or Enter key
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to use SimBrief for flight planning
        /// </summary>
        public bool UseSimBrief
        {
            get => _useSimBrief;
            set
            {
                if (SetProperty(ref _useSimBrief, value) && !_isLoadingSettings)
                {
                    _serviceModel.SetSetting("useSimBrief", value.ToString().ToLower());
                }
            }
        }

        /// <summary>
        /// Gets or sets whether a connection test is in progress
        /// </summary>
        public bool IsTestingConnection
        {
            get => _isTestingConnection;
            private set
            {
                if (SetProperty(ref _isTestingConnection, value))
                {
                    // Update commands that depend on this property
                    (TestSimbriefConnectionCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the connection test status message
        /// </summary>
        public string TestConnectionStatus
        {
            get => _testConnectionStatus;
            private set => SetProperty(ref _testConnectionStatus, value);
        }

        /// <summary>
        /// Gets or sets whether to use EFB flight plan
        /// </summary>
        public bool UseEfbPlan
        {
            get => _useEfbPlan;
            set
            {
                if (SetProperty(ref _useEfbPlan, value) && !_isLoadingSettings)
                {
                    if (value && !_useMcduPlan)
                    {
                        _serviceModel.SetSetting("flightPlanType", "EFB");

                        // Update the complementary property
                        _useMcduPlan = false;
                        OnPropertyChanged(nameof(UseMcduPlan));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to use MCDU flight plan
        /// </summary>
        public bool UseMcduPlan
        {
            get => _useMcduPlan;
            set
            {
                if (SetProperty(ref _useMcduPlan, value) && !_isLoadingSettings)
                {
                    if (value && !_useEfbPlan)
                    {
                        _serviceModel.SetSetting("flightPlanType", "MCDU");

                        // Update the complementary property
                        _useEfbPlan = false;
                        OnPropertyChanged(nameof(UseEfbPlan));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to use ACARS
        /// </summary>
        public bool UseAcars
        {
            get => _useAcars;
            set
            {
                if (SetProperty(ref _useAcars, value) && !_isLoadingSettings)
                {
                    _serviceModel.SetSetting("useAcars", value.ToString().ToLower());

                    // Update dependent properties
                    OnPropertyChanged(nameof(AcarsOptionsEnabled));
                }
            }
        }

        /// <summary>
        /// Gets whether ACARS options are enabled
        /// </summary>
        public bool AcarsOptionsEnabled => UseAcars;

        /// <summary>
        /// Gets or sets whether to use Hoppie ACARS
        /// </summary>
        public bool UseHoppie
        {
            get => _useHoppie;
            set
            {
                if (SetProperty(ref _useHoppie, value) && !_isLoadingSettings)
                {
                    if (value && AcarsOptionsEnabled)
                    {
                        _serviceModel.SetSetting("acarsNetwork", "Hoppie");

                        // Update the complementary property
                        _useSayIntentions = false;
                        OnPropertyChanged(nameof(UseSayIntentions));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to use Say Intentions ACARS
        /// </summary>
        public bool UseSayIntentions
        {
            get => _useSayIntentions;
            set
            {
                if (SetProperty(ref _useSayIntentions, value) && !_isLoadingSettings)
                {
                    if (value && AcarsOptionsEnabled)
                    {
                        _serviceModel.SetSetting("acarsNetwork", "SayIntentions");

                        // Update the complementary property
                        _useHoppie = false;
                        OnPropertyChanged(nameof(UseHoppie));
                    }
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
                // Load SimBrief settings
                _simbriefId = _serviceModel.SimBriefID;
                _useSimBrief = _serviceModel.GetSettingBool("useSimBrief", false);

                // Load flight plan settings
                string flightPlanType = _serviceModel.FlightPlanType;
                _useEfbPlan = flightPlanType == "EFB";
                _useMcduPlan = flightPlanType == "MCDU";

                // Load ACARS settings
                _useAcars = _serviceModel.UseAcars;
                string acarsNetwork = _serviceModel.AcarsNetwork;
                _useHoppie = acarsNetwork == "Hoppie";
                _useSayIntentions = acarsNetwork == "SayIntentions";

                // Initialize other properties
                _isTestingConnection = false;
                _testConnectionStatus = string.Empty;

                // Notify all properties have changed
                OnAllPropertiesChanged();
            }
            finally
            {
                _isLoadingSettings = false;
            }
        }

        /// <summary>
        /// Save the SimBrief ID
        /// </summary>
        public void SaveSimbriefId()
        {
            if (string.IsNullOrWhiteSpace(_simbriefId) || _simbriefId == "0" || !int.TryParse(_simbriefId, out _))
            {
                // Invalid ID
                TestConnectionStatus = "Invalid SimBrief ID. Please enter a numeric value.";
                return;
            }

            // Save the valid ID
            _serviceModel.SetSetting("pilotID", _simbriefId);

            // Clear any previous status
            TestConnectionStatus = string.Empty;

            // If we have a valid ID and there's a pending flight plan load, trigger a retry
            if (_serviceModel.IsValidSimbriefId())
            {
                Prosim2GSX.Events.EventAggregator.Instance.Publish(new Prosim2GSX.Events.RetryFlightPlanLoadEvent());
            }
        }

        /// <summary>
        /// Test the SimBrief connection
        /// </summary>
        private async Task TestSimbriefConnection()
        {
            if (!IsValidSimbriefId())
            {
                TestConnectionStatus = "Please enter a valid SimBrief ID first.";
                return;
            }

            IsTestingConnection = true;
            TestConnectionStatus = "Testing connection...";

            try
            {
                // Create a temporary FlightPlan object to test the connection
                var testPlan = new FlightPlan(_serviceModel, _loggerFactory.CreateLogger<FlightPlan>());
                var result = await Task.Run(() => testPlan.LoadWithValidation());

                if (result == Prosim2GSX.FlightPlan.LoadResult.Success)
                {
                    TestConnectionStatus = $"✓ Connected to SimBrief with ID: {_serviceModel.SimBriefID}\n" +
                                         $"Flight: {testPlan.Flight}\n" +
                                         $"Route: {testPlan.Origin} → {testPlan.Destination}";
                }
                else
                {
                    TestConnectionStatus = "✗ Failed to connect to SimBrief. Please check your ID and internet connection.";
                }
            }
            catch (Exception ex)
            {
                TestConnectionStatus = $"✗ Error testing connection: {ex.Message}";
            }
            finally
            {
                IsTestingConnection = false;
            }
        }

        /// <summary>
        /// Check if the current SimBrief ID is valid
        /// </summary>
        private bool IsValidSimbriefId()
        {
            return !string.IsNullOrWhiteSpace(_simbriefId) &&
                   _simbriefId != "0" &&
                   int.TryParse(_simbriefId, out _);
        }

        /// <summary>
        /// Notify that all properties have changed
        /// </summary>
        private void OnAllPropertiesChanged()
        {
            OnPropertyChanged(nameof(SimbriefId));
            OnPropertyChanged(nameof(UseSimBrief));
            OnPropertyChanged(nameof(IsTestingConnection));
            OnPropertyChanged(nameof(TestConnectionStatus));
            OnPropertyChanged(nameof(UseEfbPlan));
            OnPropertyChanged(nameof(UseMcduPlan));
            OnPropertyChanged(nameof(UseAcars));
            OnPropertyChanged(nameof(AcarsOptionsEnabled));
            OnPropertyChanged(nameof(UseHoppie));
            OnPropertyChanged(nameof(UseSayIntentions));
        }
    }
}
