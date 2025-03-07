# .NET 8.0 Performance Improvements for Prosim2GSX

This document outlines specific performance improvements that can be implemented to leverage .NET 8.0 features in the Prosim2GSX application. These improvements are designed to enhance performance, reduce memory usage, and improve overall application responsiveness.

## High-Impact Performance Improvements

### 1. Optimize Dictionary Operations with Frozen Collections

.NET 8.0 introduces `FrozenDictionary<TKey, TValue>` which provides significant performance benefits for read-heavy scenarios.

#### Implementation Areas:
- `SimConnectService.cs`: Replace `addressToIndex` and `simVars` dictionaries with `FrozenDictionary` when they're not being modified
- `ConfigurationFile.cs`: Use `FrozenDictionary` for `appSettings` after initial loading

#### Implementation Details:

```csharp
// Add using directive
using System.Collections.Frozen;

// In SimConnectService.cs
private Dictionary<string, uint> addressToIndex = new();
private Dictionary<uint, float> simVars = new();
private FrozenDictionary<string, uint> frozenAddressToIndex;
private FrozenDictionary<uint, float> frozenSimVars;

// After loading all variables, create frozen versions for faster lookups
protected void FreezeCollections()
{
    frozenAddressToIndex = addressToIndex.ToFrozenDictionary();
    frozenSimVars = simVars.ToFrozenDictionary();
}

// In ReadLvar method
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

// Similar modifications for ReadSimVar and ReadEnvVar
```

#### Benefits:
- Faster lookups for read operations (up to 30% in some scenarios)
- Reduced memory usage due to more efficient internal structure
- Thread-safe without locks, improving concurrency
- Reduced garbage collection pressure

### 2. Implement Span<T> for String Operations

.NET 8.0 has improved performance for `Span<T>` operations, which can significantly reduce allocations in string processing.

#### Implementation Areas:
- `ConfigurationFile.cs`: Use `Span<T>` for XML processing
- `SimConnectService.cs`: Use `Span<T>` for string formatting in commands
- `Logger.cs`: Use `Span<T>` for string formatting in log messages

#### Implementation Details:

```csharp
// In SimConnectService.cs - ExecuteCode method
public void ExecuteCode(string code)
{
    const string prefix = "MF.SimVars.Set.";
    Span<char> buffer = stackalloc char[prefix.Length + code.Length];
    
    prefix.AsSpan().CopyTo(buffer);
    code.AsSpan().CopyTo(buffer.Slice(prefix.Length));
    
    SendClientWasmCmd(buffer.ToString());
    SendClientWasmDummyCmd();
}

// In Logger.cs - Log method
public static void Log(LogLevel level, string context, string message)
{
    // Use string interpolation with Span<T> to avoid allocations
    ReadOnlySpan<char> formattedContext = context.Length <= 32 ? 
        context.AsSpan() : context.AsSpan(0, 32);
    
    Span<char> buffer = stackalloc char[formattedContext.Length + message.Length + 10];
    "[ ".AsSpan().CopyTo(buffer);
    formattedContext.CopyTo(buffer.Slice(2));
    " ] ".AsSpan().CopyTo(buffer.Slice(formattedContext.Length + 2));
    
    ReadOnlySpan<char> cleanMessage = message
        .Replace("\n", "")
        .Replace("\r", "")
        .Replace("\t", "")
        .AsSpan();
    
    cleanMessage.CopyTo(buffer.Slice(formattedContext.Length + 6));
    
    string entry = buffer.ToString();
    
    // Log using Serilog as before
    switch (level)
    {
        case LogLevel.Critical:
            Serilog.Log.Logger.Fatal(entry);
            break;
        // Other cases...
    }
    
    if (level != LogLevel.Debug)
        MessageQueue.Enqueue(message);
}
```

#### Benefits:
- Reduced memory allocations for string operations
- Improved garbage collection behavior
- Better performance for string formatting and manipulation
- Reduced memory fragmentation

### 3. Implement Asynchronous Improvements with ValueTask

.NET 8.0 includes performance improvements for asynchronous programming, particularly with `ValueTask`.

#### Implementation Areas:
- `GSXAudioService.cs`: Replace `Task` with `ValueTask` for better performance
- `SimConnectService.cs`: Implement asynchronous versions of key methods
- `CoreAudioSessionManager.cs`: Optimize async methods with ValueTask

#### Implementation Details:

```csharp
// In GSXAudioService.cs
public async ValueTask GetAudioSessionsAsync(CancellationToken cancellationToken = default)
{
    try
    {
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

// In CoreAudioSessionManager.cs
public async ValueTask<AudioSessionControl2> GetSessionForProcessAsync(
    string processName, 
    CancellationToken cancellationToken = default)
{
    return await ValueTask.Run(() => GetSessionForProcess(processName), cancellationToken);
}
```

#### Benefits:
- Reduced allocation overhead for async operations
- Better performance for short-running async methods
- Improved cancellation handling
- More efficient use of thread pool resources

### 4. Implement System.Threading.Channels for Audio Processing

.NET 8.0 has enhanced `System.Threading.Channels` for producer-consumer scenarios, which is ideal for audio processing.

#### Implementation Areas:
- `GSXAudioService.cs`: Replace direct method calls with a channel-based approach
- `CoreAudioSessionManager.cs`: Implement a channel for audio commands

#### Implementation Details:

```csharp
// Add using directive
using System.Threading.Channels;

// Define audio command types
public enum AudioCommandType
{
    GetSessions,
    SetVolume,
    SetMute,
    Reset
}

public class AudioCommand
{
    public AudioCommandType Type { get; set; }
    public string ProcessName { get; set; }
    public float Volume { get; set; }
    public bool Mute { get; set; }
}

// In GSXAudioService.cs
private Channel<AudioCommand> _audioCommandChannel;

public GSXAudioService(ServiceModel model, MobiSimConnect simConnect, IAudioSessionManager audioSessionManager)
{
    this.model = model ?? throw new ArgumentNullException(nameof(model));
    this.simConnect = simConnect ?? throw new ArgumentNullException(nameof(simConnect));
    this.audioSessionManager = audioSessionManager ?? throw new ArgumentNullException(nameof(audioSessionManager));
    
    if (!string.IsNullOrEmpty(model.Vhf1VolumeApp))
        lastVhf1App = model.Vhf1VolumeApp;
    
    // Initialize channel
    _audioCommandChannel = Channel.CreateUnbounded<AudioCommand>(
        new UnboundedChannelOptions { SingleReader = true });
    
    // Start background processor
    _ = ProcessAudioCommandsAsync();
}

private async Task ProcessAudioCommandsAsync()
{
    await foreach (var command in _audioCommandChannel.Reader.ReadAllAsync())
    {
        try
        {
            // Process command without locking
            switch (command.Type)
            {
                case AudioCommandType.GetSessions:
                    ProcessGetSessions();
                    break;
                case AudioCommandType.SetVolume:
                    ProcessSetVolume(command.ProcessName, command.Volume);
                    break;
                case AudioCommandType.SetMute:
                    ProcessSetMute(command.ProcessName, command.Mute);
                    break;
                case AudioCommandType.Reset:
                    ProcessReset();
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, "GSXAudioService:ProcessAudioCommandsAsync", 
                $"Exception processing audio command: {ex.Message}");
        }
    }
}

// Replace direct method calls with channel writes
public void GetAudioSessions()
{
    _audioCommandChannel.Writer.TryWrite(new AudioCommand 
    { 
        Type = AudioCommandType.GetSessions
    });
}

private void ProcessGetSessions()
{
    try
    {
        GetGsxAudioSession();
        GetVhf1AudioSession();
    }
    catch (Exception ex)
    {
        Logger.Log(LogLevel.Error, "GSXAudioService:ProcessGetSessions", 
            $"Exception getting audio sessions: {ex.Message}");
    }
}

// Similar implementations for other methods
```

#### Benefits:
- Reduced lock contention
- Better thread safety
- Improved responsiveness
- More efficient processing of audio commands
- Simplified error handling

## Medium-Impact Performance Improvements

### 5. Implement Memory Pooling for Frequently Allocated Objects

.NET 8.0 has improved object pooling capabilities, which can reduce garbage collection pressure.

#### Implementation Areas:
- `SimConnectService.cs`: Pool `ClientDataString` objects
- `Logger.cs`: Pool string builders for log message formatting

#### Implementation Details:

```csharp
// Add using directives
using Microsoft.Extensions.ObjectPool;

// In SimConnectService.cs
private readonly ObjectPool<ClientDataString> _clientDataStringPool;

public SimConnectService()
{
    _clientDataStringPool = new DefaultObjectPool<ClientDataString>(
        new ClientDataStringPoolPolicy(), 50);
}

private void SendWasmCmd(Enum cmdChannelId, Enum cmdId, string command)
{
    var clientDataString = _clientDataStringPool.Get();
    try
    {
        clientDataString.SetData(command);
        simConnect.SetClientData(cmdChannelId, cmdId, SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT, 0, clientDataString);
    }
    finally
    {
        _clientDataStringPool.Return(clientDataString);
    }
}

// ClientDataStringPoolPolicy implementation
private class ClientDataStringPoolPolicy : IPooledObjectPolicy<ClientDataString>
{
    public ClientDataString Create() => new ClientDataString();
    
    public bool Return(ClientDataString obj)
    {
        // Clear the data array
        Array.Clear(obj.data, 0, obj.data.Length);
        return true;
    }
}

// Modify ClientDataString to support pooling
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ClientDataString
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)MobiSimConnect.MOBIFLIGHT_MESSAGE_SIZE)]
    public byte[] data;

    public ClientDataString()
    {
        data = new byte[MobiSimConnect.MOBIFLIGHT_MESSAGE_SIZE];
    }
    
    public ClientDataString(string strData) : this()
    {
        SetData(strData);
    }
    
    public void SetData(string strData)
    {
        byte[] txtBytes = Encoding.ASCII.GetBytes(strData);
        Array.Clear(data, 0, data.Length);
        Array.Copy(txtBytes, data, Math.Min(txtBytes.Length, data.Length));
    }
}
```

#### Benefits:
- Reduced garbage collection pressure
- Improved memory usage
- Better performance for frequently allocated objects
- More predictable memory patterns

### 6. Implement Improved Caching with IMemoryCache

.NET 8.0 includes performance improvements for memory caching.

#### Implementation Areas:
- `SimConnectService.cs`: Cache frequently accessed variables
- `GSXAudioService.cs`: Cache audio sessions

#### Implementation Details:

```csharp
// Add using directive
using Microsoft.Extensions.Caching.Memory;

// In SimConnectService.cs
private readonly IMemoryCache _cache;

public SimConnectService(IMemoryCache cache = null)
{
    _cache = cache ?? new MemoryCache(new MemoryCacheOptions());
}

public float ReadLvar(string address)
{
    string cacheKey = $"Lvar:{address}";
    
    if (_cache.TryGetValue(cacheKey, out float cachedValue))
        return cachedValue;
        
    if (addressToIndex.TryGetValue($"(L:{address})", out uint index) && 
        simVars.TryGetValue(index, out float value))
    {
        // Cache with sliding expiration
        _cache.Set(cacheKey, value, new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromSeconds(1)));
        return value;
    }
    else
        return 0;
}
```

#### Benefits:
- Reduced lookups for frequently accessed data
- Improved response time for repeated operations
- Better memory usage patterns
- Reduced computational overhead

### 7. Optimize XML Processing with System.Text.Json

.NET 8.0 has improved JSON serialization performance, which can be used as an alternative to XML.

#### Implementation Areas:
- `ConfigurationFile.cs`: Consider replacing XML with JSON for configuration

#### Implementation Details:

```csharp
// Add using directive
using System.Text.Json;

// In ConfigurationFile.cs
private Dictionary<string, string> appSettings = new();
private readonly string configFilePath;

public ConfigurationFile(string configFilePath)
{
    this.configFilePath = configFilePath;
}

public void LoadConfiguration()
{
    if (File.Exists(configFilePath))
    {
        try
        {
            string jsonContent = File.ReadAllText(configFilePath);
            appSettings = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent) 
                ?? new Dictionary<string, string>();
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, "ConfigurationFile:LoadConfiguration", 
                $"Error loading configuration: {ex.Message}");
            appSettings = new Dictionary<string, string>();
        }
    }
    else
    {
        appSettings = new Dictionary<string, string>();
        SaveConfiguration();
    }
}

public void SaveConfiguration()
{
    try
    {
        string jsonContent = JsonSerializer.Serialize(appSettings, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        File.WriteAllText(configFilePath, jsonContent);
    }
    catch (Exception ex)
    {
        Logger.Log(LogLevel.Error, "ConfigurationFile:SaveConfiguration", 
            $"Error saving configuration: {ex.Message}");
    }
}
```

#### Benefits:
- Faster serialization and deserialization
- Reduced memory usage
- Simpler code
- Better performance characteristics

## Low-Impact Performance Improvements

### 8. Implement Hardware Intrinsics for Weight Conversion

.NET 8.0 has expanded hardware intrinsics support, which can accelerate numerical operations.

#### Implementation Areas:
- `WeightConversionUtility.cs`: Implement SIMD-accelerated weight conversion

#### Implementation Details:

```csharp
// Add using directives
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

// In WeightConversionUtility.cs
public static class WeightConversionUtility
{
    public const float KgToLbsConversionFactor = 2.205f;
    
    public static double KgToLbs(double kg)
    {
        return kg * KgToLbsConversionFactor;
    }
    
    public static double LbsToKg(double lbs)
    {
        return lbs / KgToLbsConversionFactor;
    }
    
    // SIMD-accelerated batch conversion
    public static void KgToLbsBatch(Span<float> kg, Span<float> lbs)
    {
        if (Avx2.IsSupported && kg.Length >= Vector256<float>.Count)
        {
            var factor = Vector256.Create(KgToLbsConversionFactor);
            int i = 0;
            
            // Process in chunks of Vector256 size
            for (; i <= kg.Length - Vector256<float>.Count; i += Vector256<float>.Count)
            {
                var kgVector = Vector256.Load(kg.Slice(i));
                var lbsVector = Avx.Multiply(kgVector, factor);
                lbsVector.Store(lbs.Slice(i));
            }
            
            // Process remaining elements
            for (; i < kg.Length; i++)
            {
                lbs[i] = kg[i] * KgToLbsConversionFactor;
            }
        }
        else if (Sse2.IsSupported && kg.Length >= Vector128<float>.Count)
        {
            var factor = Vector128.Create(KgToLbsConversionFactor);
            int i = 0;
            
            // Process in chunks of Vector128 size
            for (; i <= kg.Length - Vector128<float>.Count; i += Vector128<float>.Count)
            {
                var kgVector = Vector128.Load(kg.Slice(i));
                var lbsVector = Sse.Multiply(kgVector, factor);
                lbsVector.Store(lbs.Slice(i));
            }
            
            // Process remaining elements
            for (; i < kg.Length; i++)
            {
                lbs[i] = kg[i] * KgToLbsConversionFactor;
            }
        }
        else
        {
            // Fallback for non-SIMD systems
            for (int i = 0; i < kg.Length; i++)
            {
                lbs[i] = kg[i] * KgToLbsConversionFactor;
            }
        }
    }
    
    // Similar implementation for LbsToKgBatch
}
```

#### Benefits:
- Significantly faster batch weight conversions
- Reduced CPU usage for numerical operations
- Better performance on modern CPUs
- Automatic fallback for unsupported hardware

### 9. Implement Trimming for Release Builds

.NET 8.0 offers enhanced trimming capabilities to reduce application size.

#### Implementation Areas:
- `Prosim2GSX.csproj`: Add trimming configuration

#### Implementation Details:

```xml
<PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Release|AnyCPU'">
  <DebugType>embedded</DebugType>
  <PublishTrimmed>true</PublishTrimmed>
  <TrimMode>partial</TrimMode>
  <SuppressTrimAnalysisWarnings>false</SuppressTrimAnalysisWarnings>
</PropertyGroup>
```

#### Benefits:
- Reduced application size
- Potentially faster startup time
- Lower memory footprint
- More efficient deployment

## Implementation Strategy

The recommended implementation strategy is to proceed in phases, starting with the highest impact improvements:

### Phase 1: High-Impact, Low-Risk Improvements
1. Implement Span<T> for string operations
2. Optimize Dictionary operations with Frozen Collections
3. Implement asynchronous improvements with ValueTask

### Phase 2: Medium-Impact Improvements
1. Implement System.Threading.Channels for audio processing
2. Implement memory pooling for frequently allocated objects
3. Implement improved caching with IMemoryCache

### Phase 3: Specialized Optimizations
1. Optimize XML processing with System.Text.Json
2. Implement hardware intrinsics for weight conversion
3. Implement trimming for release builds

## Testing Strategy

For each performance improvement:
1. Establish baseline performance metrics using .NET 8.0 profiling tools
2. Implement the improvement in isolation
3. Measure performance impact with the same metrics
4. Verify functionality is preserved with existing tests
5. Document the results and any trade-offs

## Conclusion

These performance improvements leverage .NET 8.0 features to enhance the Prosim2GSX application. By implementing these optimizations, the application will benefit from reduced memory usage, improved response time, and better overall performance.

The highest priority improvements (Frozen Collections, Span<T>, and ValueTask) provide the best balance of impact and risk, and should be implemented first. These changes are well-supported by .NET 8.0 and have proven performance benefits in similar applications.
