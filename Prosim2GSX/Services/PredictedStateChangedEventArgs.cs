using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Event arguments for predicted state changes
    /// </summary>
    public class PredictedStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the previous predicted state
        /// </summary>
        public FlightState? PreviousPrediction { get; }
        
        /// <summary>
        /// Gets the new predicted state
        /// </summary>
        public FlightState NewPrediction { get; }
        
        /// <summary>
        /// Gets the confidence level of the prediction (0.0 to 1.0)
        /// </summary>
        public float Confidence { get; }
        
        /// <summary>
        /// Gets the timestamp of the prediction
        /// </summary>
        public DateTime Timestamp { get; }
        
        /// <summary>
        /// Initializes a new instance of the PredictedStateChangedEventArgs class
        /// </summary>
        public PredictedStateChangedEventArgs(FlightState? previousPrediction, FlightState newPrediction, float confidence)
        {
            PreviousPrediction = previousPrediction;
            NewPrediction = newPrediction;
            Confidence = confidence;
            Timestamp = DateTime.Now;
        }
    }
}
