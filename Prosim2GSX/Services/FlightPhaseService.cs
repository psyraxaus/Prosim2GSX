using System;
using System.Collections.Generic;
using System.Linq;
using Prosim2GSX.Services.EventArgs;
using Prosim2GSX.UI.EFB.Controls;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Service for flight phase management and prediction.
    /// </summary>
    public class FlightPhaseService : IFlightPhaseService
    {
        private readonly IGSXStateManager _stateManager;
        private readonly IEventAggregator _eventAggregator;
        
        private FlightPhaseIndicator.FlightPhase _currentPhase;
        private FlightPhaseIndicator.FlightPhase? _predictedNextPhase;
        private float _predictionConfidence;
        private DateTime _currentPhaseEnteredAt;
        
        // Historical data for phase duration estimation
        private readonly Dictionary<FlightPhaseIndicator.FlightPhase, List<TimeSpan>> _phaseDurations = 
            new Dictionary<FlightPhaseIndicator.FlightPhase, List<TimeSpan>>();
        
        // Maximum number of historical durations to keep per phase
        private const int MaxHistoricalDurations = 10;
        
        /// <summary>
        /// Gets the current flight phase.
        /// </summary>
        public FlightPhaseIndicator.FlightPhase CurrentPhase => _currentPhase;
        
        /// <summary>
        /// Gets the predicted next flight phase, if available.
        /// </summary>
        public FlightPhaseIndicator.FlightPhase? PredictedNextPhase => _predictedNextPhase;
        
        /// <summary>
        /// Gets the confidence level of the prediction (0.0 to 1.0).
        /// </summary>
        public float PredictionConfidence => _predictionConfidence;
        
        /// <summary>
        /// Gets the time spent in the current phase.
        /// </summary>
        public TimeSpan TimeInCurrentPhase => DateTime.Now - _currentPhaseEnteredAt;
        
        /// <summary>
        /// Gets the timestamp when the current phase was entered.
        /// </summary>
        public DateTime CurrentPhaseEnteredAt => _currentPhaseEnteredAt;
        
        /// <summary>
        /// Event raised when the flight phase changes.
        /// </summary>
        public event EventHandler<FlightPhaseChangedEventArgs> PhaseChanged;
        
        /// <summary>
        /// Event raised when the predicted next phase changes.
        /// </summary>
        public event EventHandler<PredictedPhaseChangedEventArgs> PredictedPhaseChanged;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="FlightPhaseService"/> class.
        /// </summary>
        /// <param name="stateManager">The GSX state manager.</param>
        /// <param name="eventAggregator">The event aggregator.</param>
        public FlightPhaseService(IGSXStateManager stateManager, IEventAggregator eventAggregator)
        {
            _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            
            // Initialize phase durations dictionary
            foreach (FlightPhaseIndicator.FlightPhase phase in Enum.GetValues(typeof(FlightPhaseIndicator.FlightPhase)))
            {
                _phaseDurations[phase] = new List<TimeSpan>();
            }
        }
        
        /// <summary>
        /// Initializes the flight phase service.
        /// </summary>
        public void Initialize()
        {
            // Subscribe to state manager events
            _stateManager.StateChanged += OnStateChanged;
            _stateManager.PredictedStateChanged += OnPredictedStateChanged;
            
            // Initialize current phase based on state manager
            _currentPhase = MapFlightStateToPhase(_stateManager.CurrentState);
            _currentPhaseEnteredAt = _stateManager.CurrentStateEnteredAt;
            
            // Initialize predicted phase if available
            if (_stateManager.PredictedNextState.HasValue)
            {
                var prediction = _stateManager.PredictNextState(new AircraftParameters());
                _predictedNextPhase = MapFlightStateToPhase(prediction.PredictedState);
                _predictionConfidence = prediction.Confidence;
            }
            
            Logger.Log(LogLevel.Information, "FlightPhaseService:Initialize", 
                $"Flight phase service initialized with current phase: {_currentPhase}");
        }
        
        /// <summary>
        /// Checks if the current phase is the specified phase.
        /// </summary>
        /// <param name="phase">The phase to check.</param>
        /// <returns>True if the current phase is the specified phase, false otherwise.</returns>
        public bool IsInPhase(FlightPhaseIndicator.FlightPhase phase)
        {
            return _currentPhase == phase;
        }
        
        /// <summary>
        /// Predicts the next flight phase based on current aircraft parameters.
        /// </summary>
        /// <returns>The predicted next phase and confidence level.</returns>
        public (FlightPhaseIndicator.FlightPhase PredictedPhase, float Confidence) PredictNextPhase()
        {
            // Use the state manager's prediction
            var prediction = _stateManager.PredictNextState(new AircraftParameters());
            var predictedPhase = MapFlightStateToPhase(prediction.PredictedState);
            
            // Update internal state
            if (_predictedNextPhase != predictedPhase || Math.Abs(_predictionConfidence - prediction.Confidence) > 0.1f)
            {
                var previousPrediction = _predictedNextPhase;
                _predictedNextPhase = predictedPhase;
                _predictionConfidence = prediction.Confidence;
                
                // Estimate time to next phase
                var estimatedTime = GetEstimatedTimeToNextPhase();
                
                // Raise event
                OnPredictedPhaseChanged(previousPrediction, predictedPhase, prediction.Confidence, estimatedTime);
            }
            
            return (predictedPhase, prediction.Confidence);
        }
        
        /// <summary>
        /// Gets the estimated time until the next phase change.
        /// </summary>
        /// <returns>The estimated time until the next phase change, or null if not available.</returns>
        public TimeSpan? GetEstimatedTimeToNextPhase()
        {
            // If we don't have a predicted next phase, we can't estimate time
            if (!_predictedNextPhase.HasValue)
                return null;
            
            // If we don't have historical data for the current phase, we can't estimate time
            if (_phaseDurations[_currentPhase].Count == 0)
                return null;
            
            // Calculate average duration of the current phase based on historical data
            var averageDuration = TimeSpan.FromTicks(
                (long)_phaseDurations[_currentPhase].Average(d => d.Ticks));
            
            // Estimate remaining time
            var timeSpentInCurrentPhase = TimeInCurrentPhase;
            if (timeSpentInCurrentPhase >= averageDuration)
                return TimeSpan.Zero;
            
            return averageDuration - timeSpentInCurrentPhase;
        }
        
        /// <summary>
        /// Maps a GSXStateManager.FlightState to a FlightPhaseIndicator.FlightPhase.
        /// </summary>
        /// <param name="state">The GSXStateManager.FlightState to map.</param>
        /// <returns>The corresponding FlightPhaseIndicator.FlightPhase.</returns>
        public FlightPhaseIndicator.FlightPhase MapFlightStateToPhase(FlightState state)
        {
            switch (state)
            {
                case FlightState.PREFLIGHT:
                    return FlightPhaseIndicator.FlightPhase.Preflight;
                case FlightState.DEPARTURE:
                    return FlightPhaseIndicator.FlightPhase.Departure;
                case FlightState.TAXIOUT:
                    return FlightPhaseIndicator.FlightPhase.TaxiOut;
                case FlightState.FLIGHT:
                    return FlightPhaseIndicator.FlightPhase.Flight;
                case FlightState.TAXIIN:
                    return FlightPhaseIndicator.FlightPhase.TaxiIn;
                case FlightState.ARRIVAL:
                    return FlightPhaseIndicator.FlightPhase.Arrival;
                case FlightState.TURNAROUND:
                    return FlightPhaseIndicator.FlightPhase.Turnaround;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown flight state");
            }
        }
        
        /// <summary>
        /// Maps a FlightPhaseIndicator.FlightPhase to a GSXStateManager.FlightState.
        /// </summary>
        /// <param name="phase">The FlightPhaseIndicator.FlightPhase to map.</param>
        /// <returns>The corresponding GSXStateManager.FlightState.</returns>
        public FlightState MapPhaseToFlightState(FlightPhaseIndicator.FlightPhase phase)
        {
            switch (phase)
            {
                case FlightPhaseIndicator.FlightPhase.Preflight:
                    return FlightState.PREFLIGHT;
                case FlightPhaseIndicator.FlightPhase.Departure:
                    return FlightState.DEPARTURE;
                case FlightPhaseIndicator.FlightPhase.TaxiOut:
                    return FlightState.TAXIOUT;
                case FlightPhaseIndicator.FlightPhase.Flight:
                    return FlightState.FLIGHT;
                case FlightPhaseIndicator.FlightPhase.TaxiIn:
                    return FlightState.TAXIIN;
                case FlightPhaseIndicator.FlightPhase.Arrival:
                    return FlightState.ARRIVAL;
                case FlightPhaseIndicator.FlightPhase.Turnaround:
                    return FlightState.TURNAROUND;
                default:
                    throw new ArgumentOutOfRangeException(nameof(phase), phase, "Unknown flight phase");
            }
        }
        
        private void OnStateChanged(object sender, StateChangedEventArgs e)
        {
            var previousPhase = _currentPhase;
            var newPhase = MapFlightStateToPhase(e.NewState);
            var timestamp = DateTime.Now;
            var previousPhaseDuration = timestamp - _currentPhaseEnteredAt;
            
            // Update current phase
            _currentPhase = newPhase;
            _currentPhaseEnteredAt = timestamp;
            
            // Record phase duration for historical data
            RecordPhaseDuration(previousPhase, previousPhaseDuration);
            
            // Raise event
            OnPhaseChanged(previousPhase, newPhase, timestamp, previousPhaseDuration, "State changed");
            
            // Update prediction
            PredictNextPhase();
            
            Logger.Log(LogLevel.Information, "FlightPhaseService:OnStateChanged", 
                $"Flight phase changed from {previousPhase} to {newPhase}");
        }
        
        private void OnPredictedStateChanged(object sender, PredictedStateChangedEventArgs e)
        {
            if (e.NewPrediction == null)
                return;
                
            var previousPrediction = _predictedNextPhase;
            var newPrediction = MapFlightStateToPhase(e.NewPrediction.Value);
            
            // Update predicted phase
            _predictedNextPhase = newPrediction;
            _predictionConfidence = e.Confidence;
            
            // Estimate time to next phase
            var estimatedTime = GetEstimatedTimeToNextPhase();
            
            // Raise event
            OnPredictedPhaseChanged(previousPrediction, newPrediction, e.Confidence, estimatedTime);
            
            Logger.Log(LogLevel.Information, "FlightPhaseService:OnPredictedStateChanged", 
                $"Predicted flight phase changed from {previousPrediction} to {newPrediction} with confidence {e.Confidence:P0}");
        }
        
        private void RecordPhaseDuration(FlightPhaseIndicator.FlightPhase phase, TimeSpan duration)
        {
            // Add duration to historical data
            _phaseDurations[phase].Add(duration);
            
            // Limit the number of historical durations
            if (_phaseDurations[phase].Count > MaxHistoricalDurations)
            {
                _phaseDurations[phase].RemoveAt(0);
            }
        }
        
        protected virtual void OnPhaseChanged(
            FlightPhaseIndicator.FlightPhase previousPhase, 
            FlightPhaseIndicator.FlightPhase newPhase, 
            DateTime timestamp, 
            TimeSpan previousPhaseDuration, 
            string reason)
        {
            var args = new FlightPhaseChangedEventArgs(
                previousPhase, 
                newPhase, 
                timestamp, 
                previousPhaseDuration, 
                reason);
                
            PhaseChanged?.Invoke(this, args);
            
            // Also publish through event aggregator
            _eventAggregator.Publish(args);
        }
        
        protected virtual void OnPredictedPhaseChanged(
            FlightPhaseIndicator.FlightPhase? previousPrediction, 
            FlightPhaseIndicator.FlightPhase newPrediction, 
            float confidence,
            TimeSpan? estimatedTimeToChange)
        {
            var args = new PredictedPhaseChangedEventArgs(
                previousPrediction, 
                newPrediction, 
                confidence, 
                estimatedTimeToChange);
                
            PredictedPhaseChanged?.Invoke(this, args);
            
            // Also publish through event aggregator
            _eventAggregator.Publish(args);
        }
    }
}
