using System.Net;

namespace Prosim2GSX.Services.Prosim.Models
{
    /// <summary>
    /// Result of an HTTP operation
    /// </summary>
    public class HttpOperationResult
    {
        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// HTTP status code returned by the server
        /// </summary>
        public HttpStatusCode? StatusCode { get; set; }

        /// <summary>
        /// Error message if the operation failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Response content if available
        /// </summary>
        public string ResponseContent { get; set; }

        /// <summary>
        /// Creates a successful result
        /// </summary>
        public static HttpOperationResult CreateSuccess() => new HttpOperationResult { Success = true };

        /// <summary>
        /// Creates a failed result with the specified error message
        /// </summary>
        public static HttpOperationResult CreateFailure(string errorMessage) =>
            new HttpOperationResult { Success = false, ErrorMessage = errorMessage };

        /// <summary>
        /// Creates a failed result with HTTP details
        /// </summary>
        public static HttpOperationResult CreateFailure(HttpStatusCode statusCode, string errorMessage, string responseContent = null) =>
            new HttpOperationResult
            {
                Success = false,
                StatusCode = statusCode,
                ErrorMessage = errorMessage,
                ResponseContent = responseContent
            };
    }
}