using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Prosim2GSX.Services.Audio
{
    public class VoiceMeeterApi
    {
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
                        Logger.Log(LogLevel.Error, "VoiceMeeterApi",
                            $"Failed to load VoicemeeterRemote64.dll from custom path: {customDllPath}, Error: {error}");
                    }
                    else
                    {
                        Logger.Log(LogLevel.Information, "VoiceMeeterApi",
                            $"Loaded VoicemeeterRemote64.dll from custom path: {customDllPath}");
                    }
                }

                // If custom loading failed or wasn't attempted, try the default location
                if (_dllHandle == IntPtr.Zero)
                {
                    _dllHandle = LoadLibrary(DefaultDllName);

                    if (_dllHandle == IntPtr.Zero)
                    {
                        int error = Marshal.GetLastWin32Error();
                        Logger.Log(LogLevel.Error, "VoiceMeeterApi",
                            $"Failed to load VoicemeeterRemote64.dll from default location, Error: {error}");
                        return false;
                    }

                    Logger.Log(LogLevel.Information, "VoiceMeeterApi",
                        "Loaded VoicemeeterRemote64.dll from default location");
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
                    Logger.Log(LogLevel.Error, "VoiceMeeterApi", "Failed to load one or more VoiceMeeter functions");
                    Cleanup();
                    return false;
                }

                // Login to VoiceMeeter
                int result = _loginFunc();
                _initialized = (result >= 0);

                if (!_initialized)
                {
                    Logger.Log(LogLevel.Error, "VoiceMeeterApi", $"Failed to initialize VoiceMeeter API. Error code: {result}");
                    Cleanup();
                }
                else
                {
                    Logger.Log(LogLevel.Information, "VoiceMeeterApi", "VoiceMeeter API initialized successfully");
                }

                return _initialized;
            }
            catch (DllNotFoundException ex)
            {
                Logger.Log(LogLevel.Critical, "VoiceMeeterApi", $"VoicemeeterRemote64.dll not found: {ex.Message}");
                Cleanup();
                return false;
            }
            catch (EntryPointNotFoundException ex)
            {
                Logger.Log(LogLevel.Critical, "VoiceMeeterApi", $"Entry point not found in VoicemeeterRemote64.dll: {ex.Message}");
                Cleanup();
                return false;
            }
            catch (BadImageFormatException ex)
            {
                Logger.Log(LogLevel.Critical, "VoiceMeeterApi", $"VoicemeeterRemote64.dll is not a valid image (wrong bitness or corrupted): {ex.Message}");
                Cleanup();
                return false;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "VoiceMeeterApi", $"Unexpected error initializing VoiceMeeter API: {ex.GetType().Name} - {ex.Message}");
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
                Logger.Log(LogLevel.Error, "VoiceMeeterApi", $"Function {functionName} not found in VoicemeeterRemote64.dll");
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
                    Logger.Log(LogLevel.Error, "VoiceMeeterApi", $"Failed to run VoiceMeeter type {type}. Error code: {result}");
                    return false;
                }

                Logger.Log(LogLevel.Information, "VoiceMeeterApi", $"Successfully started VoiceMeeter type {type}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "VoiceMeeterApi", $"Error running VoiceMeeter type {type}: {ex.Message}");
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
                    Logger.Log(LogLevel.Error, "VoiceMeeterApi", $"Failed to get VoiceMeeter type. Error code: {result}");
                    return VoiceMeeterType.NotInstalled;
                }

                Logger.Log(LogLevel.Debug, "VoiceMeeterApi", $"Detected VoiceMeeter type: {(VoiceMeeterType)type}");
                return (VoiceMeeterType)type;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "VoiceMeeterApi", $"Error getting VoiceMeeter type: {ex.Message}");
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
                    Logger.Log(LogLevel.Warning, "VoiceMeeterApi", $"Failed to get string parameter '{paramName}'. Error code: {result}");
                    return null;
                }

                string value = Marshal.PtrToStringAnsi(buffer);
                return value;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "VoiceMeeterApi", $"Error getting string parameter '{paramName}': {ex.Message}");
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
                    Logger.Log(LogLevel.Warning, "VoiceMeeterApi", $"Failed to set string parameter '{paramName}' to '{value}'. Error code: {result}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "VoiceMeeterApi", $"Error setting string parameter '{paramName}' to '{value}': {ex.Message}");
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
                    Logger.Log(LogLevel.Warning, "VoiceMeeterApi", $"Failed to get parameter '{paramName}'. Error code: {result}");
                    return 0.0f;
                }

                return value;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "VoiceMeeterApi", $"Error getting parameter '{paramName}': {ex.Message}");
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
                    Logger.Log(LogLevel.Warning, "VoiceMeeterApi", $"Failed to set parameter '{paramName}' to {value}. Error code: {result}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "VoiceMeeterApi", $"Error setting parameter '{paramName}' to {value}: {ex.Message}");
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
                Logger.Log(LogLevel.Debug, "VoiceMeeterApi", $"GetStripGain for {stripName}: {gain} dB");
                return gain;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "VoiceMeeterApi", $"Error getting gain for strip {stripName}: {ex.Message}");
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
                    Logger.Log(LogLevel.Debug, "VoiceMeeterApi", $"SetStripGain for {stripName}: {gain} dB");
                }
                else
                {
                    Logger.Log(LogLevel.Warning, "VoiceMeeterApi", $"Failed to set gain for strip {stripName} to {gain} dB");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "VoiceMeeterApi", $"Error setting gain for strip {stripName}: {ex.Message}");
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
                Logger.Log(LogLevel.Debug, "VoiceMeeterApi", $"GetStripMute for {stripName}: {(muted ? "Muted" : "Unmuted")}");
                return muted;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "VoiceMeeterApi", $"Error getting mute state for strip {stripName}: {ex.Message}");
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
                    Logger.Log(LogLevel.Debug, "VoiceMeeterApi", $"SetStripMute for {stripName}: {(mute ? "Muted" : "Unmuted")}");
                }
                else
                {
                    Logger.Log(LogLevel.Warning, "VoiceMeeterApi", $"Failed to set mute state for strip {stripName} to {(mute ? "Muted" : "Unmuted")}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "VoiceMeeterApi", $"Error setting mute state for strip {stripName}: {ex.Message}");
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
                    Logger.Log(LogLevel.Warning, "VoiceMeeterApi", $"Failed to check if parameters are dirty. Error code: {result}");
                    return false;
                }

                return result > 0;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "VoiceMeeterApi", $"Error checking if parameters are dirty: {ex.Message}");
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
                    Logger.Log(LogLevel.Warning, "VoiceMeeterApi", $"Failed to get level for type {type}, channel {channel}. Error code: {result}");
                    return 0.0f;
                }

                return level;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "VoiceMeeterApi", $"Error getting level for type {type}, channel {channel}: {ex.Message}");
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
                Logger.Log(LogLevel.Error, "VoiceMeeterApi", $"Error getting VoiceMeeter strips: {ex.Message}");
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
                Logger.Log(LogLevel.Error, "VoiceMeeterApi", $"Error getting VoiceMeeter buses: {ex.Message}");
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
            Logger.Log(LogLevel.Information, "VoiceMeeterApi", "Starting VoiceMeeter API diagnostics");

            bool success = true;

            try
            {
                // Check 1: DLL Loading
                string customDllPath = ServiceLocator.Model?.VoicemeeterDllPath;
                bool usingCustomDll = !string.IsNullOrEmpty(customDllPath) && File.Exists(customDllPath);

                if (usingCustomDll)
                {
                    Logger.Log(LogLevel.Information, "VoiceMeeterApi",
                        $"Using custom VoicemeeterRemote64.dll path: {customDllPath}");
                }
                else
                {
                    Logger.Log(LogLevel.Information, "VoiceMeeterApi",
                        "Using default VoicemeeterRemote64.dll path");
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
                        Logger.Log(LogLevel.Error, "VoiceMeeterApi",
                            $"Failed to load VoicemeeterRemote64.dll. Error code: {error}");
                        success = false;
                    }
                    else
                    {
                        Logger.Log(LogLevel.Information, "VoiceMeeterApi",
                            "VoicemeeterRemote64.dll loaded successfully");

                        // Check for a simple function to verify DLL validity
                        IntPtr loginFuncPtr = GetProcAddress(testDllHandle, "VBVMR_Login");
                        if (loginFuncPtr == IntPtr.Zero)
                        {
                            Logger.Log(LogLevel.Error, "VoiceMeeterApi",
                                "VBVMR_Login function not found in loaded DLL");
                            success = false;
                        }
                        else
                        {
                            Logger.Log(LogLevel.Information, "VoiceMeeterApi",
                                "VBVMR_Login function found in loaded DLL");
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

                    Logger.Log(LogLevel.Critical, "VoiceMeeterApi",
                        $"Exception loading VoicemeeterRemote64.dll: {ex.GetType().Name} - {ex.Message}");
                    success = false;
                }

                // Check 2: VoiceMeeter Installation
                try
                {
                    Logger.Log(LogLevel.Information, "VoiceMeeterApi", "Checking VoiceMeeter installation...");
                    if (Initialize())
                    {
                        VoiceMeeterType type = GetCurrentType();
                        Logger.Log(LogLevel.Information, "VoiceMeeterApi", $"VoiceMeeter is installed and running. Type: {type}");

                        // Log additional information about the VoiceMeeter installation
                        int inputCount = GetInputStripCount();
                        int virtualInputCount = GetVirtualInputCount();
                        int outputBusCount = GetOutputBusCount();

                        Logger.Log(LogLevel.Information, "VoiceMeeterApi",
                            $"VoiceMeeter configuration: {inputCount} hardware inputs, {virtualInputCount} virtual inputs, {outputBusCount} output buses");
                    }
                    else
                    {
                        Logger.Log(LogLevel.Error, "VoiceMeeterApi", "VoiceMeeter is not installed or not running");
                        success = false;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, "VoiceMeeterApi", $"Error checking VoiceMeeter installation: {ex.Message}");
                    success = false;
                }

                // Check 3: Available Strips and Buses
                try
                {
                    if (Initialize())
                    {
                        Logger.Log(LogLevel.Information, "VoiceMeeterApi", "Checking available VoiceMeeter strips...");
                        var strips = GetAvailableStripsWithLabels();
                        if (strips.Count > 0)
                        {
                            Logger.Log(LogLevel.Information, "VoiceMeeterApi", $"Found {strips.Count} VoiceMeeter strips:");
                            foreach (var strip in strips)
                            {
                                Logger.Log(LogLevel.Information, "VoiceMeeterApi", $"  - {strip.Key}: {strip.Value}");

                                // Try to get and set parameters for this strip
                                try
                                {
                                    float gain = GetStripGain(strip.Key);
                                    bool mute = GetStripMute(strip.Key);
                                    Logger.Log(LogLevel.Information, "VoiceMeeterApi",
                                        $"    Current state: Gain={gain} dB, Mute={mute}");
                                }
                                catch (Exception ex)
                                {
                                    Logger.Log(LogLevel.Warning, "VoiceMeeterApi",
                                        $"    Error getting strip parameters: {ex.Message}");
                                }
                            }
                        }
                        else
                        {
                            Logger.Log(LogLevel.Warning, "VoiceMeeterApi", "No VoiceMeeter strips found");
                        }

                        Logger.Log(LogLevel.Information, "VoiceMeeterApi", "Checking available VoiceMeeter buses...");
                        var buses = GetAvailableBusesWithLabels();
                        if (buses.Count > 0)
                        {
                            Logger.Log(LogLevel.Information, "VoiceMeeterApi", $"Found {buses.Count} VoiceMeeter buses:");
                            foreach (var bus in buses)
                            {
                                Logger.Log(LogLevel.Information, "VoiceMeeterApi", $"  - {bus.Key}: {bus.Value}");

                                // Try to get and set parameters for this bus
                                try
                                {
                                    float gain = GetBusGain(bus.Key);
                                    bool mute = GetBusMute(bus.Key);
                                    Logger.Log(LogLevel.Information, "VoiceMeeterApi",
                                        $"    Current state: Gain={gain} dB, Mute={mute}");
                                }
                                catch (Exception ex)
                                {
                                    Logger.Log(LogLevel.Warning, "VoiceMeeterApi",
                                        $"    Error getting bus parameters: {ex.Message}");
                                }
                            }
                        }
                        else
                        {
                            Logger.Log(LogLevel.Warning, "VoiceMeeterApi", "No VoiceMeeter buses found");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, "VoiceMeeterApi", $"Error checking VoiceMeeter strips and buses: {ex.Message}");
                    success = false;
                }

                // Check 4: Parameter Dirty Flag
                try
                {
                    if (Initialize())
                    {
                        Logger.Log(LogLevel.Information, "VoiceMeeterApi", "Checking VoiceMeeter parameter dirty flag...");
                        bool isDirty = AreParametersDirty();
                        Logger.Log(LogLevel.Information, "VoiceMeeterApi", $"VoiceMeeter parameters dirty flag: {isDirty}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, "VoiceMeeterApi", $"Error checking VoiceMeeter parameter dirty flag: {ex.Message}");
                    success = false;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "VoiceMeeterApi", $"Unexpected error during diagnostics: {ex.Message}");
                success = false;
            }

            Logger.Log(LogLevel.Information, "VoiceMeeterApi", $"VoiceMeeter API diagnostics completed. Success: {success}");
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
                    Logger.Log(LogLevel.Information, "VoiceMeeterApi", "VoiceMeeter is not running. Attempting to start it...");

                    // Try to run VoiceMeeter
                    int type = (int)GetCurrentType();

                    if (type <= 0)
                    {
                        Logger.Log(LogLevel.Error, "VoiceMeeterApi", $"Cannot start VoiceMeeter: Invalid type ({type}). VoiceMeeter may not be installed.");
                        return false;
                    }

                    Logger.Log(LogLevel.Information, "VoiceMeeterApi", $"Starting VoiceMeeter type: {(VoiceMeeterType)type}");

                    bool runResult = RunVoiceMeeter(type);
                    if (!runResult)
                    {
                        Logger.Log(LogLevel.Error, "VoiceMeeterApi", "Failed to run VoiceMeeter");
                        return false;
                    }

                    Logger.Log(LogLevel.Information, "VoiceMeeterApi", "VoiceMeeter started. Initializing API...");

                    bool initResult = Initialize();
                    if (!initResult)
                    {
                        Logger.Log(LogLevel.Error, "VoiceMeeterApi", "Failed to initialize VoiceMeeter API after starting VoiceMeeter");
                        return false;
                    }

                    Logger.Log(LogLevel.Information, "VoiceMeeterApi", "VoiceMeeter API initialized successfully after starting VoiceMeeter");
                    return true;
                }

                Logger.Log(LogLevel.Debug, "VoiceMeeterApi", "VoiceMeeter is already running");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "VoiceMeeterApi", $"Error ensuring VoiceMeeter is running: {ex.Message}");
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
                Logger.Log(LogLevel.Debug, "VoiceMeeterApi", $"GetBusGain for {busName}: {gain} dB");
                return gain;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "VoiceMeeterApi", $"Error getting gain for bus {busName}: {ex.Message}");
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
                    Logger.Log(LogLevel.Debug, "VoiceMeeterApi", $"SetBusGain for {busName}: {gain} dB");
                }
                else
                {
                    Logger.Log(LogLevel.Warning, "VoiceMeeterApi", $"Failed to set gain for bus {busName} to {gain} dB");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "VoiceMeeterApi", $"Error setting gain for bus {busName}: {ex.Message}");
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
                Logger.Log(LogLevel.Debug, "VoiceMeeterApi", $"GetBusMute for {busName}: {(muted ? "Muted" : "Unmuted")}");
                return muted;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "VoiceMeeterApi", $"Error getting mute state for bus {busName}: {ex.Message}");
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
                    Logger.Log(LogLevel.Debug, "VoiceMeeterApi", $"SetBusMute for {busName}: {(mute ? "Muted" : "Unmuted")}");
                }
                else
                {
                    Logger.Log(LogLevel.Warning, "VoiceMeeterApi", $"Failed to set mute state for bus {busName} to {(mute ? "Muted" : "Unmuted")}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "VoiceMeeterApi", $"Error setting mute state for bus {busName}: {ex.Message}");
            }
        }
    }
}