using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Prosim2GSX.Events;
using Prosim2GSX.Services.Prosim.Events;
using Prosim2GSX.Services.Prosim.Interfaces;
using Prosim2GSX.Services.Prosim.Models;
using System;
using System.Net.Http;
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
        private readonly ILogger<LoadsheetService> _logger;

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

        public LoadsheetService(
            ILogger<LoadsheetService> logger,
            IProsimInterface prosimService,
            IFlightPlanService flightPlanService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            _logger.LogInformation("Loadsheet generation flags reset for new flight");
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
                _logger.LogInformation("{Type} loadsheet already generated or being generated for this flight. Skipping generation.", type);
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
                                _logger.LogError("Backend URL is empty. Prosim EFB server may not be running or properly configured.");
                                return LoadsheetResult.CreateFailure(
                                    "Backend URL is empty. Prosim EFB server may not be running or properly configured.");
                            }

                            // Log the exact request being sent
                            _logger.LogDebug("Sending request to URL: {Url}, Content: {Content}",
                                fullUrl, await content.ReadAsStringAsync());

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
                                _logger.LogError("Request timed out after {Elapsed:F1} seconds. Check if Prosim EFB server is running and responsive.",
                                    elapsed.TotalSeconds);

                                if (retryCount < maxRetries)
                                {
                                    retryCount++;
                                    int delayMs = 1000 * retryCount;
                                    _logger.LogWarning("Retrying in {Delay} seconds (attempt {Attempt}/{MaxRetries})...",
                                        delayMs / 1000, retryCount, maxRetries);
                                    await Task.Delay(delayMs);
                                    continue;
                                }

                                return LoadsheetResult.CreateFailure(
                                    "Request timed out. Check if Prosim EFB server is running and responsive.");
                            }
                            catch (HttpRequestException ex)
                            {
                                // Handle connection issues
                                _logger.LogError(ex, "HTTP request error. Check network connectivity and if Prosim EFB server is running.");

                                if (retryCount < maxRetries)
                                {
                                    retryCount++;
                                    int delayMs = 1000 * retryCount;
                                    _logger.LogWarning("Retrying in {Delay} seconds (attempt {Attempt}/{MaxRetries})...",
                                        delayMs / 1000, retryCount, maxRetries);
                                    await Task.Delay(delayMs);
                                    continue;
                                }

                                return LoadsheetResult.CreateFailure(
                                    $"HTTP request error: {ex.Message}. Check network connectivity and if Prosim EFB server is running.");
                            }

                            // Calculate and log request duration
                            TimeSpan requestDuration = DateTime.Now - requestStart;
                            _logger.LogDebug("Request completed in {Duration:F0}ms", requestDuration.TotalMilliseconds);

                            string responseContent = await response.Content.ReadAsStringAsync();

                            // Always log response for debugging
                            _logger.LogDebug("Response Status: {Status}, Content: {Content}", response.StatusCode, responseContent);

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

                                _logger.LogInformation("{Type} loadsheet generated successfully", type);

                                return LoadsheetResult.CreateSuccess();
                            }
                            else
                            {
                                if (retryCount < maxRetries)
                                {
                                    // If we have retries left, log and retry
                                    retryCount++;
                                    int delayMs = 1000 * retryCount; // Exponential backoff

                                    _logger.LogWarning("Failed to generate {Type} loadsheet (attempt {Attempt}/{MaxRetries}). " +
                                        "Retrying in {Delay} seconds...", type, retryCount, maxRetries, delayMs / 1000);

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

                            _logger.LogWarning(ex, "Exception generating {Type} loadsheet (attempt {Attempt}/{MaxRetries}). " +
                                "Retrying in {Delay} seconds...", type, retryCount, maxRetries, delayMs / 1000);

                            await Task.Delay(delayMs);
                            continue;
                        }

                        // Simplified error handling for debugging
                        _logger.LogError(ex, "Exception after {Attempts} attempts", maxRetries + 1);
                        return LoadsheetResult.CreateFailure($"Exception after {maxRetries + 1} attempts: {ex.GetType().Name}, Message: {ex.Message}");
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
                _logger.LogInformation("Resending loadsheet to MCDU");

                // Use the Prosim API to resend the loadsheet
                // This is equivalent to the JavaScript code: this.backend.resendLoadsheet()
                string url = $"{_prosimService.GetBackendUrl()}/loadsheet/resend";
                bool success = await _prosimService.PostAsync(url, "{}");

                if (success)
                {
                    _logger.LogInformation("Loadsheet resent successfully");
                    return true;
                }
                else
                {
                    _logger.LogError("Failed to resend loadsheet");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending loadsheet");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> CheckServerStatus()
        {
            try
            {
                _logger.LogInformation("Checking Prosim EFB server status");

                // Validate the backend URL
                string backendUrl = _prosimService.GetBackendUrl();
                if (string.IsNullOrEmpty(backendUrl))
                {
                    _logger.LogError("Backend URL is empty. Prosim EFB server may not be properly configured.");
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
                        _logger.LogInformation("Server check completed in {Duration:F0}ms. Status: {Status}",
                            requestDuration.TotalMilliseconds, response.StatusCode);

                        return response.IsSuccessStatusCode;
                    }
                    catch (Exception ex)
                    {
                        // Log the error
                        _logger.LogError(ex, "Error checking server status");

                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error checking server status");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ResetLoadsheets()
        {
            try
            {
                _logger.LogInformation("Resetting loadsheets");

                // Use the Prosim API to reset loadsheets
                // This is equivalent to the JavaScript code: this.backend.resetLoadsheets()
                string url = $"{_prosimService.GetBackendUrl()}/loadsheet";
                bool success = await _prosimService.DeleteAsync(url);

                if (success)
                {
                    _logger.LogInformation("Loadsheets reset successfully");
                    return true;
                }
                else
                {
                    _logger.LogError("Failed to reset loadsheets");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting loadsheets");
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
                _logger.LogInformation("Preliminary loadsheet received");
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
                    _logger.LogError(ex, "Error parsing preliminary loadsheet");
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
                _logger.LogInformation("Final loadsheet received");
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
                    _logger.LogError(ex, "Error parsing final loadsheet");
                }
            }
        }
    }
}
