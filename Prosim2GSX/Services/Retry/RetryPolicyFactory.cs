using System;

namespace Prosim2GSX.Services.Retry
{
    /// <summary>
    /// Factory for creating standard retry policies
    /// </summary>
    public class RetryPolicyFactory
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryPolicyFactory"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        public RetryPolicyFactory(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a default retry policy
        /// </summary>
        /// <returns>A default retry policy</returns>
        public RetryPolicy CreateDefaultPolicy()
        {
            return new RetryPolicy(_logger)
            {
                MaxRetries = 3,
                InitialDelayMs = 1000,
                UseExponentialBackoff = true,
                MaxDelayMs = 30000,
                IncludeInnerExceptions = true
            };
        }

        /// <summary>
        /// Creates a retry policy for network operations
        /// </summary>
        /// <returns>A retry policy for network operations</returns>
        public RetryPolicy CreateNetworkPolicy()
        {
            return new RetryPolicy(_logger)
            {
                MaxRetries = 5,
                InitialDelayMs = 2000,
                UseExponentialBackoff = true,
                MaxDelayMs = 60000,
                IncludeInnerExceptions = true
            };
        }

        /// <summary>
        /// Creates a retry policy for SimConnect operations
        /// </summary>
        /// <returns>A retry policy for SimConnect operations</returns>
        public RetryPolicy CreateSimConnectPolicy()
        {
            return new RetryPolicy(_logger)
            {
                MaxRetries = 5,
                InitialDelayMs = 2000,
                UseExponentialBackoff = true,
                MaxDelayMs = 60000,
                IncludeInnerExceptions = true
            };
        }

        /// <summary>
        /// Creates a retry policy for ProSim operations
        /// </summary>
        /// <returns>A retry policy for ProSim operations</returns>
        public RetryPolicy CreateProsimPolicy()
        {
            return new RetryPolicy(_logger)
            {
                MaxRetries = 4,
                InitialDelayMs = 1500,
                UseExponentialBackoff = true,
                MaxDelayMs = 45000,
                IncludeInnerExceptions = true
            };
        }

        /// <summary>
        /// Creates a retry policy for GSX operations
        /// </summary>
        /// <returns>A retry policy for GSX operations</returns>
        public RetryPolicy CreateGSXPolicy()
        {
            return new RetryPolicy(_logger)
            {
                MaxRetries = 3,
                InitialDelayMs = 1000,
                UseExponentialBackoff = true,
                MaxDelayMs = 30000,
                IncludeInnerExceptions = true
            };
        }

        /// <summary>
        /// Creates a retry policy for quick operations that should fail fast
        /// </summary>
        /// <returns>A retry policy for quick operations</returns>
        public RetryPolicy CreateQuickPolicy()
        {
            return new RetryPolicy(_logger)
            {
                MaxRetries = 2,
                InitialDelayMs = 500,
                UseExponentialBackoff = false,
                MaxDelayMs = 1000,
                IncludeInnerExceptions = true
            };
        }

        /// <summary>
        /// Creates a retry policy for long-running operations
        /// </summary>
        /// <returns>A retry policy for long-running operations</returns>
        public RetryPolicy CreateLongRunningPolicy()
        {
            return new RetryPolicy(_logger)
            {
                MaxRetries = 3,
                InitialDelayMs = 5000,
                UseExponentialBackoff = true,
                MaxDelayMs = 120000,
                IncludeInnerExceptions = true
            };
        }

        /// <summary>
        /// Creates a custom retry policy with the specified parameters
        /// </summary>
        /// <param name="maxRetries">The maximum number of retry attempts</param>
        /// <param name="initialDelayMs">The initial delay between retries in milliseconds</param>
        /// <param name="useExponentialBackoff">Whether to use exponential backoff for retry delays</param>
        /// <param name="maxDelayMs">The maximum delay between retries in milliseconds</param>
        /// <param name="includeInnerExceptions">Whether to include inner exceptions in the retry exception</param>
        /// <returns>A custom retry policy</returns>
        public RetryPolicy CreateCustomPolicy(int maxRetries, int initialDelayMs, bool useExponentialBackoff, int maxDelayMs, bool includeInnerExceptions)
        {
            return new RetryPolicy(_logger)
            {
                MaxRetries = maxRetries,
                InitialDelayMs = initialDelayMs,
                UseExponentialBackoff = useExponentialBackoff,
                MaxDelayMs = maxDelayMs,
                IncludeInnerExceptions = includeInnerExceptions
            };
        }
    }
}
