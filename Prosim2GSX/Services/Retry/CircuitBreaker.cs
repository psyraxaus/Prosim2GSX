using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Services.Retry
{
    /// <summary>
    /// Represents the state of a circuit breaker
    /// </summary>
    public enum CircuitState
    {
        /// <summary>
        /// The circuit is closed and operations can be executed
        /// </summary>
        Closed,

        /// <summary>
        /// The circuit is open and operations will fail immediately
        /// </summary>
        Open,

        /// <summary>
        /// The circuit is half-open and a single operation will be allowed to test if the circuit can be closed
        /// </summary>
        HalfOpen
    }

    /// <summary>
    /// Implements the circuit breaker pattern to prevent cascading failures
    /// </summary>
    public class CircuitBreaker
    {
        private readonly ILogger _logger;
        private readonly object _stateLock = new object();
        private CircuitState _state = CircuitState.Closed;
        private int _failureCount;
        private DateTime _lastFailureTime = DateTime.MinValue;
        private DateTime _openTime = DateTime.MinValue;
        private bool _halfOpenTestInProgress;

        /// <summary>
        /// Gets the current state of the circuit breaker
        /// </summary>
        public CircuitState State
        {
            get
            {
                lock (_stateLock)
                {
                    return _state;
                }
            }
        }

        /// <summary>
        /// Gets or sets the number of failures required to trip the circuit breaker
        /// </summary>
        public int FailureThreshold { get; set; } = 5;

        /// <summary>
        /// Gets or sets the time to wait before transitioning from Open to HalfOpen
        /// </summary>
        public TimeSpan RecoveryTime { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets the name of the circuit breaker for logging
        /// </summary>
        public string Name { get; set; } = "Default";

        /// <summary>
        /// Event raised when the circuit state changes
        /// </summary>
        public event EventHandler<CircuitStateChangedEventArgs> StateChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreaker"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        public CircuitBreaker(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreaker"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="name">The name of the circuit breaker for logging</param>
        /// <param name="failureThreshold">The number of failures required to trip the circuit breaker</param>
        /// <param name="recoveryTime">The time to wait before transitioning from Open to HalfOpen</param>
        public CircuitBreaker(ILogger logger, string name, int failureThreshold, TimeSpan recoveryTime)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            FailureThreshold = failureThreshold;
            RecoveryTime = recoveryTime;
        }

        /// <summary>
        /// Executes the specified operation with circuit breaker protection
        /// </summary>
        /// <typeparam name="T">The type of the result</typeparam>
        /// <param name="operation">The operation to execute</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>The result of the operation</returns>
        /// <exception cref="CircuitBreakerOpenException">Thrown when the circuit is open</exception>
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            await EnsureStateAsync();

            lock (_stateLock)
            {
                if (_state == CircuitState.Open)
                {
                    _logger.Log(LogLevel.Warning, $"CircuitBreaker:{Name}", "Circuit is open, operation rejected");
                    throw new CircuitBreakerOpenException($"Circuit breaker '{Name}' is open");
                }

                if (_state == CircuitState.HalfOpen && _halfOpenTestInProgress)
                {
                    _logger.Log(LogLevel.Warning, $"CircuitBreaker:{Name}", "Circuit is half-open and a test is in progress, operation rejected");
                    throw new CircuitBreakerOpenException($"Circuit breaker '{Name}' is half-open and a test is in progress");
                }

                if (_state == CircuitState.HalfOpen)
                {
                    _halfOpenTestInProgress = true;
                    _logger.Log(LogLevel.Information, $"CircuitBreaker:{Name}", "Circuit is half-open, allowing test operation");
                }
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                T result = await operation();

                // If we get here, the operation succeeded
                if (_state == CircuitState.HalfOpen)
                {
                    Reset();
                    _logger.Log(LogLevel.Information, $"CircuitBreaker:{Name}", "Test operation succeeded, circuit closed");
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                // Don't count cancellations as failures
                _logger.Log(LogLevel.Warning, $"CircuitBreaker:{Name}", "Operation was canceled");
                throw;
            }
            catch (Exception ex)
            {
                // Record the failure
                RecordFailure(ex);
                throw;
            }
            finally
            {
                if (_state == CircuitState.HalfOpen)
                {
                    lock (_stateLock)
                    {
                        _halfOpenTestInProgress = false;
                    }
                }
            }
        }

        /// <summary>
        /// Executes the specified operation with circuit breaker protection
        /// </summary>
        /// <param name="operation">The operation to execute</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        /// <exception cref="CircuitBreakerOpenException">Thrown when the circuit is open</exception>
        public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            await EnsureStateAsync();

            lock (_stateLock)
            {
                if (_state == CircuitState.Open)
                {
                    _logger.Log(LogLevel.Warning, $"CircuitBreaker:{Name}", "Circuit is open, operation rejected");
                    throw new CircuitBreakerOpenException($"Circuit breaker '{Name}' is open");
                }

                if (_state == CircuitState.HalfOpen && _halfOpenTestInProgress)
                {
                    _logger.Log(LogLevel.Warning, $"CircuitBreaker:{Name}", "Circuit is half-open and a test is in progress, operation rejected");
                    throw new CircuitBreakerOpenException($"Circuit breaker '{Name}' is half-open and a test is in progress");
                }

                if (_state == CircuitState.HalfOpen)
                {
                    _halfOpenTestInProgress = true;
                    _logger.Log(LogLevel.Information, $"CircuitBreaker:{Name}", "Circuit is half-open, allowing test operation");
                }
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await operation();

                // If we get here, the operation succeeded
                if (_state == CircuitState.HalfOpen)
                {
                    Reset();
                    _logger.Log(LogLevel.Information, $"CircuitBreaker:{Name}", "Test operation succeeded, circuit closed");
                }
            }
            catch (OperationCanceledException)
            {
                // Don't count cancellations as failures
                _logger.Log(LogLevel.Warning, $"CircuitBreaker:{Name}", "Operation was canceled");
                throw;
            }
            catch (Exception ex)
            {
                // Record the failure
                RecordFailure(ex);
                throw;
            }
            finally
            {
                if (_state == CircuitState.HalfOpen)
                {
                    lock (_stateLock)
                    {
                        _halfOpenTestInProgress = false;
                    }
                }
            }
        }

        /// <summary>
        /// Resets the circuit breaker to the closed state
        /// </summary>
        public void Reset()
        {
            CircuitState previousState;
            lock (_stateLock)
            {
                previousState = _state;
                _state = CircuitState.Closed;
                _failureCount = 0;
                _lastFailureTime = DateTime.MinValue;
                _openTime = DateTime.MinValue;
                _halfOpenTestInProgress = false;
            }

            if (previousState != CircuitState.Closed)
            {
                _logger.Log(LogLevel.Information, $"CircuitBreaker:{Name}", $"Circuit reset from {previousState} to {CircuitState.Closed}");
                OnStateChanged(previousState, CircuitState.Closed);
            }
        }

        /// <summary>
        /// Trips the circuit breaker to the open state
        /// </summary>
        public void Trip()
        {
            CircuitState previousState;
            lock (_stateLock)
            {
                previousState = _state;
                _state = CircuitState.Open;
                _openTime = DateTime.UtcNow;
                _halfOpenTestInProgress = false;
            }

            if (previousState != CircuitState.Open)
            {
                _logger.Log(LogLevel.Warning, $"CircuitBreaker:{Name}", $"Circuit tripped from {previousState} to {CircuitState.Open}");
                OnStateChanged(previousState, CircuitState.Open);
            }
        }

        /// <summary>
        /// Records a failure and potentially trips the circuit breaker
        /// </summary>
        /// <param name="exception">The exception that caused the failure</param>
        private void RecordFailure(Exception exception)
        {
            lock (_stateLock)
            {
                _lastFailureTime = DateTime.UtcNow;

                switch (_state)
                {
                    case CircuitState.HalfOpen:
                        // Any failure in half-open state trips the circuit
                        _logger.Log(LogLevel.Warning, $"CircuitBreaker:{Name}", 
                            $"Failure in half-open state: {exception.GetType().Name}: {exception.Message}");
                        Trip();
                        break;

                    case CircuitState.Closed:
                        _failureCount++;
                        _logger.Log(LogLevel.Warning, $"CircuitBreaker:{Name}", 
                            $"Failure in closed state: {exception.GetType().Name}: {exception.Message}. Failure count: {_failureCount}/{FailureThreshold}");

                        if (_failureCount >= FailureThreshold)
                        {
                            _logger.Log(LogLevel.Warning, $"CircuitBreaker:{Name}", 
                                $"Failure threshold reached ({_failureCount}/{FailureThreshold}), tripping circuit");
                            Trip();
                        }
                        break;

                    case CircuitState.Open:
                        // This shouldn't happen, but just in case
                        _logger.Log(LogLevel.Warning, $"CircuitBreaker:{Name}", 
                            $"Failure in open state: {exception.GetType().Name}: {exception.Message}");
                        break;
                }
            }
        }

        /// <summary>
        /// Ensures the circuit breaker is in the correct state based on timing
        /// </summary>
        private async Task EnsureStateAsync()
        {
            CircuitState previousState;
            bool stateChanged = false;

            lock (_stateLock)
            {
                previousState = _state;

                if (_state == CircuitState.Open && DateTime.UtcNow - _openTime >= RecoveryTime)
                {
                    _state = CircuitState.HalfOpen;
                    _halfOpenTestInProgress = false;
                    stateChanged = true;
                }
            }

            if (stateChanged)
            {
                _logger.Log(LogLevel.Information, $"CircuitBreaker:{Name}", 
                    $"Circuit transitioned from {previousState} to {CircuitState.HalfOpen} after recovery time");
                OnStateChanged(previousState, CircuitState.HalfOpen);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Raises the StateChanged event
        /// </summary>
        /// <param name="previousState">The previous state</param>
        /// <param name="newState">The new state</param>
        protected virtual void OnStateChanged(CircuitState previousState, CircuitState newState)
        {
            StateChanged?.Invoke(this, new CircuitStateChangedEventArgs(previousState, newState));
        }
    }

    /// <summary>
    /// Event arguments for circuit state changes
    /// </summary>
    public class CircuitStateChangedEventArgs : BaseEventArgs
    {
        /// <summary>
        /// Gets the previous state of the circuit
        /// </summary>
        public CircuitState PreviousState { get; }

        /// <summary>
        /// Gets the new state of the circuit
        /// </summary>
        public CircuitState NewState { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitStateChangedEventArgs"/> class
        /// </summary>
        /// <param name="previousState">The previous state</param>
        /// <param name="newState">The new state</param>
        public CircuitStateChangedEventArgs(CircuitState previousState, CircuitState newState)
        {
            PreviousState = previousState;
            NewState = newState;
        }
    }

    /// <summary>
    /// Exception thrown when a circuit breaker is open
    /// </summary>
    public class CircuitBreakerOpenException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreakerOpenException"/> class
        /// </summary>
        public CircuitBreakerOpenException() : base("Circuit breaker is open")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreakerOpenException"/> class with a specified error message
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        public CircuitBreakerOpenException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreakerOpenException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public CircuitBreakerOpenException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
