using Microsoft.FlightSimulator.SimConnect;
using Prosim2GSX.Services.GSX.Interfaces;
using System;
using System.Collections.Generic;

namespace Prosim2GSX.Services.GSX.Implementation
{
    /// <summary>
    /// Implementation of GSX SimConnect service
    /// </summary>
    public class GsxSimConnectService : IGsxSimConnectService
    {
        private readonly MobiSimConnect _simConnect;
        private readonly Dictionary<string, Action<float, float, string>> _lvarHandlers =
            new Dictionary<string, Action<float, float, string>>();

        /// <summary>
        /// Constructor
        /// </summary>
        public GsxSimConnectService(MobiSimConnect simConnect)
        {
            _simConnect = simConnect ?? throw new ArgumentNullException(nameof(simConnect));

            // Subscribe to essential GSX LVARs
            SubscribeToEssentialLvars();
        }

        /// <summary>
        /// Subscribe to essential GSX LVARs
        /// </summary>
        private void SubscribeToEssentialLvars()
        {
            _simConnect.SubscribeLvar("FSDT_GSX_COUATL_STARTED");
            _simConnect.SubscribeLvar("FSDT_GSX_DEBOARDING_STATE");
            _simConnect.SubscribeLvar("FSDT_GSX_CATERING_STATE");
            _simConnect.SubscribeLvar("FSDT_GSX_COCKPIT_DOOR_OPEN");
            _simConnect.SubscribeLvar("FSDT_GSX_REFUELING_STATE");
            _simConnect.SubscribeLvar("FSDT_GSX_BOARDING_STATE");
            _simConnect.SubscribeLvar("FSDT_GSX_DEPARTURE_STATE");
            _simConnect.SubscribeLvar("FSDT_GSX_DEICING_STATE");
            _simConnect.SubscribeLvar("FSDT_GSX_NUMPASSENGERS");
            _simConnect.SubscribeLvar("FSDT_GSX_NUMPASSENGERS_BOARDING_TOTAL");
            _simConnect.SubscribeLvar("FSDT_GSX_NUMPASSENGERS_DEBOARDING_TOTAL");
            _simConnect.SubscribeLvar("FSDT_GSX_BOARDING_CARGO");
            _simConnect.SubscribeLvar("FSDT_GSX_DEBOARDING_CARGO");
            _simConnect.SubscribeLvar("FSDT_GSX_BOARDING_CARGO_PERCENT");
            _simConnect.SubscribeLvar("FSDT_GSX_DEBOARDING_CARGO_PERCENT");
            _simConnect.SubscribeLvar("FSDT_GSX_FUELHOSE_CONNECTED");
            _simConnect.SubscribeLvar("FSDT_GSX_JETWAY");
            _simConnect.SubscribeLvar("FSDT_GSX_OPERATEJETWAYS_STATE");
            _simConnect.SubscribeLvar("FSDT_GSX_STAIRS");
            _simConnect.SubscribeLvar("FSDT_GSX_OPERATESTAIRS_STATE");

            // Subscribe to SimVars
            _simConnect.SubscribeSimVar("SIM ON GROUND", "Bool");
            _simConnect.SubscribeSimVar("GPS GROUND SPEED", "Meters per second");
        }

        /// <inheritdoc/>
        public float ReadGsxLvar(string lvarName)
        {
            try
            {
                return _simConnect.ReadLvar(lvarName);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxSimConnectService),
                    $"Error reading LVAR {lvarName}: {ex.Message}");
                return 0;
            }
        }

        /// <inheritdoc/>
        public void WriteGsxLvar(string lvarName, float value)
        {
            try
            {
                _simConnect.WriteLvar(lvarName, value);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxSimConnectService),
                    $"Error writing LVAR {lvarName} = {value}: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public void SubscribeToGsxLvar(string lvarName, Action<float, float, string> handler)
        {
            try
            {
                // Store the handler
                _lvarHandlers[lvarName] = handler;

                // Subscribe to the LVAR with SimConnect
                _simConnect.SubscribeLvar(lvarName, OnLvarChanged);

                Logger.Log(LogLevel.Debug, nameof(GsxSimConnectService),
                    $"Subscribed to LVAR {lvarName}");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxSimConnectService),
                    $"Error subscribing to LVAR {lvarName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Handler for LVAR changes
        /// </summary>
        private void OnLvarChanged(float newValue, float oldValue, string lvarName)
        {
            try
            {
                // Check if we have a handler for this LVAR
                if (_lvarHandlers.TryGetValue(lvarName, out var handler))
                {
                    // Call the handler
                    handler(newValue, oldValue, lvarName);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxSimConnectService),
                    $"Error in LVAR change handler for {lvarName}: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public bool IsSimOnGround()
        {
            try
            {
                return _simConnect.ReadSimVar("SIM ON GROUND", "Bool") != 0.0f;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxSimConnectService),
                    $"Error checking if sim is on ground: {ex.Message}");
                return true; // Default to on ground for safety
            }
        }

        /// <inheritdoc/>
        public bool IsCouatlRunning()
        {
            try
            {
                return ReadGsxLvar("FSDT_GSX_COUATL_STARTED") == 1;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxSimConnectService),
                    $"Error checking if Couatl is running: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc/>
        public int GetDepartureState()
        {
            try
            {
                return (int)ReadGsxLvar("FSDT_GSX_DEPARTURE_STATE");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxSimConnectService),
                    $"Error getting departure state: {ex.Message}");
                return 0;
            }
        }

        /// <inheritdoc/>
        public int GetCateringState()
        {
            try
            {
                return (int)ReadGsxLvar("FSDT_GSX_CATERING_STATE");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxSimConnectService),
                    $"Error getting catering state: {ex.Message}");
                return 0;
            }
        }

        /// <inheritdoc/>
        public int GetBoardingState()
        {
            try
            {
                return (int)ReadGsxLvar("FSDT_GSX_BOARDING_STATE");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxSimConnectService),
                    $"Error getting boarding state: {ex.Message}");
                return 0;
            }
        }

        /// <inheritdoc/>
        public int GetDeboardingState()
        {
            try
            {
                return (int)ReadGsxLvar("FSDT_GSX_DEBOARDING_STATE");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxSimConnectService),
                    $"Error getting deboarding state: {ex.Message}");
                return 0;
            }
        }

        /// <inheritdoc/>
        public bool IsFuelHoseConnected()
        {
            try
            {
                return ReadGsxLvar("FSDT_GSX_FUELHOSE_CONNECTED") == 1;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxSimConnectService),
                    $"Error checking if fuel hose is connected: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc/>
        public int GetBoardedPassengerCount()
        {
            try
            {
                return (int)ReadGsxLvar("FSDT_GSX_NUMPASSENGERS_BOARDING_TOTAL");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxSimConnectService),
                    $"Error getting boarded passenger count: {ex.Message}");
                return 0;
            }
        }

        /// <inheritdoc/>
        public int GetDeboardedPassengerCount()
        {
            try
            {
                return (int)ReadGsxLvar("FSDT_GSX_NUMPASSENGERS_DEBOARDING_TOTAL");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxSimConnectService),
                    $"Error getting deboarded passenger count: {ex.Message}");
                return 0;
            }
        }

        /// <inheritdoc/>
        public int GetCargoBoardingPercentage()
        {
            try
            {
                return (int)ReadGsxLvar("FSDT_GSX_BOARDING_CARGO_PERCENT");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxSimConnectService),
                    $"Error getting cargo boarding percentage: {ex.Message}");
                return 0;
            }
        }

        /// <inheritdoc/>
        public int GetCargoDeboardingPercentage()
        {
            try
            {
                return (int)ReadGsxLvar("FSDT_GSX_DEBOARDING_CARGO_PERCENT");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxSimConnectService),
                    $"Error getting cargo deboarding percentage: {ex.Message}");
                return 0;
            }
        }

        /// <inheritdoc/>
        public int GetRefuelingState()
        {
            try
            {
                return (int)ReadGsxLvar("FSDT_GSX_REFUELING_STATE");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(GsxSimConnectService),
                    $"Error getting refueling state: {ex.Message}");
                return 0;
            }
        }
    }
}