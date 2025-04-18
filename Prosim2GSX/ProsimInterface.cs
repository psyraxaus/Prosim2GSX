using ProSimSDK;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Prosim2GSX.Models;

namespace Prosim2GSX
{
    public class ProsimInterface
    {
        protected ServiceModel Model;
        protected ProSimConnect Connection;

        public ProsimInterface(ServiceModel model, ProSimConnect _connection)
        {
            Model = model;
            Connection = _connection;
        }

        public void ConnectProsimSDK()
        {
            try
            {
                Logger.Log(LogLevel.Debug, "ProsimInterface:ConnectProsimSDK", $"Attempting to connect to Prosim Server: {Model.ProsimHostname}");
                Connection.Connect(Model.ProsimHostname);

            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimInterface:ConnectProsimSDK", $"Error connecting to ProSim System: {ex.Message}");

            }
        }

        public bool IsProsimReady()
        {
            if (Connection.isConnected)
            {
                Logger.Log(LogLevel.Debug, "ProsimInterface:IsProsimReady", $"Connection to Prosim server established populating dataref table");
                //ParseSupportedDatarefs();
                return true;
            }
            else
            {
                return false;
            }
        }

        public dynamic GetProsimVariable(string _dataRef)
        {
            Logger.Log(LogLevel.Debug, "ProsimInterface:GetProsimVariable", $"Attempting to get dataref: {_dataRef}");
            try
            {
                return Connection.ReadDataRef(_dataRef);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimInterface:ReadDataRef", $"There was an error reading {_dataRef} - exception {ex.ToString()}");
                return null;
            }
        }

        public void SetProsimVariable(string _dataRef, object value)
        {
            DataRef dataRef = new DataRef(_dataRef, 100, Connection);
            Logger.Log(LogLevel.Debug, "ProsimInterface:SetProsimVariable", $"Attempting to set dataref: {_dataRef}");
            try
            {
                dataRef.value = value;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimInterface:SetProsimSetVariable", $"There was an error setting {_dataRef} value {value} - exception {ex.ToString()}");
            }
        }

        /// <summary>
        /// Get the Prosim backend URL
        /// </summary>
        /// <returns>The backend URL</returns>
        public string GetBackendUrl()
        {
            // Return the hardcoded URL for Prosim's backend
            return "http://127.0.0.1:5000/efb";
        }

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

        /// <summary>
        /// Make a POST request to the Prosim backend
        /// </summary>
        /// <param name="url">The URL to post to</param>
        /// <param name="jsonContent">The JSON content to post</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> PostAsync(string url, string jsonContent)
        {
            var result = await PostAsyncWithDetails(url, jsonContent);
            return result.Success;
        }
        
        /// <summary>
        /// Make a POST request to the Prosim backend with detailed result
        /// </summary>
        /// <param name="url">The URL to post to</param>
        /// <param name="jsonContent">The JSON content to post</param>
        /// <returns>Detailed result of the operation</returns>
        public async Task<HttpOperationResult> PostAsyncWithDetails(string url, string jsonContent)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    
                    StringContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    
                    Logger.Log(LogLevel.Debug, "ProsimInterface:PostAsync", $"Posting to {url}");
                    HttpResponseMessage response = await client.PostAsync(url, content);
                    
                    // Read the response content regardless of status code
                    string responseContent = await response.Content.ReadAsStringAsync();
                    
                    if (response.IsSuccessStatusCode)
                    {
                        Logger.Log(LogLevel.Debug, "ProsimInterface:PostAsync", $"POST to {url} successful");
                        return HttpOperationResult.CreateSuccess();
                    }
                    else
                    {
                        string errorMessage = $"POST to {url} failed with status code {response.StatusCode}";
                        Logger.Log(LogLevel.Error, "ProsimInterface:PostAsync", errorMessage);
                        
                        // Log response content if available
                        if (!string.IsNullOrEmpty(responseContent))
                        {
                            Logger.Log(LogLevel.Error, "ProsimInterface:PostAsync", $"Response content: {responseContent}");
                        }
                        
                        return HttpOperationResult.CreateFailure(response.StatusCode, errorMessage, responseContent);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                string errorMessage = $"HTTP request exception posting to {url}: {ex.Message}";
                Logger.Log(LogLevel.Error, "ProsimInterface:PostAsync", errorMessage);
                
                // Include status code if available
                if (ex.StatusCode.HasValue)
                {
                    return HttpOperationResult.CreateFailure(ex.StatusCode.Value, errorMessage);
                }
                
                return HttpOperationResult.CreateFailure(errorMessage);
            }
            catch (TaskCanceledException ex)
            {
                string errorMessage = $"Request timeout posting to {url}: {ex.Message}";
                Logger.Log(LogLevel.Error, "ProsimInterface:PostAsync", errorMessage);
                return HttpOperationResult.CreateFailure(errorMessage);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error posting to {url}: {ex.Message}";
                Logger.Log(LogLevel.Error, "ProsimInterface:PostAsync", errorMessage);
                return HttpOperationResult.CreateFailure(errorMessage);
            }
        }

        /// <summary>
        /// Make a DELETE request to the Prosim backend
        /// </summary>
        /// <param name="url">The URL to delete from</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> DeleteAsync(string url)
        {
            var result = await DeleteAsyncWithDetails(url);
            return result.Success;
        }
        
        /// <summary>
        /// Make a DELETE request to the Prosim backend with detailed result
        /// </summary>
        /// <param name="url">The URL to delete from</param>
        /// <returns>Detailed result of the operation</returns>
        public async Task<HttpOperationResult> DeleteAsyncWithDetails(string url)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    
                    Logger.Log(LogLevel.Debug, "ProsimInterface:DeleteAsync", $"Deleting from {url}");
                    HttpResponseMessage response = await client.DeleteAsync(url);
                    
                    // Read the response content regardless of status code
                    string responseContent = await response.Content.ReadAsStringAsync();
                    
                    if (response.IsSuccessStatusCode)
                    {
                        Logger.Log(LogLevel.Debug, "ProsimInterface:DeleteAsync", $"DELETE to {url} successful");
                        return HttpOperationResult.CreateSuccess();
                    }
                    else
                    {
                        string errorMessage = $"DELETE to {url} failed with status code {response.StatusCode}";
                        Logger.Log(LogLevel.Error, "ProsimInterface:DeleteAsync", errorMessage);
                        
                        // Log response content if available
                        if (!string.IsNullOrEmpty(responseContent))
                        {
                            Logger.Log(LogLevel.Error, "ProsimInterface:DeleteAsync", $"Response content: {responseContent}");
                        }
                        
                        return HttpOperationResult.CreateFailure(response.StatusCode, errorMessage, responseContent);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                string errorMessage = $"HTTP request exception deleting from {url}: {ex.Message}";
                Logger.Log(LogLevel.Error, "ProsimInterface:DeleteAsync", errorMessage);
                
                // Include status code if available
                if (ex.StatusCode.HasValue)
                {
                    return HttpOperationResult.CreateFailure(ex.StatusCode.Value, errorMessage);
                }
                
                return HttpOperationResult.CreateFailure(errorMessage);
            }
            catch (TaskCanceledException ex)
            {
                string errorMessage = $"Request timeout deleting from {url}: {ex.Message}";
                Logger.Log(LogLevel.Error, "ProsimInterface:DeleteAsync", errorMessage);
                return HttpOperationResult.CreateFailure(errorMessage);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error deleting from {url}: {ex.Message}";
                Logger.Log(LogLevel.Error, "ProsimInterface:DeleteAsync", errorMessage);
                return HttpOperationResult.CreateFailure(errorMessage);
            }
        }
    }
}
