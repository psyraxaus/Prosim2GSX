using System;
using Prosim2GSX.Services;
using Prosim2GSX.UI.EFB.Controls;

namespace Prosim2GSX.UI.EFB.Phase
{
    /// <summary>
    /// Event arguments for phase context changes.
    /// </summary>
    public class PhaseContextChangedEventArgs : BaseEventArgs
    {
        /// <summary>
        /// Gets the previous phase.
        /// </summary>
        public FlightPhaseIndicator.FlightPhase PreviousPhase { get; }
        
        /// <summary>
        /// Gets the new phase.
        /// </summary>
        public FlightPhaseIndicator.FlightPhase NewPhase { get; }
        
        /// <summary>
        /// Gets the previous phase context.
        /// </summary>
        public PhaseContext PreviousContext { get; }
        
        /// <summary>
        /// Gets the new phase context.
        /// </summary>
        public PhaseContext NewContext { get; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="PhaseContextChangedEventArgs"/> class.
        /// </summary>
        /// <param name="previousPhase">The previous phase.</param>
        /// <param name="newPhase">The new phase.</param>
        /// <param name="previousContext">The previous phase context.</param>
        /// <param name="newContext">The new phase context.</param>
        /// <param name="timestamp">The timestamp when the context change occurred.</param>
        public PhaseContextChangedEventArgs(
            FlightPhaseIndicator.FlightPhase previousPhase,
            FlightPhaseIndicator.FlightPhase newPhase,
            PhaseContext previousContext,
            PhaseContext newContext,
            DateTime timestamp)
            : base() // Call the base constructor
        {
            PreviousPhase = previousPhase;
            NewPhase = newPhase;
            PreviousContext = previousContext;
            NewContext = newContext;
            // Timestamp is already set by the base constructor
        }
    }
}
