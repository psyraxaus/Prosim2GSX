using System;
using Prosim2GSX.UI.EFB.Controls;

namespace Prosim2GSX.Services.EventArgs
{
    /// <summary>
    /// Event arguments for predicted flight phase changes.
    /// </summary>
    public class PredictedPhaseChangedEventArgs : System.EventArgs
    {
        /// <summary>
        /// Gets the previous predicted flight phase, if any.
        /// </summary>
        public FlightPhaseIndicator.FlightPhase? PreviousPredictedPhase { get; }

        /// <summary>
        /// Gets the new predicted flight phase.
        /// </summary>
        public FlightPhaseIndicator.FlightPhase PredictedPhase { get; }

        /// <summary>
        /// Gets the confidence level of the prediction (0.0 to 1.0).
        /// </summary>
        public float Confidence { get; }

        /// <summary>
        /// Gets the estimated time until the predicted phase change.
        /// </summary>
        public TimeSpan? EstimatedTimeToChange { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PredictedPhaseChangedEventArgs"/> class.
        /// </summary>
        /// <param name="previousPredictedPhase">The previous predicted flight phase, if any.</param>
        /// <param name="predictedPhase">The new predicted flight phase.</param>
        /// <param name="confidence">The confidence level of the prediction (0.0 to 1.0).</param>
        /// <param name="estimatedTimeToChange">The estimated time until the predicted phase change, if available.</param>
        public PredictedPhaseChangedEventArgs(
            FlightPhaseIndicator.FlightPhase? previousPredictedPhase,
            FlightPhaseIndicator.FlightPhase predictedPhase,
            float confidence,
            TimeSpan? estimatedTimeToChange = null)
        {
            PreviousPredictedPhase = previousPredictedPhase;
            PredictedPhase = predictedPhase;
            Confidence = confidence;
            EstimatedTimeToChange = estimatedTimeToChange;
        }
    }
}
