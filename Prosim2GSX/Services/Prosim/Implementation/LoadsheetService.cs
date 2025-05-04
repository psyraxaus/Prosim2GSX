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
        private DataRefChangedHandler _prelimLoadsheetHandler;
        private DataRefChangedHandler _finalLoadsheetHandler;

        // Track if we've already generated a loadsheet for the current flight
        private bool _prelimLoadsheetGenerated = false;
        private bool _finalLoadsheetGenerated = false;

        /// <summary>
        /// Event raised when a loadsheet is received
        /// </summary>
        public event EventHandler<LoadsheetReceivedEventArgs> LoadsheetReceived;

        public LoadsheetService(IProsimInterface prosimService, IFlightPlanService flightPlanService, IDataRefMonitoringService dataRefMonitoringService)
        {
            _prosimService = prosimService ?? throw new ArgumentNullException(nameof(prosimService));
            _flightPlanService = flightPlanService ?? throw new ArgumentNullException(nameof(flightPlanService));
            _dataRefMonitoringService = dataRefMonitoringService ?? throw new ArgumentNullException(nameof(dataRefMonitoringService));
            _prelimLoadsheetHandler = new DataRefChangedHandler(OnPrelimLoadsheetChanged);
            _finalLoadsheetHandler = new DataRefChangedHandler(OnFinalLoadsheetChanged);
        }

        /// <summary>
        /// Safely check if a dataref exists
        /// </summary>
        /// <param name="dataRef">The dataref to check</param>
        /// <returns>True if the dataref exists, false otherwise</returns>
        private bool DataRefExists(string dataRef)
        {
            try
            {
                var value = _prosimService.GetProsimVariable(dataRef);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public void SubscribeToLoadsheetChanges()
        {
            if (!_flightPlanService.IsFlightplanLoaded())
            {
                LogService.Log(LogLevel.Warning, nameof(LoadsheetService), "Cannot subscribe to loadsheet changes - flight plan not loaded");
                return;
            }

            // Check if the datarefs exist before subscribing
            bool prelimExists = DataRefExists("efb.prelimLoadsheet");
            bool finalExists = DataRefExists("efb.finalLoadsheet");

            if (prelimExists)
            {
                _dataRefMonitoringService.SubscribeToDataRef("efb.prelimLoadsheet", _prelimLoadsheetHandler);
                LogService.Log(LogLevel.Information, nameof(LoadsheetService), "Subscribed to preliminary loadsheet changes");
            }

            if (finalExists)
            {
                _dataRefMonitoringService.SubscribeToDataRef("efb.finalLoadsheet", _finalLoadsheetHandler);
                LogService.Log(LogLevel.Information, nameof(LoadsheetService), "Subscribed to final loadsheet changes");
            }

            if (!prelimExists && !finalExists)
            {
                LogService.Log(LogLevel.Warning, nameof(LoadsheetService),
                    "Loadsheet datarefs don't exist yet. They will be created after successful loadsheet generation.");
            }
        }

        /// <summary>
        /// Reset loadsheet generation flags when flight plan changes
        /// </summary>
        public void ResetLoadsheetFlags()
        {
            _prelimLoadsheetGenerated = false;
            _finalLoadsheetGenerated = false;
            LogService.Log(LogLevel.Information, nameof(LoadsheetService),
                "Loadsheet generation flags reset for new flight");
        }

        /// <inheritdoc/>
        public async Task<LoadsheetResult> GenerateLoadsheet(string type, int maxRetries = 3, bool force = false)
        {
            // Check if we've already generated this type of loadsheet
            if (type == "Preliminary" && _prelimLoadsheetGenerated && !force)
            {
                LogService.Log(LogLevel.Information, nameof(LoadsheetService),
                    "Preliminary loadsheet already generated for this flight. Skipping generation.");
                return LoadsheetResult.CreateSuccess();
            }
            else if (type == "Final" && _finalLoadsheetGenerated && !force)
            {
                LogService.Log(LogLevel.Information, nameof(LoadsheetService),
                    "Final loadsheet already generated for this flight. Skipping generation.");
                return LoadsheetResult.CreateSuccess();
            }

            int retryCount = 0;

            while (retryCount <= maxRetries)
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        string fullUrl = $"{_prosimService.GetBackendUrl()}/loadsheet/generate?type={type}";

                        // Set headers to match exactly what's in the JavaScript request
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                        client.DefaultRequestHeaders.Add("Origin", "http://10.0.1.22:5010");
                        client.DefaultRequestHeaders.Add("Referer", "http://10.0.1.22:5010/");
                        client.DefaultRequestHeaders.Add("User-Agent", "C#HttpClient");

                        // Make sure the content is exactly the same format
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
                            $"Sending request to URL: {fullUrl}, Content: {await content.ReadAsStringAsync()}");

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
                            $"Request completed in {requestDuration.TotalMilliseconds:F0}ms");

                        string responseContent = await response.Content.ReadAsStringAsync();

                        // Always log response for debugging
                        LogService.Log(LogLevel.Debug, nameof(LoadsheetService),
                            $"Response Status: {response.StatusCode}, Content: {responseContent}");

                        if (response.IsSuccessStatusCode)
                        {
                            // After successful generation, try to subscribe to the loadsheet datarefs
                            // as they should now exist
                            SubscribeToLoadsheetChanges();

                            // Set the flag to indicate we've generated this type of loadsheet
                            if (type == "Preliminary")
                                _prelimLoadsheetGenerated = true;
                            else if (type == "Final")
                                _finalLoadsheetGenerated = true;

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
