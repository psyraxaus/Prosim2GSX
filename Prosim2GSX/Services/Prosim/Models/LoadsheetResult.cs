using System.Net;

namespace Prosim2GSX.Services.Prosim.Models
{
    /// <summary>
    /// Represents the result of a loadsheet generation operation
    /// </summary>
    public class LoadsheetResult
    {
        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// HTTP status code if applicable
        /// </summary>
        public HttpStatusCode? StatusCode { get; set; }

        /// <summary>
        /// Error message if unsuccessful
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Response content from the server
        /// </summary>
        public string ResponseContent { get; set; }

        /// <summary>
        /// Create a successful result
        /// </summary>
        public static LoadsheetResult CreateSuccess()
        {
            return new LoadsheetResult
            {
                Success = true
            };
        }

        /// <summary>
        /// Create a failed result with error message
        /// </summary>
        public static LoadsheetResult CreateFailure(string errorMessage)
        {
            return new LoadsheetResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        /// <summary>
        /// Create a failed result with HTTP details
        /// </summary>
        public static LoadsheetResult CreateFailure(HttpStatusCode statusCode, string errorMessage, string responseContent = null)
        {
            return new LoadsheetResult
            {
                Success = false,
                StatusCode = statusCode,
                ErrorMessage = errorMessage,
                ResponseContent = responseContent
            };
        }
    }
}
