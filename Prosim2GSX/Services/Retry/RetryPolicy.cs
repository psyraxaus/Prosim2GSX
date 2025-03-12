using Prosim2GSX.Services.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Services.Retry
{
    /// <summary>
    /// Defines a policy for retrying operations that may fail transiently
    /// </summary>
    public class RetryPolicy
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Gets or sets the maximum number of retry attempts
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Gets or sets the initial delay between retries in milliseconds
        /// </summary>
        public int InitialDelayMs { get; set; } = 1000;

        /// <summary>
        /// Gets or sets a value indicating whether to use exponential backoff for retry delays
        /// </summary>
        public bool UseExponentialBackoff { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum delay between retries in milliseconds
        /// </summary>
        public int MaxDelayMs { get; set; } = 30000;

        /// <summary>
        /// Gets or sets a value indicating whether to include inner exceptions in the retry exception
        /// </summary>
        public bool IncludeInnerExceptions { get; set; } = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryPolicy"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        public RetryPolicy(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executes the specified operation with retry
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

            int retryCount = 0;
            int delayMs = InitialDelayMs;
            Exception lastException = null;

            while (true)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return await operation();
                }
                catch (OperationCanceledException)
                {
                    _logger.Log(LogLevel.Warning, $"RetryPolicy:{operationName}", "Operation was canceled");
                    throw;
                }
                catch (Exception ex) when (ShouldRetry(ex, retryCount))
                {
                    lastException = ex;
                    retryCount++;

                    int recommendedDelay = GetRecommendedDelay(ex);
                    if (recommendedDelay > 0)
                    {
                        delayMs = recommendedDelay;
                    }
                    else if (UseExponentialBackoff)
                    {
                        // Exponential backoff with jitter
                        delayMs = Math.Min(
                            (int)(InitialDelayMs * Math.Pow(2, retryCount - 1) * (0.8 + 0.4 * new Random().NextDouble())),
                            MaxDelayMs);
                    }

                    _logger.Log(LogLevel.Warning, $"RetryPolicy:{operationName}", 
                        $"Attempt {retryCount} of {MaxRetries} failed with {ex.GetType().Name}: {ex.Message}. Retrying in {delayMs}ms...");

                    try
                    {
                        await Task.Delay(delayMs, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.Log(LogLevel.Warning, $"RetryPolicy:{operationName}", "Retry delay was canceled");
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, $"RetryPolicy:{operationName}", 
                        $"Operation failed with non-transient exception: {ex.GetType().Name}: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// Executes the specified operation with retry
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

            int retryCount = 0;
            int delayMs = InitialDelayMs;
            Exception lastException = null;

            while (true)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await operation();
                    return;
                }
                catch (OperationCanceledException)
                {
                    _logger.Log(LogLevel.Warning, $"RetryPolicy:{operationName}", "Operation was canceled");
                    throw;
                }
                catch (Exception ex) when (ShouldRetry(ex, retryCount))
                {
                    lastException = ex;
                    retryCount++;

                    int recommendedDelay = GetRecommendedDelay(ex);
                    if (recommendedDelay > 0)
                    {
                        delayMs = recommendedDelay;
                    }
                    else if (UseExponentialBackoff)
                    {
                        // Exponential backoff with jitter
                        delayMs = Math.Min(
                            (int)(InitialDelayMs * Math.Pow(2, retryCount - 1) * (0.8 + 0.4 * new Random().NextDouble())),
                            MaxDelayMs);
                    }

                    _logger.Log(LogLevel.Warning, $"RetryPolicy:{operationName}", 
                        $"Attempt {retryCount} of {MaxRetries} failed with {ex.GetType().Name}: {ex.Message}. Retrying in {delayMs}ms...");

                    try
                    {
                        await Task.Delay(delayMs, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.Log(LogLevel.Warning, $"RetryPolicy:{operationName}", "Retry delay was canceled");
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, $"RetryPolicy:{operationName}", 
                        $"Operation failed with non-transient exception: {ex.GetType().Name}: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// Determines whether the operation should be retried based on the exception and retry count
        /// </summary>
        /// <param name="exception">The exception that was thrown</param>
        /// <param name="retryCount">The current retry count</param>
        /// <returns>True if the operation should be retried, false otherwise</returns>
        protected virtual bool ShouldRetry(Exception exception, int retryCount)
        {
            if (retryCount >= MaxRetries)
                return false;

            // Check if it's a TransientException
            if (exception is TransientException)
                return true;

            // Check if it's a SimConnectConnectionException with IsTransient=true
            if (exception is SimConnectConnectionException simConnectEx && simConnectEx.IsTransient)
                return true;

            // Check if it's a ProsimConnectionException with IsTransient=true
            if (exception is ProsimConnectionException prosimConnEx && prosimConnEx.IsTransient)
                return true;

            // Check if it's a GSXServiceException with IsTransient=true
            if (exception is GSXServiceException gsxServiceEx && gsxServiceEx.IsTransient)
                return true;

            // Check inner exception recursively
            if (exception.InnerException != null)
                return ShouldRetry(exception.InnerException, retryCount);

            return false;
        }

        /// <summary>
        /// Gets the recommended delay for the next retry based on the exception
        /// </summary>
        /// <param name="exception">The exception that was thrown</param>
        /// <returns>The recommended delay in milliseconds, or 0 if no recommendation</returns>
        protected virtual int GetRecommendedDelay(Exception exception)
        {
            // Check if it's a TransientException with a recommended delay
            if (exception is TransientException transientEx && transientEx.RecommendedRetryDelayMs > 0)
                return transientEx.RecommendedRetryDelayMs;

            // Check if it's a SimConnectConnectionException with a recommended delay
            if (exception is SimConnectConnectionException simConnectEx && simConnectEx.RecommendedRetryDelayMs > 0)
                return simConnectEx.RecommendedRetryDelayMs;

            // Check if it's a ProsimConnectionException with a recommended delay
            if (exception is ProsimConnectionException prosimConnEx && prosimConnEx.RecommendedRetryDelayMs > 0)
                return prosimConnEx.RecommendedRetryDelayMs;

            // Check inner exception recursively
            if (exception.InnerException != null)
                return GetRecommendedDelay(exception.InnerException);

            return 0;
        }
    }
}
