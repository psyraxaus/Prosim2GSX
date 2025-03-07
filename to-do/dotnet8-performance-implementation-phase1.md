# .NET 8.0 Performance Improvements - Phase 1 Implementation

This document outlines the implementation details for the first phase of .NET 8.0 performance improvements for Prosim2GSX. These high-impact, low-risk improvements can be implemented immediately to gain significant performance benefits.

## 1. Optimize Dictionary Operations with Frozen Collections

### Overview
.NET 8.0 introduces `FrozenDictionary<TKey, TValue>` which provides significant performance benefits for read-heavy scenarios. This implementation will focus on optimizing the `SimConnectService.cs` class, which uses dictionaries for variable lookups.

### Implementation Steps

1. Add the required using directive:
```csharp
using System.Collections.Frozen;
```

2. Modify the `SimConnectService.cs` class to use frozen dictionaries:

```csharp
public class SimConnectService : ISimConnectService
{
    // Existing mutable dictionaries
    protected Dictionary<string, uint> addressToIndex = new();
    protected Dictionary<uint, float> simVars = new();
    
    // New frozen dictionaries for read operations
    protected FrozenDictionary<string, uint> frozenAddressToIndex;
    protected FrozenDictionary<uint, float> frozenSimVars;
    
    // Add a method to freeze collections after all variables are loaded
    protected void FreezeCollections()
    {
        frozenAddressToIndex = addressToIndex.ToFrozenDictionary();
        frozenSimVars = simVars.ToFrozenDictionary();
        Logger.Log(LogLevel.Debug, "SimConnectService:FreezeCollections", 
            $"Collections frozen: {addressToIndex.Count} addresses, {simVars.Count} variables");
    }
    
    // Call FreezeCollections after all variables are loaded
    // For example, in the Connect method after initialization
    public bool Connect()
    {
        try
        {
            // Existing connection code...
            
            // After all variables are loaded, freeze collections
            FreezeCollections();
            
            return true;
        }
        catch (Exception ex)
        {
            // Existing error handling...
            return false;
        }
    }
    
    // Update read methods to use frozen dictionaries when available
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
    
    // Similarly update ReadSimVar and ReadEnvVar methods
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
    
    // When variables change, update the mutable dictionaries and refreeze
    protected void UpdateVariable(uint id, float value)
    {
        if (simVars.ContainsKey(id))
        {
            simVars[id] = value;
            
            // If we're using frozen dictionaries, we need to refreeze after updates
            if (frozenSimVars != null)
            {
                frozenSimVars = simVars.ToFrozenDictionary();
            }
        }
    }
    
    // When unsubscribing, clear the frozen dictionaries as well
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
            Logger.Log(LogLevel.Error, "SimConnectService:UnsubscribeAll", 
                $"Exception while unsubscribing SimVars! (Exception: {ex.GetType()}) (Message: {ex.Message})");
        }
    }
}
```

3. Update the `MobiSimConnect.cs` class to ensure it properly delegates to the `SimConnectService` implementation:

```csharp
// No changes needed to MobiSimConnect.cs as it already delegates to ISimConnectService
```

### Expected Performance Improvement
- Up to 30% faster lookups for read operations
- Reduced memory usage due to more efficient internal structure
- Thread-safe without locks, improving concurrency
- Reduced garbage collection pressure

## 2. Implement Span<T> for String Operations

### Overview
.NET 8.0 has improved performance for `Span<T>` operations, which can significantly reduce allocations in string processing. This implementation will focus on optimizing string operations in `SimConnectService.cs` and `Logger.cs`.

### Implementation Steps

1. Update the `SimConnectService.cs` class to use `Span<T>` for string operations:

```csharp
public void ExecuteCode(string code)
{
    const string prefix = "MF.SimVars.Set.";
    
    // Use stackalloc for small buffers to avoid heap allocations
    Span<char> buffer = stackalloc char[prefix.Length + code.Length];
    
    // Copy strings to buffer without allocations
    prefix.AsSpan().CopyTo(buffer);
    code.AsSpan().CopyTo(buffer.Slice(prefix.Length));
    
    // Convert back to string only once for the final command
    SendClientWasmCmd(buffer.ToString());
    SendClientWasmDummyCmd();
}

private void SendWasmCmd(Enum cmdChannelId, Enum cmdId, string command)
{
    // Use ClientDataString with Span<T> for better performance
    simConnect.SetClientData(cmdChannelId, cmdId, SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT, 0, 
        new ClientDataString(command.AsSpan()));
}
```

2. Update the `ClientDataString` struct in `MobiDefinitions.cs` to support `Span<T>`:

```csharp
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ClientDataString
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)MobiSimConnect.MOBIFLIGHT_MESSAGE_SIZE)]
    public byte[] data;

    public ClientDataString(string strData)
    {
        byte[] txtBytes = Encoding.ASCII.GetBytes(strData);
        var ret = new byte[1024];
        Array.Copy(txtBytes, ret, txtBytes.Length);
        data = ret;
    }
    
    // Add constructor that accepts ReadOnlySpan<char> for better performance
    public ClientDataString(ReadOnlySpan<char> strData)
    {
        data = new byte[1024];
        
        // Convert directly from Span<char> to avoid string allocation
        Encoding.ASCII.GetBytes(strData, data);
    }
}
```

3. Update the `Logger.cs` class to use `Span<T>` for string formatting:

```csharp
public static void Log(LogLevel level, string context, string message)
{
    // Determine context length
    ReadOnlySpan<char> contextSpan = context.AsSpan();
    ReadOnlySpan<char> formattedContext = contextSpan.Length <= 32 ? 
        contextSpan : contextSpan.Slice(0, 32);
    
    // Calculate buffer size
    int bufferSize = formattedContext.Length + message.Length + 10; // 10 for "[ ", " ] " and some extra space
    
    // Use stackalloc for small buffers to avoid heap allocations
    Span<char> buffer = bufferSize <= 256 ? 
        stackalloc char[bufferSize] : // Use stack for small buffers
        new char[bufferSize];         // Use heap for large buffers
    
    // Format the log entry without string allocations
    "[ ".AsSpan().CopyTo(buffer);
    formattedContext.CopyTo(buffer.Slice(2));
    " ] ".AsSpan().CopyTo(buffer.Slice(formattedContext.Length + 2));
    
    // Clean the message
    ReadOnlySpan<char> cleanMessage = message
        .Replace("\n", "")
        .Replace("\r", "")
        .Replace("\t", "")
        .AsSpan();
    
    cleanMessage.CopyTo(buffer.Slice(formattedContext.Length + 6));
    
    // Convert to string only once for the final log entry
    string entry = buffer.Slice(0, formattedContext.Length + 6 + cleanMessage.Length).ToString();
    
    // Log using Serilog as before
    switch (level)
    {
        case LogLevel.Critical:
            Serilog.Log.Logger.Fatal(entry);
            break;
        case LogLevel.Error:
            Serilog.Log.Logger.Error(entry);
            break;
        case LogLevel.Warning:
            Serilog.Log.Logger.Warning(entry);
            break;
        case LogLevel.Information:
            Serilog.Log.Logger.Information(entry);
            break;
        case LogLevel.Debug:
            Serilog.Log.Logger.Debug(entry);
            break;
        case LogLevel.Verbose:
            Serilog.Log.Logger.Verbose(entry);
            break;
        default:
            Serilog.Log.Logger.Debug(entry);
            break;
    }
    
    if (level != LogLevel.Debug)
        MessageQueue.Enqueue(message);
}
```

### Expected Performance Improvement
- Reduced memory allocations for string operations
- Improved garbage collection behavior
- Better performance for string formatting and manipulation
- Reduced memory fragmentation

## 3. Implement Asynchronous Improvements with ValueTask

### Overview
.NET 8.0 includes performance improvements for asynchronous programming, particularly with `ValueTask`. This implementation will focus on optimizing asynchronous methods in `GSXAudioService.cs` and `CoreAudioSessionManager.cs`.

### Implementation Steps

1. Update the `GSXAudioService.cs` class to use `ValueTask` instead of `Task`:

```csharp
// Update the interface first
public interface IGSXAudioService
{
    // Change return type from Task to ValueTask
    ValueTask GetAudioSessionsAsync(CancellationToken cancellationToken = default);
    ValueTask ResetAudioAsync(CancellationToken cancellationToken = default);
    ValueTask ControlAudioAsync(CancellationToken cancellationToken = default);
    
    // Other methods remain unchanged
    void GetAudioSessions();
    void ResetAudio();
    void ControlAudio();
    
    // Properties remain unchanged
    int AudioSessionRetryCount { get; set; }
    TimeSpan AudioSessionRetryDelay { get; set; }
    
    // Events remain unchanged
    event EventHandler<AudioSessionEventArgs> AudioSessionFound;
    event EventHandler<AudioVolumeChangedEventArgs> VolumeChanged;
    event EventHandler<AudioMuteChangedEventArgs> MuteChanged;
}

// Then update the implementation
public class GSXAudioService : IGSXAudioService
{
    // Existing fields and properties...
    
    // Update method to use ValueTask
    public async ValueTask GetAudioSessionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Use ValueTask.Run instead of Task.Run
            await ValueTask.Run(() => 
            {
                cancellationToken.ThrowIfCancellationRequested();
                GetAudioSessions();
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Logger.Log(LogLevel.Information, "GSXAudioService:GetAudioSessionsAsync", 
                "Operation was canceled");
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, "GSXAudioService:GetAudioSessionsAsync", 
                $"Exception getting audio sessions: {ex.Message}");
        }
    }
    
    // Update method to use ValueTask
    public async ValueTask ResetAudioAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Use ValueTask.Run instead of Task.Run
            await ValueTask.Run(() => 
            {
                cancellationToken.ThrowIfCancellationRequested();
                ResetAudio();
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Logger.Log(LogLevel.Information, "GSXAudioService:ResetAudioAsync", 
                "Operation was canceled");
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, "GSXAudioService:ResetAudioAsync", 
                $"Exception resetting audio: {ex.Message}");
        }
    }
    
    // Update method to use ValueTask
    public async ValueTask ControlAudioAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Use ValueTask.Run instead of Task.Run
            await ValueTask.Run(() => 
            {
                cancellationToken.ThrowIfCancellationRequested();
                ControlAudio();
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Logger.Log(LogLevel.Information, "GSXAudioService:ControlAudioAsync", 
                "Operation was canceled");
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, "GSXAudioService:ControlAudioAsync", 
                $"Exception controlling audio: {ex.Message}");
        }
    }
    
    // Other methods remain unchanged...
}
```

2. Update the `CoreAudioSessionManager.cs` class to use `ValueTask`:

```csharp
// Update the interface first
public interface IAudioSessionManager
{
    // Change return type from Task to ValueTask
    ValueTask<AudioSessionControl2> GetSessionForProcessAsync(string processName, CancellationToken cancellationToken = default);
    
    // Other methods remain unchanged
    AudioSessionControl2 GetSessionForProcess(string processName);
    AudioSessionControl2 GetSessionForProcessWithRetry(string processName, int retryCount, TimeSpan retryDelay);
    void SetVolume(AudioSessionControl2 session, float volume);
    void SetMute(AudioSessionControl2 session, bool mute);
    void ResetSession(AudioSessionControl2 session);
    bool IsProcessRunning(string processName);
}

// Then update the implementation
public class CoreAudioSessionManager : IAudioSessionManager
{
    // Existing fields and methods...
    
    // Update method to use ValueTask
    public async ValueTask<AudioSessionControl2> GetSessionForProcessAsync(
        string processName, 
        CancellationToken cancellationToken = default)
    {
        // Use ValueTask.Run instead of Task.Run for better performance with short-running operations
        return await ValueTask.Run(() => GetSessionForProcess(processName), cancellationToken);
    }
    
    // Other methods remain unchanged...
}
```

### Expected Performance Improvement
- Reduced allocation overhead for async operations
- Better performance for short-running async methods
- Improved cancellation handling
- More efficient use of thread pool resources

## Testing Strategy

For each performance improvement:

1. **Baseline Measurement**
   - Use .NET 8.0 profiling tools to establish baseline performance metrics
   - Measure memory allocations, CPU usage, and response time for key operations
   - Document the baseline metrics for comparison

2. **Implementation Testing**
   - Implement each improvement in isolation
   - Run unit tests to verify functionality is preserved
   - Measure performance metrics after implementation
   - Compare with baseline to quantify improvement

3. **Integration Testing**
   - Test the improvements together to ensure they work correctly in combination
   - Verify that all application features continue to function as expected
   - Measure overall application performance

4. **Documentation**
   - Document the performance improvements achieved
   - Note any trade-offs or considerations
   - Update implementation documentation with the changes made

## Implementation Order

1. **Frozen Collections**
   - Implement in SimConnectService.cs first
   - Test thoroughly before proceeding
   - This provides immediate benefits for read-heavy operations

2. **Span<T> for String Operations**
   - Implement in Logger.cs first
   - Then implement in SimConnectService.cs
   - This reduces memory allocations in frequently called code paths

3. **ValueTask Improvements**
   - Update interfaces first
   - Then update implementations
   - This improves asynchronous operation performance

## Conclusion

These Phase 1 improvements focus on high-impact, low-risk changes that leverage .NET 8.0 features to enhance performance. By implementing these optimizations, the Prosim2GSX application will benefit from reduced memory allocations, improved response time, and better overall performance.

The implementation strategy prioritizes changes that provide immediate benefits with minimal risk, ensuring that the application remains stable while gaining performance improvements.
