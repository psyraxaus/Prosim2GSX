using ProSimSDK;
using Prosim2GSX.Models;
using Prosim2GSX.Services.Prosim.Interfaces;
using Prosim2GSX.Services.Prosim.Models;
using System;
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

        public ProsimInterface(ServiceModel model, ProSimConnect connection)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public void ConnectProsimSDK()
        {
            try
            {
                Logger.Log(LogLevel.Debug, nameof(ProsimInterface), $"Attempting to connect to Prosim Server: {_model.ProsimHostname}");
                _connection.Connect(_model.ProsimHostname);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(ProsimInterface), $"Error connecting to ProSim System: {ex.Message}");
            }
        }

        public bool IsProsimReady()
        {
            if (_connection.isConnected)
            {
                Logger.Log(LogLevel.Debug, nameof(ProsimInterface), "Connection to Prosim server established");
                return true;
            }
            else
            {
                return false;
            }
        }

        public dynamic GetProsimVariable(string dataRef)
        {
            Logger.Log(LogLevel.Debug, nameof(ProsimInterface), $"Attempting to get dataref: {dataRef}");
            try
            {
                return _connection.ReadDataRef(dataRef);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(ProsimInterface), $"Error reading {dataRef}: {ex.Message}");
                return null;
            }
        }

        public void SetProsimVariable(string dataRef, object value)
        {
            DataRef prosimDataRef = new DataRef(dataRef, 100, _connection);
            Logger.Log(LogLevel.Debug, nameof(ProsimInterface), $"Attempting to set dataref: {dataRef}");
            try
            {
                prosimDataRef.value = value;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(ProsimInterface), $"Error setting {dataRef} to {value}: {ex.Message}");
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

                    Logger.Log(LogLevel.Debug, nameof(ProsimInterface), $"Posting to {url}");
                    HttpResponseMessage response = await client.PostAsync(url, content);

                    // Read the response content regardless of status code
                    string responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        Logger.Log(LogLevel.Debug, nameof(ProsimInterface), $"POST to {url} successful");
                        return HttpOperationResult.CreateSuccess();
                    }
                    else
                    {
                        string errorMessage = $"POST to {url} failed with status code {response.StatusCode}";
                        Logger.Log(LogLevel.Error, nameof(ProsimInterface), errorMessage);

                        // Log response content if available
                        if (!string.IsNullOrEmpty(responseContent))
                        {
                            Logger.Log(LogLevel.Error, nameof(ProsimInterface), $"Response content: {responseContent}");
                        }

                        return HttpOperationResult.CreateFailure(response.StatusCode, errorMessage, responseContent);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                string errorMessage = $"HTTP request exception posting to {url}: {ex.Message}";
                Logger.Log(LogLevel.Error, nameof(ProsimInterface), errorMessage);

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
                Logger.Log(LogLevel.Error, nameof(ProsimInterface), errorMessage);
                return HttpOperationResult.CreateFailure(errorMessage);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error posting to {url}: {ex.Message}";
                Logger.Log(LogLevel.Error, nameof(ProsimInterface), errorMessage);
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

                    Logger.Log(LogLevel.Debug, nameof(ProsimInterface), $"Deleting from {url}");
                    HttpResponseMessage response = await client.DeleteAsync(url);

                    // Read the response content regardless of status code
                    string responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        Logger.Log(LogLevel.Debug, nameof(ProsimInterface), $"DELETE to {url} successful");
                        return HttpOperationResult.CreateSuccess();
                    }
                    else
                    {
                        string errorMessage = $"DELETE to {url} failed with status code {response.StatusCode}";
                        Logger.Log(LogLevel.Error, nameof(ProsimInterface), errorMessage);

                        // Log response content if available
                        if (!string.IsNullOrEmpty(responseContent))
                        {
                            Logger.Log(LogLevel.Error, nameof(ProsimInterface), $"Response content: {responseContent}");
                        }

                        return HttpOperationResult.CreateFailure(response.StatusCode, errorMessage, responseContent);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                string errorMessage = $"HTTP request exception deleting from {url}: {ex.Message}";
                Logger.Log(LogLevel.Error, nameof(ProsimInterface), errorMessage);

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
                Logger.Log(LogLevel.Error, nameof(ProsimInterface), errorMessage);
                return HttpOperationResult.CreateFailure(errorMessage);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error deleting from {url}: {ex.Message}";
                Logger.Log(LogLevel.Error, nameof(ProsimInterface), errorMessage);
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
                Logger.Log(LogLevel.Warning, nameof(ProsimInterface),
                    $"Could not convert value of type {value?.GetType().Name ?? "null"} to int for dataRef {dataRef}");
                return 0;
            }
        }
    }
}