using Prosim2GSX.Events;
using Prosim2GSX.Services.GSX.Interfaces;
using System;
using System.Collections.Generic;

namespace Prosim2GSX.Services.GSX.Implementation
{
    /// <summary>
    /// Implementation of flight state service
    /// </summary>
    public class GsxFlightStateService : IGsxFlightStateService
    {
        private FlightState _currentState = FlightState.PREFLIGHT;
        private FlightState _previousState = FlightState.PREFLIGHT;
        private readonly List<(SubscriptionToken Token, Action<FlightState, FlightState> Handler)> _handlers = new List<(SubscriptionToken, Action<FlightState, FlightState>)>();

        /// <inheritdoc/>
        public FlightState CurrentFlightState => _currentState;

        /// <inheritdoc/>
        public FlightState PreviousFlightState => _previousState;

        /// <inheritdoc/>
        public void TransitionToState(FlightState newState)
        {
            if (_currentState == newState)
                return;

            _previousState = _currentState;
            _currentState = newState;

            // Notify subscribers
            foreach (var (_, handler) in _handlers)
            {
                try
                {
                    handler(_previousState, _currentState);
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, nameof(GsxFlightStateService),
                        $"Error in state change handler: {ex.Message}");
                }
            }

            // Publish event
            EventAggregator.Instance.Publish(new FlightPhaseChangedEvent(_previousState, _currentState));

            Logger.Log(LogLevel.Information, nameof(GsxFlightStateService),
                $"State Change: {_previousState} -> {_currentState}");
        }

        /// <inheritdoc/>
        public bool CanTransitionTo(FlightState newState)
        {
            // Define valid transitions
            switch (_currentState)
            {
                case FlightState.PREFLIGHT:
                    return newState == FlightState.DEPARTURE || newState == FlightState.FLIGHT;

                case FlightState.DEPARTURE:
                    return newState == FlightState.TAXIOUT;

                case FlightState.TAXIOUT:
                    return newState == FlightState.FLIGHT;

                case FlightState.FLIGHT:
                    return newState == FlightState.TAXIIN;

                case FlightState.TAXIIN:
                    return newState == FlightState.ARRIVAL;

                case FlightState.ARRIVAL:
                    return newState == FlightState.TURNAROUND;

                case FlightState.TURNAROUND:
                    return newState == FlightState.DEPARTURE;

                default:
                    return false;
            }
        }

        /// <inheritdoc/>
        public void ResetState()
        {
            _previousState = _currentState;
            _currentState = FlightState.PREFLIGHT;

            // Publish event
            EventAggregator.Instance.Publish(new FlightPhaseChangedEvent(_previousState, _currentState));

            Logger.Log(LogLevel.Information, nameof(GsxFlightStateService),
                $"State reset to {_currentState}");
        }

        /// <inheritdoc/>
        public SubscriptionToken SubscribeToStateChanges(Action<FlightState, FlightState> handler)
        {
            var token = new SubscriptionToken();
            _handlers.Add((token, handler));
            return token;
        }

        /// <inheritdoc/>
        public void UnsubscribeFromStateChanges(SubscriptionToken token)
        {
            _handlers.RemoveAll(h => h.Token == token);
        }
    }
}