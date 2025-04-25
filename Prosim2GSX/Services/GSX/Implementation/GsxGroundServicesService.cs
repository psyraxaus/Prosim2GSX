using Prosim2GSX.Events;
using Prosim2GSX.Models;
using Prosim2GSX.Services.GSX.Interfaces;
using Prosim2GSX.Services.Prosim.Interfaces;
using System;
using System.Threading;

namespace Prosim2GSX.Services.GSX.Implementation
{
    /// <summary>
    /// Implementation of ground services service
    /// </summary>
    public class GsxGroundServicesService : IGsxGroundServicesService
    {
        private readonly IProsimInterface _prosimInterface;
        private readonly IGsxSimConnectService _simConnectService;
        private readonly IGsxMenuService _menuService;
        private readonly IDataRefMonitoringService _dataRefMonitoringService;
        private readonly ServiceModel _model;

        /// <inheritdoc/>
        public bool IsGpuConnected => _prosimInterface.GetProsimVariable("groundservice.groundpower") != null && (bool)_prosimInterface.GetProsimVariable("groundservice.groundpower");

        /// <inheritdoc/>
        public bool IsPcaConnected => _prosimInterface.GetProsimVariable("groundservice.preconditionedAir") != null && (bool)_prosimInterface.GetProsimVariable("groundservice.preconditionedAir");

        /// <inheritdoc/>
        public bool AreChocksSet => _prosimInterface.GetProsimVariable("efb.chocks") != null && (bool)_prosimInterface.GetProsimVariable("efb.chocks");

        /// <inheritdoc/>
        public bool IsGroundEquipmentConnected => IsGpuConnected && IsPcaConnected && AreChocksSet;

        /// <summary>
        /// Constructor
        /// </summary>
        public GsxGroundServicesService(
            IProsimInterface prosimInterface,
            IGsxSimConnectService simConnectService,
            IGsxMenuService menuService,
            IDataRefMonitoringService dataRefMonitoringService,
            ServiceModel model)
        {
            _prosimInterface = prosimInterface ?? throw new ArgumentNullException(nameof(prosimInterface));
            _simConnectService = simConnectService ?? throw new ArgumentNullException(nameof(simConnectService));
            _menuService = menuService ?? throw new ArgumentNullException(nameof(menuService));
            _dataRefMonitoringService = dataRefMonitoringService ?? throw new ArgumentNullException(nameof(dataRefMonitoringService));
            _model = model ?? throw new ArgumentNullException(nameof(model));

            // Subscribe to service state changes
            SubscribeToStateChanges();

            // Publish initial service status events
            try
            {
                bool gpuConnected = IsGpuConnected;
                bool pcaConnected = IsPcaConnected;
                bool chocksSet = AreChocksSet;

                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent(
                    "GPU", gpuConnected ? ServiceStatus.Completed : ServiceStatus.Disconnected));
                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent(
                    "PCA", pcaConnected ? ServiceStatus.Completed : ServiceStatus.Disconnected));
                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent(
                    "Chocks", chocksSet ? ServiceStatus.Completed : ServiceStatus.Disconnected));

                Logger.Log(LogLevel.Information, nameof(GsxGroundServicesService),
                    $"Published initial service status events - GPU: {(gpuConnected ? "Connected" : "Disconnected")}, " +
                    $"PCA: {(pcaConnected ? "Connected" : "Disconnected")}, " +
                    $"Chocks: {(chocksSet ? "Set" : "Removed")}");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxGroundServicesService),
                    $"Error publishing initial service status: {ex.Message}");
            }
        }

        /// <summary>
        /// Subscribe to service state changes
        /// </summary>
        private void SubscribeToStateChanges()
        {
            Logger.Log(LogLevel.Information, nameof(GsxGroundServicesService),
                "Subscribing to ground service state changes");

            // Log dataref monitoring status
            Logger.Log(LogLevel.Information, nameof(GsxGroundServicesService),
                $"DataRef monitoring active: {_dataRefMonitoringService.IsMonitoringActive}");

            // Force start monitoring if needed
            if (!_dataRefMonitoringService.IsMonitoringActive)
            {
                Logger.Log(LogLevel.Warning, nameof(GsxGroundServicesService),
                    "DataRef monitoring not active, starting it manually");
                _dataRefMonitoringService.StartMonitoring();
            }

            // Subscribe to GPU state changes
            Logger.Log(LogLevel.Information, nameof(GsxGroundServicesService),
                "Subscribing to GPU state changes");
            _dataRefMonitoringService.SubscribeToDataRef("groundservice.groundpower", OnGpuStateChanged);

            // Subscribe to PCA state changes
            Logger.Log(LogLevel.Information, nameof(GsxGroundServicesService),
                "Subscribing to PCA state changes");
            _dataRefMonitoringService.SubscribeToDataRef("groundservice.preconditionedAir", OnPcaStateChanged);

            // Subscribe to chocks state changes
            Logger.Log(LogLevel.Information, nameof(GsxGroundServicesService),
                "Subscribing to chocks state changes");
            _dataRefMonitoringService.SubscribeToDataRef("efb.chocks", OnChocksStateChanged);

            // Log current values
            try
            {
                dynamic gpuState = _prosimInterface.GetProsimVariable("groundservice.groundpower");
                dynamic pcaState = _prosimInterface.GetProsimVariable("groundservice.preconditionedAir");
                dynamic chocksState = _prosimInterface.GetProsimVariable("efb.chocks");

                Logger.Log(LogLevel.Information, nameof(GsxGroundServicesService),
                    $"Initial states - GPU: {gpuState}, PCA: {pcaState}, Chocks: {chocksState}");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxGroundServicesService),
                    $"Error getting initial states: {ex.Message}");
            }
        }

        /// <summary>
        /// Handler for GPU state changes
        /// </summary>
        private void OnGpuStateChanged(string dataRef, dynamic oldValue, dynamic newValue)
        {
            if ((bool)newValue != (bool)oldValue)
            {
                var status = (bool)newValue ? ServiceStatus.Completed : ServiceStatus.Disconnected;
                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("GPU", status));

                Logger.Log(LogLevel.Information, nameof(GsxGroundServicesService),
                    $"GPU state changed to {(status == ServiceStatus.Completed ? "connected" : "disconnected")}");
            }
        }

        /// <summary>
        /// Handler for PCA state changes
        /// </summary>
        private void OnPcaStateChanged(string dataRef, dynamic oldValue, dynamic newValue)
        {
            if ((bool)newValue != (bool)oldValue)
            {
                var status = (bool)newValue ? ServiceStatus.Completed : ServiceStatus.Disconnected;
                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("PCA", status));

                Logger.Log(LogLevel.Information, nameof(GsxGroundServicesService),
                    $"PCA state changed to {(status == ServiceStatus.Completed ? "connected" : "disconnected")}");
            }
        }

        /// <summary>
        /// Handler for chocks state changes
        /// </summary>
        private void OnChocksStateChanged(string dataRef, dynamic oldValue, dynamic newValue)
        {
            if ((bool)newValue != (bool)oldValue)
            {
                var status = (bool)newValue ? ServiceStatus.Completed : ServiceStatus.Disconnected;
                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("Chocks", status));

                Logger.Log(LogLevel.Information, nameof(GsxGroundServicesService),
                    $"Chocks state changed to {(status == ServiceStatus.Completed ? "set" : "removed")}");
            }
        }

        /// <inheritdoc/>
        public void ConnectGpu()
        {
            try
            {
                Logger.Log(LogLevel.Information, nameof(GsxGroundServicesService), "Connecting GPU");
                _prosimInterface.SetProsimVariable("groundservice.groundpower", true);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxGroundServicesService),
                    $"Error connecting GPU: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public void DisconnectGpu()
        {
            try
            {
                Logger.Log(LogLevel.Information, nameof(GsxGroundServicesService), "Disconnecting GPU");
                _prosimInterface.SetProsimVariable("groundservice.groundpower", false);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxGroundServicesService),
                    $"Error disconnecting GPU: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public void ConnectPca()
        {
            try
            {
                Logger.Log(LogLevel.Information, nameof(GsxGroundServicesService), "Connecting PCA");
                _prosimInterface.SetProsimVariable("groundservice.preconditionedAir", true);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxGroundServicesService),
                    $"Error connecting PCA: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public void DisconnectPca()
        {
            try
            {
                Logger.Log(LogLevel.Information, nameof(GsxGroundServicesService), "Disconnecting PCA");
                _prosimInterface.SetProsimVariable("groundservice.preconditionedAir", false);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxGroundServicesService),
                    $"Error disconnecting PCA: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public void SetChocks(bool enable)
        {
            try
            {
                Logger.Log(LogLevel.Information, nameof(GsxGroundServicesService),
                    $"{(enable ? "Setting" : "Removing")} chocks");
                _prosimInterface.SetProsimVariable("efb.chocks", enable);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxGroundServicesService),
                    $"Error {(enable ? "setting" : "removing")} chocks: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public void CallJetwayStairs(bool jetwayOnly = false)
        {
            try
            {
                _menuService.OpenMenu();

                // Call jetway if needed
                if (_simConnectService.ReadGsxLvar("FSDT_GSX_JETWAY") != 2 &&
                    _simConnectService.ReadGsxLvar("FSDT_GSX_JETWAY") != 5 &&
                    _simConnectService.ReadGsxLvar("FSDT_GSX_OPERATEJETWAYS_STATE") < 3)
                {
                    Logger.Log(LogLevel.Information, nameof(GsxGroundServicesService), "Calling Jetway");
                    _menuService.SelectMenuItem(6);
                    _menuService.HandleOperatorSelection((int)_model.OperatorDelay);

                    // Wait for jetway to connect before calling stairs
                    Thread.Sleep(1500);
                }

                // Call stairs if needed and allowed
                if (_simConnectService.ReadGsxLvar("FSDT_GSX_JETWAY") != 2 &&
                    _simConnectService.ReadGsxLvar("FSDT_GSX_JETWAY") != 5 &&
                    _simConnectService.ReadGsxLvar("FSDT_GSX_OPERATEJETWAYS_STATE") < 3)
                {
                    Logger.Log(LogLevel.Information, nameof(GsxGroundServicesService), "Calling Jetway");
                    _menuService.SelectMenuItem(6);
                    _menuService.HandleOperatorSelection((int)_model.OperatorDelay);

                    // Reduce wait time from 1500ms to 500ms
                    Thread.Sleep(500);  // Was 1500ms
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxGroundServicesService),
                    $"Error calling jetway/stairs: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public void RemoveJetwayStairs()
        {
            try
            {
                _menuService.OpenMenu();

                // Remove jetway if connected
                if (_simConnectService.ReadGsxLvar("FSDT_GSX_JETWAY") == 5 &&
                    _simConnectService.ReadGsxLvar("FSDT_GSX_OPERATEJETWAYS_STATE") < 3)
                {
                    Logger.Log(LogLevel.Information, nameof(GsxGroundServicesService), "Removing Jetway");
                    _menuService.SelectMenuItem(6);
                    _menuService.HandleOperatorSelection((int)_model.OperatorDelay);

                    // Wait for jetway to disconnect before removing stairs
                    Thread.Sleep(1500);
                }

                // Remove stairs if connected
                if (_simConnectService.ReadGsxLvar("FSDT_GSX_STAIRS") == 5 &&
                    _simConnectService.ReadGsxLvar("FSDT_GSX_OPERATESTAIRS_STATE") < 3)
                {
                    _menuService.OpenMenu();
                    Logger.Log(LogLevel.Information, nameof(GsxGroundServicesService), "Removing Stairs");
                    _menuService.SelectMenuItem(7);
                    _menuService.HandleOperatorSelection((int)_model.OperatorDelay);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxGroundServicesService),
                    $"Error removing jetway/stairs: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public void ConnectAllGroundServices(bool jetwayOnly = false)
        {
            // Set chocks first
            SetChocks(true);

            // Connect GPU and PCA
            ConnectGpu();
            ConnectPca();

            // Call jetway and/or stairs
            CallJetwayStairs(jetwayOnly);

            Logger.Log(LogLevel.Information, nameof(GsxGroundServicesService),
                "All ground services connected");
        }

        /// <inheritdoc/>
        public void DisconnectAllGroundServices()
        {
            // Remove jetway and stairs
            RemoveJetwayStairs();

            // Disconnect PCA and GPU
            DisconnectPca();
            DisconnectGpu();

            // Remove chocks last
            SetChocks(false);

            Logger.Log(LogLevel.Information, nameof(GsxGroundServicesService),
                "All ground services disconnected");
        }
    }
}