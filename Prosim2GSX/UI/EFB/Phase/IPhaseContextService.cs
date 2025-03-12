using System;
using Prosim2GSX.UI.EFB.Controls;

namespace Prosim2GSX.UI.EFB.Phase
{
    /// <summary>
    /// Interface for the phase context service.
    /// </summary>
    public interface IPhaseContextService
    {
        /// <summary>
        /// Gets the current phase context.
        /// </summary>
        PhaseContext CurrentContext { get; }
        
        /// <summary>
        /// Gets the current flight phase.
        /// </summary>
        FlightPhaseIndicator.FlightPhase CurrentPhase { get; }
        
        /// <summary>
        /// Event raised when the phase context changes.
        /// </summary>
        event EventHandler<PhaseContextChangedEventArgs> ContextChanged;
        
        /// <summary>
        /// Initializes the phase context service.
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Registers a phase context for a specific flight phase.
        /// </summary>
        /// <param name="phase">The flight phase.</param>
        /// <param name="context">The phase context.</param>
        void RegisterPhaseContext(FlightPhaseIndicator.FlightPhase phase, PhaseContext context);
        
        /// <summary>
        /// Gets the phase context for a specific flight phase.
        /// </summary>
        /// <param name="phase">The flight phase.</param>
        /// <returns>The phase context for the specified flight phase.</returns>
        PhaseContext GetContextForPhase(FlightPhaseIndicator.FlightPhase phase);
        
        /// <summary>
        /// Updates the current phase context based on the current flight phase.
        /// </summary>
        void UpdateCurrentContext();
        
        /// <summary>
        /// Checks if a control is visible in the current phase context.
        /// </summary>
        /// <param name="controlName">The name of the control.</param>
        /// <returns>True if the control is visible, false otherwise.</returns>
        bool IsControlVisible(string controlName);
        
        /// <summary>
        /// Checks if a control is enabled in the current phase context.
        /// </summary>
        /// <param name="controlName">The name of the control.</param>
        /// <returns>True if the control is enabled, false otherwise.</returns>
        bool IsControlEnabled(string controlName);
        
        /// <summary>
        /// Gets the recommended actions for the current phase context.
        /// </summary>
        /// <returns>The list of recommended actions.</returns>
        string[] GetRecommendedActions();
        
        /// <summary>
        /// Gets the available services for the current phase context.
        /// </summary>
        /// <returns>The list of available services.</returns>
        string[] GetAvailableServices();
        
        /// <summary>
        /// Gets the notifications for the current phase context.
        /// </summary>
        /// <returns>The list of notifications.</returns>
        PhaseNotification[] GetNotifications();
        
        /// <summary>
        /// Gets the checklists for the current phase context.
        /// </summary>
        /// <returns>The list of checklists.</returns>
        PhaseChecklist[] GetChecklists();
        
        /// <summary>
        /// Gets the actions for the current phase context.
        /// </summary>
        /// <returns>The list of actions.</returns>
        PhaseAction[] GetActions();
        
        /// <summary>
        /// Gets the recommended actions for the current phase context.
        /// </summary>
        /// <returns>The list of recommended actions.</returns>
        PhaseAction[] GetRecommendedPhaseActions();
    }
}
