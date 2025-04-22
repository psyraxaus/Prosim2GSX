﻿﻿﻿﻿﻿﻿﻿﻿﻿using System;
using System.Threading;
using Microsoft.FlightSimulator.SimConnect;
using Prosim2GSX.Events;
using Prosim2GSX.Models;
using Prosim2GSX.Services.Audio;
using Prosim2GSX.Services;
using Prosim2GSX.Services.Prosim.Interfaces;

namespace Prosim2GSX
{
    public class ServiceController
    {
        private IAudioService _audioService;
        protected ServiceModel Model;
        private readonly IProsimInterface _prosimInterface;
        private readonly IDataRefMonitoringService _dataRefService;
        private readonly IFlightPlanService _flightPlanService;
        protected FlightPlan FlightPlan;
        protected int Interval = 1000;

        private SubscriptionToken _retryToken;

        public ServiceController(ServiceModel model)
        {
            this.Model = model;
            _prosimInterface = ServiceLocator.ProsimInterface;
            _dataRefService = ServiceLocator.DataRefService;
            _flightPlanService = ServiceLocator.FlightPlanService;
            this._audioService = new AudioService(_prosimInterface, _dataRefService, IPCManager.SimConnect);

            // Add this line to set the AudioService in the ServiceModel
            if (model is ServiceModel serviceModel)
            {
                serviceModel.SetAudioService((AudioService)_audioService);
            }
            
            // Subscribe to the retry event
            _retryToken = EventAggregator.Instance.Subscribe<RetryFlightPlanLoadEvent>(OnRetryFlightPlanLoad);
        }
        
        private void OnRetryFlightPlanLoad(RetryFlightPlanLoadEvent evt)
        {
            // Retry loading the flight plan
            if (FlightPlan != null && Model.IsValidSimbriefId())
            {
                Logger.Log(LogLevel.Information, "ServiceController", "Retrying flight plan load with new Simbrief ID");
                FlightPlan.LoadWithValidation();
            }
        }

        public void Run()
        {
            try
            {
                Logger.Log(LogLevel.Information, "ServiceController:Run", $"Service starting ...");
                while (!Model.CancellationRequested)
                {
                    if (Wait())
                    {
                        ServiceLoop();
                    }
                    else
                    {
                        if (!IPCManager.IsSimRunning())
                        {
                            // Update MSFS status
                            Model.IsSimRunning = false;
                            EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("MSFS", Model.IsSimRunning));
                            
                            Model.CancellationRequested = true;
                            Model.ServiceExited = true;
                            Logger.Log(LogLevel.Critical, "ServiceController:Run", $"Session aborted, Retry not possible - exiting Program");
                            return;
                        }
                        else
                        {
                            Reset();
                            Logger.Log(LogLevel.Information, "ServiceController:Run", $"Session aborted, Retry possible - Waiting for new Session");
                        }
                    }
                }

                IPCManager.CloseSafe();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ServiceController:Run", $"Critical Exception occured: {ex.Source} - {ex.Message}");
            }
        }

        protected bool Wait()
        {
            if (!IPCManager.WaitForSimulator(Model))
                return false;
            else
                Model.IsSimRunning = true;

            if (!IPCManager.WaitForConnection(Model))
                return false;

            if (!ServiceLocator.ConnectionService.WaitForAvailability())
                return false;
            else
                Model.IsProsimRunning = true;

            // Initialize FlightPlan after Prosim is connected
            if (FlightPlan == null)
            {
                // Create and load FlightPlan using the manually entered Simbrief ID
                FlightPlan = new FlightPlan(Model);
                
                // Try to load with validation
                var loadResult = FlightPlan.LoadWithValidation();
                
                // Handle different error cases
                switch (loadResult)
                {
                    case FlightPlan.LoadResult.Success:
                        // Make FlightPlan available to ProsimController
                        ServiceLocator.FlightPlanService.SetFlightPlan(FlightPlan);
                        break;
                        
                    case FlightPlan.LoadResult.InvalidId:
                        // Special handling for default SimBrief ID (0)
                        if (Model.SimBriefID == "0")
                        {
                            // Just log a message and return false, don't start a background task
                            Logger.Log(LogLevel.Warning, "ServiceController:Wait", 
                                "Default SimBrief ID detected. Please enter a valid Simbrief ID in Settings tab.");
                            
                            // Don't try to load the flight plan or start a background task
                            return false;
                        }
                        else
                        {
                            // For other invalid IDs, wait for user to enter a valid ID
                            Logger.Log(LogLevel.Warning, "ServiceController:Wait", 
                                "Waiting for valid Simbrief ID to be entered in Settings tab...");
                            
                            // Start a background task to periodically check for valid ID
                            System.Threading.Tasks.Task.Run(async () => {
                                while (!Model.CancellationRequested && !Model.IsValidSimbriefId())
                                {
                                    await System.Threading.Tasks.Task.Delay(5000); // Check every 5 seconds
                                }
                                
                                // When valid ID is detected, try loading again
                                if (!Model.CancellationRequested && Model.IsValidSimbriefId())
                                {
                                    EventAggregator.Instance.Publish(new RetryFlightPlanLoadEvent());
                                }
                            });
                            
                            return false;
                        }
                        
                    case FlightPlan.LoadResult.NetworkError:
                        Logger.Log(LogLevel.Error, "ServiceController:Wait", 
                            "Network error loading flight plan. Check your internet connection.");
                        Thread.Sleep(5000);
                        return false;
                        
                    case FlightPlan.LoadResult.ParseError:
                        Logger.Log(LogLevel.Error, "ServiceController:Wait", 
                            "Error parsing flight plan data. The Simbrief API may have changed or returned invalid data.");
                        Thread.Sleep(5000);
                        return false;
                }
                
                // Make FlightPlan available to ProsimController
                ServiceLocator.FlightPlanService.SetFlightPlan(FlightPlan);
            }

            if (!IPCManager.WaitForSessionReady(Model))
                return false;
            else
                Model.IsSessionRunning = true;

            return true;
        }

        protected void Reset()
        {
            try
            {
                IPCManager.SimConnect?.Disconnect();
                IPCManager.SimConnect = null;
                
                // Update session status
                Model.IsSessionRunning = false;
                EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("Session", Model.IsSessionRunning));
                
                // Update Prosim status
                Model.IsProsimRunning = false;
                EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("Prosim", Model.IsProsimRunning));
                
                // Update SimConnect status
                EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("SimConnect", false));
                
                // We don't set Model.IsSimRunning to false here because the simulator might still be running
                // We'll check again in the next iteration of the main loop
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ServiceController:Reset", $"Exception during Reset: {ex.Source} - {ex.Message}");
            }
        }

        protected void ServiceLoop()
        {
            try 
            {
                var gsxController = new GsxController(Model, FlightPlan, _audioService);
                // Store the GsxController in IPCManager so it can be accessed by the MainWindow
                IPCManager.GsxController = gsxController;
                
                // Re-publish connection status events to ensure UI is updated
                EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("MSFS", Model.IsSimRunning));
                EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("Prosim", Model.IsProsimRunning));
                EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("SimConnect", IPCManager.SimConnect?.IsConnected == true));
                EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("Session", Model.IsSessionRunning));
                
                int elapsedMS = gsxController.Interval;
                int delay = 100;
                Thread.Sleep(1000);
                Logger.Log(LogLevel.Information, "ServiceController:ServiceLoop", "Starting Service Loop");
                while (!Model.CancellationRequested && Model.IsProsimRunning && IPCManager.IsSimRunning() && IPCManager.IsCamReady())
                {
                    try
                    {
                        if (elapsedMS >= gsxController.Interval)
                        {
                            gsxController.RunServices();
                            elapsedMS = 0;
                        }

                        if (Model.GsxVolumeControl || Model.IsVhf1Controllable())
                            _audioService.ControlAudio();

                        Thread.Sleep(delay);
                        elapsedMS += delay;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Critical, "ServiceController:ServiceLoop", $"Critical Exception during ServiceLoop() {ex.GetType()} {ex.Message} {ex.Source}");
                    }
                }

                Logger.Log(LogLevel.Information, "ServiceController:ServiceLoop", "ServiceLoop ended");

                // Check and publish connection status changes that might have caused the loop to exit
                bool isSimRunning = IPCManager.IsSimRunning();
                if (Model.IsSimRunning != isSimRunning)
                {
                    Model.IsSimRunning = isSimRunning;
                    EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("MSFS", Model.IsSimRunning));
                }
                
                if (!IPCManager.IsCamReady())
                {
                    EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("Session", false));
                    Model.IsSessionRunning = false;
                }
                
                if (Model.GsxVolumeControl || Model.IsVhf1Controllable())
                {
                    Logger.Log(LogLevel.Information, "ServiceController:ServiceLoop", "Resetting GSX/VHF1 Audio");
                    _audioService.ResetAudio();
                }
                // Clear the GsxController reference when the service loop ends
                IPCManager.GsxController = null;
            }
            finally 
            {
                if (_audioService is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        public IAudioService GetAudioService()
        {
            return _audioService;
        }
        
        /// <summary>
        /// Performs a diagnostic check of the VoiceMeeter API and logs detailed information
        /// </summary>
        /// <returns>True if all checks pass, false otherwise</returns>
        public bool PerformVoiceMeeterDiagnostics()
        {
            if (_audioService is AudioService audioService)
            {
                return audioService.PerformVoiceMeeterDiagnostics();
            }
            
            Logger.Log(LogLevel.Warning, "ServiceController", "Cannot perform VoiceMeeter diagnostics: AudioService is not available");
            return false;
        }
    }
}
