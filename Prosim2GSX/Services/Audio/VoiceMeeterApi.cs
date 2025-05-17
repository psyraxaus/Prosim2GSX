using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Prosim2GSX.Services.Audio
{
    public class VoiceMeeterApi
    {
        private readonly ILogger<VoiceMeeterApi> _logger;

        private const string DefaultDllName = "VoicemeeterRemote64.dll";
        private IntPtr _dllHandle = IntPtr.Zero;
        private bool _initialized = false;

        // Function delegates for dynamic DLL loading
        private delegate int VBVMR_Login();
        private delegate int VBVMR_Logout();
        private delegate int VBVMR_RunVoicemeeter(int voicemeeterId);
        private delegate int VBVMR_GetVoicemeeterType(ref int type);
        private delegate int VBVMR_GetParameterFloat(string paramName, ref float value);
        private delegate int VBVMR_SetParameterFloat(string paramName, float value);
        private delegate int VBVMR_GetParameterStringA(string paramName, IntPtr value);
        private delegate int VBVMR_SetParameterStringA(string paramName, string value);
        private delegate int VBVMR_IsParametersDirty();
        private delegate int VBVMR_GetLevel(int type, int channel, ref float level);

        // Function pointers
        private VBVMR_Login _loginFunc;
        private VBVMR_Logout _logoutFunc;
        private VBVMR_RunVoicemeeter _runVoicemeeterFunc;
        private VBVMR_GetVoicemeeterType _getVoicemeeterTypeFunc;
        private VBVMR_GetParameterFloat _getParameterFloatFunc;
        private VBVMR_SetParameterFloat _setParameterFloatFunc;
        private VBVMR_GetParameterStringA _getParameterStringFunc;
        private VBVMR_SetParameterStringA _setParameterStringFunc;
        private VBVMR_IsParametersDirty _isParametersDirtyFunc;
        private VBVMR_GetLevel _getLevelFunc;

        // P/Invoke imports for dynamic loading
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        // VoiceMeeter types
        public enum VoiceMeeterType
        {
            NotInstalled = 0,
            Standard = 1,   // VoiceMeeter
            Banana = 2,     // VoiceMeeter Banana
            Potato = 3      // VoiceMeeter Potato
        }

        /// <summary>
        /// Initializes a new instance of the VoiceMeeterApi class with logging.
        /// </summary>
        /// <param name="logger">The logger to use for logging messages</param>
        public VoiceMeeterApi(ILogger<VoiceMeeterApi> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Initializes a new instance of the VoiceMeeterApi class without logging.
        /// </summary>
        /// <remarks>This constructor is provided for backward compatibility</remarks>
        public VoiceMeeterApi()
        {
            _logger = null;
        }

        /// <summary>
        /// Initialize the VoiceMeeter Remote API
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool Initialize()
        {
            if (_initialized)
                return true;

            try
            {
                // Try to use the custom DLL path if configured
                string customDllPath = ServiceLocator.Model?.VoicemeeterDllPath;
                bool usingCustomDll = false;

                if (!string.IsNullOrEmpty(customDllPath) && File.Exists(customDllPath))
                {
                    // Load from custom path
                    _dllHandle = LoadLibrary(customDllPath);
                    usingCustomDll = true;

                    if (_dllHandle == IntPtr.Zero)
                    {
                        int error = Marshal.GetLastWin32Error();
                        _logger?.LogError("Failed to load VoicemeeterRemote64.dll from custom path: {Path}, Error: {Error}",
                            customDllPath, error);
                    }
                    else
                    {
                        _logger?.LogInformation("Loaded VoicemeeterRemote64.dll from custom path: {Path}", customDllPath);
                    }
                }

                // If custom loading failed or wasn't attempted, try the default location
                if (_dllHandle == IntPtr.Zero)
                {
                    _dllHandle = LoadLibrary(DefaultDllName);

                    if (_dllHandle == IntPtr.Zero)
                    {
                        int error = Marshal.GetLastWin32Error();
                        _logger?.LogError("Failed to load VoicemeeterRemote64.dll from default location, Error: {Error}", error);
                        return false;
                    }

                    _logger?.LogInformation("Loaded VoicemeeterRemote64.dll from default location");
                }

                // Get function pointers
                _loginFunc = GetFunctionDelegate<VBVMR_Login>(_dllHandle, "VBVMR_Login");
                _logoutFunc = GetFunctionDelegate<VBVMR_Logout>(_dllHandle, "VBVMR_Logout");
                _runVoicemeeterFunc = GetFunctionDelegate<VBVMR_RunVoicemeeter>(_dllHandle, "VBVMR_RunVoicemeeter");
                _getVoicemeeterTypeFunc = GetFunctionDelegate<VBVMR_GetVoicemeeterType>(_dllHandle, "VBVMR_GetVoicemeeterType");
                _getParameterFloatFunc = GetFunctionDelegate<VBVMR_GetParameterFloat>(_dllHandle, "VBVMR_GetParameterFloat");
                _setParameterFloatFunc = GetFunctionDelegate<VBVMR_SetParameterFloat>(_dllHandle, "VBVMR_SetParameterFloat");
                _getParameterStringFunc = GetFunctionDelegate<VBVMR_GetParameterStringA>(_dllHandle, "VBVMR_GetParameterStringA");
                _setParameterStringFunc = GetFunctionDelegate<VBVMR_SetParameterStringA>(_dllHandle, "VBVMR_SetParameterStringA");
                _isParametersDirtyFunc = GetFunctionDelegate<VBVMR_IsParametersDirty>(_dllHandle, "VBVMR_IsParametersDirty");
                _getLevelFunc = GetFunctionDelegate<VBVMR_GetLevel>(_dllHandle, "VBVMR_GetLevel");

                // Validate that all functions were loaded
                if (_loginFunc == null || _logoutFunc == null || _runVoicemeeterFunc == null ||
                    _getVoicemeeterTypeFunc == null || _getParameterFloatFunc == null ||
                    _setParameterFloatFunc == null || _getParameterStringFunc == null ||
                    _setParameterStringFunc == null || _isParametersDirtyFunc == null ||
                    _getLevelFunc == null)
                {
                    _logger?.LogError("Failed to load one or more VoiceMeeter functions");
                    Cleanup();
                    return false;
                }

                // Login to VoiceMeeter
                int result = _loginFunc();
                _initialized = (result >= 0);

                if (!_initialized)
                {
                    _logger?.LogError("Failed to initialize VoiceMeeter API. Error code: {ErrorCode}", result);
                    Cleanup();
                }
                else
                {
                    _logger?.LogInformation("VoiceMeeter API initialized successfully");
                }

                return _initialized;
            }
            catch (DllNotFoundException ex)
            {
                _logger?.LogCritical(ex, "VoicemeeterRemote64.dll not found");
                Cleanup();
                return false;
            }
            catch (EntryPointNotFoundException ex)
            {
                _logger?.LogCritical(ex, "Entry point not found in VoicemeeterRemote64.dll");
                Cleanup();
                return false;
            }
            catch (BadImageFormatException ex)
            {
                _logger?.LogCritical(ex, "VoicemeeterRemote64.dll is not a valid image (wrong bitness or corrupted)");
                Cleanup();
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogCritical(ex, "Unexpected error initializing VoiceMeeter API");
                Cleanup();
                return false;
            }
        }

        /// <summary>
        /// Get a function delegate from the loaded DLL
        /// </summary>
        /// <typeparam name="T">Type of delegate</typeparam>
        /// <param name="module">DLL handle</param>
        /// <param name="functionName">Function name</param>
        /// <returns>Function delegate or null if not found</returns>
        private T GetFunctionDelegate<T>(IntPtr module, string functionName) where T : class
        {
            IntPtr functionPtr = GetProcAddress(module, functionName);
            if (functionPtr == IntPtr.Zero)
            {
                _logger?.LogError("Function {FunctionName} not found in VoicemeeterRemote64.dll", functionName);
                return null;
            }

            return Marshal.GetDelegateForFunctionPointer(functionPtr, typeof(T)) as T;
        }

        /// <summary>
        /// Clean up resources
        /// </summary>
        private void Cleanup()
        {
            if (_initialized && _logoutFunc != null)
            {
                try
                {
                    _logoutFunc();
                }
                catch
                {
                    // Ignore errors during logout
                }
            }

            _initialized = false;

            if (_dllHandle != IntPtr.Zero)
            {
                try
                {
                    FreeLibrary(_dllHandle);
                }
                catch
                {
                    // Ignore errors during FreeLibrary
                }

                _dllHandle = IntPtr.Zero;
            }

            // Clear function pointers
            _loginFunc = null;
            _logoutFunc = null;
            _runVoicemeeterFunc = null;
            _getVoicemeeterTypeFunc = null;
            _getParameterFloatFunc = null;
            _setParameterFloatFunc = null;
            _getParameterStringFunc = null;
            _setParameterStringFunc = null;
            _isParametersDirtyFunc = null;
            _getLevelFunc = null;
        }

        /// <summary>
        /// Shutdown the VoiceMeeter Remote API
        /// </summary>
        public void Shutdown()
        {
            Cleanup();
        }

        /// <summary>
        /// Run VoiceMeeter
        /// </summary>
        /// <param name="type">VoiceMeeter type (1=Standard, 2=Banana, 3=Potato)</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool RunVoiceMeeter(int type)
        {
            if (!_initialized && !Initialize())
                return false;

            try
            {
                int result = _runVoicemeeterFunc(type);

                if (result != 0)
                {
                    _logger?.LogError("Failed to run VoiceMeeter type {Type}. Error code: {ErrorCode}", type, result);
                    return false;
                }

                _logger?.LogInformation("Successfully started VoiceMeeter type {Type}", type);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error running VoiceMeeter type {Type}", type);
                return false;
            }
        }

        /// <summary>
        /// Get the current VoiceMeeter type
        /// </summary>
        /// <returns>VoiceMeeter type</returns>
        public VoiceMeeterType GetCurrentType()
        {
            if (!_initialized && !Initialize())
                return VoiceMeeterType.NotInstalled;

            try
            {
                int type = 0;
                int result = _getVoicemeeterTypeFunc(ref type);

                if (result < 0)
                {
                    _logger?.LogError("Failed to get VoiceMeeter type. Error code: {ErrorCode}", result);
                    return VoiceMeeterType.NotInstalled;
                }

                _logger?.LogDebug("Detected VoiceMeeter type: {Type}", (VoiceMeeterType)type);
                return (VoiceMeeterType)type;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting VoiceMeeter type");
                return VoiceMeeterType.NotInstalled;
            }
        }

        /// <summary>
        /// Get the number of input strips for the current VoiceMeeter type
        /// </summary>
        /// <returns>Number of input strips</returns>
        public int GetInputStripCount()
        {
            switch (GetCurrentType())
            {
                case VoiceMeeterType.Standard:
                    return 3;  // VoiceMeeter has 3 input strips
                case VoiceMeeterType.Banana:
                    return 5;  // VoiceMeeter Banana has 5 input strips
                case VoiceMeeterType.Potato:
                    return 8;  // VoiceMeeter Potato has 8 input strips
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Get the number of virtual input strips for the current VoiceMeeter type
        /// </summary>
        /// <returns>Number of virtual input strips</returns>
        public int GetVirtualInputCount()
        {
            switch (GetCurrentType())
            {
                case VoiceMeeterType.Standard:
                    return 1;  // VoiceMeeter has 1 virtual input
                case VoiceMeeterType.Banana:
                    return 2;  // VoiceMeeter Banana has 2 virtual inputs
                case VoiceMeeterType.Potato:
                    return 3;  // VoiceMeeter Potato has 3 virtual inputs
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Get the number of output buses for the current VoiceMeeter type
        /// </summary>
        /// <returns>Number of output buses</returns>
        public int GetOutputBusCount()
        {
            switch (GetCurrentType())
            {
                case VoiceMeeterType.Standard:
                    return 2;  // VoiceMeeter has 2 output buses (A and B)
                case VoiceMeeterType.Banana:
                    return 5;  // VoiceMeeter Banana has 5 output buses (A1, A2, A3, B1, B2)
                case VoiceMeeterType.Potato:
                    return 8;  // VoiceMeeter Potato has 8 output buses (A1-A5, B1-B3)
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Get a string parameter from VoiceMeeter
        /// </summary>
        /// <param name="paramName">Parameter name</param>
        /// <returns>Parameter value</returns>
        public string GetStringParameter(string paramName)
        {
            if (!_initialized && !Initialize())
                return null;

            IntPtr buffer = Marshal.AllocHGlobal(512);
            try
            {
                int result = _getParameterStringFunc(paramName, buffer);

                if (result < 0)
                {
                    _logger?.LogWarning("Failed to get string parameter '{ParamName}'. Error code: {ErrorCode}",
                        paramName, result);
                    return null;
                }

                string value = Marshal.PtrToStringAnsi(buffer);
                return value;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting string parameter '{ParamName}'", paramName);
                return null;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        /// <summary>
        /// Set a string parameter in VoiceMeeter
        /// </summary>
        /// <param name="paramName">Parameter name</param>
        /// <param name="value">Parameter value</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool SetStringParameter(string paramName, string value)
        {
            if (!_initialized && !Initialize())
                return false;

            try
            {
                int result = _setParameterStringFunc(paramName, value);

                if (result < 0)
                {
                    _logger?.LogWarning("Failed to set string parameter '{ParamName}' to '{Value}'. Error code: {ErrorCode}",
                        paramName, value, result);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error setting string parameter '{ParamName}' to '{Value}'",
                    paramName, value);
                return false;
            }
        }

        /// <summary>
        /// Get a float parameter from VoiceMeeter
        /// </summary>
        /// <param name="paramName">Parameter name</param>
        /// <returns>Parameter value</returns>
        public float GetFloatParameter(string paramName)
        {
            if (!_initialized && !Initialize())
                return 0.0f;

            try
            {
                float value = 0.0f;
                int result = _getParameterFloatFunc(paramName, ref value);

                if (result < 0)
                {
                    _logger?.LogWarning("Failed to get parameter '{ParamName}'. Error code: {ErrorCode}",
                        paramName, result);
                    return 0.0f;
                }

                return value;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting parameter '{ParamName}'", paramName);
                return 0.0f;
            }
        }

        /// <summary>
        /// Set a float parameter in VoiceMeeter
        /// </summary>
        /// <param name="paramName">Parameter name</param>
        /// <param name="value">Parameter value</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool SetFloatParameter(string paramName, float value)
        {
            if (!_initialized && !Initialize())
                return false;

            try
            {
                int result = _setParameterFloatFunc(paramName, value);

                if (result < 0)
                {
                    _logger?.LogWarning("Failed to set parameter '{ParamName}' to {Value}. Error code: {ErrorCode}",
                        paramName, value, result);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error setting parameter '{ParamName}' to {Value}",
                    paramName, value);
                return false;
            }
        }

        /// <summary>
        /// Get the label for a strip
        /// </summary>
        /// <param name="stripIndex">Strip index</param>
        /// <returns>Strip label</returns>
        public string GetStripLabel(int stripIndex)
        {
            return GetStringParameter($"Strip[{stripIndex}].label");
        }

        /// <summary>
        /// Get a strip parameter
        /// </summary>
        /// <param name="stripName">Strip name (e.g., "Strip[0]")</param>
        /// <param name="paramName">Parameter name</param>
        /// <returns>Parameter value</returns>
        public float GetStripParameter(string stripName, string paramName)
        {
            string fullParamName = $"{stripName}.{paramName}";
            return GetFloatParameter(fullParamName);
        }

        /// <summary>
        /// Set a strip parameter
        /// </summary>
        /// <param name="stripName">Strip name (e.g., "Strip[0]")</param>
        /// <param name="paramName">Parameter name</param>
        /// <param name="value">Parameter value</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool SetStripParameter(string stripName, string paramName, float value)
        {
            string fullParamName = $"{stripName}.{paramName}";
            return SetFloatParameter(fullParamName, value);
        }

        /// <summary>
        /// Get the gain for a strip
        /// </summary>
        /// <param name="stripName">Strip name (e.g., "Strip[0]")</param>
        /// <returns>Gain value</returns>
        public float GetStripGain(string stripName)
        {
            try
            {
                float gain = GetStripParameter(stripName, "Gain");
                _logger?.LogDebug("GetStripGain for {StripName}: {Gain} dB", stripName, gain);
                return gain;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting gain for strip {StripName}", stripName);
                return -60.0f; // Return minimum gain as a fallback
            }
        }

        /// <summary>
        /// Set the gain for a strip
        /// </summary>
        /// <param name="stripName">Strip name (e.g., "Strip[0]")</param>
        /// <param name="gain">Gain value</param>
        /// <returns>True if successful, false otherwise</returns>
        public void SetStripGain(string stripName, float gain)
        {
            try
            {
                bool success = SetStripParameter(stripName, "Gain", gain);
                if (success)
                {
                    _logger?.LogDebug("SetStripGain for {StripName}: {Gain} dB", stripName, gain);
                }
                else
                {
                    _logger?.LogWarning("Failed to set gain for strip {StripName} to {Gain} dB",
                        stripName, gain);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error setting gain for strip {StripName}", stripName);
            }
        }

        /// <summary>
        /// Get the mute state for a strip
        /// </summary>
        /// <param name="stripName">Strip name (e.g., "Strip[0]")</param>
        /// <returns>True if muted, false otherwise</returns>
        public bool GetStripMute(string stripName)
        {
            try
            {
                bool muted = GetStripParameter(stripName, "Mute") > 0.5f;
                _logger?.LogDebug("GetStripMute for {StripName}: {MuteState}",
                    stripName, (muted ? "Muted" : "Unmuted"));
                return muted;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting mute state for strip {StripName}", stripName);
                return false; // Return unmuted as a fallback
            }
        }

        /// <summary>
        /// Set the mute state for a strip
        /// </summary>
        /// <param name="stripName">Strip name (e.g., "Strip[0]")</param>
        /// <param name="mute">True to mute, false to unmute</param>
        /// <returns>True if successful, false otherwise</returns>
        public void SetStripMute(string stripName, bool mute)
        {
            try
            {
                bool success = SetStripParameter(stripName, "Mute", mute ? 1.0f : 0.0f);
                if (success)
                {
                    _logger?.LogDebug("SetStripMute for {StripName}: {MuteState}",
                        stripName, (mute ? "Muted" : "Unmuted"));
                }
                else
                {
                    _logger?.LogWarning("Failed to set mute state for strip {StripName} to {MuteState}",
                        stripName, (mute ? "Muted" : "Unmuted"));
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error setting mute state for strip {StripName}", stripName);
            }
        }

        /// <summary>
        /// Check if parameters have changed
        /// </summary>
        /// <returns>True if parameters have changed, false otherwise</returns>
        public bool AreParametersDirty()
        {
            if (!_initialized && !Initialize())
                return false;

            try
            {
                int result = _isParametersDirtyFunc();

                if (result < 0)
                {
                    _logger?.LogWarning("Failed to check if parameters are dirty. Error code: {ErrorCode}", result);
                    return false;
                }

                return result > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking if parameters are dirty");
                return false;
            }
        }

        /// <summary>
        /// Get the level for a channel
        /// </summary>
        /// <param name="type">Level type (0=PreFader, 1=PostFader, 2=PostMute)</param>
        /// <param name="channel">Channel index</param>
        /// <returns>Level value</returns>
        public float GetLevel(int type, int channel)
        {
            if (!_initialized && !Initialize())
                return 0.0f;

            try
            {
                float level = 0.0f;
                int result = _getLevelFunc(type, channel, ref level);

                if (result < 0)
                {
                    _logger?.LogWarning("Failed to get level for type {Type}, channel {Channel}. Error code: {ErrorCode}",
                        type, channel, result);
                    return 0.0f;
                }

                return level;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting level for type {Type}, channel {Channel}", type, channel);
                return 0.0f;
            }
        }

        /// <summary>
        /// Get a list of all available strips with their actual labels
        /// </summary>
        /// <returns>List of strip names and labels</returns>
        public List<KeyValuePair<string, string>> GetAvailableStripsWithLabels()
        {
            List<KeyValuePair<string, string>> strips = new List<KeyValuePair<string, string>>();

            // Initialize VoiceMeeter if not already initialized
            if (!Initialize())
            {
                return strips;
            }

            try
            {
                // Get hardware input strips
                int inputCount = GetInputStripCount();
                for (int i = 0; i < inputCount; i++)
                {
                    string stripName = $"Strip[{i}]";
                    string label = GetStripLabel(i);

                    // If no label is set, use a default name
                    if (string.IsNullOrEmpty(label))
                    {
                        label = $"Hardware Input {i + 1}";
                    }

                    strips.Add(new KeyValuePair<string, string>(stripName, label));
                }

                // Get virtual input strips
                int virtualInputCount = GetVirtualInputCount();
                int virtualStartIndex = inputCount;

                // VoiceMeeter Potato has a different indexing scheme for virtual inputs
                if (GetCurrentType() == VoiceMeeterType.Potato)
                {
                    virtualStartIndex = 8;
                }

                for (int i = 0; i < virtualInputCount; i++)
                {
                    int index = virtualStartIndex + i;
                    string stripName = $"Strip[{index}]";
                    string label = GetStripLabel(index);

                    // If no label is set, use a default name
                    if (string.IsNullOrEmpty(label))
                    {
                        label = $"Virtual Input {i + 1}";
                    }

                    strips.Add(new KeyValuePair<string, string>(stripName, label));
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger?.LogError(ex, "Error getting VoiceMeeter strips");
            }

            return strips;
        }

        /// <summary>
        /// Get a list of all available bus names with their labels
        /// </summary>
        /// <returns>List of bus names and labels</returns>
        public List<KeyValuePair<string, string>> GetAvailableBusesWithLabels()
        {
            List<KeyValuePair<string, string>> buses = new List<KeyValuePair<string, string>>();

            // Initialize VoiceMeeter if not already initialized
            if (!Initialize())
            {
                return buses;
            }

            try
            {
                // Get output buses
                int busCount = GetOutputBusCount();

                for (int i = 0; i < busCount; i++)
                {
                    string busName = $"Bus[{i}]";
                    string label = GetStringParameter($"Bus[{i}].label");

                    // If no label is set, use a default name
                    if (string.IsNullOrEmpty(label))
                    {
                        // Determine bus type (A or B)
                        string busType;
                        if (GetCurrentType() == VoiceMeeterType.Standard)
                        {
                            busType = (i == 0) ? "A" : "B";
                        }
                        else if (GetCurrentType() == VoiceMeeterType.Banana)
                        {
                            busType = (i < 3) ? $"A{i + 1}" : $"B{i - 2}";
                        }
                        else // Potato
                        {
                            busType = (i < 5) ? $"A{i + 1}" : $"B{i - 4}";
                        }

                        label = $"Output {busType}";
                    }

                    buses.Add(new KeyValuePair<string, string>(busName, label));
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger?.LogError(ex, "Error getting VoiceMeeter buses");
            }

            return buses;
        }

        /// <summary>
        /// Check if VoiceMeeter is installed and running
        /// </summary>
        /// <returns>True if VoiceMeeter is running, false otherwise</returns>
        public bool IsVoiceMeeterRunning()
        {
            return Initialize();
        }

        /// <summary>
        /// Performs a diagnostic check of the VoiceMeeter API and logs detailed information
        /// </summary>
        /// <returns>True if all checks pass, false otherwise</returns>
        public bool PerformDiagnostics()
        {
            _logger?.LogInformation("Starting VoiceMeeter API diagnostics");

            bool success = true;

            try
            {
                // Check 1: DLL Loading
                string customDllPath = ServiceLocator.Model?.VoicemeeterDllPath;
                bool usingCustomDll = !string.IsNullOrEmpty(customDllPath) && File.Exists(customDllPath);

                if (usingCustomDll)
                {
                    _logger?.LogInformation("Using custom VoicemeeterRemote64.dll path: {Path}", customDllPath);
                }
                else
                {
                    _logger?.LogInformation("Using default VoicemeeterRemote64.dll path");
                }

                IntPtr testDllHandle = IntPtr.Zero;
                try
                {
                    if (usingCustomDll)
                    {
                        testDllHandle = LoadLibrary(customDllPath);
                    }
                    else
                    {
                        testDllHandle = LoadLibrary(DefaultDllName);
                    }

                    if (testDllHandle == IntPtr.Zero)
                    {
                        int error = Marshal.GetLastWin32Error();
                        _logger?.LogError("Failed to load VoicemeeterRemote64.dll. Error code: {ErrorCode}", error);
                        success = false;
                    }
                    else
                    {
                        _logger?.LogInformation("VoicemeeterRemote64.dll loaded successfully");

                        // Check for a simple function to verify DLL validity
                        IntPtr loginFuncPtr = GetProcAddress(testDllHandle, "VBVMR_Login");
                        if (loginFuncPtr == IntPtr.Zero)
                        {
                            _logger?.LogError("VBVMR_Login function not found in loaded DLL");
                            success = false;
                        }
                        else
                        {
                            _logger?.LogInformation("VBVMR_Login function found in loaded DLL");
                        }

                        // Free the test handle
                        FreeLibrary(testDllHandle);
                        testDllHandle = IntPtr.Zero;
                    }
                }
                catch (Exception ex)
                {
                    if (testDllHandle != IntPtr.Zero)
                    {
                        try { FreeLibrary(testDllHandle); } catch { }
                    }

                    _logger?.LogCritical(ex, "Exception loading VoicemeeterRemote64.dll");
                    success = false;
                }

                // Check 2: VoiceMeeter Installation
                try
                {
                    _logger?.LogInformation("Checking VoiceMeeter installation...");
                    if (Initialize())
                    {
                        VoiceMeeterType type = GetCurrentType();
                        _logger?.LogInformation("VoiceMeeter is installed and running. Type: {Type}", type);

                        // Log additional information about the VoiceMeeter installation
                        int inputCount = GetInputStripCount();
                        int virtualInputCount = GetVirtualInputCount();
                        int outputBusCount = GetOutputBusCount();

                        _logger?.LogInformation("VoiceMeeter configuration: {InputCount} hardware inputs, {VirtualInputCount} virtual inputs, {OutputBusCount} output buses",
                            inputCount, virtualInputCount, outputBusCount);
                    }
                    else
                    {
                        _logger?.LogError("VoiceMeeter is not installed or not running");
                        success = false;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error checking VoiceMeeter installation");
                    success = false;
                }

                // Check 3: Available Strips and Buses
                try
                {
                    if (Initialize())
                    {
                        _logger?.LogInformation("Checking available VoiceMeeter strips...");
                        var strips = GetAvailableStripsWithLabels();
                        if (strips.Count > 0)
                        {
                            _logger?.LogInformation("Found {Count} VoiceMeeter strips:", strips.Count);
                            foreach (var strip in strips)
                            {
                                _logger?.LogInformation("  - {StripKey}: {StripValue}", strip.Key, strip.Value);

                                // Try to get and set parameters for this strip
                                try
                                {
                                    float gain = GetStripGain(strip.Key);
                                    bool mute = GetStripMute(strip.Key);
                                    _logger?.LogInformation("    Current state: Gain={Gain} dB, Mute={Mute}", gain, mute);
                                }
                                catch (Exception ex)
                                {
                                    _logger?.LogWarning(ex, "Error getting strip parameters for {StripKey}", strip.Key);
                                }
                            }
                        }
                        else
                        {
                            _logger?.LogWarning("No VoiceMeeter strips found");
                        }

                        _logger?.LogInformation("Checking available VoiceMeeter buses...");
                        var buses = GetAvailableBusesWithLabels();
                        if (buses.Count > 0)
                        {
                            _logger?.LogInformation("Found {Count} VoiceMeeter buses:", buses.Count);
                            foreach (var bus in buses)
                            {
                                _logger?.LogInformation("  - {BusKey}: {BusValue}", bus.Key, bus.Value);

                                // Try to get and set parameters for this bus
                                try
                                {
                                    float gain = GetBusGain(bus.Key);
                                    bool mute = GetBusMute(bus.Key);
                                    _logger?.LogInformation("    Current state: Gain={Gain} dB, Mute={Mute}", gain, mute);
                                }
                                catch (Exception ex)
                                {
                                    _logger?.LogWarning(ex, "Error getting bus parameters for {BusKey}", bus.Key);
                                }
                            }
                        }
                        else
                        {
                            _logger?.LogWarning("No VoiceMeeter buses found");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error checking VoiceMeeter strips and buses");
                    success = false;
                }

                // Check 4: Parameter Dirty Flag
                try
                {
                    if (Initialize())
                    {
                        _logger?.LogInformation("Checking VoiceMeeter parameter dirty flag...");
                        bool isDirty = AreParametersDirty();
                        _logger?.LogInformation("VoiceMeeter parameters dirty flag: {IsDirty}", isDirty);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error checking VoiceMeeter parameter dirty flag");
                    success = false;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogCritical(ex, "Unexpected error during diagnostics");
                success = false;
            }

            _logger?.LogInformation("VoiceMeeter API diagnostics completed. Success: {Success}", success);
            return success;
        }

        /// <summary>
        /// Run VoiceMeeter if it's not already running
        /// </summary>
        /// <returns>True if VoiceMeeter is running, false otherwise</returns>
        public bool EnsureVoiceMeeterIsRunning()
        {
            try
            {
                if (!IsVoiceMeeterRunning())
                {
                    _logger?.LogInformation("VoiceMeeter is not running. Attempting to start it...");

                    // Try to run VoiceMeeter
                    int type = (int)GetCurrentType();

                    if (type <= 0)
                    {
                        _logger?.LogError("Cannot start VoiceMeeter: Invalid type ({Type}). VoiceMeeter may not be installed.", type);
                        return false;
                    }

                    _logger?.LogInformation("Starting VoiceMeeter type: {Type}", (VoiceMeeterType)type);

                    bool runResult = RunVoiceMeeter(type);
                    if (!runResult)
                    {
                        _logger?.LogError("Failed to run VoiceMeeter");
                        return false;
                    }

                    _logger?.LogInformation("VoiceMeeter started. Initializing API...");

                    bool initResult = Initialize();
                    if (!initResult)
                    {
                        _logger?.LogError("Failed to initialize VoiceMeeter API after starting VoiceMeeter");
                        return false;
                    }

                    _logger?.LogInformation("VoiceMeeter API initialized successfully after starting VoiceMeeter");
                    return true;
                }

                _logger?.LogDebug("VoiceMeeter is already running");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error ensuring VoiceMeeter is running");
                return false;
            }
        }

        /// <summary>
        /// Convert a VoiceMeeter gain value (-60 to 12 dB) to a normalized volume (0 to 1)
        /// </summary>
        /// <param name="gain">VoiceMeeter gain value</param>
        /// <returns>Normalized volume</returns>
        public float GainToVolume(float gain)
        {
            // VoiceMeeter gain range is -60 to 12 dB
            return (gain + 60.0f) / 72.0f;
        }

        /// <summary>
        /// Convert a normalized volume (0 to 1) to a VoiceMeeter gain value (-60 to 12 dB)
        /// </summary>
        /// <param name="volume">Normalized volume</param>
        /// <returns>VoiceMeeter gain value</returns>
        public float VolumeToGain(float volume)
        {
            // VoiceMeeter gain range is -60 to 12 dB
            return (volume * 72.0f) - 60.0f;
        }

        /// <summary>
        /// Get a bus parameter
        /// </summary>
        /// <param name="busName">Bus name (e.g., "Bus[0]")</param>
        /// <param name="paramName">Parameter name</param>
        /// <returns>Parameter value</returns>
        public float GetBusParameter(string busName, string paramName)
        {
            string fullParamName = $"{busName}.{paramName}";
            return GetFloatParameter(fullParamName);
        }

        /// <summary>
        /// Set a bus parameter
        /// </summary>
        /// <param name="busName">Bus name (e.g., "Bus[0]")</param>
        /// <param name="paramName">Parameter name</param>
        /// <param name="value">Parameter value</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool SetBusParameter(string busName, string paramName, float value)
        {
            string fullParamName = $"{busName}.{paramName}";
            return SetFloatParameter(fullParamName, value);
        }

        /// <summary>
        /// Get the gain for a bus
        /// </summary>
        /// <param name="busName">Bus name (e.g., "Bus[0]")</param>
        /// <returns>Gain value</returns>
        public float GetBusGain(string busName)
        {
            try
            {
                float gain = GetBusParameter(busName, "Gain");
                _logger?.LogDebug("GetBusGain for {BusName}: {Gain} dB", busName, gain);
                return gain;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting gain for bus {BusName}", busName);
                return -60.0f; // Return minimum gain as a fallback
            }
        }

        /// <summary>
        /// Set the gain for a bus
        /// </summary>
        /// <param name="busName">Bus name (e.g., "Bus[0]")</param>
        /// <param name="gain">Gain value</param>
        /// <returns>True if successful, false otherwise</returns>
        public void SetBusGain(string busName, float gain)
        {
            try
            {
                bool success = SetBusParameter(busName, "Gain", gain);
                if (success)
                {
                    _logger?.LogDebug("SetBusGain for {BusName}: {Gain} dB", busName, gain);
                }
                else
                {
                    _logger?.LogWarning("Failed to set gain for bus {BusName} to {Gain} dB", busName, gain);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error setting gain for bus {BusName}", busName);
            }
        }

        /// <summary>
        /// Get the mute state for a bus
        /// </summary>
        /// <param name="busName">Bus name (e.g., "Bus[0]")</param>
        /// <returns>True if muted, false otherwise</returns>
        public bool GetBusMute(string busName)
        {
            try
            {
                bool muted = GetBusParameter(busName, "Mute") > 0.5f;
                _logger?.LogDebug("GetBusMute for {BusName}: {MuteState}",
                    busName, (muted ? "Muted" : "Unmuted"));
                return muted;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting mute state for bus {BusName}", busName);
                return false; // Return unmuted as a fallback
            }
        }

        /// <summary>
        /// Set the mute state for a bus
        /// </summary>
        /// <param name="busName">Bus name (e.g., "Bus[0]")</param>
        /// <param name="mute">True to mute, false to unmute</param>
        /// <returns>True if successful, false otherwise</returns>
        public void SetBusMute(string busName, bool mute)
        {
            try
            {
                bool success = SetBusParameter(busName, "Mute", mute ? 1.0f : 0.0f);
                if (success)
                {
                    _logger?.LogDebug("SetBusMute for {BusName}: {MuteState}",
                        busName, (mute ? "Muted" : "Unmuted"));
                }
                else
                {
                    _logger?.LogWarning("Failed to set mute state for bus {BusName} to {MuteState}",
                        busName, (mute ? "Muted" : "Unmuted"));
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error setting mute state for bus {BusName}", busName);
            }
        }
    }
}
