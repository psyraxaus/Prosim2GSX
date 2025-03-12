using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Services.Retry
{
    /// <summary>
    /// Combines retry policies and circuit breakers to provide a comprehensive resilience strategy
    /// </summary>
    public class ResilienceStrategy
    {
        private readonly RetryPolicy _retryPolicy;
        private readonly CircuitBreaker _circuitBreaker;
        private readonly ILogger _logger;

        /// <summary>
        /// Gets the retry policy
        /// </summary>
        public RetryPolicy RetryPolicy => _retryPolicy;

        /// <summary>
        /// Gets the circuit breaker
        /// </summary>
        public CircuitBreaker CircuitBreaker => _circuitBreaker;

        /// <summary>
        /// Gets the name of the resilience strategy
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResilienceStrategy"/> class
        /// </summary>
        /// <param name="retryPolicy">The retry policy</param>
        /// <param name="circuitBreaker">The circuit breaker</param>
        /// <param name="logger">The logger</param>
        /// <param name="name">The name of the resilience strategy</param>
        public ResilienceStrategy(RetryPolicy retryPolicy, CircuitBreaker circuitBreaker, ILogger logger, string name)
        {
            _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
            _circuitBreaker = circuitBreaker ?? throw new ArgumentNullException(nameof(circuitBreaker));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Executes the specified operation with retry and circuit breaker protection
        /// </summary>
        /// <typeparam name="T">The type of the result</typeparam>
        /// <param name="operation">The operation to execute</param>
        /// <param name="operationName">The name of the operation for logging</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>The result of the operation</returns>
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationName, CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            if (string.IsNullOrEmpty(operationName))
                operationName = "UnnamedOperation";

            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                return await _retryPolicy.ExecuteAsync(operation, operationName, cancellationToken);
            }, cancellationToken);
        }

        /// <summary>
        /// Executes the specified operation with retry and circuit breaker protection
        /// </summary>
        /// <param name="operation">The operation to execute</param>
        /// <param name="operationName">The name of the operation for logging</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task ExecuteAsync(Func<Task> operation, string operationName, CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            if (string.IsNullOrEmpty(operationName))
                operationName = "UnnamedOperation";

            await _circuitBreaker.ExecuteAsync(async () =>
            {
                await _retryPolicy.ExecuteAsync(operation, operationName, cancellationToken);
            }, cancellationToken);
        }

        /// <summary>
        /// Resets the resilience strategy to its initial state
        /// </summary>
        public void Reset()
        {
            _circuitBreaker.Reset();
            _logger.Log(LogLevel.Information, $"ResilienceStrategy:{Name}", "Resilience strategy reset");
        }
    }
}
