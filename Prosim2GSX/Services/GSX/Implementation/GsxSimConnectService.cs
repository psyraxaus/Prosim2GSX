﻿using Microsoft.Extensions.Logging;
using Microsoft.FlightSimulator.SimConnect;
using Prosim2GSX.Events;
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
        private readonly ILogger<GsxSimConnectService> _logger;
        private readonly Dictionary<string, Action<float, float, string>> _lvarHandlers =
            new Dictionary<string, Action<float, float, string>>();

        /// <summary>
        /// Constructor
        /// </summary>
        public GsxSimConnectService(ILogger<GsxSimConnectService> logger, MobiSimConnect simConnect)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _simConnect = simConnect ?? throw new ArgumentNullException(nameof(simConnect));

            // Subscribe to essential GSX LVARs
            SubscribeToEssentialLvars();

            _logger.LogInformation("GSX SimConnect Service initialized");
        }

        /// <summary>
        /// Subscribe to essential GSX LVARs
        /// </summary>
        private void SubscribeToEssentialLvars()
        {
            // Subscribe to LVARs with callbacks
            SubscribeToGsxLvar("FSDT_GSX_JETWAY", OnJetwayStateChanged);
            SubscribeToGsxLvar("FSDT_GSX_STAIRS", OnStairsStateChanged);
            SubscribeToGsxLvar("FSDT_GSX_DEPARTURE_STATE", OnDepartureStateChanged);

            // Subscribe to other LVARs without callbacks
            _simConnect.SubscribeLvar("FSDT_GSX_COUATL_STARTED");
            _simConnect.SubscribeLvar("FSDT_GSX_COCKPIT_DOOR_OPEN");
            _simConnect.SubscribeLvar("FSDT_GSX_DEICING_STATE");
            _simConnect.SubscribeLvar("FSDT_GSX_NUMPASSENGERS");
            _simConnect.SubscribeLvar("FSDT_GSX_NUMPASSENGERS_BOARDING_TOTAL");
            _simConnect.SubscribeLvar("FSDT_GSX_NUMPASSENGERS_DEBOARDING_TOTAL");
            _simConnect.SubscribeLvar("FSDT_GSX_BOARDING_CARGO_PERCENT");
            _simConnect.SubscribeLvar("FSDT_GSX_DEBOARDING_CARGO_PERCENT");
            _simConnect.SubscribeLvar("FSDT_GSX_OPERATEJETWAYS_STATE");
            _simConnect.SubscribeLvar("FSDT_GSX_OPERATESTAIRS_STATE");
            _simConnect.SubscribeLvar("FSDT_GSX_REFUELING_STATE");

            // Subscribe to SimVars
            _simConnect.SubscribeSimVar("SIM ON GROUND", "Bool");
            _simConnect.SubscribeSimVar("GPS GROUND SPEED", "Meters per second");

            _logger.LogDebug("Essential LVARS and SimVars subscribed");
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
                _logger.LogError(ex, "Error reading LVAR {LvarName}", lvarName);
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
                _logger.LogError(ex, "Error writing LVAR {LvarName} = {Value}", lvarName, value);
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

                _logger.LogDebug("Subscribed to LVAR {LvarName}", lvarName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to LVAR {LvarName}", lvarName);
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
                _logger.LogError(ex, "Error in LVAR change handler for {LvarName}", lvarName);
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
                _logger.LogError(ex, "Error checking if sim is on ground");
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
                _logger.LogError(ex, "Error checking if Couatl is running");
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
                _logger.LogError(ex, "Error getting departure state");
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
                _logger.LogError(ex, "Error getting catering state");
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
                _logger.LogError(ex, "Error getting boarding state");
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
                _logger.LogError(ex, "Error getting deboarding state");
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
                _logger.LogError(ex, "Error checking if fuel hose is connected");
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
                _logger.LogError(ex, "Error getting boarded passenger count");
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
                _logger.LogError(ex, "Error getting deboarded passenger count");
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
                _logger.LogError(ex, "Error getting cargo boarding percentage");
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
                _logger.LogError(ex, "Error getting cargo deboarding percentage");
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
                _logger.LogError(ex, "Error getting refueling state");
                return 0;
            }
        }

        /// <summary>
        /// Handler for jetway state changes
        /// </summary>
        private void OnJetwayStateChanged(float newValue, float oldValue, string lvarName)
        {
            _logger.LogDebug("Jetway state changed to {NewValue}", newValue);

            if (newValue != oldValue)
            {
                ServiceStatus status = newValue == 5 ? ServiceStatus.Completed :
                                       newValue == 1 ? ServiceStatus.Disconnected :
                                       ServiceStatus.Inactive;
                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("Jetway", status));
            }
        }

        /// <summary>
        /// Handler for stairs state changes
        /// </summary>
        private void OnStairsStateChanged(float newValue, float oldValue, string lvarName)
        {
            _logger.LogDebug("Stairs state changed to {NewValue}", newValue);

            if (newValue != oldValue)
            {
                ServiceStatus status = newValue == 5 ? ServiceStatus.Completed :
                                       newValue == 1 ? ServiceStatus.Disconnected :
                                       ServiceStatus.Inactive;
                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("Stairs", status));
            }
        }

        /// <summary>
        /// Handler for departure state changes
        /// </summary>
        private void OnDepartureStateChanged(float newValue, float oldValue, string lvarName)
        {
            if (newValue != oldValue)
            {
                var status = GetStatusFromGsxState((int)newValue);
                EventAggregator.Instance.Publish(new ServiceStatusChangedEvent("Pushback", status));
                _logger.LogInformation("Departure state changed to {Status}", status);
            }
        }

        /// <summary>
        /// Helper method to convert GSX state values to ServiceStatus
        /// </summary>
        private ServiceStatus GetStatusFromGsxState(int state)
        {
            // GSX states: 0=Inactive, 4=Requested, 5=Active, 6=Completed
            switch (state)
            {
                case 6:
                    return ServiceStatus.Completed;
                case 5:
                    return ServiceStatus.Active;
                case 4:
                    return ServiceStatus.Requested;
                case 2:
                    return ServiceStatus.Disconnected;
                default:
                    return ServiceStatus.Inactive;
            }
        }
    }
}
