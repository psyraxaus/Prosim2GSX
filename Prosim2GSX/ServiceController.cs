using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.FlightSimulator.SimConnect;
using Prosim2GSX.Events;
using Prosim2GSX.Models;
using Prosim2GSX.Services;
using Prosim2GSX.Services.Audio;
using Prosim2GSX.Services.Connection.Enum;
using Prosim2GSX.Services.Connection.Events;
using Prosim2GSX.Services.Connection.Interfaces;
using Prosim2GSX.Services.GSX;
using Prosim2GSX.Services.Prosim.Interfaces;

namespace Prosim2GSX
{
    /// <summary>
    /// Controls the main service loop for the application
    /// </summary>
    public class ServiceController
    {
        private readonly IAudioService _audioService;
        private readonly ServiceModel _model;
        private readonly IConnectionService _connectionService;
        private readonly IProsimInterface _prosimInterface;
        private readonly IDataRefMonitoringService _dataRefService;
        private readonly IFlightPlanService _flightPlanService;
        private FlightPlan _flightPlan;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);

        private SubscriptionToken _retryToken;

        /// <summary>
        /// Creates a new instance of ServiceController
        /// </summary>
        /// <param name="model">The service model</param>
        public ServiceController(ServiceModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _connectionService = ServiceLocator.ConnectionService;
            _prosimInterface = ServiceLocator.ProsimInterface;
            _dataRefService = ServiceLocator.DataRefService;
            _flightPlanService = ServiceLocator.FlightPlanService;
            _audioService = new AudioService(_prosimInterface, _dataRefService, IPCManager.SimConnect, _model);

            // Add this line to set the AudioService in the ServiceModel
            if (_model is ServiceModel serviceModel)
            {
                serviceModel.SetAudioService((AudioService)_audioService);
            }

            // Subscribe to the retry event
            _retryToken = EventAggregator.Instance.Subscribe<RetryFlightPlanLoadEvent>(OnRetryFlightPlanLoad);

            // Subscribe to connection events
            _connectionService.ConnectionStatusChanged += OnConnectionStatusChanged;
        }

        private void OnConnectionStatusChanged(object sender, ConnectionStatusEventArgs e)
        {
            // Update model and publish events based on connection type
            switch (e.ConnectionType)
            {
                case ConnectionType.FlightSimulator:
                    _model.IsSimRunning = e.IsConnected;
                    break;
                case ConnectionType.SimConnect:
                    // SimConnect status is handled directly by the connection service
                    break;
                case ConnectionType.Prosim:
                    _model.IsProsimRunning = e.IsConnected;
                    break;
                case ConnectionType.Session:
                    _model.IsSessionRunning = e.IsConnected;
                    break;
            }

            // Publish event for other components
            EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent(
                e.ConnectionType.ToString(), e.IsConnected));
        }

        private void OnRetryFlightPlanLoad(RetryFlightPlanLoadEvent evt)
        {
            // Retry loading the flight plan
            if (_flightPlan != null && _model.IsValidSimbriefId())
            {
                Logger.Log(LogLevel.Information, nameof(ServiceController), "Retrying flight plan load with new Simbrief ID");
                _flightPlan.LoadWithValidation();
            }
        }

        /// <summary>
        /// Runs the service controller
        /// </summary>
        public async Task RunAsync()
        {
            try
            {
                Logger.Log(LogLevel.Information, nameof(ServiceController), "Service starting...");

                while (!_model.CancellationRequested)
                {
                    try
                    {
                        // Wait for all connections and initialize flight plan
                        bool ready = await EstablishConnectionsAsync();

                        if (ready)
                        {
                            // Run the service loop
                            await RunServiceLoopAsync();
                        }
                        else if (!_connectionService.IsFlightSimulatorConnected)
                        {
                            // Flight simulator is not running
                            _model.IsSimRunning = false;
                            EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("MSFS", false));

                            _model.CancellationRequested = true;
                            _model.ServiceExited = true;
                            Logger.Log(LogLevel.Critical, nameof(ServiceController), "Session aborted, Retry not possible - exiting Program");
                            return;
                        }
                        else
                        {
                            // Some other connection failed but flight simulator is running
                            await ResetConnectionsAsync();
                            Logger.Log(LogLevel.Information, nameof(ServiceController), "Session aborted, Retry possible - Waiting for new Session");

                            // Add a delay before retry
                            await Task.Delay(5000, _cts.Token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Cancellation requested
                        break;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Error, nameof(ServiceController),
                            $"Exception in service loop: {ex.Message}. Attempting recovery...");

                        await ResetConnectionsAsync();

                        // Add a delay before retry
                        await Task.Delay(5000, _cts.Token);
                    }
                }

                // Clean up connections
                await _connectionService.DisconnectAsync();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, nameof(ServiceController),
                    $"Critical Exception occurred: {ex.Source} - {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                // Dispose resources
                if (_audioService is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                _cts.Dispose();
                _connectionLock.Dispose();
            }
        }

        private async Task<bool> EstablishConnectionsAsync()
        {
            // Acquire lock to prevent concurrent connection attempts
            await _connectionLock.WaitAsync();

            try
            {
                // Step 1: Connect to Flight Simulator
                if (!await _connectionService.ConnectToFlightSimulatorAsync())
                {
                    return false;
                }

                _model.IsSimRunning = true;

                // Step 2: Connect to SimConnect
                if (!await _connectionService.ConnectToSimConnectAsync())
                {
                    return false;
                }

                // Step 3: Connect to Prosim
                if (!await _connectionService.ConnectToProsimAsync())
                {
                    return false;
                }

                _model.IsProsimRunning = true;

                // Step 4: Initialize Flight Plan if needed
                if (_flightPlan == null)
                {
                    if (!await InitializeFlightPlanAsync())
                    {
                        return false;
                    }
                }

                // Step 5: Wait for session to be ready
                if (!await _connectionService.WaitForSessionReadyAsync())
                {
                    return false;
                }

                _model.IsSessionRunning = true;

                // All connections established successfully
                return true;
            }
            finally
            {
                // Release lock
                _connectionLock.Release();
            }
        }

        private async Task<bool> InitializeFlightPlanAsync()
        {
            try
            {
                // Create and load FlightPlan using the manually entered Simbrief ID
                _flightPlan = new FlightPlan(_model);

                // Try to load with validation
                var loadResult = _flightPlan.LoadWithValidation();

                // Handle different error cases
                switch (loadResult)
                {
                    case FlightPlan.LoadResult.Success:
                        // Make FlightPlan available to services
                        _flightPlanService.SetFlightPlan(_flightPlan);
                        return true;

                    case FlightPlan.LoadResult.InvalidId:
                        // Special handling for default SimBrief ID (0)
                        if (_model.SimBriefID == "0")
                        {
                            // Just log a message and return false, don't start a background task
                            Logger.Log(LogLevel.Warning, nameof(ServiceController),
                                "Default SimBrief ID detected. Please enter a valid Simbrief ID in Settings tab.");

                            // Don't try to load the flight plan or start a background task
                            return false;
                        }
                        else
                        {
                            // For other invalid IDs, wait for user to enter a valid ID
                            Logger.Log(LogLevel.Warning, nameof(ServiceController),
                                "Waiting for valid Simbrief ID to be entered in Settings tab...");

                            // Start a background task to periodically check for valid ID
                            _ = Task.Run(async () => {
                                while (!_model.CancellationRequested && !_model.IsValidSimbriefId())
                                {
                                    await Task.Delay(5000, _cts.Token); // Check every 5 seconds
                                }

                                // When valid ID is detected, try loading again
                                if (!_model.CancellationRequested && _model.IsValidSimbriefId())
                                {
                                    EventAggregator.Instance.Publish(new RetryFlightPlanLoadEvent());
                                }
                            });

                            return false;
                        }

                    case FlightPlan.LoadResult.NetworkError:
                        Logger.Log(LogLevel.Error, nameof(ServiceController),
                            "Network error loading flight plan. Check your internet connection.");
                        await Task.Delay(5000, _cts.Token);
                        return false;

                    case FlightPlan.LoadResult.ParseError:
                        Logger.Log(LogLevel.Error, nameof(ServiceController),
                            "Error parsing flight plan data. The Simbrief API may have changed or returned invalid data.");
                        await Task.Delay(5000, _cts.Token);
                        return false;

                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(ServiceController),
                    $"Exception initializing flight plan: {ex.Message}");
                return false;
            }
        }

        private async Task ResetConnectionsAsync()
        {
            await _connectionLock.WaitAsync();

            try
            {
                // Disconnect from SimConnect
                await _connectionService.DisconnectAsync();

                // Update status
                _model.IsSessionRunning = false;
                _model.IsProsimRunning = false;

                // Publish events
                EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("Session", false));
                EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("Prosim", false));
                EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("SimConnect", false));
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, nameof(ServiceController),
                    $"Exception during Reset: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        private async Task RunServiceLoopAsync()
        {
            try
            {
                Logger.Log(LogLevel.Debug, nameof(ServiceController), "Ensuring GSX services are initialized");

                // Initialize GSX services if SimConnect is available
                if (IPCManager.SimConnect != null)
                {
                    try
                    {
                        // Force re-creation of GSX services with the current SimConnect instance
                        ServiceLocator.UpdateGsxServices(IPCManager.SimConnect);
                        Logger.Log(LogLevel.Information, nameof(ServiceController),
                            "GSX services initialized successfully");
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Error, nameof(ServiceController),
                            $"Error initializing GSX services: {ex.Message}. Some features may not work correctly.");
                    }
                }
                else
                {
                    Logger.Log(LogLevel.Warning, nameof(ServiceController),
                        "SimConnect is not available. GSX services will not be initialized.");
                }

                Logger.Log(LogLevel.Debug, nameof(ServiceController), "Creating GsxController");

                // Create the GSX controller
                var gsxController = new GsxController(_model, _flightPlan, _audioService);

                // Store the GsxController in IPCManager
                IPCManager.GsxController = gsxController;

                Logger.Log(LogLevel.Debug, nameof(ServiceController), "Publishing connection status events");
                // Re-publish connection status events
                EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("MSFS", _model.IsSimRunning));
                EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("Prosim", _model.IsProsimRunning));
                EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("SimConnect",
                    _connectionService.IsSimConnectConnected));
                EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("Session", _model.IsSessionRunning));

                int elapsedMS = gsxController.Interval;
                int delay = 100;

                Logger.Log(LogLevel.Debug, nameof(ServiceController), "Sleeping for 1 second");
                await Task.Delay(1000, _cts.Token);

                Logger.Log(LogLevel.Information, nameof(ServiceController), "Starting Service Loop");

                // Main service loop
                while (!_model.CancellationRequested &&
                       _model.IsProsimRunning &&
                       _connectionService.IsFlightSimulatorConnected &&
                       _connectionService.IsSessionReady)
                {
                    try
                    {
                        // Check if we need to run services
                        if (elapsedMS >= gsxController.Interval)
                        {
                            Logger.Log(LogLevel.Debug, nameof(ServiceController), "Calling gsxController.RunServices()");
                            gsxController.RunServices();
                            Logger.Log(LogLevel.Debug, nameof(ServiceController), "Completed gsxController.RunServices()");
                            elapsedMS = 0;
                        }

                        // Control audio if needed
                        if (_model.GsxVolumeControl || _model.IsVhf1Controllable())
                        {
                            _audioService.ControlAudio();
                        }

                        // Wait for next iteration
                        await Task.Delay(delay, _cts.Token);
                        elapsedMS += delay;
                    }
                    catch (OperationCanceledException)
                    {
                        // Cancellation requested
                        break;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Error, nameof(ServiceController),
                            $"Exception in service loop iteration: {ex.Message}");

                        // Continue with next iteration
                    }
                }

                Logger.Log(LogLevel.Information, nameof(ServiceController), "Service Loop ended");

                // Check and publish connection status changes that might have caused the loop to exit
                if (_model.IsSimRunning != _connectionService.IsFlightSimulatorConnected)
                {
                    _model.IsSimRunning = _connectionService.IsFlightSimulatorConnected;
                    EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("MSFS", _model.IsSimRunning));
                }

                if (!_connectionService.IsSessionReady)
                {
                    EventAggregator.Instance.Publish(new ConnectionStatusChangedEvent("Session", false));
                    _model.IsSessionRunning = false;
                }

                // Reset audio if needed
                if (_model.GsxVolumeControl || _model.IsVhf1Controllable())
                {
                    Logger.Log(LogLevel.Information, nameof(ServiceController), "Resetting GSX/VHF1 Audio");
                    _audioService.ResetAudio();
                }

                // Clear the GsxController reference
                IPCManager.GsxController = null;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, nameof(ServiceController),
                    $"Critical Exception in service loop: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Run method for backward compatibility - uses the older synchronous style
        /// </summary>
        public void Run()
        {
            // Run the async method synchronously
            RunAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Gets the audio service
        /// </summary>
        /// <returns>The audio service</returns>
        public IAudioService GetAudioService() => _audioService;

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

            Logger.Log(LogLevel.Warning, nameof(ServiceController),
                "Cannot perform VoiceMeeter diagnostics: AudioService is not available");
            return false;
        }

        /// <summary>
        /// Stops the service controller
        /// </summary>
        public void Stop()
        {
            _cts.Cancel();
        }
    }
}