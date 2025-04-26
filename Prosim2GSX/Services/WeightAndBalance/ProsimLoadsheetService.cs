using Newtonsoft.Json;
using Prosim2GSX.Events;
using System;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Prosim2GSX.Services.Prosim.Interfaces;
using Prosim2GSX.Services.Logger.Enums;
using Prosim2GSX.Services.Logger.Implementation;

namespace Prosim2GSX.Services.WeightAndBalance
{
    /// <summary>
    /// Result of a loadsheet generation operation
    /// </summary>
    public class LoadsheetResult
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
        public static LoadsheetResult CreateSuccess() => new LoadsheetResult { Success = true };
        
        /// <summary>
        /// Creates a failed result with the specified error message
        /// </summary>
        public static LoadsheetResult CreateFailure(string errorMessage) => 
            new LoadsheetResult { Success = false, ErrorMessage = errorMessage };
            
        /// <summary>
        /// Creates a failed result with HTTP details
        /// </summary>
        public static LoadsheetResult CreateFailure(HttpStatusCode statusCode, string errorMessage, string responseContent = null) => 
            new LoadsheetResult 
            { 
                Success = false, 
                StatusCode = statusCode, 
                ErrorMessage = errorMessage,
                ResponseContent = responseContent
            };
    }

    /// <summary>
    /// Service for interacting with Prosim's native loadsheet functionality
    /// </summary>
    public class ProsimLoadsheetService
    {
        //private readonly ProsimController _prosimController;
        private IProsimInterface _prosimInterface;
        private IFlightPlanService _flightPlanService;
        private DataRefChangedHandler _prelimLoadsheetHandler;
        private DataRefChangedHandler _finalLoadsheetHandler;

        /// <summary>
        /// Event raised when a loadsheet is received
        /// </summary>
        public event EventHandler<LoadsheetReceivedEventArgs> LoadsheetReceived;
/*
        public ProsimLoadsheetService(ProsimController prosimController)
        {
            _prosimController = prosimController ?? throw new ArgumentNullException(nameof(prosimController));
            _prelimLoadsheetHandler = new DataRefChangedHandler(OnPrelimLoadsheetChanged);
            _finalLoadsheetHandler = new DataRefChangedHandler(OnFinalLoadsheetChanged);
        }
*/
        public ProsimLoadsheetService(IProsimInterface prosimInterface, IFlightPlanService flightPlanService)
        {
            _prosimInterface = prosimInterface;
            _flightPlanService = flightPlanService;
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
                var value = _prosimInterface.GetProsimVariable(dataRef);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Subscribe to loadsheet data changes - should only be called after flight plan is loaded
        /// and loadsheet datarefs exist
        /// </summary>
        public void SubscribeToLoadsheetChanges()
        {
            if (!ServiceLocator.FlightPlanService.IsFlightplanLoaded())
            {
                LogService.Log(LogLevel.Warning, "ProsimLoadsheetService", "Cannot subscribe to loadsheet changes - flight plan not loaded");
                return;
            }
            
            // Check if the datarefs exist before subscribing
            bool prelimExists = DataRefExists("efb.prelimLoadsheet");
            bool finalExists = DataRefExists("efb.finalLoadsheet");
            
            if (prelimExists)
            {
                ServiceLocator.DataRefService.SubscribeToDataRef("efb.prelimLoadsheet", _prelimLoadsheetHandler);
                LogService.Log(LogLevel.Information, "ProsimLoadsheetService", "Subscribed to preliminary loadsheet changes");
            }
            
            if (finalExists)
            {
                ServiceLocator.DataRefService.SubscribeToDataRef("efb.finalLoadsheet", _finalLoadsheetHandler);
                LogService.Log(LogLevel.Information, "ProsimLoadsheetService", "Subscribed to final loadsheet changes");
            }
            
            if (!prelimExists && !finalExists)
            {
                LogService.Log(LogLevel.Warning, "ProsimLoadsheetService", 
                    "Loadsheet datarefs don't exist yet. They will be created after successful loadsheet generation.");
            }
        }

        /// <summary>
        /// Prepares the necessary datarefs for loadsheet generation
        /// </summary>
        /// <param name="paxCount">Number of passengers</param>
        /// <param name="cargoKg">Cargo weight in kg</param>
        /// <param name="fuelKg">Fuel weight in kg</param>
        private void PrepareLoadsheetDatarefs(int paxCount, double cargoKg, double fuelKg)
        {
            try
            {
                // 1. Set passenger seat map using ProsimController's existing method
                // This ensures we use the same format that's already working elsewhere
                bool[] seatMap = ServiceLocator.PassengerService.RandomizePassengerSeating(paxCount);

                // Convert to comma-separated string
                StringBuilder seatMapString = new StringBuilder();
                for (int i = 0; i < seatMap.Length; i++)
                {
                    seatMapString.Append(seatMap[i] ? "true" : "false");
                    if (i < seatMap.Length - 1)
                        seatMapString.Append(",");
                }
                
                ServiceLocator.ProsimInterface.SetProsimVariable("efb.passengers.booked.string", seatMapString.ToString());
                
                // 2. Set passenger statistics (simplified - we'll assume all economy)
                var passengerStats = new
                {
                    NumOfPaxInBusiness = 0,
                    NumOfPaxInEconomy = paxCount,
                    NumOfPaxInSection1 = (int)(paxCount * 0.5), // Front half
                    NumOfPaxInSection2 = (int)(paxCount * 0.4), // Middle section
                    NumOfPaxInSection3 = paxCount - (int)(paxCount * 0.5) - (int)(paxCount * 0.4), // Remainder
                    Total = paxCount
                };
                ServiceLocator.ProsimInterface.SetProsimVariable("efb.passengerStatistics", 
                    JsonConvert.SerializeObject(passengerStats));
                
                // 3. Set planned cargo
                ServiceLocator.ProsimInterface.SetProsimVariable("efb.plannedCargoKg", cargoKg);
                
                // 4. Set fuel target
                var fuelTarget = new { refuelTarget = fuelKg };
                ServiceLocator.ProsimInterface.SetProsimVariable("aircraft.refuel.fuelTarget", 
                    JsonConvert.SerializeObject(fuelTarget));
                
                LogService.Log(LogLevel.Information, "ProsimLoadsheetService", 
                    $"Prepared loadsheet datarefs: {paxCount} passengers, {cargoKg}kg cargo, {fuelKg}kg fuel");
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, "ProsimLoadsheetService", 
                    $"Error preparing loadsheet datarefs: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if fuel planning is complete and loadsheet generation can proceed
        /// </summary>
        /// <returns>True if fuel target is set and valid, false otherwise</returns>
        public bool IsFuelPlanningComplete()
        {
            try
            {
                // Get current fuel amount and fuel target
                double currentFuel = ServiceLocator.RefuelingService.GetFuelAmount();
                double fuelTarget = 0;
                
                // Try to get the fuel target value
                try
                {
                    var targetValue = _prosimInterface.GetProsimVariable("aircraft.refuel.fuelTarget");
                    if (targetValue != null)
                    {
                        // Convert to double if possible
                        fuelTarget = Convert.ToDouble(targetValue);
                    }
                }
                catch (Exception ex)
                {
                    LogService.Log(LogLevel.Debug, "ProsimLoadsheetService", 
                        $"Could not get fuel target: {ex.Message}");
                    return false;
                }
                
                // Check if fuel target is set (greater than 0)
                if (fuelTarget <= 0)
                {
                    LogService.Log(LogLevel.Debug, "ProsimLoadsheetService", 
                        $"Fuel target not set yet (value: {fuelTarget})");
                    return false;
                }
                
                // Log the current fuel situation
                LogService.Log(LogLevel.Debug, "ProsimLoadsheetService", 
                    $"Fuel planning check: Current={currentFuel}kg, Target={fuelTarget}kg, " +
                    $"Sufficient={currentFuel >= fuelTarget}");
                
                // Fuel planning is complete if target is set
                return true;
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, "ProsimLoadsheetService", 
                    $"Error checking fuel planning: {ex.Message}");
                return false;
            }
        }
        
        // Track if we've already generated a loadsheet for the current flight
        private bool _prelimLoadsheetGenerated = false;
        private bool _finalLoadsheetGenerated = false;
        
        /// <summary>
        /// Reset loadsheet generation flags when flight plan changes
        /// </summary>
        public void ResetLoadsheetFlags()
        {
            _prelimLoadsheetGenerated = false;
            _finalLoadsheetGenerated = false;
            LogService.Log(LogLevel.Information, "ProsimLoadsheetService", 
                "Loadsheet generation flags reset for new flight");
        }

        /// <summary>
        /// Generate a loadsheet using Prosim's native functionality
        /// </summary>
        /// <param name="type">Type of loadsheet ("Preliminary" or "Final")</param>
        /// <param name="maxRetries">Maximum number of retries if generation fails</param>
        /// <param name="force">Force generation even if already generated</param>
        /// <returns>Task that completes with detailed result information</returns>
        public async Task<LoadsheetResult> GenerateLoadsheet(string type, int maxRetries = 3, bool force = false)
        {
            // Check if we've already generated this type of loadsheet
            if (type == "Preliminary" && _prelimLoadsheetGenerated && !force)
            {
                LogService.Log(LogLevel.Information, "ProsimLoadsheetService", 
                    "Preliminary loadsheet already generated for this flight. Skipping generation.");
                return LoadsheetResult.CreateSuccess();
            }
            else if (type == "Final" && _finalLoadsheetGenerated && !force)
            {
                LogService.Log(LogLevel.Information, "ProsimLoadsheetService", 
                    "Final loadsheet already generated for this flight. Skipping generation.");
                return LoadsheetResult.CreateSuccess();
            }
            
            // Check if fuel planning is complete before proceeding
            if (!IsFuelPlanningComplete())
            {
                LogService.Log(LogLevel.Warning, "ProsimLoadsheetService", 
                    $"Cannot generate {type} loadsheet - fuel planning not complete");
                return LoadsheetResult.CreateFailure(
                    $"Fuel planning not complete. Set fuel target before generating loadsheet.");
            }
            
            int retryCount = 0;
            
            while (retryCount <= maxRetries)
            {
                try
                {
                    // Get flight data from the controller
                    int paxCount = ServiceLocator.PassengerService.GetPlannedPassengers();
                    double cargoKg = 6000; // Default cargo weight if not available
                    double fuelKg = ServiceLocator.RefuelingService.GetFuelAmount();
                    
                    // Prepare the datarefs
                    PrepareLoadsheetDatarefs(paxCount, cargoKg, fuelKg);
                    
                    using (var client = new HttpClient())
                    {
                        string fullUrl = $"{ServiceLocator.ProsimInterface.GetBackendUrl()}/loadsheet/generate?type={type}";
                        
                        // Set headers to match exactly what's in the JavaScript request
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                        client.DefaultRequestHeaders.Add("Origin", "http://10.0.1.22:5010");
                        client.DefaultRequestHeaders.Add("Referer", "http://10.0.1.22:5010/");
                        client.DefaultRequestHeaders.Add("User-Agent", "C#HttpClient");
                        
                        // Make sure the content is exactly the same format
                        var content = new StringContent("{}", Encoding.UTF8, "application/json");
                        
                        // Validate the backend URL
                        if (string.IsNullOrEmpty(ServiceLocator.ProsimInterface.GetBackendUrl()))
                        {
                            LogService.Log(LogLevel.Error, "ProsimLoadsheetService", 
                                "Backend URL is empty. Prosim EFB server may not be running or properly configured.");
                            return LoadsheetResult.CreateFailure(
                                "Backend URL is empty. Prosim EFB server may not be running or properly configured.");
                        }
                        
                        // Log the exact request being sent
                        LogService.Log(LogLevel.Debug, "ProsimLoadsheetService", 
                            $"Sending request to URL: {fullUrl}, Content: {await content.ReadAsStringAsync()}");
                        
                        // Log flight data being used for loadsheet generation
                        LogService.Log(LogLevel.Debug, "ProsimLoadsheetService", 
                            $"Generating {type} loadsheet with: {paxCount} passengers, {cargoKg}kg cargo, {fuelKg}kg fuel");
                        
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
                            LogService.Log(LogLevel.Error, "ProsimLoadsheetService", 
                                $"Request timed out after {elapsed.TotalSeconds:F1} seconds. " +
                                "Check if Prosim EFB server is running and responsive.");
                            
                            if (retryCount < maxRetries)
                            {
                                retryCount++;
                                int delayMs = 1000 * retryCount;
                                LogService.Log(LogLevel.Warning, "ProsimLoadsheetService", 
                                    $"Retrying in {delayMs/1000} seconds (attempt {retryCount}/{maxRetries})...");
                                await Task.Delay(delayMs);
                                continue;
                            }
                            
                            return LoadsheetResult.CreateFailure(
                                "Request timed out. Check if Prosim EFB server is running and responsive.");
                        }
                        catch (HttpRequestException ex)
                        {
                            // Handle connection issues
                            LogService.Log(LogLevel.Error, "ProsimLoadsheetService", 
                                $"HTTP request error: {ex.Message}. " +
                                "Check network connectivity and if Prosim EFB server is running.");
                            
                            if (retryCount < maxRetries)
                            {
                                retryCount++;
                                int delayMs = 1000 * retryCount;
                                LogService.Log(LogLevel.Warning, "ProsimLoadsheetService", 
                                    $"Retrying in {delayMs/1000} seconds (attempt {retryCount}/{maxRetries})...");
                                await Task.Delay(delayMs);
                                continue;
                            }
                            
                            return LoadsheetResult.CreateFailure(
                                $"HTTP request error: {ex.Message}. Check network connectivity and if Prosim EFB server is running.");
                        }
                        
                        // Calculate and log request duration
                        TimeSpan requestDuration = DateTime.Now - requestStart;
                        LogService.Log(LogLevel.Debug, "ProsimLoadsheetService", 
                            $"Request completed in {requestDuration.TotalMilliseconds:F0}ms");
                        
                        string responseContent = await response.Content.ReadAsStringAsync();
                        
                        // Always log response for debugging
                        LogService.Log(LogLevel.Debug, "ProsimLoadsheetService", 
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
                            
                            LogService.Log(LogLevel.Information, "ProsimLoadsheetService", 
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
                                
                                LogService.Log(LogLevel.Warning, "ProsimLoadsheetService", 
                                    $"Failed to generate {type} loadsheet (attempt {retryCount}/{maxRetries}). " +
                                    $"Retrying in {delayMs/1000} seconds...");
                                
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
                        
                        LogService.Log(LogLevel.Warning, "ProsimLoadsheetService", 
                            $"Exception generating {type} loadsheet (attempt {retryCount}/{maxRetries}): {ex.Message}. " +
                            $"Retrying in {delayMs/1000} seconds...");
                        
                        await Task.Delay(delayMs);
                        continue;
                    }
                    
                    // Simplified error handling for debugging
                    string errorMessage = $"Exception after {maxRetries + 1} attempts: {ex.GetType().Name}, Message: {ex.Message}";
                    LogService.Log(LogLevel.Error, "ProsimLoadsheetService", errorMessage);
                    return LoadsheetResult.CreateFailure(errorMessage);
                }
            }
            
            // This should never be reached, but just in case
            return LoadsheetResult.CreateFailure($"Unexpected error generating {type} loadsheet after {maxRetries + 1} attempts");
        }

        /// <summary>
        /// Resend the current loadsheet to the MCDU
        /// </summary>
        public async Task<bool> ResendLoadsheet()
        {
            try
            {
                LogService.Log(LogLevel.Information, "ProsimLoadsheetService", "Resending loadsheet to MCDU");
                
                // Use the Prosim API to resend the loadsheet
                // This is equivalent to the JavaScript code: this.backend.resendLoadsheet()
                string url = $"{ServiceLocator.ProsimInterface.GetBackendUrl()}/loadsheet/resend";
                bool success = await ServiceLocator.ProsimInterface.PostAsync(url, "{}");
                
                if (success)
                {
                    LogService.Log(LogLevel.Information, "ProsimLoadsheetService", "Loadsheet resent successfully");
                    return true;
                }
                else
                {
                    LogService.Log(LogLevel.Error, "ProsimLoadsheetService", "Failed to resend loadsheet");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, "ProsimLoadsheetService", $"Error resending loadsheet: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if the Prosim EFB server is running and accessible
        /// </summary>
        /// <returns>True if the server is accessible, false otherwise</returns>
        public async Task<bool> CheckServerStatus()
        {
            try
            {
                LogService.Log(LogLevel.Information, "ProsimLoadsheetService", "Checking Prosim EFB server status");
                
                // Validate the backend URL
                string backendUrl = ServiceLocator.ProsimInterface.GetBackendUrl();
                if (string.IsNullOrEmpty(backendUrl))
                {
                    LogService.Log(LogLevel.Error, "ProsimLoadsheetService", 
                        "Backend URL is empty. Prosim EFB server may not be properly configured.");
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
                        LogService.Log(LogLevel.Information, "ProsimLoadsheetService", 
                            $"Server check completed in {requestDuration.TotalMilliseconds:F0}ms. " +
                            $"Status: {response.StatusCode}");
                        
                        return response.IsSuccessStatusCode;
                    }
                    catch (Exception ex)
                    {
                        // Log the error
                        LogService.Log(LogLevel.Error, "ProsimLoadsheetService", 
                            $"Error checking server status: {ex.Message}");
                        
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, "ProsimLoadsheetService", 
                    $"Unexpected error checking server status: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Reset all loadsheets
        /// </summary>
        public async Task<bool> ResetLoadsheets()
        {
            try
            {
                LogService.Log(LogLevel.Information, "ProsimLoadsheetService", "Resetting loadsheets");
                
                // Use the Prosim API to reset loadsheets
                // This is equivalent to the JavaScript code: this.backend.resetLoadsheets()
                string url = $"{ServiceLocator.ProsimInterface.GetBackendUrl()}/loadsheet";
                bool success = await ServiceLocator.ProsimInterface.DeleteAsync(url);
                
                if (success)
                {
                    LogService.Log(LogLevel.Information, "ProsimLoadsheetService", "Loadsheets reset successfully");
                    return true;
                }
                else
                {
                    LogService.Log(LogLevel.Error, "ProsimLoadsheetService", "Failed to reset loadsheets");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Error, "ProsimLoadsheetService", $"Error resetting loadsheets: {ex.Message}");
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
                LogService.Log(LogLevel.Information, "ProsimLoadsheetService", "Preliminary loadsheet received");
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
                    LogService.Log(LogLevel.Error, "ProsimLoadsheetService", $"Error parsing preliminary loadsheet: {ex.Message}");
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
                LogService.Log(LogLevel.Information, "ProsimLoadsheetService", "Final loadsheet received");
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
                    LogService.Log(LogLevel.Error, "ProsimLoadsheetService", $"Error parsing final loadsheet: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Event arguments for loadsheet received events
    /// </summary>
    public class LoadsheetReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Type of loadsheet (Preliminary or Final)
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// Loadsheet data
        /// </summary>
        public dynamic Data { get; set; }
    }
}
