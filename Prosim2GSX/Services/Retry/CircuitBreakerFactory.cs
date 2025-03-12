using System;

namespace Prosim2GSX.Services.Retry
{
    /// <summary>
    /// Factory for creating standard circuit breakers
    /// </summary>
    public class CircuitBreakerFactory
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreakerFactory"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        public CircuitBreakerFactory(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a default circuit breaker
        /// </summary>
        /// <param name="name">The name of the circuit breaker for logging</param>
        /// <returns>A default circuit breaker</returns>
        public CircuitBreaker CreateDefaultCircuitBreaker(string name)
        {
            return new CircuitBreaker(_logger, name, 5, TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// Creates a circuit breaker for network operations
        /// </summary>
        /// <param name="name">The name of the circuit breaker for logging</param>
        /// <returns>A circuit breaker for network operations</returns>
        public CircuitBreaker CreateNetworkCircuitBreaker(string name)
        {
            return new CircuitBreaker(_logger, name, 3, TimeSpan.FromMinutes(2));
        }

        /// <summary>
        /// Creates a circuit breaker for SimConnect operations
        /// </summary>
        /// <param name="name">The name of the circuit breaker for logging</param>
        /// <returns>A circuit breaker for SimConnect operations</returns>
        public CircuitBreaker CreateSimConnectCircuitBreaker(string name)
        {
            return new CircuitBreaker(_logger, name, 3, TimeSpan.FromMinutes(2));
        }

        /// <summary>
        /// Creates a circuit breaker for ProSim operations
        /// </summary>
        /// <param name="name">The name of the circuit breaker for logging</param>
        /// <returns>A circuit breaker for ProSim operations</returns>
        public CircuitBreaker CreateProsimCircuitBreaker(string name)
        {
            return new CircuitBreaker(_logger, name, 4, TimeSpan.FromMinutes(1.5));
        }

        /// <summary>
        /// Creates a circuit breaker for GSX operations
        /// </summary>
        /// <param name="name">The name of the circuit breaker for logging</param>
        /// <returns>A circuit breaker for GSX operations</returns>
        public CircuitBreaker CreateGSXCircuitBreaker(string name)
        {
            return new CircuitBreaker(_logger, name, 5, TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// Creates a circuit breaker for quick operations that should fail fast
        /// </summary>
        /// <param name="name">The name of the circuit breaker for logging</param>
        /// <returns>A circuit breaker for quick operations</returns>
        public CircuitBreaker CreateQuickCircuitBreaker(string name)
        {
            return new CircuitBreaker(_logger, name, 3, TimeSpan.FromSeconds(30));
        }

        /// <summary>
        /// Creates a circuit breaker for long-running operations
        /// </summary>
        /// <param name="name">The name of the circuit breaker for logging</param>
        /// <returns>A circuit breaker for long-running operations</returns>
        public CircuitBreaker CreateLongRunningCircuitBreaker(string name)
        {
            return new CircuitBreaker(_logger, name, 2, TimeSpan.FromMinutes(5));
        }

        /// <summary>
        /// Creates a custom circuit breaker with the specified parameters
        /// </summary>
        /// <param name="name">The name of the circuit breaker for logging</param>
        /// <param name="failureThreshold">The number of failures required to trip the circuit breaker</param>
        /// <param name="recoveryTime">The time to wait before transitioning from Open to HalfOpen</param>
        /// <returns>A custom circuit breaker</returns>
        public CircuitBreaker CreateCustomCircuitBreaker(string name, int failureThreshold, TimeSpan recoveryTime)
        {
            return new CircuitBreaker(_logger, name, failureThreshold, recoveryTime);
        }
    }
}
