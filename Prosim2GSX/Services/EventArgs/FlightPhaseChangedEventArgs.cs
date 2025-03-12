using System;
using Prosim2GSX.UI.EFB.Controls;

namespace Prosim2GSX.Services.EventArgs
{
    /// <summary>
    /// Event arguments for flight phase changes.
    /// </summary>
    public class FlightPhaseChangedEventArgs : System.EventArgs
    {
        /// <summary>
        /// Gets the previous flight phase.
        /// </summary>
        public FlightPhaseIndicator.FlightPhase PreviousPhase { get; }

        /// <summary>
        /// Gets the new flight phase.
        /// </summary>
        public FlightPhaseIndicator.FlightPhase NewPhase { get; }

        /// <summary>
        /// Gets the timestamp when the phase change occurred.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Gets the duration spent in the previous phase.
        /// </summary>
        public TimeSpan PreviousPhaseDuration { get; }

        /// <summary>
        /// Gets the reason for the phase change, if available.
        /// </summary>
        public string Reason { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FlightPhaseChangedEventArgs"/> class.
        /// </summary>
        /// <param name="previousPhase">The previous flight phase.</param>
        /// <param name="newPhase">The new flight phase.</param>
        /// <param name="timestamp">The timestamp when the phase change occurred.</param>
        /// <param name="previousPhaseDuration">The duration spent in the previous phase.</param>
        /// <param name="reason">The reason for the phase change, if available.</param>
        public FlightPhaseChangedEventArgs(
            FlightPhaseIndicator.FlightPhase previousPhase,
            FlightPhaseIndicator.FlightPhase newPhase,
            DateTime timestamp,
            TimeSpan previousPhaseDuration,
            string reason = null)
        {
            PreviousPhase = previousPhase;
            NewPhase = newPhase;
            Timestamp = timestamp;
            PreviousPhaseDuration = previousPhaseDuration;
            Reason = reason;
        }
    }
}
