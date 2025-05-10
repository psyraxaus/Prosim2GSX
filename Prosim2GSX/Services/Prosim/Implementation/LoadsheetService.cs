using Newtonsoft.Json;
using Prosim2GSX.Events;
using Prosim2GSX.Services.Logger.Enums;
using Prosim2GSX.Services.Logger.Implementation;
using Prosim2GSX.Services.Prosim.Events;
using Prosim2GSX.Services.Prosim.Interfaces;
using Prosim2GSX.Services.Prosim.Models;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Prosim2GSX.Services.Prosim.Implementation
{
    /// <summary>
    /// Service for interacting with Prosim's native loadsheet functionality
    /// </summary>
    public class LoadsheetService : ILoadsheetService
    {
        private readonly IProsimInterface _prosimService;
        private readonly IFlightPlanService _flightPlanService;
        private readonly IDataRefMonitoringService _dataRefMonitoringService;


        // Track if we've already generated a loadsheet for the current flight
        private bool _preliminaryLoadsheetRequested = false;
        private bool _finalLoadsheetRequested = false;
        private readonly object _loadsheetLock = new object();
        private bool _preliminaryLoadsheetGenerating = false;
        private bool _finalLoadsheetGenerating = false;

        /// <summary>
        /// Event raised when a loadsheet is received
        /// </summary>
        public event EventHandler<LoadsheetReceivedEventArgs> LoadsheetReceived;

        public LoadsheetService(IProsimInterface prosimService, IFlightPlanService flightPlanService)
        {
            _prosimService = prosimService ?? throw new ArgumentNullException(nameof(prosimService));
            _flightPlanService = flightPlanService ?? throw new ArgumentNullException(nameof(flightPlanService));
        }

        /// <inheritdoc/>
        public bool PreliminaryLoadsheetRequested => _preliminaryLoadsheetRequested;

        /// <inheritdoc/>
        public bool FinalLoadsheetRequested => _finalLoadsheetRequested;

        /// <inheritdoc/>
        public void ResetLoadsheetFlags()
        {
            _preliminaryLoadsheetRequested = false;
            _finalLoadsheetRequested = false;
            LogService.Log(LogLevel.Information, nameof(LoadsheetService),
                "Loadsheet generation flags reset for new flight");
        }

        /// <inheritdoc/>
        public async Task<LoadsheetResult> GenerateLoadsheet(string type, int maxRetries = 3, bool force = false)
        {
            // Use a lock to check and set the "generating" flag
            bool shouldGenerate = false;
            lock (_loadsheetLock)
            {
                if (type == "Preliminary")
                {
                    if (!_preliminaryLoadsheetRequested && !_preliminaryLoadsheetGenerating || force)
                    {
                        _preliminaryLoadsheetGenerating = true;
                        shouldGenerate = true;
                    }
                }
                else if (type == "Final")
                {
                    if (!_finalLoadsheetRequested && !_finalLoadsheetGenerating || force)
                    {
                        _finalLoadsheetGenerating = true;
                        shouldGenerate = true;
                    }
                }
            }

            // If we shouldn't generate, return early
            if (!shouldGenerate)
            {
                LogService.Log(LogLevel.Information, nameof(LoadsheetService),
                    $"{type} loadsheet already generated or being generated for this flight. Skipping generation.");
                return LoadsheetResult.CreateSuccess();
            }

            int retryCount = 0;

            try
            {
                while (retryCount <= maxRetries)
                {
                    try
                    {
                        using (var client = new HttpClient())
                        {
                            string fullUrl = $"{_prosimService.GetBackendUrl()}/loadsheet/generate?type={type}";

                            // Make sure the content is exactly the same format - empty JSON object
                            var content = new StringContent("{}", Encoding.UTF8, "application/json");

                            // Validate the backend URL
                            if (string.IsNullOrEmpty(_prosimService.GetBackendUrl()))
                            {
                                LogService.Log(LogLevel.Error, nameof(LoadsheetService),
                                    "Backend URL is empty. Prosim EFB server may not be running or properly configured.");
                                return LoadsheetResult.CreateFailure(
                                    "Backend URL is empty. Prosim EFB server may not be running or properly configured.");
                            }

                            // Log the exact request being sent
                            LogService.Log(LogLevel.Debug, nameof(LoadsheetService),
                                $"Sending request to URL: {fullUrl}, Content: {await content.ReadAsStringAsync()}", LogCategory.Loadsheet);

                            // Set timeout to detect connection issues faster
                            client.Timeout = TimeSpan.FromSeconds(10);

                            // Send the request and capture detailed timing
                            DateTime requestStart = DateTime.Now;
                            HttpResponseMessage response;

                            try
                            {
                                response = await client.PostAsync(fullUrl, content);
                            }
                            catch (TaskCanceledException)
                            {
                                // Handle timeout specifically
                                TimeSpan elapsed = DateTime.Now - requestStart;
                                LogService.Log(LogLevel.Error, nameof(LoadsheetService),
                                    $"Request timed out after {elapsed.TotalSeconds:F1} seconds. Check if Prosim EFB server is running and responsive.");

                                if (retryCount < maxRetries)
                                {
                                    retryCount++;
                                    int delayMs = 1000 * retryCount;
                                    LogService.Log(LogLevel.Warning, nameof(LoadsheetService),
                                        $"Retrying in {delayMs / 1000} seconds (attempt {retryCount}/{maxRetries})...");
                                    await Task.Delay(delayMs);
                                    continue;
                                }

                                return LoadsheetResult.CreateFailure(
                                    "Request timed out. Check if Prosim EFB server is running and responsive.");
                            }
                            catch (HttpRequestException ex)
                            {
                                // Handle connection issues
                                LogService.Log(LogLevel.Error, nameof(LoadsheetService),
                                    $"HTTP request error: {ex.Message}. " +
                                    "Check network connectivity and if Prosim EFB server is running.");

                                if (retryCount < maxRetries)
                                {
                                    retryCount++;
                                    int delayMs = 1000 * retryCount;
                                    LogService.Log(LogLevel.Warning, nameof(LoadsheetService),
                                        $"Retrying in {delayMs / 1000} seconds (attempt {retryCount}/{maxRetries})...");
                                    await Task.Delay(delayMs);
                                    continue;
                                }

                                return LoadsheetResult.CreateFailure(
                                    $"HTTP request error: {ex.Message}. Check network connectivity and if Prosim EFB server is running.");
                            }

                            // Calculate and log request duration
                            TimeSpan requestDuration = DateTime.Now - requestStart;
                            LogService.Log(LogLevel.Debug, nameof(LoadsheetService),
                                $"Request completed in {requestDuration.TotalMilliseconds:F0}ms", LogCategory.Loadsheet);

                            string responseContent = await response.Content.ReadAsStringAsync();

                            // Always log response for debugging
                            LogService.Log(LogLevel.Debug, nameof(LoadsheetService),
                                $"Response Status: {response.StatusCode}, Content: {responseContent}", LogCategory.Loadsheet);

                            if (response.IsSuccessStatusCode)
                            {
                                // Set the flag to indicate we've generated this type of loadsheet
                                lock (_loadsheetLock)
                                {
                                    if (type == "Preliminary")
                                        _preliminaryLoadsheetRequested = true;
                                    else if (type == "Final")
                                        _finalLoadsheetRequested = true;
                                }

                                LogService.Log(LogLevel.Information, nameof(LoadsheetService),
                                    $"{type} loadsheet generated successfully");

                                return LoadsheetResult.CreateSuccess();
                            }
                            else
                            {
                                if (retryCount < maxRetries)
                                {
                                    // If we have retries left, log and retry
                                    retryCount++;
                                    int delayMs = 1000 * retryCount; // Exponential backoff

                                    LogService.Log(LogLevel.Warning, nameof(LoadsheetService),
                                        $"Failed to generate {type} loadsheet (attempt {retryCount}/{maxRetries}). " +
                                        $"Retrying in {delayMs / 1000} seconds...");

                                    await Task.Delay(delayMs);
                                    continue;
                                }

                                // If we've exhausted retries or this is the last attempt, return the failure
                                return LoadsheetResult.CreateFailure(
                                    response.StatusCode,
                                    $"Failed to generate {type} loadsheet after {maxRetries + 1} attempts. Status: {response.StatusCode}",
                                    responseContent
                                );
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (retryCount < maxRetries)
                        {
                            // If we have retries left, log and retry
                            retryCount++;
                            int delayMs = 1000 * retryCount; // Exponential backoff

                            LogService.Log(LogLevel.Warning, nameof(LoadsheetService),
                                $"Exception generating {type} loadsheet (attempt {retryCount}/{maxRetries}): {ex.Message}. " +
                                $"Retrying in {delayMs / 1000} seconds...");

                            await Task.Delay(delayMs);
                            continue;
                        }

                        // Simplified error handling for debugging
                        string errorMessage = $"Exception after {maxRetries + 1} attempts: {ex.GetType().Name}, Message: {ex.Message}";
                        LogService.Log(LogLevel.Error, nameof(LoadsheetService), errorMessage);
                        return LoadsheetResult.CreateFailure(errorMessage);
                    }
                }

                // This should never be reached, but just in case
                return LoadsheetResult.CreateFailure($"Unexpected error generating {type} loadsheet after {maxRetries + 1} attempts");
            }
            finally
            {
                // Always reset the "generating" flag when the method exits (success or failure)
                lock (_loadsheetLock)
                {
                    if (type == "Preliminary")
                        _preliminaryLoadsheetGenerating = false;
                    else if (type == "Final")
                        _finalLoadsheetGenerating = false;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ResendLoadsheet()
        {
            try
            {
                LogService.Log(LogLevel.Information, nameof(LoadsheetService), "Resending loadsheet to MCDU");

                // Use the Prosim API to resend the loadsheet
                // This is equivalent to the JavaScript code: this.backend.resendLoadsheet()
                string url = $"{_prosimService.GetBackendUrl()}/loadsheet/resend";
                bool success = await _prosimService.PostAsync(url, "{}");

                if (success)
                {
                    LogService.Log(LogLevel.Information, nameof(LoadsheetService), "Loadsheet resent successfully");
                    return true;
                }
                else
                {
                    LogService.Log(LogLevel.Error, nameof(LoadsheetService), "Failed to resend loadsheet");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, nameof(LoadsheetService), $"Error resending loadsheet: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> CheckServerStatus()
        {
            try
            {
                LogService.Log(LogLevel.Information, nameof(LoadsheetService), "Checking Prosim EFB server status");

                // Validate the backend URL
                string backendUrl = _prosimService.GetBackendUrl();
                if (string.IsNullOrEmpty(backendUrl))
                {
                    LogService.Log(LogLevel.Error, nameof(LoadsheetService), "Backend URL is empty. Prosim EFB server may not be properly configured.");
                    return false;
                }

                // Create a simple GET request to check if the server is running
                using (var client = new HttpClient())
                {
                    // Set a short timeout to avoid hanging
                    client.Timeout = TimeSpan.FromSeconds(5);

                    // Send the request and capture timing
                    DateTime requestStart = DateTime.Now;

                    try
                    {
                        // Use the health endpoint if available, or fall back to the root
                        string url = $"{backendUrl}/health";
                        var response = await client.GetAsync(url);

                        // Calculate request duration
                        TimeSpan requestDuration = DateTime.Now - requestStart;

                        // Log the result
                        LogService.Log(LogLevel.Information, nameof(LoadsheetService), $"Server check completed in {requestDuration.TotalMilliseconds:F0}ms. Status: {response.StatusCode}");

                        return response.IsSuccessStatusCode;
                    }
                    catch (Exception ex)
                    {
                        // Log the error
                        LogService.Log(LogLevel.Error, nameof(LoadsheetService), $"Error checking server status: {ex.Message}");

                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, nameof(LoadsheetService), $"Unexpected error checking server status: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ResetLoadsheets()
        {
            try
            {
                LogService.Log(LogLevel.Information, nameof(LoadsheetService), "Resetting loadsheets");

                // Use the Prosim API to reset loadsheets
                // This is equivalent to the JavaScript code: this.backend.resetLoadsheets()
                string url = $"{_prosimService.GetBackendUrl()}/loadsheet";
                bool success = await _prosimService.DeleteAsync(url);

                if (success)
                {
                    LogService.Log(LogLevel.Information, nameof(LoadsheetService), "Loadsheets reset successfully");
                    return true;
                }
                else
                {
                    LogService.Log(LogLevel.Error, nameof(LoadsheetService), "Failed to reset loadsheets");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, nameof(LoadsheetService), $"Error resetting loadsheets: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Handler for preliminary loadsheet changes
        /// </summary>
        private void OnPrelimLoadsheetChanged(string dataRef, dynamic oldValue, dynamic newValue)
        {
            if (newValue != null && !string.IsNullOrEmpty(newValue.ToString()))
            {
                LogService.Log(LogLevel.Information, nameof(LoadsheetService), "Preliminary loadsheet received");
                try
                {
                    // Parse the loadsheet data
                    var loadsheetData = JsonConvert.DeserializeObject<dynamic>(newValue.ToString());

                    // Raise the event
                    LoadsheetReceived?.Invoke(this, new LoadsheetReceivedEventArgs
                    {
                        Type = "Preliminary",
                        Data = loadsheetData
                    });
                }
                catch (Exception ex)
                {
                    LogService.Log(LogLevel.Error, nameof(LoadsheetService), $"Error parsing preliminary loadsheet: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handler for final loadsheet changes
        /// </summary>
        private void OnFinalLoadsheetChanged(string dataRef, dynamic oldValue, dynamic newValue)
        {
            if (newValue != null && !string.IsNullOrEmpty(newValue.ToString()))
            {
                LogService.Log(LogLevel.Information, nameof(LoadsheetService), "Final loadsheet received");
                try
                {
                    // Parse the loadsheet data
                    var loadsheetData = JsonConvert.DeserializeObject<dynamic>(newValue.ToString());

                    // Raise the event
                    LoadsheetReceived?.Invoke(this, new LoadsheetReceivedEventArgs
                    {
                        Type = "Final",
                        Data = loadsheetData
                    });
                }
                catch (Exception ex)
                {
                    LogService.Log(LogLevel.Error, nameof(LoadsheetService), $"Error parsing final loadsheet: {ex.Message}");
                }
            }
        }
    }
}
