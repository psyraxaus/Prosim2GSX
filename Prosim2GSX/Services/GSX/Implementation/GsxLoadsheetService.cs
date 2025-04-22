using Newtonsoft.Json;
using Prosim2GSX.Services.GSX.Interfaces;
using Prosim2GSX.Services.GSX.Models;
using Prosim2GSX.Services.Prosim.Interfaces;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Services.GSX.Implementation
{
    /// <summary>
    /// Implementation of loadsheet service
    /// </summary>
    public class GsxLoadsheetService : IGsxLoadsheetService
    {
        private readonly IProsimInterface _prosimInterface;
        private readonly IFlightPlanService _flightPlanService;
        private readonly HttpClient _httpClient;

        private LoadsheetState _prelimLoadsheetState = LoadsheetState.NotStarted;
        private LoadsheetState _finalLoadsheetState = LoadsheetState.NotStarted;

        private DateTime _lastPrelimAttemptTime = DateTime.MinValue;
        private DateTime _lastFinalAttemptTime = DateTime.MinValue;

        private Task<LoadsheetResult> _currentPrelimTask = null;
        private Task<LoadsheetResult> _currentFinalTask = null;

        private string _loadsheetFlightPlanId = null;
        private readonly TimeSpan _minTimeBetweenAttempts = TimeSpan.FromSeconds(30);

        private readonly SemaphoreSlim _prelimLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _finalLock = new SemaphoreSlim(1, 1);

        /// <inheritdoc/>
        public LoadsheetState PreliminaryLoadsheetState => _prelimLoadsheetState;

        /// <inheritdoc/>
        public LoadsheetState FinalLoadsheetState => _finalLoadsheetState;

        /// <summary>
        /// Constructor
        /// </summary>
        public GsxLoadsheetService(IProsimInterface prosimInterface, IFlightPlanService flightPlanService)
        {
            _prosimInterface = prosimInterface ?? throw new ArgumentNullException(nameof(prosimInterface));
            _flightPlanService = flightPlanService ?? throw new ArgumentNullException(nameof(flightPlanService));
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <inheritdoc/>
        public async Task<LoadsheetResult> GeneratePreliminaryLoadsheet()
        {
            // Ensure we don't have multiple attempts running simultaneously
            if (!await _prelimLock.WaitAsync(0))
            {
                Logger.Log(LogLevel.Warning, nameof(GsxLoadsheetService),
                    "Another preliminary loadsheet generation is already in progress");
                return LoadsheetResult.CreateFailure("Another generation already in progress");
            }

            try
            {
                // Check if we've recently tried
                if (DateTime.Now - _lastPrelimAttemptTime < _minTimeBetweenAttempts)
                {
                    Logger.Log(LogLevel.Warning, nameof(GsxLoadsheetService),
                        $"Attempted to generate preliminary loadsheet too soon after previous attempt. " +
                        $"Please wait {_minTimeBetweenAttempts.TotalSeconds} seconds between attempts.");
                    return LoadsheetResult.CreateFailure("Please wait between generation attempts");
                }

                // Update the attempt time
                _lastPrelimAttemptTime = DateTime.Now;

                // Update state
                _prelimLoadsheetState = LoadsheetState.Generating;

                // Store the current flight plan ID to detect changes
                _loadsheetFlightPlanId = _flightPlanService.FlightPlanID;

                // Generate the loadsheet with a timeout
                var result = await GenerateLoadsheet("Preliminary");

                // Update state based on result
                _prelimLoadsheetState = result.Success ? LoadsheetState.Completed : LoadsheetState.Failed;

                return result;
            }
            catch (Exception ex)
            {
                // Update state on error
                _prelimLoadsheetState = LoadsheetState.Failed;

                Logger.Log(LogLevel.Error, nameof(GsxLoadsheetService),
                    $"Exception generating preliminary loadsheet: {ex.Message}");
                return LoadsheetResult.CreateFailure($"Exception: {ex.Message}");
            }
            finally
            {
                // Always release the lock
                _prelimLock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<LoadsheetResult> GenerateFinalLoadsheet()
        {
            // Ensure we don't have multiple attempts running simultaneously
            if (!await _finalLock.WaitAsync(0))
            {
                Logger.Log(LogLevel.Warning, nameof(GsxLoadsheetService),
                    "Another final loadsheet generation is already in progress");
                return LoadsheetResult.CreateFailure("Another generation already in progress");
            }

            try
            {
                // Check if we've recently tried
                if (DateTime.Now - _lastFinalAttemptTime < _minTimeBetweenAttempts)
                {
                    Logger.Log(LogLevel.Warning, nameof(GsxLoadsheetService),
                        $"Attempted to generate final loadsheet too soon after previous attempt. " +
                        $"Please wait {_minTimeBetweenAttempts.TotalSeconds} seconds between attempts.");
                    return LoadsheetResult.CreateFailure("Please wait between generation attempts");
                }

                // Update the attempt time
                _lastFinalAttemptTime = DateTime.Now;

                // Update state
                _finalLoadsheetState = LoadsheetState.Generating;

                // Store the current flight plan ID to detect changes
                _loadsheetFlightPlanId = _flightPlanService.FlightPlanID;

                // Generate the loadsheet with a timeout
                var result = await GenerateLoadsheet("Final");

                // Update state based on result
                _finalLoadsheetState = result.Success ? LoadsheetState.Completed : LoadsheetState.Failed;

                return result;
            }
            catch (Exception ex)
            {
                // Update state on error
                _finalLoadsheetState = LoadsheetState.Failed;

                Logger.Log(LogLevel.Error, nameof(GsxLoadsheetService),
                    $"Exception generating final loadsheet: {ex.Message}");
                return LoadsheetResult.CreateFailure($"Exception: {ex.Message}");
            }
            finally
            {
                // Always release the lock
                _finalLock.Release();
            }
        }

        /// <summary>
        /// Generate a loadsheet
        /// </summary>
        /// <param name="type">Type of loadsheet ("Preliminary" or "Final")</param>
        /// <returns>Result of the loadsheet generation</returns>
        private async Task<LoadsheetResult> GenerateLoadsheet(string type)
        {
            try
            {
                // First check if server is available
                bool serverAvailable = await CheckServerStatus();
                if (!serverAvailable)
                {
                    return LoadsheetResult.CreateFailure(
                        "Prosim EFB server is not available. Check if Prosim is running and the EFB server is properly configured.");
                }

                // Get the backend URL
                string baseUrl = _prosimInterface.GetBackendUrl();

                // Construct the URL for loadsheet generation
                string url = $"{baseUrl}/generateLoadsheet/{type.ToLower()}";
                Logger.Log(LogLevel.Debug, nameof(GsxLoadsheetService), $"Generating {type} loadsheet with URL: {url}");

                // Make the request with timeout
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                {
                    HttpResponseMessage response = await _httpClient.PostAsync(
                        url, new StringContent("{}", System.Text.Encoding.UTF8, "application/json"), cts.Token);

                    // Read the response content regardless of status code
                    string responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        Logger.Log(LogLevel.Information, nameof(GsxLoadsheetService),
                            $"{type} loadsheet generated successfully");
                        return LoadsheetResult.CreateSuccess();
                    }
                    else
                    {
                        string errorMessage = $"Failed to generate {type} loadsheet. Status code: {response.StatusCode}";
                        Logger.Log(LogLevel.Error, nameof(GsxLoadsheetService), errorMessage);

                        if (!string.IsNullOrEmpty(responseContent))
                        {
                            Logger.Log(LogLevel.Error, nameof(GsxLoadsheetService), $"Response content: {responseContent}");
                        }

                        return LoadsheetResult.CreateFailure(response.StatusCode, errorMessage, responseContent);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                string errorMessage = $"Request timeout generating {type} loadsheet";
                Logger.Log(LogLevel.Error, nameof(GsxLoadsheetService), errorMessage);
                return LoadsheetResult.CreateFailure(errorMessage);
            }
            catch (HttpRequestException ex)
            {
                string errorMessage = $"HTTP request exception generating {type} loadsheet: {ex.Message}";
                Logger.Log(LogLevel.Error, nameof(GsxLoadsheetService), errorMessage);

                // Include status code if available
                if (ex.StatusCode.HasValue)
                {
                    return LoadsheetResult.CreateFailure(ex.StatusCode.Value, errorMessage);
                }

                return LoadsheetResult.CreateFailure(errorMessage);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Unexpected error generating {type} loadsheet: {ex.Message}";
                Logger.Log(LogLevel.Error, nameof(GsxLoadsheetService), errorMessage);
                return LoadsheetResult.CreateFailure(errorMessage);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> CheckServerStatus()
        {
            try
            {
                // Get the backend URL
                string baseUrl = _prosimInterface.GetBackendUrl();

                // Construct the URL for status check
                string url = $"{baseUrl}/status";

                // Make the request with a short timeout
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                {
                    HttpResponseMessage response = await _httpClient.GetAsync(url, cts.Token);

                    if (response.IsSuccessStatusCode)
                    {
                        Logger.Log(LogLevel.Debug, nameof(GsxLoadsheetService), "Server status check successful");
                        return true;
                    }
                    else
                    {
                        Logger.Log(LogLevel.Warning, nameof(GsxLoadsheetService),
                            $"Server status check failed. Status code: {response.StatusCode}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, nameof(GsxLoadsheetService),
                    $"Error checking server status: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc/>
        public bool IsLoadsheetAvailable(string type)
        {
            try
            {
                var loadsheetData = _prosimInterface.GetProsimVariable($"efb.{type.ToLower()}Loadsheet");
                return loadsheetData != null && !string.IsNullOrEmpty(loadsheetData.ToString());
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxLoadsheetService),
                    $"Error checking {type} loadsheet: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc/>
        public dynamic GetLoadsheetData(string type)
        {
            try
            {
                return _prosimInterface.GetProsimVariable($"efb.{type.ToLower()}Loadsheet");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxLoadsheetService),
                    $"Error getting {type} loadsheet data: {ex.Message}");
                return null;
            }
        }

        /// <inheritdoc/>
        public void ResetLoadsheetStates()
        {
            // Reset states
            _prelimLoadsheetState = LoadsheetState.NotStarted;
            _finalLoadsheetState = LoadsheetState.NotStarted;

            // Reset timestamps
            _lastPrelimAttemptTime = DateTime.MinValue;
            _lastFinalAttemptTime = DateTime.MinValue;

            // Reset flight plan ID
            _loadsheetFlightPlanId = null;

            Logger.Log(LogLevel.Information, nameof(GsxLoadsheetService),
                "Loadsheet states reset");
        }

        /// <inheritdoc/>
        public void SubscribeToLoadsheetChanges()
        {
            // Subscribe to relevant datarefs if needed
            // For now, this is a placeholder as the current implementation doesn't require subscriptions
            Logger.Log(LogLevel.Debug, nameof(GsxLoadsheetService),
                "Subscribed to loadsheet changes");
        }
    }
}