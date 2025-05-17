using Microsoft.Extensions.Logging;
using ProSimSDK;
using Prosim2GSX.Models;
using Prosim2GSX.Services.Prosim.Interfaces;
using Prosim2GSX.Services.Prosim.Models;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Prosim2GSX.Services.Prosim.Implementation
{
    public class ProsimInterface : IProsimInterface
    {
        private readonly ServiceModel _model;
        private readonly ProSimConnect _connection;
        private readonly ILogger<ProsimInterface> _logger;

        public ProsimInterface(
            ILogger<ProsimInterface> logger,
            ServiceModel model,
            ProSimConnect connection)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _model = model ?? throw new ArgumentNullException(nameof(model));

            // If custom path is specified, set it for ProSimConnect
            if (!string.IsNullOrEmpty(_model.ProsimSDKPath) && File.Exists(_model.ProsimSDKPath))
            {
                string directory = Path.GetDirectoryName(_model.ProsimSDKPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    DllLoader.AddDllDirectory(directory);
                    _logger.LogInformation("Using Prosim SDK from: {Path}", _model.ProsimSDKPath);
                }
            }

            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public void ConnectProsimSDK()
        {
            try
            {
                _logger.LogDebug("Attempting to connect to Prosim Server: {Hostname}", _model.ProsimHostname);
                _connection.Connect(_model.ProsimHostname);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to ProSim System");
            }
        }

        public bool IsProsimReady()
        {
            if (_connection.isConnected)
            {
                _logger.LogDebug("Connection to Prosim server established");
                return true;
            }
            else
            {
                return false;
            }
        }

        public dynamic GetProsimVariable(string dataRef)
        {
            _logger.LogDebug("Attempting to get dataref: {DataRef}", dataRef);
            try
            {
                return _connection.ReadDataRef(dataRef);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading {DataRef}", dataRef);
                return null;
            }
        }

        public void SetProsimVariable(string dataRef, object value)
        {
            DataRef prosimDataRef = new DataRef(dataRef, 100, _connection);
            _logger.LogDebug("Attempting to set dataref: {DataRef}", dataRef);
            try
            {
                prosimDataRef.value = value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting {DataRef} to {Value}", dataRef, value);
            }
        }

        public string GetBackendUrl()
        {
            return "http://127.0.0.1:5000/efb";
        }

        public async Task<bool> PostAsync(string url, string jsonContent)
        {
            var result = await PostAsyncWithDetails(url, jsonContent);
            return result.Success;
        }

        public async Task<HttpOperationResult> PostAsyncWithDetails(string url, string jsonContent)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);

                    StringContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    _logger.LogDebug("Posting to {Url}", url);
                    HttpResponseMessage response = await client.PostAsync(url, content);

                    // Read the response content regardless of status code
                    string responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogDebug("POST to {Url} successful", url);
                        return HttpOperationResult.CreateSuccess();
                    }
                    else
                    {
                        string errorMessage = $"POST to {url} failed with status code {response.StatusCode}";
                        _logger.LogError(errorMessage);

                        // Log response content if available
                        if (!string.IsNullOrEmpty(responseContent))
                        {
                            _logger.LogError("Response content: {Content}", responseContent);
                        }

                        return HttpOperationResult.CreateFailure(response.StatusCode, errorMessage, responseContent);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                string errorMessage = $"HTTP request exception posting to {url}";
                _logger.LogError(ex, errorMessage);

                // Include status code if available
                if (ex.StatusCode.HasValue)
                {
                    return HttpOperationResult.CreateFailure(ex.StatusCode.Value, errorMessage);
                }

                return HttpOperationResult.CreateFailure(errorMessage);
            }
            catch (TaskCanceledException ex)
            {
                string errorMessage = $"Request timeout posting to {url}";
                _logger.LogError(ex, errorMessage);
                return HttpOperationResult.CreateFailure(errorMessage);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error posting to {url}";
                _logger.LogError(ex, errorMessage);
                return HttpOperationResult.CreateFailure(errorMessage);
            }
        }

        public async Task<bool> DeleteAsync(string url)
        {
            var result = await DeleteAsyncWithDetails(url);
            return result.Success;
        }

        public async Task<HttpOperationResult> DeleteAsyncWithDetails(string url)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);

                    _logger.LogDebug("Deleting from {Url}", url);
                    HttpResponseMessage response = await client.DeleteAsync(url);

                    // Read the response content regardless of status code
                    string responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogDebug("DELETE to {Url} successful", url);
                        return HttpOperationResult.CreateSuccess();
                    }
                    else
                    {
                        string errorMessage = $"DELETE to {url} failed with status code {response.StatusCode}";
                        _logger.LogError(errorMessage);

                        // Log response content if available
                        if (!string.IsNullOrEmpty(responseContent))
                        {
                            _logger.LogError("Response content: {Content}", responseContent);
                        }

                        return HttpOperationResult.CreateFailure(response.StatusCode, errorMessage, responseContent);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                string errorMessage = $"HTTP request exception deleting from {url}";
                _logger.LogError(ex, errorMessage);

                // Include status code if available
                if (ex.StatusCode.HasValue)
                {
                    return HttpOperationResult.CreateFailure(ex.StatusCode.Value, errorMessage);
                }

                return HttpOperationResult.CreateFailure(errorMessage);
            }
            catch (TaskCanceledException ex)
            {
                string errorMessage = $"Request timeout deleting from {url}";
                _logger.LogError(ex, errorMessage);
                return HttpOperationResult.CreateFailure(errorMessage);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error deleting from {url}";
                _logger.LogError(ex, errorMessage);
                return HttpOperationResult.CreateFailure(errorMessage);
            }
        }

        public int GetStatusFunction(string dataRef)
        {
            var value = GetProsimVariable(dataRef);

            // Convert boolean values to integers
            if (value is bool boolValue)
            {
                return boolValue ? 1 : 0;
            }

            // Try to convert to integer if it's not already
            if (value is int intValue)
            {
                return intValue;
            }

            // For other numeric types, convert to int
            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                // Log the issue and return 0 as a fallback
                LoggerExtensions.LogWarning(_logger,
                    "Could not convert value of type {Type} to int for dataRef {DataRef}",
                    value?.GetType().Name ?? "null", dataRef);
                return 0;
            }
        }
    }
}
