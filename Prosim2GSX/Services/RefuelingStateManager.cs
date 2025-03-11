using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Manages the state transitions for the refueling process
    /// </summary>
    public class RefuelingStateManager
    {
        private RefuelingState _state;
        private readonly object _stateLock = new object();
        private readonly ILogger _logger;
        
        /// <summary>
        /// Event raised when the refueling state changes
        /// </summary>
        public event EventHandler<RefuelingStateChangedEventArgs> StateChanged;
        
        /// <summary>
        /// Gets the current refueling state
        /// </summary>
        public RefuelingState State => _state;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="RefuelingStateManager"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        public RefuelingStateManager(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _state = RefuelingState.Idle;
        }
        
        /// <summary>
        /// Attempts to transition to a new state
        /// </summary>
        /// <param name="newState">The new state to transition to</param>
        /// <returns>True if the transition was successful, false otherwise</returns>
        public bool TransitionTo(RefuelingState newState)
        {
            lock (_stateLock)
            {
                if (!IsValidTransition(_state, newState))
                {
                    _logger.Log(LogLevel.Warning, "RefuelingStateManager:TransitionTo", 
                        $"Invalid state transition from {_state} to {newState}");
                    return false;
                }
                
                var oldState = _state;
                _state = newState;
                
                _logger.Log(LogLevel.Debug, "RefuelingStateManager:TransitionTo", 
                    $"State transitioned from {oldState} to {newState}");
                
                OnStateChanged(oldState, newState);
                return true;
            }
        }
        
        /// <summary>
        /// Determines if a state transition is valid
        /// </summary>
        /// <param name="from">The current state</param>
        /// <param name="to">The target state</param>
        /// <returns>True if the transition is valid, false otherwise</returns>
        private bool IsValidTransition(RefuelingState from, RefuelingState to)
        {
            // Define valid state transitions
            switch (from)
            {
                case RefuelingState.Idle:
                    return to == RefuelingState.Requested || to == RefuelingState.Defueling;
                    
                case RefuelingState.Requested:
                    return to == RefuelingState.Refueling || to == RefuelingState.Idle || to == RefuelingState.Error;
                    
                case RefuelingState.Refueling:
                    return to == RefuelingState.Complete || to == RefuelingState.Idle || to == RefuelingState.Error;
                    
                case RefuelingState.Defueling:
                    return to == RefuelingState.Idle || to == RefuelingState.Error;
                    
                case RefuelingState.Complete:
                    return to == RefuelingState.Idle;
                    
                case RefuelingState.Error:
                    return to == RefuelingState.Idle;
                    
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Raises the StateChanged event
        /// </summary>
        /// <param name="oldState">The previous state</param>
        /// <param name="newState">The new state</param>
        protected virtual void OnStateChanged(RefuelingState oldState, RefuelingState newState)
        {
            try
            {
                StateChanged?.Invoke(this, new RefuelingStateChangedEventArgs(newState, oldState));
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "RefuelingStateManager:OnStateChanged", 
                    $"Error raising StateChanged event: {ex.Message}");
            }
        }
    }
}
