using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Prosim2GSX.Services.Audio
{
    public class VoiceMeeterApi
    {
        private const string DllName = "VoicemeeterRemote64.dll";

        [DllImport(DllName)]
        private static extern int VBVMR_Login();

        [DllImport(DllName)]
        private static extern int VBVMR_Logout();

        [DllImport(DllName)]
        private static extern int VBVMR_RunVoicemeeter(int voicemeeterId);

        [DllImport(DllName)]
        private static extern int VBVMR_GetVoicemeeterType(ref int type);

        [DllImport(DllName)]
        private static extern int VBVMR_GetParameterFloat(string paramName, ref float value);

        [DllImport(DllName)]
        private static extern int VBVMR_SetParameterFloat(string paramName, float value);

        [DllImport(DllName)]
        private static extern int VBVMR_GetParameterStringA(string paramName, IntPtr value);

        [DllImport(DllName)]
        private static extern int VBVMR_SetParameterStringA(string paramName, string value);

        [DllImport(DllName)]
        private static extern int VBVMR_IsParametersDirty();

        [DllImport(DllName)]
        private static extern int VBVMR_GetLevel(int type, int channel, ref float level);

        private bool _isLoggedIn = false;

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
            if (_isLoggedIn)
                return true;

            int result = VBVMR_Login();
            _isLoggedIn = (result >= 0);
            return _isLoggedIn;
        }

        /// <summary>
        /// Shutdown the VoiceMeeter Remote API
        /// </summary>
        public void Shutdown()
        {
            if (_isLoggedIn)
            {
                VBVMR_Logout();
                _isLoggedIn = false;
            }
        }

        /// <summary>
        /// Run VoiceMeeter
        /// </summary>
        /// <param name="type">VoiceMeeter type (1=Standard, 2=Banana, 3=Potato)</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool RunVoiceMeeter(int type)
        {
            return VBVMR_RunVoicemeeter(type) == 0;
        }

        /// <summary>
        /// Get the current VoiceMeeter type
        /// </summary>
        /// <returns>VoiceMeeter type</returns>
        public VoiceMeeterType GetCurrentType()
        {
            int type = 0;
            VBVMR_GetVoicemeeterType(ref type);
            return (VoiceMeeterType)type;
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
            IntPtr buffer = Marshal.AllocHGlobal(512);
            try
            {
                int result = VBVMR_GetParameterStringA(paramName, buffer);
                if (result >= 0)
                {
                    return Marshal.PtrToStringAnsi(buffer);
                }
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
            int result = VBVMR_SetParameterStringA(paramName, value);
            return result >= 0;
        }

        /// <summary>
        /// Get a float parameter from VoiceMeeter
        /// </summary>
        /// <param name="paramName">Parameter name</param>
        /// <returns>Parameter value</returns>
        public float GetFloatParameter(string paramName)
        {
            float value = 0.0f;
            VBVMR_GetParameterFloat(paramName, ref value);
            return value;
        }

        /// <summary>
        /// Set a float parameter in VoiceMeeter
        /// </summary>
        /// <param name="paramName">Parameter name</param>
        /// <param name="value">Parameter value</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool SetFloatParameter(string paramName, float value)
        {
            int result = VBVMR_SetParameterFloat(paramName, value);
            return result >= 0;
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
            return GetStripParameter(stripName, "Gain");
        }

        /// <summary>
        /// Set the gain for a strip
        /// </summary>
        /// <param name="stripName">Strip name (e.g., "Strip[0]")</param>
        /// <param name="gain">Gain value</param>
        /// <returns>True if successful, false otherwise</returns>
        public void SetStripGain(string stripName, float gain)
        {
            SetStripParameter(stripName, "Gain", gain);
        }

        /// <summary>
        /// Get the mute state for a strip
        /// </summary>
        /// <param name="stripName">Strip name (e.g., "Strip[0]")</param>
        /// <returns>True if muted, false otherwise</returns>
        public bool GetStripMute(string stripName)
        {
            return GetStripParameter(stripName, "Mute") > 0.5f;
        }

        /// <summary>
        /// Set the mute state for a strip
        /// </summary>
        /// <param name="stripName">Strip name (e.g., "Strip[0]")</param>
        /// <param name="mute">True to mute, false to unmute</param>
        /// <returns>True if successful, false otherwise</returns>
        public void SetStripMute(string stripName, bool mute)
        {
            SetStripParameter(stripName, "Mute", mute ? 1.0f : 0.0f);
        }

        /// <summary>
        /// Check if parameters have changed
        /// </summary>
        /// <returns>True if parameters have changed, false otherwise</returns>
        public bool AreParametersDirty()
        {
            return VBVMR_IsParametersDirty() > 0;
        }

        /// <summary>
        /// Get the level for a channel
        /// </summary>
        /// <param name="type">Level type (0=PreFader, 1=PostFader, 2=PostMute)</param>
        /// <param name="channel">Channel index</param>
        /// <returns>Level value</returns>
        public float GetLevel(int type, int channel)
        {
            float level = 0.0f;
            VBVMR_GetLevel(type, channel, ref level);
            return level;
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
        /// Run VoiceMeeter if it's not already running
        /// </summary>
        /// <returns>True if VoiceMeeter is running, false otherwise</returns>
        public bool EnsureVoiceMeeterIsRunning()
        {
            if (!IsVoiceMeeterRunning())
            {
                // Try to run VoiceMeeter
                int type = (int)GetCurrentType();
                if (type > 0)
                {
                    return RunVoiceMeeter(type) && Initialize();
                }
            }
            return IsVoiceMeeterRunning();
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
            return GetBusParameter(busName, "Gain");
        }

        /// <summary>
        /// Set the gain for a bus
        /// </summary>
        /// <param name="busName">Bus name (e.g., "Bus[0]")</param>
        /// <param name="gain">Gain value</param>
        /// <returns>True if successful, false otherwise</returns>
        public void SetBusGain(string busName, float gain)
        {
            SetBusParameter(busName, "Gain", gain);
        }

        /// <summary>
        /// Get the mute state for a bus
        /// </summary>
        /// <param name="busName">Bus name (e.g., "Bus[0]")</param>
        /// <returns>True if muted, false otherwise</returns>
        public bool GetBusMute(string busName)
        {
            return GetBusParameter(busName, "Mute") > 0.5f;
        }

        /// <summary>
        /// Set the mute state for a bus
        /// </summary>
        /// <param name="busName">Bus name (e.g., "Bus[0]")</param>
        /// <param name="mute">True to mute, false to unmute</param>
        /// <returns>True if successful, false otherwise</returns>
        public void SetBusMute(string busName, bool mute)
        {
            SetBusParameter(busName, "Mute", mute ? 1.0f : 0.0f);
        }
    }
}