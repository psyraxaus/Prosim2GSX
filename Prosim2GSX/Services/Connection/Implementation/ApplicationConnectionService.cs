using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prosim2GSX.Models;
using Prosim2GSX.Services.Connection.Events;
using Prosim2GSX.Services.Connection.Interfaces;
using Prosim2GSX.Services.Connection.Enums;
using Prosim2GSX.Services.Prosim.Interfaces;

namespace Prosim2GSX.Services.Connection.Implementation
{
    /// <summary>
    /// Implementation of IConnectionService
    /// </summary>
    public class ApplicationConnectionService : IConnectionService
    {
        private readonly ServiceModel _model;
        private readonly IProsimConnectionService _prosimConnectionService;
        private readonly ILogger<ApplicationConnectionService> _logger;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        /// <inheritdoc/>
        public bool IsFlightSimulatorConnected { get; private set; }

        /// <inheritdoc/>
        public bool IsSimConnectConnected { get; private set; }

        /// <inheritdoc/>
        public bool IsProsimConnected { get; private set; }

        /// <inheritdoc/>
        public bool IsSessionReady { get; private set; }

        /// <inheritdoc/>
        public event EventHandler<ConnectionStatusEventArgs> ConnectionStatusChanged;

        /// <summary>
        /// Creates a new instance of ApplicationConnectionService
        /// </summary>
        /// <param name="logger">Logger for this service</param>
        /// <param name="model">The service model</param>
        /// <param name="prosimConnectionService">The Prosim connection service</param>
        public ApplicationConnectionService(
            ILogger<ApplicationConnectionService> logger,
            ServiceModel model,
            IProsimConnectionService prosimConnectionService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _prosimConnectionService = prosimConnectionService ?? throw new ArgumentNullException(nameof(prosimConnectionService));
        }

        /// <inheritdoc/>
        public async Task<bool> ConnectToFlightSimulatorAsync()
        {
            // Check if flight simulator is running
            bool isRunning = IPCManager.IsSimRunning();

            // Update state and raise event
            if (IsFlightSimulatorConnected != isRunning)
            {
                IsFlightSimulatorConnected = isRunning;
                OnConnectionStatusChanged(ConnectionType.FlightSimulator, isRunning);
            }

            // If simulator is already running, return success
            if (isRunning)
            {
                return true;
            }

            // If not in wait mode, return failure
            if (!_model.WaitForConnect)
            {
                _logger.LogError("Flight simulator not running and wait mode disabled");
                return false;
            }

            // Wait for simulator to start
            _logger.LogInformation("Waiting for flight simulator to start...");

            try
            {
                // Poll for simulator to start
                int waitDuration = 10000; // 10 seconds
                int checkInterval = 1000; // 1 second
                int maxAttempts = 60; // 60 attempts = 60 seconds

                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    // Check if simulator is running
                    isRunning = IPCManager.IsSimRunning();

                    if (isRunning)
                    {
                        // Update state and raise event
                        IsFlightSimulatorConnected = true;
                        OnConnectionStatusChanged(ConnectionType.FlightSimulator, true);
                        return true;
                    }

                    // Check for cancellation
                    if (_model.CancellationRequested)
                    {
                        return false;
                    }

                    // Wait before next check
                    await Task.Delay(checkInterval, _cts.Token);
                }

                // Timeout reached
                _logger.LogError("Timed out waiting for flight simulator to start");
                return false;
            }
            catch (OperationCanceledException)
            {
                // Cancellation requested
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ConnectToSimConnectAsync()
        {
            // Check if simulator is running
            if (!IsFlightSimulatorConnected)
            {
                _logger.LogError("Cannot connect to SimConnect: Flight simulator not running");
                return false;
            }

            // Check if already connected
            if (IPCManager.SimConnect?.IsConnected == true)
            {
                // Update state and raise event if needed
                if (!IsSimConnectConnected)
                {
                    IsSimConnectConnected = true;
                    OnConnectionStatusChanged(ConnectionType.SimConnect, true);
                }

                return true;
            }

            // Initialize SimConnect
            try
            {
                // Create new SimConnect instance
                IPCManager.SimConnect = new MobiSimConnect();

                // Attempt connection
                bool connectRequested = IPCManager.SimConnect.Connect();

                // Check initial connection result
                if (IPCManager.SimConnect.IsConnected)
                {
                    // Connection successful
                    IsSimConnectConnected = true;
                    OnConnectionStatusChanged(ConnectionType.SimConnect, true);
                    return true;
                }

                // Wait for connection if initial attempt failed
                if (connectRequested)
                {
                    _logger.LogInformation("Waiting for SimConnect connection...");

                    int waitDuration = 5000; // 5 seconds
                    int maxAttempts = 12; // 12 attempts = 60 seconds

                    for (int attempt = 0; attempt < maxAttempts; attempt++)
                    {
                        // Check for cancellation
                        if (_model.CancellationRequested)
                        {
                            return false;
                        }

                        // Wait before checking again
                        await Task.Delay(waitDuration, _cts.Token);

                        // Check if connected
                        if (IPCManager.SimConnect?.IsConnected == true)
                        {
                            // Connection successful
                            IsSimConnectConnected = true;
                            OnConnectionStatusChanged(ConnectionType.SimConnect, true);
                            return true;
                        }

                        // Check if simulator is still running
                        if (!IPCManager.IsSimRunning())
                        {
                            _logger.LogError("Flight simulator closed while waiting for SimConnect");
                            IsFlightSimulatorConnected = false;
                            OnConnectionStatusChanged(ConnectionType.FlightSimulator, false);
                            return false;
                        }

                        // Retry connection if needed
                        if (!connectRequested)
                        {
                            connectRequested = IPCManager.SimConnect.Connect();
                        }
                    }

                    // Timeout reached
                    _logger.LogError("Timed out waiting for SimConnect connection");
                }
                else
                {
                    _logger.LogError("Failed to request SimConnect connection");
                }

                // Connection failed
                IsSimConnectConnected = false;
                OnConnectionStatusChanged(ConnectionType.SimConnect, false);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception connecting to SimConnect");

                // Connection failed
                IsSimConnectConnected = false;
                OnConnectionStatusChanged(ConnectionType.SimConnect, false);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ConnectToProsimAsync()
        {
            try
            {
                // Check if already connected
                if (IsProsimConnected)
                {
                    return true;
                }

                // Attempt to connect to Prosim
                _logger.LogInformation("Connecting to Prosim...");

                bool connected = await Task.Run(() => _prosimConnectionService.WaitForAvailability());

                // Update state and raise event
                IsProsimConnected = connected;
                OnConnectionStatusChanged(ConnectionType.Prosim, connected);

                if (connected)
                {
                    _logger.LogInformation("Connected to Prosim");
                }
                else
                {
                    _logger.LogError("Failed to connect to Prosim");
                }

                return connected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception connecting to Prosim");

                // Connection failed
                IsProsimConnected = false;
                OnConnectionStatusChanged(ConnectionType.Prosim, false);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> WaitForSessionReadyAsync()
        {
            try
            {
                // Check if SimConnect is connected
                if (!IsSimConnectConnected || IPCManager.SimConnect == null)
                {
                    _logger.LogError("Cannot wait for session: SimConnect not connected");
                    return false;
                }

                // Subscribe to camera state
                IPCManager.SimConnect.SubscribeSimVar("CAMERA STATE", "Enum");

                // Wait for camera to be ready
                await Task.Delay(250, _cts.Token);

                bool isReady = IPCManager.IsCamReady();

                // If already ready, return success
                if (isReady)
                {
                    // Update state and raise event if needed
                    if (!IsSessionReady)
                    {
                        IsSessionReady = true;
                        OnConnectionStatusChanged(ConnectionType.Session, true);
                    }

                    return true;
                }

                // Poll for camera to be ready
                int waitDuration = 5000; // 5 seconds
                int maxAttempts = 12; // 12 attempts = 60 seconds

                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    // Check for cancellation
                    if (_model.CancellationRequested)
                    {
                        return false;
                    }

                    // Check if simulator is still running
                    if (!IPCManager.IsSimRunning())
                    {
                        _logger.LogError("Flight simulator closed while waiting for session");
                        IsFlightSimulatorConnected = false;
                        OnConnectionStatusChanged(ConnectionType.FlightSimulator, false);
                        return false;
                    }

                    // Wait before checking again
                    _logger.LogInformation("Waiting for session to be ready ({Attempt}/{MaxAttempts})...",
                        attempt + 1, maxAttempts);
                    await Task.Delay(waitDuration, _cts.Token);

                    // Check if ready
                    isReady = IPCManager.IsCamReady();

                    if (isReady)
                    {
                        // Update state and raise event
                        IsSessionReady = true;
                        OnConnectionStatusChanged(ConnectionType.Session, true);

                        _logger.LogInformation("Session is ready");
                        return true;
                    }
                }

                // Timeout reached
                _logger.LogError("Timed out waiting for session to be ready");

                // Session not ready
                IsSessionReady = false;
                OnConnectionStatusChanged(ConnectionType.Session, false);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception waiting for session");

                // Session not ready
                IsSessionReady = false;
                OnConnectionStatusChanged(ConnectionType.Session, false);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task DisconnectAsync()
        {
            try
            {
                // Cancel any pending operations
                var oldCts = _cts;
                _cts = new CancellationTokenSource();
                oldCts.Cancel();

                // Disconnect SimConnect
                if (IPCManager.SimConnect != null)
                {
                    IPCManager.SimConnect.Disconnect();
                    IPCManager.SimConnect = null;
                }

                // Update state
                IsSimConnectConnected = false;
                IsSessionReady = false;

                // Raise events
                OnConnectionStatusChanged(ConnectionType.SimConnect, false);
                OnConnectionStatusChanged(ConnectionType.Session, false);

                // Allow any pending operations to complete
                await Task.Yield();

                // Dispose old cancellation token source
                oldCts.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during disconnect");
            }
        }

        /// <summary>
        /// Raises the ConnectionStatusChanged event
        /// </summary>
        /// <param name="connectionType">The type of connection</param>
        /// <param name="isConnected">Whether the connection is established</param>
        private void OnConnectionStatusChanged(ConnectionType connectionType, bool isConnected)
        {
            try
            {
                // Log connection status change
                _logger.LogInformation("Connection status changed: {ConnectionType} = {IsConnected}",
                    connectionType, isConnected);

                // Raise event
                ConnectionStatusChanged?.Invoke(this, new ConnectionStatusEventArgs(connectionType, isConnected));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception raising ConnectionStatusChanged event");
            }
        }
    }
}
