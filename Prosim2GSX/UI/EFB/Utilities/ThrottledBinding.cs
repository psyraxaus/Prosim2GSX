using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Prosim2GSX.UI.EFB.Utilities
{
    /// <summary>
    /// Provides throttling functionality for data binding to reduce UI updates and improve performance.
    /// </summary>
    public class ThrottledBinding
    {
        private readonly TimeSpan _throttleInterval;
        private DateTime _lastUpdate = DateTime.MinValue;
        private readonly object _lock = new();
        private readonly DispatcherTimer _timer;
        private Action _pendingAction;
        private readonly Dispatcher _dispatcher;

        /// <summary>
        /// Initializes a new instance of the ThrottledBinding class.
        /// </summary>
        /// <param name="throttleInterval">The minimum time interval between updates.</param>
        /// <param name="dispatcher">The dispatcher to use for UI updates. If null, the current dispatcher will be used.</param>
        public ThrottledBinding(TimeSpan throttleInterval, Dispatcher dispatcher = null)
        {
            _throttleInterval = throttleInterval;
            _dispatcher = dispatcher ?? Dispatcher.CurrentDispatcher;
            
            _timer = new DispatcherTimer(DispatcherPriority.DataBind, _dispatcher)
            {
                Interval = throttleInterval
            };
            
            _timer.Tick += (s, e) =>
            {
                _timer.Stop();
                ExecutePendingAction();
            };
        }

        /// <summary>
        /// Determines whether an update should be processed immediately or throttled.
        /// </summary>
        /// <returns>True if the update should be processed immediately, false if it should be throttled.</returns>
        public bool ShouldUpdate()
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                if (now - _lastUpdate < _throttleInterval)
                {
                    return false;
                }
                
                _lastUpdate = now;
                return true;
            }
        }

        /// <summary>
        /// Executes an action with throttling.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        public void Execute(Action action)
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                if (now - _lastUpdate < _throttleInterval)
                {
                    // Store the action for later execution
                    _pendingAction = action;
                    
                    if (!_timer.IsEnabled)
                    {
                        _timer.Start();
                    }
                    
                    return;
                }
                
                _lastUpdate = now;
            }
            
            // Execute the action immediately
            _dispatcher.InvokeAsync(action, DispatcherPriority.DataBind);
        }

        /// <summary>
        /// Executes the pending action if one exists.
        /// </summary>
        private void ExecutePendingAction()
        {
            Action actionToExecute = null;
            
            lock (_lock)
            {
                if (_pendingAction != null)
                {
                    actionToExecute = _pendingAction;
                    _pendingAction = null;
                    _lastUpdate = DateTime.UtcNow;
                }
            }
            
            if (actionToExecute != null)
            {
                _dispatcher.InvokeAsync(actionToExecute, DispatcherPriority.DataBind);
            }
        }

        /// <summary>
        /// Cancels any pending updates.
        /// </summary>
        public void Cancel()
        {
            lock (_lock)
            {
                _pendingAction = null;
                _timer.Stop();
            }
        }

        /// <summary>
        /// Creates a new ThrottledBinding with the specified interval in milliseconds.
        /// </summary>
        /// <param name="intervalMilliseconds">The throttle interval in milliseconds.</param>
        /// <returns>A new ThrottledBinding instance.</returns>
        public static ThrottledBinding FromMilliseconds(int intervalMilliseconds)
        {
            return new ThrottledBinding(TimeSpan.FromMilliseconds(intervalMilliseconds));
        }

        /// <summary>
        /// Creates a new ThrottledBinding with the specified interval in seconds.
        /// </summary>
        /// <param name="intervalSeconds">The throttle interval in seconds.</param>
        /// <returns>A new ThrottledBinding instance.</returns>
        public static ThrottledBinding FromSeconds(int intervalSeconds)
        {
            return new ThrottledBinding(TimeSpan.FromSeconds(intervalSeconds));
        }
    }
}
