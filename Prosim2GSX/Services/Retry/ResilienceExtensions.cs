using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Services.Retry
{
    /// <summary>
    /// Extension methods for applying resilience strategies to operations
    /// </summary>
    public static class ResilienceExtensions
    {
        /// <summary>
        /// Executes the specified operation with the provided resilience strategy
        /// </summary>
        /// <typeparam name="T">The type of the result</typeparam>
        /// <param name="operation">The operation to execute</param>
        /// <param name="resilienceStrategy">The resilience strategy to apply</param>
        /// <param name="operationName">The name of the operation for logging</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>The result of the operation</returns>
        public static Task<T> WithResilienceAsync<T>(this Func<Task<T>> operation, ResilienceStrategy resilienceStrategy, string operationName, CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            if (resilienceStrategy == null)
                throw new ArgumentNullException(nameof(resilienceStrategy));

            return resilienceStrategy.ExecuteAsync(operation, operationName, cancellationToken);
        }

        /// <summary>
        /// Executes the specified operation with the provided resilience strategy
        /// </summary>
        /// <param name="operation">The operation to execute</param>
        /// <param name="resilienceStrategy">The resilience strategy to apply</param>
        /// <param name="operationName">The name of the operation for logging</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public static Task WithResilienceAsync(this Func<Task> operation, ResilienceStrategy resilienceStrategy, string operationName, CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            if (resilienceStrategy == null)
                throw new ArgumentNullException(nameof(resilienceStrategy));

            return resilienceStrategy.ExecuteAsync(operation, operationName, cancellationToken);
        }

        /// <summary>
        /// Executes the specified operation with the provided retry policy
        /// </summary>
        /// <typeparam name="T">The type of the result</typeparam>
        /// <param name="operation">The operation to execute</param>
        /// <param name="retryPolicy">The retry policy to apply</param>
        /// <param name="operationName">The name of the operation for logging</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>The result of the operation</returns>
        public static Task<T> WithRetryAsync<T>(this Func<Task<T>> operation, RetryPolicy retryPolicy, string operationName, CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            if (retryPolicy == null)
                throw new ArgumentNullException(nameof(retryPolicy));

            return retryPolicy.ExecuteAsync(operation, operationName, cancellationToken);
        }

        /// <summary>
        /// Executes the specified operation with the provided retry policy
        /// </summary>
        /// <param name="operation">The operation to execute</param>
        /// <param name="retryPolicy">The retry policy to apply</param>
        /// <param name="operationName">The name of the operation for logging</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public static Task WithRetryAsync(this Func<Task> operation, RetryPolicy retryPolicy, string operationName, CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            if (retryPolicy == null)
                throw new ArgumentNullException(nameof(retryPolicy));

            return retryPolicy.ExecuteAsync(operation, operationName, cancellationToken);
        }

        /// <summary>
        /// Executes the specified operation with the provided circuit breaker
        /// </summary>
        /// <typeparam name="T">The type of the result</typeparam>
        /// <param name="operation">The operation to execute</param>
        /// <param name="circuitBreaker">The circuit breaker to apply</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>The result of the operation</returns>
        public static Task<T> WithCircuitBreakerAsync<T>(this Func<Task<T>> operation, CircuitBreaker circuitBreaker, CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            if (circuitBreaker == null)
                throw new ArgumentNullException(nameof(circuitBreaker));

            return circuitBreaker.ExecuteAsync(operation, cancellationToken);
        }

        /// <summary>
        /// Executes the specified operation with the provided circuit breaker
        /// </summary>
        /// <param name="operation">The operation to execute</param>
        /// <param name="circuitBreaker">The circuit breaker to apply</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public static Task WithCircuitBreakerAsync(this Func<Task> operation, CircuitBreaker circuitBreaker, CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            if (circuitBreaker == null)
                throw new ArgumentNullException(nameof(circuitBreaker));

            return circuitBreaker.ExecuteAsync(operation, cancellationToken);
        }

        /// <summary>
        /// Executes the specified operation with retry and circuit breaker protection
        /// </summary>
        /// <typeparam name="T">The type of the result</typeparam>
        /// <param name="operation">The operation to execute</param>
        /// <param name="retryPolicy">The retry policy to apply</param>
        /// <param name="circuitBreaker">The circuit breaker to apply</param>
        /// <param name="operationName">The name of the operation for logging</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>The result of the operation</returns>
        public static async Task<T> WithResilienceAsync<T>(this Func<Task<T>> operation, RetryPolicy retryPolicy, CircuitBreaker circuitBreaker, string operationName, CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            if (retryPolicy == null)
                throw new ArgumentNullException(nameof(retryPolicy));

            if (circuitBreaker == null)
                throw new ArgumentNullException(nameof(circuitBreaker));

            return await circuitBreaker.ExecuteAsync(async () =>
            {
                return await retryPolicy.ExecuteAsync(operation, operationName, cancellationToken);
            }, cancellationToken);
        }

        /// <summary>
        /// Executes the specified operation with retry and circuit breaker protection
        /// </summary>
        /// <param name="operation">The operation to execute</param>
        /// <param name="retryPolicy">The retry policy to apply</param>
        /// <param name="circuitBreaker">The circuit breaker to apply</param>
        /// <param name="operationName">The name of the operation for logging</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public static async Task WithResilienceAsync(this Func<Task> operation, RetryPolicy retryPolicy, CircuitBreaker circuitBreaker, string operationName, CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            if (retryPolicy == null)
                throw new ArgumentNullException(nameof(retryPolicy));

            if (circuitBreaker == null)
                throw new ArgumentNullException(nameof(circuitBreaker));

            await circuitBreaker.ExecuteAsync(async () =>
            {
                await retryPolicy.ExecuteAsync(operation, operationName, cancellationToken);
            }, cancellationToken);
        }
    }
}
