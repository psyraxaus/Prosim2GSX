using Prosim2GSX.Events;
using Prosim2GSX.ViewModels.Base;
using Prosim2GSX.ViewModels.Commands;
using System;

namespace Prosim2GSX.ViewModels.Components
{
    /// <summary>
    /// ViewModel for the application header bar, managing flight information and navigation controls
    /// </summary>
    public class HeaderBarViewModel : ViewModelBase
    {
        #region Fields

        private string _flightNumber = "No Flight";
        private string _currentDate = DateTime.Now.ToString("dd.MM.yyyy");
        private SubscriptionToken _flightPlanChangedSubscription;
        private readonly System.Windows.Threading.DispatcherTimer _timer;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the current flight number to display
        /// </summary>
        public string FlightNumber
        {
            get => _flightNumber;
            private set => SetProperty(ref _flightNumber, value);
        }

        /// <summary>
        /// Gets or sets the formatted current date
        /// </summary>
        public string CurrentDate
        {
            get => _currentDate;
            private set => SetProperty(ref _currentDate, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command to show audio settings
        /// </summary>
        public RelayCommand ShowAudioSettingsCommand { get; }

        /// <summary>
        /// Command to show application settings
        /// </summary>
        public RelayCommand ShowSettingsCommand { get; }

        /// <summary>
        /// Command to show help information
        /// </summary>
        public RelayCommand ShowHelpCommand { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new instance of HeaderBarViewModel
        /// </summary>
        /// <param name="showAudioSettingsAction">Action to execute when audio settings button is clicked</param>
        /// <param name="showSettingsAction">Action to execute when settings button is clicked</param>
        /// <param name="showHelpAction">Action to execute when help button is clicked</param>
        public HeaderBarViewModel(Action showAudioSettingsAction = null, Action showSettingsAction = null, Action showHelpAction = null)
        {
            // Initialize commands with provided actions or empty actions if null
            ShowAudioSettingsCommand = new RelayCommand(_ => showAudioSettingsAction?.Invoke());
            ShowSettingsCommand = new RelayCommand(_ => showSettingsAction?.Invoke());
            ShowHelpCommand = new RelayCommand(_ => showHelpAction?.Invoke());

            // Initialize timer for date updates
            _timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += OnTimerTick;
            _timer.Start();

            // Subscribe to flight plan changes
            _flightPlanChangedSubscription = EventAggregator.Instance.Subscribe<FlightPlanChangedEvent>(OnFlightPlanChanged);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Updates the current date display when the timer ticks
        /// </summary>
        private void OnTimerTick(object sender, EventArgs e)
        {
            CurrentDate = DateTime.Now.ToString("dd.MM.yyyy");
        }

        /// <summary>
        /// Updates the flight number when flight plan changes
        /// </summary>
        private void OnFlightPlanChanged(FlightPlanChangedEvent evt)
        {
            FlightNumber = string.IsNullOrEmpty(evt.FlightNumber) ? "No Flight" : evt.FlightNumber;
        }

        /// <summary>
        /// Cleans up resources used by the ViewModel
        /// </summary>
        public void Cleanup()
        {
            // Stop the timer
            _timer.Stop();

            // Unsubscribe from events
            if (_flightPlanChangedSubscription != null)
            {
                EventAggregator.Instance.Unsubscribe<FlightPlanChangedEvent>(_flightPlanChangedSubscription);
                _flightPlanChangedSubscription = null;
            }
        }

        #endregion
    }
}
