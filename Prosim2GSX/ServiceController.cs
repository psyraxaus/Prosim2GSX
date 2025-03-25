using System;
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
                EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("MSFS", Model.IsSimRunning));

            if (!IPCManager.WaitForConnection(Model))
                return false;
            else
                EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("SimConnect", true));

            if (!ProsimController.IsProsimConnectionAvailable(Model))
                return false;
            else
                Model.IsProsimRunning = true;
                EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("Prosim", Model.IsProsimRunning));

            if (!IPCManager.WaitForSessionReady(Model))
                return false;
            else
                Model.IsSessionRunning = true;
                EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("Session", Model.IsSessionRunning));

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
            var gsxController = new GsxController(Model, ProsimController, FlightPlan);
            // Store the GsxController in IPCManager so it can be accessed by the MainWindow
            IPCManager.GsxController = gsxController;
            
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
