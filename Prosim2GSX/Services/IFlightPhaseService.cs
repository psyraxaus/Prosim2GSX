using System;
using Prosim2GSX.Services.EventArgs;
using Prosim2GSX.UI.EFB.Controls;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for the flight phase service.
    /// </summary>
    public interface IFlightPhaseService
    {
        /// <summary>
        /// Gets the current flight phase.
        /// </summary>
        FlightPhaseIndicator.FlightPhase CurrentPhase { get; }

        /// <summary>
        /// Gets the predicted next flight phase, if available.
        /// </summary>
        FlightPhaseIndicator.FlightPhase? PredictedNextPhase { get; }

        /// <summary>
        /// Gets the confidence level of the prediction (0.0 to 1.0).
        /// </summary>
        float PredictionConfidence { get; }

        /// <summary>
        /// Gets the time spent in the current phase.
        /// </summary>
        TimeSpan TimeInCurrentPhase { get; }

        /// <summary>
        /// Gets the timestamp when the current phase was entered.
        /// </summary>
        DateTime CurrentPhaseEnteredAt { get; }

        /// <summary>
        /// Event raised when the flight phase changes.
        /// </summary>
        event EventHandler<FlightPhaseChangedEventArgs> PhaseChanged;

        /// <summary>
        /// Event raised when the predicted next phase changes.
        /// </summary>
        event EventHandler<PredictedPhaseChangedEventArgs> PredictedPhaseChanged;

        /// <summary>
        /// Initializes the flight phase service.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Checks if the current phase is the specified phase.
        /// </summary>
        /// <param name="phase">The phase to check.</param>
        /// <returns>True if the current phase is the specified phase, false otherwise.</returns>
        bool IsInPhase(FlightPhaseIndicator.FlightPhase phase);

        /// <summary>
        /// Predicts the next flight phase based on current aircraft parameters.
        /// </summary>
        /// <returns>The predicted next phase and confidence level.</returns>
        (FlightPhaseIndicator.FlightPhase PredictedPhase, float Confidence) PredictNextPhase();

        /// <summary>
        /// Gets the estimated time until the next phase change.
        /// </summary>
        /// <returns>The estimated time until the next phase change, or null if not available.</returns>
        TimeSpan? GetEstimatedTimeToNextPhase();

        /// <summary>
        /// Maps a GSXStateManager.FlightState to a FlightPhaseIndicator.FlightPhase.
        /// </summary>
        /// <param name="state">The GSXStateManager.FlightState to map.</param>
        /// <returns>The corresponding FlightPhaseIndicator.FlightPhase.</returns>
        FlightPhaseIndicator.FlightPhase MapFlightStateToPhase(FlightState state);

        /// <summary>
        /// Maps a FlightPhaseIndicator.FlightPhase to a GSXStateManager.FlightState.
        /// </summary>
        /// <param name="phase">The FlightPhaseIndicator.FlightPhase to map.</param>
        /// <returns>The corresponding GSXStateManager.FlightState.</returns>
        FlightState MapPhaseToFlightState(FlightPhaseIndicator.FlightPhase phase);
    }
}
