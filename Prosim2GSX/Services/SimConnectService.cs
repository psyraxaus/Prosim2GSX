using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Collections.Generic;
using System.Collections.Frozen;
using System.Globalization;
using System.Threading;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Service for interacting with Microsoft Flight Simulator via SimConnect
    /// </summary>
    public class SimConnectService : ISimConnectService
    {
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
        
        // Frozen dictionaries for read operations
        protected FrozenDictionary<string, uint> frozenAddressToIndex;
        protected FrozenDictionary<uint, float> frozenSimVars;

        public SimConnectService()
        {
            
        }

        // Add a method to freeze collections after all variables are loaded
        protected void FreezeCollections()
        {
            frozenAddressToIndex = addressToIndex.ToFrozenDictionary();
            frozenSimVars = simVars.ToFrozenDictionary();
            Logger.Log(LogLevel.Debug, "SimConnectService:FreezeCollections", 
                $"Collections frozen: {addressToIndex.Count} addresses, {simVars.Count} variables");
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

                Logger.Log(LogLevel.Information, "SimConnectService:Connect", $"SimConnect Connection open");
                
                // After all variables are loaded, freeze collections
                // This will be called again when variables are added or updated
                FreezeCollections();
                
                return true;
            }
            catch (Exception ex)
            {
                simConnectThread = null;
                simConnectHandle = IntPtr.Zero;
                cancelThread = true;
                simConnect = null;

                Logger.Log(LogLevel.Error, "SimConnectService:Connect", $"Exception while opening SimConnect! (Exception: {ex.GetType()} {ex.Message})");
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
                Logger.Log(LogLevel.Information, "SimConnectService:SimConnect_OnOpen", $"SimConnect OnOpen received");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "SimConnectService:SimConnect_OnOpen", $"Exception during SimConnect OnOpen! (Exception: {ex.GetType()} {ex.Message})");
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
                        Logger.Log(LogLevel.Debug, "SimConnectService:SimConnect_ReceiveThread", $"Sending Ping to MobiFlight WASM Module");
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
                        Logger.Log(LogLevel.Error, "SimConnectService:SimConnect_ReceiveThread", $"Maximum Errors reached, closing Receive Thread! (Exception: {ex.GetType()})");
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
                            Logger.Log(LogLevel.Information, "SimConnectService:SimConnect_OnClientData", $"MobiFlight WASM Ping acknowledged - opening Client Connection");
                            SendMobiWasmCmd($"MF.Clients.Add.{CLIENT_NAME}");
                        }
                    }
                    if (request.Data == $"MF.Clients.Add.{CLIENT_NAME}.Finished")
                    {
                        CreateDataAreaClientChannel();
                        isMobiConnected = true;
                        SendClientWasmCmd("MF.SimVars.Clear");
                        SendClientWasmCmd("MF.Config.MAX_VARS_PER_FRAME.Set.15");
                        Logger.Log(LogLevel.Information, "SimConnectService:SimConnect_OnClientData", $"MobiFlight WASM Client Connection opened");
                    }
                }
                else
                {
                    var simData = (ClientDataValue)data.dwData[0];
                    if (simVars.ContainsKey(data.dwRequestID))
                    {
                        simVars[data.dwRequestID] = simData.data;
                        
                        // If we're using frozen dictionaries, we need to refreeze after updates
                        // Note: This is a simple approach that recreates the entire frozen dictionary
                        // For better performance, you might want to batch updates and refreeze less frequently
                        if (frozenSimVars != null)
                        {
                            frozenSimVars = simVars.ToFrozenDictionary();
                        }
                    }
                    else
                        Logger.Log(LogLevel.Warning, "SimConnectService:SimConnect_OnClientData", $"The received ID '{data.dwRequestID}' is not subscribed! (Data: {data})");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "SimConnectService:SimConnect_OnClientData", $"Exception during SimConnect OnClientData! (Exception: {ex.GetType()}) (Data: {data})");
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
                
                // Clear frozen dictionaries
                frozenAddressToIndex = null;
                frozenSimVars = null;
                
                Logger.Log(LogLevel.Information, "SimConnectService:Disconnect", $"SimConnect Connection closed");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "SimConnectService:Disconnect", $"Exception during disconnecting from SimConnect! (Exception: {ex.GetType()} {ex.Message})");
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
        
        // Add overload that accepts ReadOnlySpan<char>
        private void SendClientWasmCmd(ReadOnlySpan<char> command)
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
        
        // Add overload that accepts ReadOnlySpan<char>
        private void SendMobiWasmCmd(ReadOnlySpan<char> command)
        {
            SendWasmCmd(MOBIFLIGHT_CLIENT_DATA_ID.MOBIFLIGHT_CMD, (MOBIFLIGHT_CLIENT_DATA_ID)0, command);
        }

        private void SendWasmCmd(Enum cmdChannelId, Enum cmdId, string command)
        {
            simConnect.SetClientData(cmdChannelId, cmdId, SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT, 0, new ClientDataString(command));
        }
        
        // Add overload that accepts ReadOnlySpan<char>
        private void SendWasmCmd(Enum cmdChannelId, Enum cmdId, ReadOnlySpan<char> command)
        {
            simConnect.SetClientData(cmdChannelId, cmdId, SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT, 0, new ClientDataString(command));
        }

        protected void SimConnect_OnException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            if (data.dwException != 3 && data.dwException != 29)
                Logger.Log(LogLevel.Error, "SimConnectService:SimConnect_OnException", $"Exception received: (Exception: {data.dwException})");
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

                    nextID++;
                    
                    // If we're using frozen dictionaries, we need to refreeze after adding variables
                    if (frozenAddressToIndex != null)
                    {
                        frozenAddressToIndex = addressToIndex.ToFrozenDictionary();
                        frozenSimVars = simVars.ToFrozenDictionary();
                    }
                }
                else
                    Logger.Log(LogLevel.Warning, "SimConnectService:SubscribeAddress", $"The Address '{address}' is already subscribed");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "SimConnectService:SubscribeAddress", $"Exception while subscribing SimVar '{address}'! (Exception: {ex.GetType()}) (Message: {ex.Message})");
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

        public void UnsubscribeAll()
        {
            try
            {
                SendClientWasmCmd("MF.SimVars.Clear");
                nextID = 1;
                simVars.Clear();
                addressToIndex.Clear();
                
                // Clear frozen dictionaries
                frozenAddressToIndex = null;
                frozenSimVars = null;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "SimConnectService:UnsubscribeAll", $"Exception while unsubscribing SimVars! (Exception: {ex.GetType()}) (Message: {ex.Message})");
            }
        }

        public float ReadLvar(string address)
        {
            string lookupAddress = $"(L:{address})";
            
            // Use frozen dictionaries if available
            if (frozenAddressToIndex != null)
            {
                if (frozenAddressToIndex.TryGetValue(lookupAddress, out uint index) && 
                    frozenSimVars.TryGetValue(index, out float value))
                    return value;
            }
            else
            {
                if (addressToIndex.TryGetValue(lookupAddress, out uint index) && 
                    simVars.TryGetValue(index, out float value))
                    return value;
            }
            
            return 0;
        }

        public float ReadSimVar(string name, string unit)
        {
            string lookupAddress = $"(A:{name}, {unit})";
            
            if (frozenAddressToIndex != null)
            {
                if (frozenAddressToIndex.TryGetValue(lookupAddress, out uint index) && 
                    frozenSimVars.TryGetValue(index, out float value))
                    return value;
            }
            else
            {
                if (addressToIndex.TryGetValue(lookupAddress, out uint index) && 
                    simVars.TryGetValue(index, out float value))
                    return value;
            }
            
            return 0;
        }

        public float ReadEnvVar(string name, string unit)
        {
            string lookupAddress = $"(E:{name}, {unit})";
            
            if (frozenAddressToIndex != null)
            {
                if (frozenAddressToIndex.TryGetValue(lookupAddress, out uint index) && 
                    frozenSimVars.TryGetValue(index, out float value))
                    return value;
            }
            else
            {
                if (addressToIndex.TryGetValue(lookupAddress, out uint index) && 
                    simVars.TryGetValue(index, out float value))
                    return value;
            }
            
            return 0;
        }

        public void WriteLvar(string address, float value)
        {
            try
            {
                // Format value using invariant culture
                Span<char> valueBuffer = stackalloc char[32]; // Enough for any float value
                if (!value.TryFormat(valueBuffer, out int valueCharsWritten, provider: CultureInfo.InvariantCulture))
                {
                    // Fallback if formatting fails
                    SendClientWasmCmd($"MF.SimVars.Set.{string.Format(CultureInfo.InvariantCulture, "{0:G}", value)} (>L:{address})");
                    SendClientWasmDummyCmd();
                    return;
                }
                
                // Calculate total buffer size with extra padding
                const string prefix = "MF.SimVars.Set.";
                const string middle = " (>L:";
                const string suffix = ")";
                int totalLength = prefix.Length + valueCharsWritten + middle.Length + address.Length + suffix.Length + 10; // Add padding
                
                // Allocate buffer
                Span<char> buffer = stackalloc char[totalLength];
                int position = 0;
                
                // Copy prefix
                prefix.AsSpan().CopyTo(buffer.Slice(position));
                position += prefix.Length;
                
                // Copy formatted value
                valueBuffer.Slice(0, valueCharsWritten).CopyTo(buffer.Slice(position));
                position += valueCharsWritten;
                
                // Copy middle part
                middle.AsSpan().CopyTo(buffer.Slice(position));
                position += middle.Length;
                
                // Copy address
                address.AsSpan().CopyTo(buffer.Slice(position));
                position += address.Length;
                
                // Copy suffix
                suffix.AsSpan().CopyTo(buffer.Slice(position));
                
                // Send command
                SendClientWasmCmd(buffer);
                SendClientWasmDummyCmd();
            }
            catch (Exception ex)
            {
                // Fallback to string concatenation if Span operations fail
                Logger.Log(LogLevel.Warning, "SimConnectService:WriteLvar", 
                    $"Span operation failed, using string fallback: {ex.Message}");
                SendClientWasmCmd($"MF.SimVars.Set.{string.Format(CultureInfo.InvariantCulture, "{0:G}", value)} (>L:{address})");
                SendClientWasmDummyCmd();
            }
        }

        public void ExecuteCode(string code)
        {
            try
            {
                const string prefix = "MF.SimVars.Set.";
                
                // Use stackalloc for small buffers to avoid heap allocations
                // Add extra padding to ensure buffer is large enough
                Span<char> buffer = stackalloc char[prefix.Length + code.Length + 10];
                
                // Copy strings to buffer without allocations
                prefix.AsSpan().CopyTo(buffer);
                code.AsSpan().CopyTo(buffer.Slice(prefix.Length));
                
                // Send command
                SendClientWasmCmd(buffer.Slice(0, prefix.Length + code.Length));
                SendClientWasmDummyCmd();
            }
            catch (Exception ex)
            {
                // Fallback to string concatenation if Span operations fail
                Logger.Log(LogLevel.Warning, "SimConnectService:ExecuteCode", 
                    $"Span operation failed, using string fallback: {ex.Message}");
                SendClientWasmCmd($"MF.SimVars.Set.{code}");
                SendClientWasmDummyCmd();
            }
        }
    }
}
