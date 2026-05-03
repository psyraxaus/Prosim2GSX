using CFIT.AppFramework.Services;
using CFIT.AppLogger;
using CFIT.AppTools;
using Prosim2GSX.AppConfig;
using ProsimInterface;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Prosim
{
    /// <summary>
    /// Service controller for managing ProSim SDK lifecycle, connection, and operations
    /// </summary>
    public class ProsimSdkService : ServiceController<Prosim2GSX, AppService, Config, Definition>, IProsimSdkService
    {
        // Connection state
        public virtual bool IsConnected { get; protected set; } = false;
        public virtual bool IsInitialized { get; protected set; } = false;
        protected virtual bool IsMonitoring { get; set; } = false;
        protected virtual DateTime NextConnectionCheck { get; set; } = DateTime.MinValue;
        protected virtual int ReconnectAttempts { get; set; } = 0;
        protected virtual bool ProsimBinaryRunning => !Config.IsProsimLocal || Sys.GetProcessRunning(Config.ProsimBinary);

        // SDK Interface
        public virtual ProsimAircraftInterface AircraftInterface { get; protected set; }
        public virtual ProsimAudioInterface AudioInterface { get; protected set; }

        // Events
        public event Action<bool> OnConnectionChanged;

        // Cancellation
        public virtual CancellationToken RequestToken => AppService.Instance?.RequestToken ?? Token;

        public ProsimSdkService(Config config) : base(config)
        {
            Logger.Information("ProsimSdkService created");
        }

        /// <summary>
        /// Initialize the SDK service - verifies SDK assembly is loaded
        /// Note: AircraftInterface creation deferred until SetAircraftInterface() is called with IGsxController
        /// </summary>
        public virtual async Task<bool> Initialize()
        {
            if (IsInitialized)
            {
                Logger.Warning("ProsimSdkService already initialized");
                return true;
            }

            try
            {
                Logger.Information("Initializing ProsimSdkService...");

                // Verify SDK assembly is loaded by checking if we can access ProsimInterface types
                // The actual ProsimAircraftInterface will be created when SetAircraftInterface is called
                var prosimType = typeof(ProsimAircraftInterface);
                Logger.Information($"ProSim SDK types accessible: {prosimType.FullName}");

                IsInitialized = true;

                // Attempt initial connection verification (checks if ProSim binary is running)
                bool binaryRunning = await VerifyConnection();
                if (binaryRunning)
                {
                    Logger.Information("ProsimSdkService initialized - ProSim binary detected");
                }
                else
                {
                    Logger.Warning("ProsimSdkService initialized but ProSim binary not running yet");
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to initialize ProsimSdkService");
                Logger.LogException(ex);
                IsInitialized = false;
                return false;
            }
        }

        /// <summary>
        /// Set the aircraft interface instance (called by GsxController after it's created)
        /// </summary>
        public virtual void SetAircraftInterface(ProsimAircraftInterface aircraftInterface)
        {
            if (aircraftInterface == null)
            {
                Logger.Warning("SetAircraftInterface called with null interface");
                return;
            }

            Logger.Information("ProsimAircraftInterface instance registered with SDK service");
            AircraftInterface = aircraftInterface;

            try
            {
                AudioInterface = new ProsimAudioInterface(aircraftInterface.SdkInterface);
                Logger.Debug("ProsimAudioInterface created alongside AircraftInterface");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to create ProsimAudioInterface");
                Logger.LogException(ex);
                AudioInterface = null;
            }
        }

        /// <summary>
        /// Verify that the SDK can connect to ProSim
        /// </summary>
        public virtual async Task<bool> VerifyConnection()
        {
            if (!IsInitialized)
            {
                Logger.Warning("Cannot verify connection - SDK not initialized");
                return false;
            }

            try
            {
                Logger.Debug("Verifying ProSim SDK connection...");

                // Check if ProSim binary is running
                if (!ProsimBinaryRunning)
                {
                    Logger.Debug($"ProSim binary '{Config.ProsimBinary}' is not running");
                    SetConnectionState(false);
                    return false;
                }

                // If AircraftInterface is available, check if it's loaded
                if (AircraftInterface != null)
                {
                    // Give a small delay for the interface to initialize
                    await Task.Delay(500, Token);

                    bool isConnected = AircraftInterface.IsLoaded;
                    SetConnectionState(isConnected);

                    if (isConnected)
                    {
                        Logger.Information("ProSim SDK connection verified successfully");
                        ReconnectAttempts = 0;
                    }
                    else
                    {
                        Logger.Warning("ProSim SDK connection verification failed - interface not loaded");
                    }

                    return isConnected;
                }
                else
                {
                    // AircraftInterface not yet created, but binary is running
                    Logger.Debug("ProSim binary running, AircraftInterface not yet created");
                    SetConnectionState(true);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error verifying ProSim SDK connection");
                Logger.LogException(ex);
                SetConnectionState(false);
                return false;
            }
        }

        /// <summary>
        /// Attempt to reconnect to ProSim
        /// </summary>
        public virtual async Task Reconnect()
        {
            ReconnectAttempts++;
            Logger.Information($"Attempting to reconnect to ProSim (attempt #{ReconnectAttempts}/{Config.ProSimSdkMaxReconnectAttempts})...");

            bool connected = await VerifyConnection();

            if (connected)
            {
                Logger.Information("Reconnection successful");
                ReconnectAttempts = 0;
            }
            else
            {
                Logger.Warning($"Reconnection failed (attempt #{ReconnectAttempts}/{Config.ProSimSdkMaxReconnectAttempts})");
                
                if (ReconnectAttempts >= Config.ProSimSdkMaxReconnectAttempts)
                {
                    Logger.Error($"Maximum reconnection attempts reached. No further automatic reconnection will be attempted.");
                }
            }
        }

        /// <summary>
        /// Start the SDK service and begin connection monitoring
        /// </summary>
        public new virtual void Start()
        {
            if (!IsInitialized)
            {
                Logger.Error("Cannot start ProsimSdkService - not initialized");
                return;
            }

            Logger.Information("Starting ProsimSdkService...");
            base.Start();
        }

        /// <summary>
        /// Main monitoring loop for SDK connection
        /// </summary>
        protected override async Task DoRun()
        {
            Logger.Information("ProsimSdkService monitoring started");
            IsMonitoring = true;

            try
            {
                while (IsExecutionAllowed && !Token.IsCancellationRequested)
                {
                    // Periodic connection check
                    if (NextConnectionCheck <= DateTime.Now)
                    {
                        bool previousState = IsConnected;
                        await VerifyConnection();

                        // If connection was lost and auto-reconnect is enabled, attempt reconnection
                        if (previousState && !IsConnected && Config.ProSimSdkAutoReconnect)
                        {
                            Logger.Warning("ProSim SDK connection lost, attempting reconnection...");
                            
                            // Check if we've exceeded max reconnect attempts
                            if (ReconnectAttempts < Config.ProSimSdkMaxReconnectAttempts)
                            {
                                await Reconnect();
                            }
                            else
                            {
                                Logger.Error($"Max reconnection attempts ({Config.ProSimSdkMaxReconnectAttempts}) exceeded. Stopping reconnection attempts.");
                                Logger.Information("ProSim SDK will continue monitoring. Manual restart or ProSim restart may restore connection.");
                            }
                        }
                        else if (!previousState && IsConnected)
                        {
                            // Connection was restored
                            Logger.Information("ProSim SDK connection restored!");
                            ReconnectAttempts = 0;
                        }

                        NextConnectionCheck = DateTime.Now + TimeSpan.FromMilliseconds(Config.ProSimSdkReconnectInterval);
                    }

                    await Task.Delay(1000, Token);
                }
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                {
                    Logger.Error("Error in ProsimSdkService monitoring loop");
                    Logger.LogException(ex);
                }
            }

            IsMonitoring = false;
            Logger.Information("ProsimSdkService monitoring stopped");
        }

        /// <summary>
        /// Stop the SDK service
        /// </summary>
        public new virtual async Task Stop()
        {
            Logger.Information("Stopping ProsimSdkService...");
            IsMonitoring = false;
            SetConnectionState(false);
            await base.Stop();
            Logger.Information("ProsimSdkService stopped");
        }

        /// <summary>
        /// Set connection state and raise event if changed
        /// </summary>
        protected virtual void SetConnectionState(bool connected)
        {
            if (IsConnected != connected)
            {
                IsConnected = connected;
                Logger.Information($"ProSim SDK connection state changed: {(connected ? "CONNECTED" : "DISCONNECTED")}");
                OnConnectionChanged?.Invoke(connected);
            }
        }

        #region Facade Methods for Common SDK Operations

        // These methods provide a clean interface for common SDK operations
        // They can be expanded as needed for future SDK features

        /// <summary>
        /// Get whether engines are running
        /// </summary>
        public virtual bool GetEnginesRunning()
        {
            return AircraftInterface?.GetEnginesRunning() ?? false;
        }

        /// <summary>
        /// Get whether BOTH engines are running. Use this for gates that must
        /// not fire on a single-engine start (e.g. GSX's "good engine start"
        /// confirmation).
        /// </summary>
        public virtual bool GetAllEnginesRunning()
        {
            return AircraftInterface?.GetAllEnginesRunning() ?? false;
        }

        /// <summary>
        /// Get current fuel amount
        /// </summary>
        public virtual double GetFuelCurrent()
        {
            return AircraftInterface?.FuelCurrent ?? 0.0;
        }

        /// <summary>
        /// Get target fuel amount
        /// </summary>
        public virtual double GetFuelTarget()
        {
            return AircraftInterface?.FuelTarget ?? 0.0;
        }

        /// <summary>
        /// Get aircraft registration
        /// </summary>
        public virtual string GetRegistration()
        {
            return AircraftInterface?.Registration ?? string.Empty;
        }

        /// <summary>
        /// Get whether aircraft is currently refueling
        /// </summary>
        public virtual bool GetIsRefueling()
        {
            return AircraftInterface?.IsRefueling ?? false;
        }

        /// <summary>
        /// Get GPU state
        /// </summary>
        public virtual bool GetGpuState()
        {
            return AircraftInterface?.GetGpuState() ?? false;
        }

        /// <summary>
        /// Get PCA state
        /// </summary>
        public virtual bool GetPcaState()
        {
            return AircraftInterface?.GetPcaState() ?? false;
        }

        /// <summary>
        /// Get chocks state
        /// </summary>
        public virtual bool GetChocksState()
        {
            return AircraftInterface?.GetChocksState() ?? false;
        }

        /// <summary>
        /// Set ground power
        /// </summary>
        public virtual async Task SetGroundPower(bool state, bool force = false)
        {
            if (AircraftInterface != null)
            {
                await AircraftInterface.SetGroundPower(state, force);
            }
        }

        /// <summary>
        /// Set chocks
        /// </summary>
        public virtual async Task SetChocks(bool state, bool force = false)
        {
            if (AircraftInterface != null)
            {
                await AircraftInterface.SetChocks(state, force);
            }
        }

        /// <summary>
        /// Set PCA
        /// </summary>
        public virtual async Task SetPca(bool state)
        {
            if (AircraftInterface != null)
            {
                await AircraftInterface.SetPca(state);
            }
        }

        /// <summary>
        /// Set forward stairs
        /// </summary>
        public virtual async Task SetStairsFwd(bool state)
        {
            if (AircraftInterface != null)
            {
                await AircraftInterface.SetStairsFwd(state);
            }
        }

        /// <summary>
        /// Start boarding
        /// </summary>
        public virtual async Task BoardingStart()
        {
            if (AircraftInterface != null)
            {
                await AircraftInterface.BoardingStart();
            }
        }

        /// <summary>
        /// Stop boarding
        /// </summary>
        public virtual async Task BoardingStop()
        {
            if (AircraftInterface != null)
            {
                await AircraftInterface.BoardingStop();
            }
        }

        /// <summary>
        /// Get SDK version if available
        /// </summary>
        public virtual Version GetSdkVersion()
        {
            // This would require the SDK to expose version information
            // For now, return a placeholder
            return new Version(1, 0, 0, 0);
        }

        /// <summary>
        /// Check if SDK version is compatible
        /// </summary>
        public virtual bool IsVersionCompatible(Version minVersion)
        {
            var currentVersion = GetSdkVersion();
            return currentVersion >= minVersion;
        }

        #endregion

        protected override Task FreeResources()
        {
            base.FreeResources();
            try { AudioInterface?.UnsubscribeAll(); } catch { }
            AudioInterface = null;
            AircraftInterface = null;
            IsInitialized = false;
            IsConnected = false;
            return Task.CompletedTask;
        }
    }
}
