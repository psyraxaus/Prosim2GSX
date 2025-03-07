# Implementing Span<T> for String Operations in Prosim2GSX

This document provides detailed implementation instructions for optimizing string operations in Prosim2GSX using .NET 8.0's improved `Span<T>` support.

## Overview

.NET 8.0 has improved performance for `Span<T>` operations, which can significantly reduce allocations in string processing. The `Logger.cs` and `SimConnectService.cs` classes in Prosim2GSX perform many string operations that could benefit from this optimization.

## Implementation Steps

### 1. Update Logger.cs

The `Logger.cs` class formats log messages by concatenating strings, which creates unnecessary allocations. We can optimize this using `Span<T>`.

#### Original Code

```csharp
public static void Log(LogLevel level, string context, string message)
{
    string entry = string.Format("[ {0,-32} ] {1}", (context.Length <= 32 ? context : context[0..32]), message.Replace("\n", "").Replace("\r", "").Replace("\t", ""));
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

#### Optimized Code

```csharp
public static void Log(LogLevel level, string context, string message)
{
    // Determine context length
    ReadOnlySpan<char> contextSpan = context.AsSpan();
    ReadOnlySpan<char> formattedContext = contextSpan.Length <= 32 ? 
        contextSpan : contextSpan.Slice(0, 32);
    
    // Clean the message
    string cleanMessage = message.Replace("\n", "").Replace("\r", "").Replace("\t", "");
    
    // Calculate buffer size
    int bufferSize = formattedContext.Length + cleanMessage.Length + 10; // 10 for "[ ", " ] " and some extra space
    
    // Use stackalloc for small buffers to avoid heap allocations
    Span<char> buffer = bufferSize <= 256 ? 
        stackalloc char[bufferSize] : // Use stack for small buffers
        new char[bufferSize];         // Use heap for large buffers
    
    // Format the log entry without string allocations
    int position = 0;
    
    // Add prefix "[ "
    "[ ".AsSpan().CopyTo(buffer.Slice(position));
    position += 2;
    
    // Add formatted context
    formattedContext.CopyTo(buffer.Slice(position));
    position += formattedContext.Length;
    
    // Add padding spaces to align to 32 characters
    int padding = Math.Max(0, 32 - formattedContext.Length);
    for (int i = 0; i < padding; i++)
    {
        buffer[position++] = ' ';
    }
    
    // Add separator " ] "
    " ] ".AsSpan().CopyTo(buffer.Slice(position));
    position += 3;
    
    // Add clean message
    cleanMessage.AsSpan().CopyTo(buffer.Slice(position));
    position += cleanMessage.Length;
    
    // Convert to string only once for the final log entry
    string entry = buffer.Slice(0, position).ToString();
    
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

### 2. Update ClientDataString in MobiDefinitions.cs

The `ClientDataString` struct in `MobiDefinitions.cs` creates a new byte array for each string, which can be optimized using `Span<T>`.

#### Original Code

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
}
```

#### Optimized Code

```csharp
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
    
    // Add constructor that accepts ReadOnlySpan<char> for better performance
    public ClientDataString(ReadOnlySpan<char> strData) : this()
    {
        SetData(strData);
    }
    
    // Add method to set data from string
    public void SetData(string strData)
    {
        SetData(strData.AsSpan());
    }
    
    // Add method to set data from ReadOnlySpan<char>
    public void SetData(ReadOnlySpan<char> strData)
    {
        // Clear existing data
        Array.Clear(data, 0, data.Length);
        
        // Convert directly from Span<char> to avoid string allocation
        Encoding.ASCII.GetBytes(strData, data);
    }
}
```

### 3. Update SimConnectService.cs

The `SimConnectService.cs` class constructs commands by concatenating strings, which can be optimized using `Span<T>`.

#### Original Code

```csharp
public void ExecuteCode(string code)
{
    SendClientWasmCmd($"MF.SimVars.Set.{code}");
    SendClientWasmDummyCmd();
}

public void WriteLvar(string address, float value)
{
    SendClientWasmCmd($"MF.SimVars.Set.{string.Format(new CultureInfo("en-US").NumberFormat, "{0:G}", value)} (>L:{address})");
    SendClientWasmDummyCmd();
}

private void SendWasmCmd(Enum cmdChannelId, Enum cmdId, string command)
{
    simConnect.SetClientData(cmdChannelId, cmdId, SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT, 0, new ClientDataString(command));
}
```

#### Optimized Code

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

public void WriteLvar(string address, float value)
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
    
    // Calculate total buffer size
    const string prefix = "MF.SimVars.Set.";
    const string middle = " (>L:";
    const string suffix = ")";
    int totalLength = prefix.Length + valueCharsWritten + middle.Length + address.Length + suffix.Length;
    
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
    SendClientWasmCmd(buffer.ToString());
    SendClientWasmDummyCmd();
}

private void SendWasmCmd(Enum cmdChannelId, Enum cmdId, string command)
{
    // Use ClientDataString with ReadOnlySpan<char> for better performance
    simConnect.SetClientData(cmdChannelId, cmdId, SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT, 0, 
        new ClientDataString(command.AsSpan()));
}

// Add overload that accepts ReadOnlySpan<char>
private void SendWasmCmd(Enum cmdChannelId, Enum cmdId, ReadOnlySpan<char> command)
{
    simConnect.SetClientData(cmdChannelId, cmdId, SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT, 0, 
        new ClientDataString(command));
}
```

### 4. Update SendClientWasmCmd and SendMobiWasmCmd Methods

The `SendClientWasmCmd` and `SendMobiWasmCmd` methods can be optimized to accept `ReadOnlySpan<char>` parameters.

#### Original Code

```csharp
private void SendClientWasmCmd(string command)
{
    SendWasmCmd(PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_CMD, (MOBIFLIGHT_CLIENT_DATA_ID)0, command);
}

private void SendMobiWasmCmd(string command)
{
    SendWasmCmd(MOBIFLIGHT_CLIENT_DATA_ID.MOBIFLIGHT_CMD, (MOBIFLIGHT_CLIENT_DATA_ID)0, command);
}
```

#### Optimized Code

```csharp
private void SendClientWasmCmd(string command)
{
    SendWasmCmd(PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_CMD, (MOBIFLIGHT_CLIENT_DATA_ID)0, command);
}

// Add overload that accepts ReadOnlySpan<char>
private void SendClientWasmCmd(ReadOnlySpan<char> command)
{
    SendWasmCmd(PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_CMD, (MOBIFLIGHT_CLIENT_DATA_ID)0, command);
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
```

## Performance Considerations

### When to Use Span<T>

`Span<T>` is most beneficial in the following scenarios:

1. **String Concatenation**: When combining multiple strings, use `Span<T>` to avoid allocations.
2. **String Formatting**: When formatting strings with placeholders, use `Span<T>` to avoid allocations.
3. **String Slicing**: When extracting parts of strings, use `Span<T>` to avoid allocations.
4. **String Parsing**: When parsing strings, use `Span<T>` to avoid allocations.

### Stackalloc vs. Heap Allocation

`stackalloc` is faster than heap allocation but has limitations:

1. **Size Limit**: `stackalloc` should only be used for small buffers (typically less than 1KB) to avoid stack overflow.
2. **Scope**: `stackalloc` buffers are only valid within the method they are declared in.
3. **Thread Safety**: `stackalloc` buffers are not thread-safe.

For larger buffers or buffers that need to be passed between methods, use heap allocation with `new char[]` or `ArrayPool<char>.Shared.Rent()`.

### String.Create

For complex string creation, consider using `String.Create`:

```csharp
string result = String.Create(totalLength, (value, address), (span, state) =>
{
    int position = 0;
    
    // Copy prefix
    "MF.SimVars.Set.".AsSpan().CopyTo(span.Slice(position));
    position += 15;
    
    // Format value
    state.value.TryFormat(span.Slice(position), out int valueCharsWritten, provider: CultureInfo.InvariantCulture);
    position += valueCharsWritten;
    
    // Copy middle part
    " (>L:".AsSpan().CopyTo(span.Slice(position));
    position += 5;
    
    // Copy address
    state.address.AsSpan().CopyTo(span.Slice(position));
    position += state.address.Length;
    
    // Copy suffix
    ")".AsSpan().CopyTo(span.Slice(position));
});
```

## Testing

After implementing these changes, test the application thoroughly to ensure that:

1. **Functionality**: All features continue to work as expected.
2. **Performance**: Measure the performance impact of the changes, particularly for string operations.
3. **Memory Usage**: Monitor memory usage to ensure it doesn't increase unexpectedly.
4. **Thread Safety**: Verify that the application remains thread-safe, particularly if multiple threads access the strings.

## Conclusion

Implementing `Span<T>` for string operations in Prosim2GSX can provide significant performance benefits by reducing memory allocations and improving garbage collection behavior. By carefully choosing when to use `Span<T>` and balancing stack and heap allocations, you can optimize the application's performance while maintaining its functionality and stability.
