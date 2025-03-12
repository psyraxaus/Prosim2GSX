using System;

namespace Prosim2GSX.Services.Retry
{
    /// <summary>
    /// Factory for creating standard resilience strategies
    /// </summary>
    public class ResilienceStrategyFactory
    {
        private readonly RetryPolicyFactory _retryPolicyFactory;
        private readonly CircuitBreakerFactory _circuitBreakerFactory;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResilienceStrategyFactory"/> class
        /// </summary>
        /// <param name="retryPolicyFactory">The retry policy factory</param>
        /// <param name="circuitBreakerFactory">The circuit breaker factory</param>
        /// <param name="logger">The logger</param>
        public ResilienceStrategyFactory(RetryPolicyFactory retryPolicyFactory, CircuitBreakerFactory circuitBreakerFactory, ILogger logger)
        {
            _retryPolicyFactory = retryPolicyFactory ?? throw new ArgumentNullException(nameof(retryPolicyFactory));
            _circuitBreakerFactory = circuitBreakerFactory ?? throw new ArgumentNullException(nameof(circuitBreakerFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a default resilience strategy
        /// </summary>
        /// <param name="name">The name of the resilience strategy</param>
        /// <returns>A default resilience strategy</returns>
        public ResilienceStrategy CreateDefaultStrategy(string name)
        {
            return new ResilienceStrategy(
                _retryPolicyFactory.CreateDefaultPolicy(),
                _circuitBreakerFactory.CreateDefaultCircuitBreaker(name),
                _logger,
                name);
        }

        /// <summary>
        /// Creates a resilience strategy for network operations
        /// </summary>
        /// <param name="name">The name of the resilience strategy</param>
        /// <returns>A resilience strategy for network operations</returns>
        public ResilienceStrategy CreateNetworkStrategy(string name)
        {
            return new ResilienceStrategy(
                _retryPolicyFactory.CreateNetworkPolicy(),
                _circuitBreakerFactory.CreateNetworkCircuitBreaker(name),
                _logger,
                name);
        }

        /// <summary>
        /// Creates a resilience strategy for SimConnect operations
        /// </summary>
        /// <param name="name">The name of the resilience strategy</param>
        /// <returns>A resilience strategy for SimConnect operations</returns>
        public ResilienceStrategy CreateSimConnectStrategy(string name)
        {
            return new ResilienceStrategy(
                _retryPolicyFactory.CreateSimConnectPolicy(),
                _circuitBreakerFactory.CreateSimConnectCircuitBreaker(name),
                _logger,
                name);
        }

        /// <summary>
        /// Creates a resilience strategy for ProSim operations
        /// </summary>
        /// <param name="name">The name of the resilience strategy</param>
        /// <returns>A resilience strategy for ProSim operations</returns>
        public ResilienceStrategy CreateProsimStrategy(string name)
        {
            return new ResilienceStrategy(
                _retryPolicyFactory.CreateProsimPolicy(),
                _circuitBreakerFactory.CreateProsimCircuitBreaker(name),
                _logger,
                name);
        }

        /// <summary>
        /// Creates a resilience strategy for GSX operations
        /// </summary>
        /// <param name="name">The name of the resilience strategy</param>
        /// <returns>A resilience strategy for GSX operations</returns>
        public ResilienceStrategy CreateGSXStrategy(string name)
        {
            return new ResilienceStrategy(
                _retryPolicyFactory.CreateGSXPolicy(),
                _circuitBreakerFactory.CreateGSXCircuitBreaker(name),
                _logger,
                name);
        }

        /// <summary>
        /// Creates a resilience strategy for quick operations that should fail fast
        /// </summary>
        /// <param name="name">The name of the resilience strategy</param>
        /// <returns>A resilience strategy for quick operations</returns>
        public ResilienceStrategy CreateQuickStrategy(string name)
        {
            return new ResilienceStrategy(
                _retryPolicyFactory.CreateQuickPolicy(),
                _circuitBreakerFactory.CreateQuickCircuitBreaker(name),
                _logger,
                name);
        }

        /// <summary>
        /// Creates a resilience strategy for long-running operations
        /// </summary>
        /// <param name="name">The name of the resilience strategy</param>
        /// <returns>A resilience strategy for long-running operations</returns>
        public ResilienceStrategy CreateLongRunningStrategy(string name)
        {
            return new ResilienceStrategy(
                _retryPolicyFactory.CreateLongRunningPolicy(),
                _circuitBreakerFactory.CreateLongRunningCircuitBreaker(name),
                _logger,
                name);
        }

        /// <summary>
        /// Creates a custom resilience strategy with the specified parameters
        /// </summary>
        /// <param name="name">The name of the resilience strategy</param>
        /// <param name="maxRetries">The maximum number of retry attempts</param>
        /// <param name="initialDelayMs">The initial delay between retries in milliseconds</param>
        /// <param name="useExponentialBackoff">Whether to use exponential backoff for retry delays</param>
        /// <param name="maxDelayMs">The maximum delay between retries in milliseconds</param>
        /// <param name="includeInnerExceptions">Whether to include inner exceptions in the retry exception</param>
        /// <param name="failureThreshold">The number of failures required to trip the circuit breaker</param>
        /// <param name="recoveryTime">The time to wait before transitioning from Open to HalfOpen</param>
        /// <returns>A custom resilience strategy</returns>
        public ResilienceStrategy CreateCustomStrategy(
            string name,
            int maxRetries,
            int initialDelayMs,
            bool useExponentialBackoff,
            int maxDelayMs,
            bool includeInnerExceptions,
            int failureThreshold,
            TimeSpan recoveryTime)
        {
            return new ResilienceStrategy(
                _retryPolicyFactory.CreateCustomPolicy(maxRetries, initialDelayMs, useExponentialBackoff, maxDelayMs, includeInnerExceptions),
                _circuitBreakerFactory.CreateCustomCircuitBreaker(name, failureThreshold, recoveryTime),
                _logger,
                name);
        }
    }
}
