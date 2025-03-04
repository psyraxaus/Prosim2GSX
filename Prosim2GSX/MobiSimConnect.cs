﻿using Microsoft.FlightSimulator.SimConnect;
using Prosim2GSX.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace Prosim2GSX
{
    public class MobiSimConnect : ISimConnectService, IDisposable
    {
        // Constants are kept for backward compatibility
        public const string MOBIFLIGHT_CLIENT_DATA_NAME_COMMAND = "MobiFlight.Command";
        public const string MOBIFLIGHT_CLIENT_DATA_NAME_RESPONSE = "MobiFlight.Response";
        public const uint MOBIFLIGHT_MESSAGE_SIZE = 1024;

        public const uint WM_PILOTSDECK_SIMCONNECT = 0x1988;
        public const string CLIENT_NAME = "Prosim2GSX";
        public const string PILOTSDECK_CLIENT_DATA_NAME_SIMVAR = $"{CLIENT_NAME}.LVars";
        public const string PILOTSDECK_CLIENT_DATA_NAME_COMMAND = $"{CLIENT_NAME}.Command";
        public const string PILOTSDECK_CLIENT_DATA_NAME_RESPONSE = $"{CLIENT_NAME}.Response";

        private readonly ISimConnectService _simConnectService;

        public bool IsConnected => _simConnectService.IsConnected;
        public bool IsReady => _simConnectService.IsReady;
        public bool IsGsxMenuReady 
        { 
            get => _simConnectService.IsGsxMenuReady; 
            set => _simConnectService.IsGsxMenuReady = value; 
        }

        public MobiSimConnect()
        {
            _simConnectService = new SimConnectService();
        }

        public MobiSimConnect(ISimConnectService simConnectService)
        {
            _simConnectService = simConnectService ?? throw new ArgumentNullException(nameof(simConnectService));
        }

        public bool Connect()
        {
            return _simConnectService.Connect();
        }

        public void Disconnect()
        {
            _simConnectService.Disconnect();
        }

        public void Dispose()
        {
            _simConnectService.Dispose();
            GC.SuppressFinalize(this);
        }

        public void ExecuteCode(string code)
        {
            _simConnectService.ExecuteCode(code);
        }

        public float ReadEnvVar(string name, string unit)
        {
            return _simConnectService.ReadEnvVar(name, unit);
        }

        public float ReadLvar(string address)
        {
            return _simConnectService.ReadLvar(address);
        }

        public float ReadSimVar(string name, string unit)
        {
            return _simConnectService.ReadSimVar(name, unit);
        }

        public void SubscribeEnvVar(string name, string unit)
        {
            _simConnectService.SubscribeEnvVar(name, unit);
        }

        public void SubscribeLvar(string address)
        {
            _simConnectService.SubscribeLvar(address);
        }

        public void SubscribeSimVar(string name, string unit)
        {
            _simConnectService.SubscribeSimVar(name, unit);
        }

        public void UnsubscribeAll()
        {
            _simConnectService.UnsubscribeAll();
        }

        public void WriteLvar(string address, float value)
        {
            _simConnectService.WriteLvar(address, value);
        }
    }
}
