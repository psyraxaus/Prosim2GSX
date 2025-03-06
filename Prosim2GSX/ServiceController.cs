﻿﻿﻿﻿﻿using System;
using System.Threading;
using Prosim2GSX.Models;
using Prosim2GSX.Services;

namespace Prosim2GSX
{
    public class ServiceController
    {
        protected ServiceModel Model;
        protected ProsimController ProsimController;
        protected FlightPlan FlightPlan;
        protected int Interval = 1000;

        public ServiceController(ServiceModel model)
        {
            this.Model = model;
            this.ProsimController = new(model);
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

            if (!ProsimController.IsProsimConnectionAvailable(Model))
                return false;
            else
                Model.IsProsimRunning = true;

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
                Model.IsSessionRunning = false;
                Model.IsProsimRunning = false;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ServiceController:Reset", $"Exception during Reset: {ex.Source} - {ex.Message}");
            }
        }

        protected void ServiceLoop()
        {
            // Initialize services in the correct order
            InitializeServices();
            
            // Get references to the initialized services
            var gsxController = IPCManager.GsxController;
            
            // Initialize timing variables
            int elapsedMS = gsxController?.Interval ?? 1000;
            int delay = 100;
            Thread.Sleep(1000);
            Logger.Log(LogLevel.Information, "ServiceController:ServiceLoop", "Starting Service Loop");
            
            // Main service loop
            while (!Model.CancellationRequested && Model.IsProsimRunning && IPCManager.IsSimRunning() && IPCManager.IsCamReady() && gsxController != null)
            {
                try
                {
                    if (elapsedMS >= gsxController.Interval)
                    {
                        gsxController.RunServices();
                        elapsedMS = 0;
                    }

                    if (Model.GsxVolumeControl || Model.IsVhf1Controllable())
                        gsxController.ControlAudio();

                    Thread.Sleep(delay);
                    elapsedMS += delay;
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Critical, "ServiceController:ServiceLoop", $"Critical Exception during ServiceLoop() {ex.GetType()} {ex.Message} {ex.Source}");
                }
            }

            Logger.Log(LogLevel.Information, "ServiceController:ServiceLoop", "ServiceLoop ended");

            if (Model.GsxVolumeControl || Model.IsVhf1Controllable())
            {
                Logger.Log(LogLevel.Information, "ServiceController:ServiceLoop", "Resetting GSX/VHF1 Audio");
                gsxController?.ResetAudio();
            }
            
            // Clean up services
            CleanupServices();
        }
        
        /// <summary>
        /// Initializes all services in the correct order after connections are established
        /// </summary>
        protected void InitializeServices()
        {
            Logger.Log(LogLevel.Information, "ServiceController:InitializeServices", "Initializing services...");
            
            // Step 1: Create FlightPlanService
            var flightPlanService = new FlightPlanService(Model);
            
            // Step 2: Create FlightPlan
            FlightPlan = new FlightPlan(Model, flightPlanService);
            
            // Step 3: Load flight plan
            if (!FlightPlan.Load())
            {
                Logger.Log(LogLevel.Warning, "ServiceController:InitializeServices", "Could not load flight plan, will retry in service loop");
                // We'll continue even if flight plan isn't loaded yet, as it might be loaded later
            }
            
            // Step 4: Initialize FlightPlan in ProsimController
            ProsimController.InitializeFlightPlan(FlightPlan);
            
            // Step 5: Create AcarsService
            var acarsService = new AcarsService(Model.AcarsSecret, Model.AcarsNetworkUrl);
            
            // Step 6: Create AudioSessionManager
            var audioSessionManager = new CoreAudioSessionManager();
            
            // Step 7: Create GSX services
            var menuService = new GSXMenuService(Model, IPCManager.SimConnect);
            var audioService = new GSXAudioService(Model, IPCManager.SimConnect, audioSessionManager);
            
            // Configure audio service properties
            audioService.AudioSessionRetryCount = 5; // Increase retry count for better reliability
            audioService.AudioSessionRetryDelay = TimeSpan.FromSeconds(1); // Shorter delay between retries
            
            // Step 8: Create GsxController
            var gsxController = new GsxController(Model, ProsimController, FlightPlan, acarsService, menuService, audioService);
            
            // Store the GsxController in IPCManager so it can be accessed by the MainWindow
            IPCManager.GsxController = gsxController;
            
            Logger.Log(LogLevel.Information, "ServiceController:InitializeServices", "Services initialized successfully");
        }
        
        /// <summary>
        /// Cleans up services when the service loop ends
        /// </summary>
        protected void CleanupServices()
        {
            // Clear the GsxController reference when the service loop ends
            IPCManager.GsxController = null;
            
            // Clear other service references as needed
            FlightPlan = null;
            
            Logger.Log(LogLevel.Information, "ServiceController:CleanupServices", "Services cleaned up");
        }
    }
}
