﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using System;
using System.Threading;
using Microsoft.FlightSimulator.SimConnect;
using Prosim2GSX.Events;
using Prosim2GSX.Models;

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

            if (!ProsimController.IsProsimConnectionAvailable(Model))
                return false;
            else
                Model.IsProsimRunning = true;

            // Initialize FlightPlan after Prosim is connected
            if (FlightPlan == null)
            {
                // Get SimBrief ID from ProsimController
                ProsimController.SetSimBriefID(Model);

                // Create and load FlightPlan
                FlightPlan = new FlightPlan(Model);
                if (!FlightPlan.Load())
                {
                    Logger.Log(LogLevel.Error, "ServiceController:Wait", "Could not load Flightplan");
                    Thread.Sleep(5000);
                }

                // Make FlightPlan available to ProsimController
                ProsimController.SetFlightPlan(FlightPlan);
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
            var gsxController = new GsxController(Model, ProsimController, FlightPlan);
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
                gsxController.ResetAudio();
            }
            // Clear the GsxController reference when the service loop ends
            IPCManager.GsxController = null;
        }
    }
}
