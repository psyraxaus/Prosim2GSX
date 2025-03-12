using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Prosim2GSX.Services;
using Prosim2GSX.Services.EventArgs;

namespace Prosim2GSX.UI.EFB.Controls
{
    /// <summary>
    /// Interaction logic for FlightPhaseIndicator.xaml
    /// </summary>
    public partial class FlightPhaseIndicator : UserControl, INotifyPropertyChanged
    {
        /// <summary>
        /// Identifies the CurrentPhase dependency property.
        /// </summary>
        public static readonly DependencyProperty CurrentPhaseProperty =
            DependencyProperty.Register(
                nameof(CurrentPhase),
                typeof(FlightPhase),
                typeof(FlightPhaseIndicator),
                new PropertyMetadata(FlightPhase.Preflight, OnCurrentPhaseChanged));

        private static void OnCurrentPhaseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlightPhaseIndicator control)
            {
                control._currentPhase = (FlightPhase)e.NewValue;
                control.UpdatePhaseIndicator();
                control.OnPropertyChanged(nameof(CurrentPhaseText));
            }
        }

        private readonly IFlightPhaseService _flightPhaseService;
        private readonly IEventAggregator _eventAggregator;
        
        private FlightPhase _currentPhase;
        private FlightPhase? _predictedNextPhase;
        private TimeSpan _timeInPhase;
        private TimeSpan? _estimatedTimeToNextPhase;
        private float _predictionConfidence;
        
        /// <summary>
        /// Event raised when a property changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        
        /// <summary>
        /// Gets or sets the current flight phase.
        /// </summary>
        public FlightPhase CurrentPhase
        {
            get => (FlightPhase)GetValue(CurrentPhaseProperty);
            set => SetValue(CurrentPhaseProperty, value);
        }

        /// <summary>
        /// Gets the current phase text.
        /// </summary>
        public string CurrentPhaseText => $"Current Phase: {GetPhaseDisplayName(_currentPhase)}";
        
        /// <summary>
        /// Gets the time in phase text.
        /// </summary>
        public string TimeInPhaseText => $"Time in Phase: {FormatTimeSpan(_timeInPhase)}";
        
        /// <summary>
        /// Gets the predicted next phase text.
        /// </summary>
        public string PredictedNextPhaseText => _predictedNextPhase.HasValue
            ? $"Predicted Next Phase: {GetPhaseDisplayName(_predictedNextPhase.Value)} ({_predictionConfidence:P0} confidence)"
            : string.Empty;
        
        /// <summary>
        /// Gets a value indicating whether there is a predicted next phase.
        /// </summary>
        public bool HasPredictedNextPhase => _predictedNextPhase.HasValue;
        
        /// <summary>
        /// Gets a value indicating whether there is an estimated time to the next phase.
        /// </summary>
        public bool HasEstimatedTimeToNextPhase => _estimatedTimeToNextPhase.HasValue;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="FlightPhaseIndicator"/> class.
        /// </summary>
        public FlightPhaseIndicator()
        {
            InitializeComponent();
            DataContext = this;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FlightPhaseIndicator"/> class.
        /// </summary>
        /// <param name="flightPhaseService">The flight phase service.</param>
        /// <param name="eventAggregator">The event aggregator.</param>
        public FlightPhaseIndicator(IFlightPhaseService flightPhaseService, IEventAggregator eventAggregator)
        {
            _flightPhaseService = flightPhaseService ?? throw new ArgumentNullException(nameof(flightPhaseService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            
            InitializeComponent();
            
            // Subscribe to flight phase events
            _flightPhaseService.PhaseChanged += OnPhaseChanged;
            _flightPhaseService.PredictedPhaseChanged += OnPredictedPhaseChanged;
            _eventAggregator.Subscribe<FlightPhaseChangedEventArgs>(OnFlightPhaseChangedEvent);
            _eventAggregator.Subscribe<PredictedPhaseChangedEventArgs>(OnPredictedPhaseChangedEvent);
            
            // Initialize with current phase
            _currentPhase = _flightPhaseService.CurrentPhase;
            CurrentPhase = _currentPhase;
            _predictedNextPhase = _flightPhaseService.PredictedNextPhase;
            _timeInPhase = _flightPhaseService.TimeInCurrentPhase;
            _predictionConfidence = _flightPhaseService.PredictionConfidence;
            _estimatedTimeToNextPhase = _flightPhaseService.GetEstimatedTimeToNextPhase();
            
            // Update UI
            UpdatePhaseIndicator();
            UpdatePredictionCountdown();
            
            // Start a timer to update the time in phase
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += OnTimerTick;
            timer.Start();
        }
        
        private void OnPhaseChanged(object sender, FlightPhaseChangedEventArgs e)
        {
            // Update current phase
            _currentPhase = e.NewPhase;
            CurrentPhase = e.NewPhase;
            _timeInPhase = TimeSpan.Zero;
            
            // Update UI
            UpdatePhaseIndicator();
            
            // Update properties
            OnPropertyChanged(nameof(CurrentPhaseText));
            OnPropertyChanged(nameof(TimeInPhaseText));
        }
        
        private void OnPredictedPhaseChanged(object sender, PredictedPhaseChangedEventArgs e)
        {
            // Update predicted phase
            _predictedNextPhase = e.PredictedPhase;
            _predictionConfidence = e.Confidence;
            _estimatedTimeToNextPhase = e.EstimatedTimeToChange;
            
            // Update UI
            UpdatePhaseIndicator();
            UpdatePredictionCountdown();
            
            // Update properties
            OnPropertyChanged(nameof(PredictedNextPhaseText));
            OnPropertyChanged(nameof(HasPredictedNextPhase));
            OnPropertyChanged(nameof(HasEstimatedTimeToNextPhase));
        }
        
        private void OnFlightPhaseChangedEvent(FlightPhaseChangedEventArgs e)
        {
            // This method is called when the event is published through the event aggregator
            OnPhaseChanged(this, e);
        }
        
        private void OnPredictedPhaseChangedEvent(PredictedPhaseChangedEventArgs e)
        {
            // This method is called when the event is published through the event aggregator
            OnPredictedPhaseChanged(this, e);
        }
        
        private void OnTimerTick(object sender, EventArgs e)
        {
            // Update time in phase
            _timeInPhase = _flightPhaseService.TimeInCurrentPhase;
            
            // Update UI
            OnPropertyChanged(nameof(TimeInPhaseText));
        }
        
        private void UpdatePhaseIndicator()
        {
            // Reset all phase items to default style
            ResetPhaseItems();
            
            // Set the current phase item to active style
            SetPhaseItemActive(_currentPhase);
            
            // Set the predicted phase item to predicted style
            if (_predictedNextPhase.HasValue)
            {
                SetPhaseItemPredicted(_predictedNextPhase.Value);
                
                // Set the connector between current and predicted phase to predicted style
                SetConnectorPredicted(_currentPhase, _predictedNextPhase.Value);
            }
        }
        
        private void UpdatePredictionCountdown()
        {
            if (_estimatedTimeToNextPhase.HasValue && _predictedNextPhase.HasValue)
            {
                // Set the countdown timer
                PredictionCountdown.Duration = _estimatedTimeToNextPhase.Value;
                PredictionCountdown.Reset();
                PredictionCountdown.Start();
                
                // Show the countdown timer
                PredictionCountdown.Visibility = Visibility.Visible;
            }
            else
            {
                // Hide the countdown timer
                PredictionCountdown.Visibility = Visibility.Collapsed;
            }
        }
        
        private void ResetPhaseItems()
        {
            // Reset all phase items to default style
            PreflightBorder.Style = (Style)FindResource("PhaseItemStyle");
            PreflightText.Style = (Style)FindResource("PhaseItemTextStyle");
            
            DepartureBorder.Style = (Style)FindResource("PhaseItemStyle");
            DepartureText.Style = (Style)FindResource("PhaseItemTextStyle");
            
            TaxiOutBorder.Style = (Style)FindResource("PhaseItemStyle");
            TaxiOutText.Style = (Style)FindResource("PhaseItemTextStyle");
            
            FlightBorder.Style = (Style)FindResource("PhaseItemStyle");
            FlightText.Style = (Style)FindResource("PhaseItemTextStyle");
            
            TaxiInBorder.Style = (Style)FindResource("PhaseItemStyle");
            TaxiInText.Style = (Style)FindResource("PhaseItemTextStyle");
            
            ArrivalBorder.Style = (Style)FindResource("PhaseItemStyle");
            ArrivalText.Style = (Style)FindResource("PhaseItemTextStyle");
            
            TurnaroundBorder.Style = (Style)FindResource("PhaseItemStyle");
            TurnaroundText.Style = (Style)FindResource("PhaseItemTextStyle");
            
            // Reset all connectors to default style
            PreflightToDepartureConnector.Style = (Style)FindResource("ConnectorStyle");
            DepartureToTaxiOutConnector.Style = (Style)FindResource("ConnectorStyle");
            TaxiOutToFlightConnector.Style = (Style)FindResource("ConnectorStyle");
            FlightToTaxiInConnector.Style = (Style)FindResource("ConnectorStyle");
            TaxiInToArrivalConnector.Style = (Style)FindResource("ConnectorStyle");
            ArrivalToTurnaroundConnector.Style = (Style)FindResource("ConnectorStyle");
        }
        
        private void SetPhaseItemActive(FlightPhase phase)
        {
            // Set the phase item to active style
            switch (phase)
            {
                case FlightPhase.Preflight:
                    PreflightBorder.Style = (Style)FindResource("ActivePhaseItemStyle");
                    PreflightText.Style = (Style)FindResource("ActivePhaseItemTextStyle");
                    break;
                case FlightPhase.Departure:
                    DepartureBorder.Style = (Style)FindResource("ActivePhaseItemStyle");
                    DepartureText.Style = (Style)FindResource("ActivePhaseItemTextStyle");
                    break;
                case FlightPhase.TaxiOut:
                    TaxiOutBorder.Style = (Style)FindResource("ActivePhaseItemStyle");
                    TaxiOutText.Style = (Style)FindResource("ActivePhaseItemTextStyle");
                    break;
                case FlightPhase.Flight:
                    FlightBorder.Style = (Style)FindResource("ActivePhaseItemStyle");
                    FlightText.Style = (Style)FindResource("ActivePhaseItemTextStyle");
                    break;
                case FlightPhase.TaxiIn:
                    TaxiInBorder.Style = (Style)FindResource("ActivePhaseItemStyle");
                    TaxiInText.Style = (Style)FindResource("ActivePhaseItemTextStyle");
                    break;
                case FlightPhase.Arrival:
                    ArrivalBorder.Style = (Style)FindResource("ActivePhaseItemStyle");
                    ArrivalText.Style = (Style)FindResource("ActivePhaseItemTextStyle");
                    break;
                case FlightPhase.Turnaround:
                    TurnaroundBorder.Style = (Style)FindResource("ActivePhaseItemStyle");
                    TurnaroundText.Style = (Style)FindResource("ActivePhaseItemTextStyle");
                    break;
            }
        }
        
        private void SetPhaseItemPredicted(FlightPhase phase)
        {
            // Set the phase item to predicted style
            switch (phase)
            {
                case FlightPhase.Preflight:
                    PreflightBorder.Style = (Style)FindResource("PredictedPhaseItemStyle");
                    PreflightText.Style = (Style)FindResource("PredictedPhaseItemTextStyle");
                    break;
                case FlightPhase.Departure:
                    DepartureBorder.Style = (Style)FindResource("PredictedPhaseItemStyle");
                    DepartureText.Style = (Style)FindResource("PredictedPhaseItemTextStyle");
                    break;
                case FlightPhase.TaxiOut:
                    TaxiOutBorder.Style = (Style)FindResource("PredictedPhaseItemStyle");
                    TaxiOutText.Style = (Style)FindResource("PredictedPhaseItemTextStyle");
                    break;
                case FlightPhase.Flight:
                    FlightBorder.Style = (Style)FindResource("PredictedPhaseItemStyle");
                    FlightText.Style = (Style)FindResource("PredictedPhaseItemTextStyle");
                    break;
                case FlightPhase.TaxiIn:
                    TaxiInBorder.Style = (Style)FindResource("PredictedPhaseItemStyle");
                    TaxiInText.Style = (Style)FindResource("PredictedPhaseItemTextStyle");
                    break;
                case FlightPhase.Arrival:
                    ArrivalBorder.Style = (Style)FindResource("PredictedPhaseItemStyle");
                    ArrivalText.Style = (Style)FindResource("PredictedPhaseItemTextStyle");
                    break;
                case FlightPhase.Turnaround:
                    TurnaroundBorder.Style = (Style)FindResource("PredictedPhaseItemStyle");
                    TurnaroundText.Style = (Style)FindResource("PredictedPhaseItemTextStyle");
                    break;
            }
        }
        
        private void SetConnectorActive(FlightPhase fromPhase, FlightPhase toPhase)
        {
            // Set the connector to active style
            if (fromPhase == FlightPhase.Preflight && toPhase == FlightPhase.Departure)
            {
                PreflightToDepartureConnector.Style = (Style)FindResource("ActiveConnectorStyle");
            }
            else if (fromPhase == FlightPhase.Departure && toPhase == FlightPhase.TaxiOut)
            {
                DepartureToTaxiOutConnector.Style = (Style)FindResource("ActiveConnectorStyle");
            }
            else if (fromPhase == FlightPhase.TaxiOut && toPhase == FlightPhase.Flight)
            {
                TaxiOutToFlightConnector.Style = (Style)FindResource("ActiveConnectorStyle");
            }
            else if (fromPhase == FlightPhase.Flight && toPhase == FlightPhase.TaxiIn)
            {
                FlightToTaxiInConnector.Style = (Style)FindResource("ActiveConnectorStyle");
            }
            else if (fromPhase == FlightPhase.TaxiIn && toPhase == FlightPhase.Arrival)
            {
                TaxiInToArrivalConnector.Style = (Style)FindResource("ActiveConnectorStyle");
            }
            else if (fromPhase == FlightPhase.Arrival && toPhase == FlightPhase.Turnaround)
            {
                ArrivalToTurnaroundConnector.Style = (Style)FindResource("ActiveConnectorStyle");
            }
        }
        
        private void SetConnectorPredicted(FlightPhase fromPhase, FlightPhase toPhase)
        {
            // Set the connector to predicted style
            if (fromPhase == FlightPhase.Preflight && toPhase == FlightPhase.Departure)
            {
                PreflightToDepartureConnector.Style = (Style)FindResource("PredictedConnectorStyle");
            }
            else if (fromPhase == FlightPhase.Departure && toPhase == FlightPhase.TaxiOut)
            {
                DepartureToTaxiOutConnector.Style = (Style)FindResource("PredictedConnectorStyle");
            }
            else if (fromPhase == FlightPhase.TaxiOut && toPhase == FlightPhase.Flight)
            {
                TaxiOutToFlightConnector.Style = (Style)FindResource("PredictedConnectorStyle");
            }
            else if (fromPhase == FlightPhase.Flight && toPhase == FlightPhase.TaxiIn)
            {
                FlightToTaxiInConnector.Style = (Style)FindResource("PredictedConnectorStyle");
            }
            else if (fromPhase == FlightPhase.TaxiIn && toPhase == FlightPhase.Arrival)
            {
                TaxiInToArrivalConnector.Style = (Style)FindResource("PredictedConnectorStyle");
            }
            else if (fromPhase == FlightPhase.Arrival && toPhase == FlightPhase.Turnaround)
            {
                ArrivalToTurnaroundConnector.Style = (Style)FindResource("PredictedConnectorStyle");
            }
        }
        
        private string GetPhaseDisplayName(FlightPhase phase)
        {
            switch (phase)
            {
                case FlightPhase.Preflight:
                    return "Preflight";
                case FlightPhase.Departure:
                    return "Departure";
                case FlightPhase.TaxiOut:
                    return "Taxi Out";
                case FlightPhase.Flight:
                    return "Flight";
                case FlightPhase.TaxiIn:
                    return "Taxi In";
                case FlightPhase.Arrival:
                    return "Arrival";
                case FlightPhase.Turnaround:
                    return "Turnaround";
                default:
                    return phase.ToString();
            }
        }
        
        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalHours >= 1)
            {
                return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m {timeSpan.Seconds}s";
            }
            else if (timeSpan.TotalMinutes >= 1)
            {
                return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
            }
            else
            {
                return $"{timeSpan.Seconds}s";
            }
        }
        
        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        /// <summary>
        /// Represents a flight phase.
        /// </summary>
        public enum FlightPhase
        {
            /// <summary>
            /// Preflight phase.
            /// </summary>
            Preflight,
            
            /// <summary>
            /// Departure phase.
            /// </summary>
            Departure,
            
            /// <summary>
            /// Taxi out phase.
            /// </summary>
            TaxiOut,
            
            /// <summary>
            /// Flight phase.
            /// </summary>
            Flight,
            
            /// <summary>
            /// Taxi in phase.
            /// </summary>
            TaxiIn,
            
            /// <summary>
            /// Arrival phase.
            /// </summary>
            Arrival,
            
            /// <summary>
            /// Turnaround phase.
            /// </summary>
            Turnaround
        }
    }
}
