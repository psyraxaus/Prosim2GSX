using Microsoft.Extensions.Logging;
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
        private readonly ILogger<GsxGroundServicesService> _logger;

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
            ILogger<GsxGroundServicesService> logger,
            IProsimInterface prosimInterface,
            IGsxSimConnectService simConnectService,
            IGsxMenuService menuService,
            IDataRefMonitoringService dataRefMonitoringService,
            ServiceModel model)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

                _logger.LogInformation(
                    "Published initial service status events - GPU: {GpuStatus}, PCA: {PcaStatus}, Chocks: {ChocksStatus}",
                    gpuConnected ? "Connected" : "Disconnected",
                    pcaConnected ? "Connected" : "Disconnected",
                    chocksSet ? "Set" : "Removed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing initial service status");
            }
        }

        /// <summary>
        /// Subscribe to service state changes
        /// </summary>
        private void SubscribeToStateChanges()
        {
            _logger.LogInformation("Subscribing to ground service state changes");

            // Log dataref monitoring status
            _logger.LogInformation("DataRef monitoring active: {IsMonitoringActive}", _dataRefMonitoringService.IsMonitoringActive);

            // Force start monitoring if needed
            if (!_dataRefMonitoringService.IsMonitoringActive)
            {
                _logger.LogWarning("DataRef monitoring not active, starting it manually");
                _dataRefMonitoringService.StartMonitoring();
            }

            // Subscribe to GPU state changes
            _logger.LogInformation("Subscribing to GPU state changes");
            _dataRefMonitoringService.SubscribeToDataRef("groundservice.groundpower", OnGpuStateChanged);

            // Subscribe to PCA state changes
            _logger.LogInformation("Subscribing to PCA state changes");
            _dataRefMonitoringService.SubscribeToDataRef("groundservice.preconditionedAir", OnPcaStateChanged);

            // Subscribe to chocks state changes
            _logger.LogInformation("Subscribing to chocks state changes");
            _dataRefMonitoringService.SubscribeToDataRef("efb.chocks", OnChocksStateChanged);

            // Log current values
            try
            {
                string gpuState = _prosimInterface.GetProsimVariable("groundservice.groundpower");
                string pcaState = _prosimInterface.GetProsimVariable("groundservice.preconditionedAir");
                string chocksState = _prosimInterface.GetProsimVariable("efb.chocks");

                _logger.LogInformation("Initial states - GPU: {GpuState}, PCA: {PcaState}, Chocks: {ChocksState}",
                    gpuState, pcaState, chocksState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting initial states");
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

                _logger.LogInformation("GPU state changed to {Status}",
                    status == ServiceStatus.Completed ? "connected" : "disconnected");
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

                _logger.LogInformation("PCA state changed to {Status}",
                    status == ServiceStatus.Completed ? "connected" : "disconnected");
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

                _logger.LogInformation("Chocks state changed to {Status}",
                    status == ServiceStatus.Completed ? "set" : "removed");
            }
        }

        /// <inheritdoc/>
        public void ConnectGpu()
        {
            try
            {
                _logger.LogInformation("Connecting GPU");
                _prosimInterface.SetProsimVariable("groundservice.groundpower", true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting GPU");
            }
        }

        /// <inheritdoc/>
        public void DisconnectGpu()
        {
            try
            {
                _logger.LogInformation("Disconnecting GPU");
                _prosimInterface.SetProsimVariable("groundservice.groundpower", false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting GPU");
            }
        }

        /// <inheritdoc/>
        public void ConnectPca()
        {
            try
            {
                _logger.LogInformation("Connecting PCA");
                _prosimInterface.SetProsimVariable("groundservice.preconditionedAir", true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting PCA");
            }
        }

        /// <inheritdoc/>
        public void DisconnectPca()
        {
            try
            {
                _logger.LogInformation("Disconnecting PCA");
                _prosimInterface.SetProsimVariable("groundservice.preconditionedAir", false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting PCA");
            }
        }

        /// <inheritdoc/>
        public void SetChocks(bool enable)
        {
            try
            {
                _logger.LogInformation("{Action} chocks", enable ? "Setting" : "Removing");
                _prosimInterface.SetProsimVariable("efb.chocks", enable);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error {Action} chocks", enable ? "setting" : "removing");
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
                    _logger.LogInformation("Calling Jetway");
                    _menuService.SelectMenuItem(6);
                    _menuService.HandleOperatorSelection();

                    // Wait for jetway to connect before calling stairs
                    Thread.Sleep(1500);
                }

                // Call stairs if needed and allowed
                if (_simConnectService.ReadGsxLvar("FSDT_GSX_JETWAY") != 2 &&
                    _simConnectService.ReadGsxLvar("FSDT_GSX_JETWAY") != 5 &&
                    _simConnectService.ReadGsxLvar("FSDT_GSX_OPERATEJETWAYS_STATE") < 3)
                {
                    _logger.LogInformation("Calling Jetway");
                    _menuService.SelectMenuItem(6);
                    _menuService.HandleOperatorSelection();

                    // Reduce wait time from 1500ms to 500ms
                    Thread.Sleep(500);  // Was 1500ms
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling jetway/stairs");
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
                    _logger.LogInformation("Removing Jetway");
                    _menuService.SelectMenuItem(6);
                    _menuService.HandleOperatorSelection();

                    // Wait for jetway to disconnect before removing stairs
                    Thread.Sleep(1500);
                }

                // Remove stairs if connected
                if (_simConnectService.ReadGsxLvar("FSDT_GSX_STAIRS") == 5 &&
                    _simConnectService.ReadGsxLvar("FSDT_GSX_OPERATESTAIRS_STATE") < 3)
                {
                    _menuService.OpenMenu();
                    _logger.LogInformation("Removing Stairs");
                    _menuService.SelectMenuItem(7);
                    _menuService.HandleOperatorSelection();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing jetway/stairs");
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

            _logger.LogInformation("All ground services connected");
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

            _logger.LogInformation("All ground services disconnected");
        }
    }
}
