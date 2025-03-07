# Implementing Frozen Collections in Prosim2GSX

This document provides detailed implementation instructions for optimizing dictionary operations in Prosim2GSX using .NET 8.0's `FrozenDictionary<TKey, TValue>`.

## Overview

.NET 8.0 introduces `FrozenDictionary<TKey, TValue>`, an immutable dictionary that provides significant performance benefits for read-heavy scenarios. The `SimConnectService.cs` class in Prosim2GSX uses dictionaries extensively for variable lookups, making it an ideal candidate for this optimization.

## Implementation Steps

### 1. Add Required Using Directive

Add the following using directive to `SimConnectService.cs`:

```csharp
using System.Collections.Frozen;
```

### 2. Add Frozen Dictionary Fields

Add the following fields to the `SimConnectService` class:

```csharp
// Existing mutable dictionaries
protected Dictionary<string, uint> addressToIndex = new();
protected Dictionary<uint, float> simVars = new();

// New frozen dictionaries for read operations
protected FrozenDictionary<string, uint> frozenAddressToIndex;
protected FrozenDictionary<uint, float> frozenSimVars;
```

### 3. Add Method to Freeze Collections

Add the following method to the `SimConnectService` class:

```csharp
// Add a method to freeze collections after all variables are loaded
protected void FreezeCollections()
{
    frozenAddressToIndex = addressToIndex.ToFrozenDictionary();
    frozenSimVars = simVars.ToFrozenDictionary();
    Logger.Log(LogLevel.Debug, "SimConnectService:FreezeCollections", 
        $"Collections frozen: {addressToIndex.Count} addresses, {simVars.Count} variables");
}
```

### 4. Call FreezeCollections After Loading Variables

Modify the `Connect` method to call `FreezeCollections` after all variables are loaded:

```csharp
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
        // Note: This is called here for demonstration, but in practice
        // you might want to call it after all variables are actually loaded,
        // which might be after the SimConnect_OnOpen event or after specific
        // variables are subscribed.
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
```

### 5. Update Read Methods to Use Frozen Dictionaries

Modify the `ReadLvar`, `ReadSimVar`, and `ReadEnvVar` methods to use frozen dictionaries when available:

```csharp
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
```

### 6. Update SimConnect_OnClientData Method

Modify the `SimConnect_OnClientData` method to update the frozen dictionaries when variables change:

```csharp
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
```

### 7. Update UnsubscribeAll Method

Modify the `UnsubscribeAll` method to clear the frozen dictionaries as well:

```csharp
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
```

### 8. Update SubscribeVariable Method

Modify the `SubscribeVariable` method to update the frozen dictionaries when variables are added:

```csharp
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
```

## Performance Considerations

### When to Freeze Collections

The optimal time to freeze collections depends on the application's usage pattern:

1. **After Initial Loading**: If variables are loaded once at startup and rarely change, freeze collections after initial loading.
2. **After Stable State**: If variables are loaded dynamically but eventually reach a stable state, freeze collections after reaching that state.
3. **Periodically**: If variables change frequently but lookups are more common than changes, periodically refreeze collections.

### Balancing Updates and Lookups

Freezing collections has a cost, so it's important to balance the frequency of updates and lookups:

1. **High Update Frequency**: If variables change very frequently, consider not using frozen dictionaries or freezing less frequently.
2. **High Lookup Frequency**: If lookups are much more common than updates, freeze collections and accept the cost of occasional refreezing.
3. **Batch Updates**: Consider batching updates and refreezing only after a batch of updates is complete.

## Testing

After implementing these changes, test the application thoroughly to ensure that:

1. **Functionality**: All features continue to work as expected.
2. **Performance**: Measure the performance impact of the changes, particularly for read operations.
3. **Memory Usage**: Monitor memory usage to ensure it doesn't increase unexpectedly.
4. **Thread Safety**: Verify that the application remains thread-safe, particularly if multiple threads access the dictionaries.

## Conclusion

Implementing `FrozenDictionary<TKey, TValue>` in Prosim2GSX can provide significant performance benefits for read-heavy scenarios. By carefully managing when collections are frozen and balancing the cost of freezing with the benefits of faster lookups, you can optimize the application's performance while maintaining its functionality and stability.
