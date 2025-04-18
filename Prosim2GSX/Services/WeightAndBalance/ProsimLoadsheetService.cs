using Newtonsoft.Json;
using Prosim2GSX.Events;
using System;
using System.Threading.Tasks;

namespace Prosim2GSX.Services.WeightAndBalance
{
    /// <summary>
    /// Service for interacting with Prosim's native loadsheet functionality
    /// </summary>
    public class ProsimLoadsheetService
    {
        private readonly ProsimController _prosimController;
        private DataRefChangedHandler _prelimLoadsheetHandler;
        private DataRefChangedHandler _finalLoadsheetHandler;

        /// <summary>
        /// Event raised when a loadsheet is received
        /// </summary>
        public event EventHandler<LoadsheetReceivedEventArgs> LoadsheetReceived;

        public ProsimLoadsheetService(ProsimController prosimController)
        {
            _prosimController = prosimController ?? throw new ArgumentNullException(nameof(prosimController));
            _prelimLoadsheetHandler = new DataRefChangedHandler(OnPrelimLoadsheetChanged);
            _finalLoadsheetHandler = new DataRefChangedHandler(OnFinalLoadsheetChanged);
        }

        /// <summary>
        /// Subscribe to loadsheet data changes
        /// </summary>
        public void SubscribeToLoadsheetChanges()
        {
            _prosimController.SubscribeToDataRef("efb.prelimLoadsheet", _prelimLoadsheetHandler);
            _prosimController.SubscribeToDataRef("efb.finalLoadsheet", _finalLoadsheetHandler);
            Logger.Log(LogLevel.Information, "ProsimLoadsheetService", "Subscribed to loadsheet changes");
        }

        /// <summary>
        /// Generate a loadsheet using Prosim's native functionality
        /// </summary>
        /// <param name="type">Type of loadsheet ("Preliminary" or "Final")</param>
        /// <returns>Task that completes when the loadsheet is generated</returns>
        public async Task<bool> GenerateLoadsheet(string type)
        {
            try
            {
                Logger.Log(LogLevel.Information, "ProsimLoadsheetService", $"Generating {type} loadsheet");
                
                // Check if we're using the EFB flight plan type
                if (_prosimController.Model.FlightPlanType != "EFB")
                {
                    Logger.Log(LogLevel.Warning, "ProsimLoadsheetService", 
                        $"Cannot generate loadsheet - FlightPlanType is not EFB (current: {_prosimController.Model.FlightPlanType})");
                    return false;
                }

                // Use the Prosim API to generate the loadsheet
                // This is equivalent to the JavaScript code: this.backend.generateLoadsheet(type)
                string url = $"{_prosimController.Interface.GetBackendUrl()}/loadsheet/generate?type={type}";
                bool success = await _prosimController.Interface.PostAsync(url, "{}");
                
                if (success)
                {
                    Logger.Log(LogLevel.Information, "ProsimLoadsheetService", $"{type} loadsheet generation requested successfully");
                    
                    // Wait for the loadsheet to be available (with timeout)
                    int attempts = 0;
                    const int maxAttempts = 30; // 30 seconds timeout
                    
                    while (!_prosimController.IsLoadsheetAvailable(type) && attempts < maxAttempts)
                    {
                        await Task.Delay(1000); // Wait 1 second between checks
                        attempts++;
                    }
                    
                    if (_prosimController.IsLoadsheetAvailable(type))
                    {
                        Logger.Log(LogLevel.Information, "ProsimLoadsheetService", $"{type} loadsheet is now available");
                        return true;
                    }
                    else
                    {
                        Logger.Log(LogLevel.Warning, "ProsimLoadsheetService", $"Timeout waiting for {type} loadsheet");
                        return false;
                    }
                }
                else
                {
                    Logger.Log(LogLevel.Error, "ProsimLoadsheetService", $"Failed to request {type} loadsheet generation");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimLoadsheetService", $"Error generating {type} loadsheet: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Resend the current loadsheet to the MCDU
        /// </summary>
        public async Task<bool> ResendLoadsheet()
        {
            try
            {
                Logger.Log(LogLevel.Information, "ProsimLoadsheetService", "Resending loadsheet to MCDU");
                
                // Use the Prosim API to resend the loadsheet
                // This is equivalent to the JavaScript code: this.backend.resendLoadsheet()
                string url = $"{_prosimController.Interface.GetBackendUrl()}/loadsheet/resend";
                bool success = await _prosimController.Interface.PostAsync(url, "{}");
                
                if (success)
                {
                    Logger.Log(LogLevel.Information, "ProsimLoadsheetService", "Loadsheet resent successfully");
                    return true;
                }
                else
                {
                    Logger.Log(LogLevel.Error, "ProsimLoadsheetService", "Failed to resend loadsheet");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimLoadsheetService", $"Error resending loadsheet: {ex.Message}");
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
                Logger.Log(LogLevel.Information, "ProsimLoadsheetService", "Resetting loadsheets");
                
                // Use the Prosim API to reset loadsheets
                // This is equivalent to the JavaScript code: this.backend.resetLoadsheets()
                string url = $"{_prosimController.Interface.GetBackendUrl()}/loadsheet";
                bool success = await _prosimController.Interface.DeleteAsync(url);
                
                if (success)
                {
                    Logger.Log(LogLevel.Information, "ProsimLoadsheetService", "Loadsheets reset successfully");
                    return true;
                }
                else
                {
                    Logger.Log(LogLevel.Error, "ProsimLoadsheetService", "Failed to reset loadsheets");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimLoadsheetService", $"Error resetting loadsheets: {ex.Message}");
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
                Logger.Log(LogLevel.Information, "ProsimLoadsheetService", "Preliminary loadsheet received");
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
                    Logger.Log(LogLevel.Error, "ProsimLoadsheetService", $"Error parsing preliminary loadsheet: {ex.Message}");
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
                Logger.Log(LogLevel.Information, "ProsimLoadsheetService", "Final loadsheet received");
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
                    Logger.Log(LogLevel.Error, "ProsimLoadsheetService", $"Error parsing final loadsheet: {ex.Message}");
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
