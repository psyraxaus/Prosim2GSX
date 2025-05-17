using Microsoft.Extensions.Logging;
using Microsoft.FlightSimulator.SimConnect;
using Prosim2GSX.Events;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace Prosim2GSX
{
    public class MobiSimConnect : IDisposable
    {
        private readonly ILogger<MobiSimConnect> _logger;

        public const string MOBIFLIGHT_CLIENT_DATA_NAME_COMMAND = "MobiFlight.Command";
        public const string MOBIFLIGHT_CLIENT_DATA_NAME_RESPONSE = "MobiFlight.Response";
        public const uint MOBIFLIGHT_MESSAGE_SIZE = 1024;

        public const uint WM_PILOTSDECK_SIMCONNECT = 0x1988;
        public const string CLIENT_NAME = "Prosim2GSX";
        public const string PILOTSDECK_CLIENT_DATA_NAME_SIMVAR = $"{CLIENT_NAME}.LVars";
        public const string PILOTSDECK_CLIENT_DATA_NAME_COMMAND = $"{CLIENT_NAME}.Command";
        public const string PILOTSDECK_CLIENT_DATA_NAME_RESPONSE = $"{CLIENT_NAME}.Response";

        protected SimConnect simConnect = null;
        protected IntPtr simConnectHandle = IntPtr.Zero;
        protected Thread simConnectThread = null;
        private static bool cancelThread = false;

        protected bool isSimConnected = false;
        protected bool isMobiConnected = false;
        protected bool isReceiveRunning = false;
        public bool IsConnected { get { return isSimConnected && isMobiConnected; } }
        public bool IsReady { get { return IsConnected && isReceiveRunning; } }
        public bool IsGsxMenuReady { get; set; } = false;

        protected uint nextID = 1;
        protected const int reorderTreshold = 150;
        protected Dictionary<string, uint> addressToIndex = new();
        protected Dictionary<uint, float> simVars = new();

        // Define a delegate for LVAR change callbacks
        public delegate void LvarChangedCallback(float newValue, float oldValue, string lvarName);

        // Add dictionary to track callbacks for each LVAR
        private Dictionary<string, List<LvarChangedCallback>> lvarCallbacks = new Dictionary<string, List<LvarChangedCallback>>();

        // Dictionary to track previous values - using address as key
        private Dictionary<string, float> previousLvarValues = new Dictionary<string, float>();

        // Maintain a reverse mapping from ID to address for looking up in callbacks
        private Dictionary<uint, string> indexToAddress = new Dictionary<uint, string>();

        /// <summary>
        /// Creates a new instance of MobiSimConnect with a logger
        /// </summary>
        /// <param name="logger">The logger to use for logging messages</param>
        public MobiSimConnect(ILogger<MobiSimConnect> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Creates a new instance of MobiSimConnect without logging
        /// </summary>
        /// <remarks>This constructor is provided for backwards compatibility</remarks>
        public MobiSimConnect()
        {
            _logger = null;
        }

        public bool Connect()
        {
            try
            {
                if (isSimConnected)
                    return true;

                simConnect = new SimConnect(CLIENT_NAME, simConnectHandle, WM_PILOTSDECK_SIMCONNECT, null, 0);
                simConnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(SimConnect_OnOpen);
                simConnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(SimConnect_OnQuit);
                simConnect.OnRecvException += new SimConnect.RecvExceptionEventHandler(SimConnect_OnException);

                cancelThread = false;
                simConnectThread = new(new ThreadStart(SimConnect_ReceiveThread))
                {
                    IsBackground = true
                };
                simConnectHandle = new IntPtr(simConnectThread.ManagedThreadId);
                simConnectThread.Start();

                _logger?.LogInformation("SimConnect Connection open");
                return true;
            }
            catch (Exception ex)
            {
                simConnectThread = null;
                simConnectHandle = IntPtr.Zero;
                cancelThread = true;
                simConnect = null;

                _logger?.LogError(ex, "Exception while opening SimConnect!");
            }

            return false;
        }

        protected void SimConnect_OnOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            try
            {
                isSimConnected = true;
                simConnect.OnRecvClientData += new SimConnect.RecvClientDataEventHandler(SimConnect_OnClientData);
                simConnect.OnRecvEvent += new SimConnect.RecvEventEventHandler(SimConnect_OnReceiveEvent);
                CreateDataAreaDefaultChannel();
                CreateEventSubscription();
                _logger?.LogInformation("SimConnect OnOpen received");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception during SimConnect OnOpen!");
            }
        }

        protected void SimConnect_ReceiveThread()
        {
            ulong ticks = 0;
            int delay = 100;
            int repeat = 5000 / delay;
            int errors = 0;
            isReceiveRunning = true;
            while (!cancelThread && simConnect != null && isReceiveRunning)
            {
                try
                {
                    simConnect.ReceiveMessage();

                    if (isSimConnected && !isMobiConnected && ticks % (ulong)repeat == 0)
                    {
                        _logger?.LogDebug("Sending Ping to MobiFlight WASM Module");
                        SendMobiWasmCmd("MF.DummyCmd");
                        SendMobiWasmCmd("MF.Ping");
                    }
                }
                catch (Exception ex)
                {
                    errors++;
                    if (errors > 6)
                    {
                        isReceiveRunning = false;
                        _logger?.LogError(ex, "Maximum Errors reached, closing Receive Thread!");
                        return;
                    }
                }
                Thread.Sleep(delay);
                ticks++;
            }
            isReceiveRunning = false;
            return;
        }

        protected void CreateEventSubscription()
        {
            simConnect.MapClientEventToSimEvent(SIM_EVENTS.EXTERNAL_SYSTEM_TOGGLE, "EXTERNAL_SYSTEM_TOGGLE");
            simConnect.AddClientEventToNotificationGroup(NOTFIY_GROUP.GROUP0, SIM_EVENTS.EXTERNAL_SYSTEM_TOGGLE, false);
        }

        protected void CreateDataAreaDefaultChannel()
        {
            simConnect.MapClientDataNameToID(MOBIFLIGHT_CLIENT_DATA_NAME_COMMAND, MOBIFLIGHT_CLIENT_DATA_ID.MOBIFLIGHT_CMD);

            simConnect.MapClientDataNameToID(MOBIFLIGHT_CLIENT_DATA_NAME_RESPONSE, MOBIFLIGHT_CLIENT_DATA_ID.MOBIFLIGHT_RESPONSE);

            simConnect.AddToClientDataDefinition((SIMCONNECT_DEFINE_ID)0, 0, MOBIFLIGHT_MESSAGE_SIZE, 0, 0);
            simConnect.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, ResponseString>((SIMCONNECT_DEFINE_ID)0);
            simConnect.RequestClientData(MOBIFLIGHT_CLIENT_DATA_ID.MOBIFLIGHT_RESPONSE,
                (SIMCONNECT_REQUEST_ID)0,
                (SIMCONNECT_DEFINE_ID)0,
                SIMCONNECT_CLIENT_DATA_PERIOD.ON_SET,
                SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.CHANGED,
                0,
                0,
                0);
        }

        protected void CreateDataAreaClientChannel()
        {
            simConnect.MapClientDataNameToID(PILOTSDECK_CLIENT_DATA_NAME_COMMAND, PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_CMD);

            simConnect.MapClientDataNameToID(PILOTSDECK_CLIENT_DATA_NAME_RESPONSE, PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_RESPONSE);

            simConnect.MapClientDataNameToID(PILOTSDECK_CLIENT_DATA_NAME_SIMVAR, PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_LVARS);

            simConnect.AddToClientDataDefinition((SIMCONNECT_DEFINE_ID)0, 0, MOBIFLIGHT_MESSAGE_SIZE, 0, 0);
            simConnect.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, ResponseString>((SIMCONNECT_DEFINE_ID)0);
            simConnect.RequestClientData(PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_RESPONSE,
                (SIMCONNECT_REQUEST_ID)0,
                (SIMCONNECT_DEFINE_ID)0,
                SIMCONNECT_CLIENT_DATA_PERIOD.ON_SET,
                SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.CHANGED,
                0,
                0,
                0);
        }

        protected void SimConnect_OnClientData(SimConnect sender, SIMCONNECT_RECV_CLIENT_DATA data)
        {
            try
            {
                if (data.dwRequestID == 0)
                {
                    var request = (ResponseString)data.dwData[0];
                    if (request.Data == "MF.Pong")
                    {
                        if (!isMobiConnected)
                        {
                            _logger?.LogInformation("MobiFlight WASM Ping acknowledged - opening Client Connection");
                            SendMobiWasmCmd($"MF.Clients.Add.{CLIENT_NAME}");
                        }
                    }
                    if (request.Data == $"MF.Clients.Add.{CLIENT_NAME}.Finished")
                    {
                        CreateDataAreaClientChannel();
                        isMobiConnected = true;
                        SendClientWasmCmd("MF.SimVars.Clear");
                        SendClientWasmCmd("MF.Config.MAX_VARS_PER_FRAME.Set.15");
                        _logger?.LogInformation("MobiFlight WASM Client Connection opened");
                    }
                }
                else
                {
                    var simData = (ClientDataValue)data.dwData[0];
                    if (simVars.ContainsKey(data.dwRequestID))
                    {
                        // Get the old value before updating
                        float oldValue = simVars[data.dwRequestID];
                        float newValue = simData.data;

                        // Update the stored value
                        simVars[data.dwRequestID] = newValue;

                        // Check if this is an LVAR and if we have callbacks registered
                        if (indexToAddress.ContainsKey(data.dwRequestID))
                        {
                            string fullAddress = indexToAddress[data.dwRequestID];

                            // Check if it's an LVAR (starts with "(L:")
                            if (fullAddress.StartsWith("(L:") && fullAddress.EndsWith(")"))
                            {
                                // Extract the LVAR name without the "(L:" and ")" wrapper
                                string lvarName = fullAddress.Substring(3, fullAddress.Length - 4);

                                // Only invoke callbacks if value changed
                                if (Math.Abs(oldValue - newValue) > float.Epsilon)
                                {
                                    // Publish event through the aggregator
                                    EventAggregator.Instance.Publish(new LvarChangedEvent(lvarName, oldValue, newValue));

                                    // If callbacks exist for this LVAR, invoke them
                                    if (lvarCallbacks.ContainsKey(lvarName) && lvarCallbacks[lvarName].Count > 0)
                                    {
                                        foreach (var callback in lvarCallbacks[lvarName])
                                        {
                                            try
                                            {
                                                // Pass the LVAR name to the callback
                                                callback(newValue, oldValue, lvarName);
                                            }
                                            catch (Exception callbackEx)
                                            {
                                                // Log callback exceptions
                                                _logger?.LogError(callbackEx, "Exception in callback for LVAR '{LvarName}'!", lvarName);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                        _logger?.LogWarning("The received ID '{RequestId}' is not subscribed!", data.dwRequestID);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception during SimConnect OnClientData!");
            }
        }

        protected void SimConnect_OnQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            Disconnect();
        }

        public void Disconnect()
        {
            try
            {
                if (isMobiConnected)
                    SendClientWasmCmd("MF.SimVars.Clear");

                cancelThread = true;
                if (simConnectThread != null)
                {
                    simConnectThread.Interrupt();
                    simConnectThread.Join(500);
                    simConnectThread = null;
                }

                if (simConnect != null)
                {
                    simConnect.Dispose();
                    simConnect = null;
                    simConnectHandle = IntPtr.Zero;
                }

                isSimConnected = false;
                isMobiConnected = false;

                nextID = 1;
                simVars.Clear();
                addressToIndex.Clear();
                _logger?.LogInformation("SimConnect Connection closed");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception during disconnecting from SimConnect!");
            }
        }

        private void SimConnect_OnReceiveEvent(SimConnect sender, SIMCONNECT_RECV_EVENT recEvent)
        {
            if (recEvent != null && recEvent.uEventID == 0 && recEvent.dwID == 4 && recEvent.dwData == 1)
                IsGsxMenuReady = true;
        }

        public void Dispose()
        {
            Disconnect();
            GC.SuppressFinalize(this);
        }

        private void SendClientWasmCmd(string command)
        {
            SendWasmCmd(PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_CMD, (MOBIFLIGHT_CLIENT_DATA_ID)0, command);
        }

        private void SendClientWasmDummyCmd()
        {
            SendWasmCmd(PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_CMD, (MOBIFLIGHT_CLIENT_DATA_ID)0, "MF.DummyCmd");
        }

        private void SendMobiWasmCmd(string command)
        {
            SendWasmCmd(MOBIFLIGHT_CLIENT_DATA_ID.MOBIFLIGHT_CMD, (MOBIFLIGHT_CLIENT_DATA_ID)0, command);
        }

        private void SendWasmCmd(Enum cmdChannelId, Enum cmdId, string command)
        {
            simConnect.SetClientData(cmdChannelId, cmdId, SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT, 0, new ClientDataString(command));
        }

        protected void SimConnect_OnException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            if (data.dwException != 3 && data.dwException != 29)
                _logger?.LogError("Exception received: {ExceptionCode}", data.dwException);
        }

        // Overloaded method that accepts a callback
        public void SubscribeLvar(string address, LvarChangedCallback callback)
        {
            // First ensure the LVAR is subscribed in SimConnect
            SubscribeLvar(address);

            // Format the address as it's stored internally
            string formattedAddress = $"(L:{address})";

            // Store the callback
            if (!lvarCallbacks.ContainsKey(address))
            {
                lvarCallbacks[address] = new List<LvarChangedCallback>();
                previousLvarValues[address] = 0.0f; // Initialize previous value
            }

            lvarCallbacks[address].Add(callback);
        }

        public void SubscribeLvar(string address)
        {
            SubscribeVariable($"(L:{address})");
        }

        public void SubscribeSimVar(string name, string unit)
        {
            SubscribeVariable($"(A:{name}, {unit})");
        }

        public void SubscribeEnvVar(string name, string unit)
        {
            SubscribeVariable($"(E:{name}, {unit})");
        }

        protected void SubscribeVariable(string address)
        {
            try
            {
                if (!addressToIndex.ContainsKey(address))
                {
                    RegisterVariable(nextID, address);
                    simVars.Add(nextID, 0.0f);
                    addressToIndex.Add(address, nextID);
                    indexToAddress.Add(nextID, address); // Add to reverse mapping
                    nextID++;
                }
                else
                    _logger?.LogWarning("The Address '{Address}' is already subscribed", address);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception while subscribing SimVar '{Address}'!", address);
            }
        }

        protected void RegisterVariable(uint ID, string address)
        {
            simConnect.AddToClientDataDefinition(
                (SIMCONNECT_DEFINE_ID)ID,
                (ID - 1) * sizeof(float),
                sizeof(float),
                0,
                0);

            simConnect?.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, ClientDataValue>((SIMCONNECT_DEFINE_ID)ID);

            simConnect?.RequestClientData(
                PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_LVARS,
                (SIMCONNECT_REQUEST_ID)ID,
                (SIMCONNECT_DEFINE_ID)ID,
                SIMCONNECT_CLIENT_DATA_PERIOD.ON_SET,
                SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.CHANGED,
                0,
                0,
                0
            );

            SendClientWasmCmd($"MF.SimVars.Add.{address}");
        }

        // Method to unsubscribe a specific callback
        public void UnsubscribeLvar(string address, LvarChangedCallback callback)
        {
            if (lvarCallbacks.ContainsKey(address) && lvarCallbacks[address].Contains(callback))
            {
                lvarCallbacks[address].Remove(callback);
                _logger?.LogDebug("Unsubscribed callback for LVAR '{Address}'", address);
            }
        }

        public void UnsubscribeAll()
        {
            try
            {
                SendClientWasmCmd("MF.SimVars.Clear");
                nextID = 1;
                simVars.Clear();
                addressToIndex.Clear();
                _logger?.LogInformation("Unsubscribed all SimVars");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception while unsubscribing SimVars!");
            }
        }

        public float ReadLvar(string address)
        {
            if (addressToIndex.TryGetValue($"(L:{address})", out uint index) && simVars.TryGetValue(index, out float value))
                return value;
            else
                return 0;
        }

        public float ReadSimVar(string name, string unit)
        {
            string address = $"(A:{name}, {unit})";
            if (addressToIndex.TryGetValue(address, out uint index) && simVars.TryGetValue(index, out float value))
                return value;
            else
                return 0;
        }

        public float ReadEnvVar(string name, string unit)
        {
            string address = $"(E:{name}, {unit})";
            if (addressToIndex.TryGetValue(address, out uint index) && simVars.TryGetValue(index, out float value))
                return value;
            else
                return 0;
        }

        public void WriteLvar(string address, float value)
        {
            SendClientWasmCmd($"MF.SimVars.Set.{string.Format(new CultureInfo("en-US").NumberFormat, "{0:G}", value)} (>L:{address})");
            SendClientWasmDummyCmd();
        }

        public void ExecuteCode(string code)
        {
            SendClientWasmCmd($"MF.SimVars.Set.{code}");
            SendClientWasmDummyCmd();
        }
    }
}
