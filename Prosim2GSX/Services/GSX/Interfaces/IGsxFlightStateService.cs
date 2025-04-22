using System;
using Prosim2GSX.Events;

namespace Prosim2GSX.Services.GSX.Interfaces
{
    /// <summary>
    /// Service for managing GSX flight states
    /// </summary>
    public interface IGsxFlightStateService
    {
        /// <summary>
        /// Current flight state
        /// </summary>
        FlightState CurrentFlightState { get; }

        /// <summary>
        /// Previous flight state
        /// </summary>
        FlightState PreviousFlightState { get; }

        /// <summary>
        /// Transition to a new state
        /// </summary>
        /// <param name="newState">The new state to transition to</param>
        void TransitionToState(FlightState newState);

        /// <summary>
        /// Check if a specific state transition is valid
        /// </summary>
        /// <param name="newState">The state to check</param>
        /// <returns>True if transition is valid</returns>
        bool CanTransitionTo(FlightState newState);

        /// <summary>
        /// Reset the state machine
        /// </summary>
        void ResetState();

        /// <summary>
        /// Subscribe to state changes
        /// </summary>
        /// <param name="handler">Handler to call when state changes</param>
        /// <returns>Subscription token</returns>
        SubscriptionToken SubscribeToStateChanges(Action<FlightState, FlightState> handler);

        /// <summary>
        /// Unsubscribe from state changes
        /// </summary>
        /// <param name="token">Subscription token</param>
        void UnsubscribeFromStateChanges(SubscriptionToken token);
    }
}