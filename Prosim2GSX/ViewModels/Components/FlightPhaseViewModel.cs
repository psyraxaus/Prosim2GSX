using Prosim2GSX.Events;
using Prosim2GSX.Services.GSX.Enums;
using Prosim2GSX.ViewModels.Base;
using System;
using System.Windows.Media;

namespace Prosim2GSX.ViewModels.Components
{
    /// <summary>
    /// ViewModel for displaying and managing flight phase information
    /// </summary>
    public class FlightPhaseViewModel : ViewModelBase
    {
        #region Fields

        private string _flightPhaseText = "PREFLIGHT";
        private Brush _flightPhaseBrush = new SolidColorBrush(Colors.RoyalBlue);
        private int _activePhaseIndex = 0;
        private SubscriptionToken _flightPhaseChangedSubscription;

        // Constants for phase count and names
        private const int PHASE_COUNT = 10;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the current flight phase display text
        /// </summary>
        public string FlightPhaseText
        {
            get => _flightPhaseText;
            set => SetProperty(ref _flightPhaseText, value);
        }

        /// <summary>
        /// Gets or sets the color brush for the flight phase text
        /// </summary>
        public Brush FlightPhaseBrush
        {
            get => _flightPhaseBrush;
            set => SetProperty(ref _flightPhaseBrush, value);
        }

        /// <summary>
        /// Gets or sets the index of the active phase in the progress bar (0-9)
        /// </summary>
        public int ActivePhaseIndex
        {
            get => _activePhaseIndex;
            set => SetProperty(ref _activePhaseIndex, value);
        }

        /// <summary>
        /// Gets whether each phase is active or not
        /// </summary>
        public bool IsPhase0Active => ActivePhaseIndex == 0;
        public bool IsPhase1Active => ActivePhaseIndex == 1;
        public bool IsPhase2Active => ActivePhaseIndex == 2;
        public bool IsPhase3Active => ActivePhaseIndex == 3;
        public bool IsPhase4Active => ActivePhaseIndex == 4;
        public bool IsPhase5Active => ActivePhaseIndex == 5;
        public bool IsPhase6Active => ActivePhaseIndex == 6;
        public bool IsPhase7Active => ActivePhaseIndex == 7;
        public bool IsPhase8Active => ActivePhaseIndex == 8;
        public bool IsPhase9Active => ActivePhaseIndex == 9;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new instance of the FlightPhaseViewModel
        /// </summary>
        public FlightPhaseViewModel()
        {
            // Subscribe to flight phase changes
            _flightPhaseChangedSubscription = EventAggregator.Instance.Subscribe<FlightPhaseChangedEvent>(OnFlightPhaseChanged);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Maps the FlightState enum to our expanded flight phases and updates the display
        /// </summary>
        /// <param name="state">The current flight state</param>
        public void UpdateFlightPhase(FlightState state)
        {
            switch (state)
            {
                case FlightState.PREFLIGHT:
                    SetPhaseDisplay("PREFLIGHT", Colors.RoyalBlue, 0);
                    break;

                case FlightState.DEPARTURE:
                    SetPhaseDisplay("DEPARTURE", Colors.RoyalBlue, 1);
                    break;

                case FlightState.TAXIOUT:
                    SetPhaseDisplay("TAXI OUT", Colors.Gold, 3);
                    break;

                case FlightState.FLIGHT:
                    SetPhaseDisplay("CRUISE", Colors.Green, 5);
                    break;

                case FlightState.TAXIIN:
                    SetPhaseDisplay("TAXI IN", Colors.Purple, 8);
                    break;

                case FlightState.ARRIVAL:
                    SetPhaseDisplay("ARRIVAL", Colors.Teal, 9);
                    break;

                case FlightState.TURNAROUND:
                    SetPhaseDisplay("TURNAROUND", Colors.Teal, 9);
                    break;

                default:
                    SetPhaseDisplay("UNKNOWN", Colors.Gray, 0);
                    break;
            }

            // Notify UI of property changes for phase indicators
            OnPropertyChanged(nameof(IsPhase0Active));
            OnPropertyChanged(nameof(IsPhase1Active));
            OnPropertyChanged(nameof(IsPhase2Active));
            OnPropertyChanged(nameof(IsPhase3Active));
            OnPropertyChanged(nameof(IsPhase4Active));
            OnPropertyChanged(nameof(IsPhase5Active));
            OnPropertyChanged(nameof(IsPhase6Active));
            OnPropertyChanged(nameof(IsPhase7Active));
            OnPropertyChanged(nameof(IsPhase8Active));
            OnPropertyChanged(nameof(IsPhase9Active));
        }

        /// <summary>
        /// Sets the phase display properties
        /// </summary>
        /// <param name="text">Text to display for the phase</param>
        /// <param name="color">Color for the phase text</param>
        /// <param name="index">Index of the active phase (0-9)</param>
        private void SetPhaseDisplay(string text, Color color, int index)
        {
            FlightPhaseText = text;
            FlightPhaseBrush = new SolidColorBrush(color);
            ActivePhaseIndex = index;
        }

        /// <summary>
        /// Handles flight phase changed events
        /// </summary>
        /// <param name="evt">The flight phase changed event</param>
        private void OnFlightPhaseChanged(FlightPhaseChangedEvent evt)
        {
            UpdateFlightPhase(evt.NewState);
        }

        /// <summary>
        /// Cleans up resources used by the ViewModel
        /// </summary>
        public void Cleanup()
        {
            // Unsubscribe from events
            if (_flightPhaseChangedSubscription != null)
            {
                EventAggregator.Instance.Unsubscribe<FlightPhaseChangedEvent>(_flightPhaseChangedSubscription);
                _flightPhaseChangedSubscription = null;
            }
        }

        #endregion
    }
}
