using CFIT.AppLogger;
using CFIT.AppTools;
using CoreAudio;
using Prosim2GSX.AppConfig;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Prosim2GSX.Audio
{
    public class DeviceManager(AudioController controller)
    {
        protected virtual AudioController Controller { get; } = controller;
        protected virtual Config Config => Controller.Config;

        // CoreAudio's MMDeviceEnumerator inherits the apartment of the thread
        // that constructs it. AudioController is built during AppService init
        // on the WPF STA UI thread, so a plain `new MMDeviceEnumerator(...)`
        // would bind every subsequent CoreAudio call (EnumerateAudioEndPoints,
        // device.AudioSessionManager2.Sessions, every SimpleAudioVolume write)
        // to the STA thread. Background-thread callers then marshal each call
        // back to the dispatcher, which stalls whenever the Audio Settings
        // tab is busy painting and starves the volume worker.
        //
        // Force creation on an MTA thread-pool thread so the enumerator and
        // every MMDevice it returns are free-threaded; CoreAudio calls from
        // the audio service no longer depend on UI thread availability.
        protected virtual MMDeviceEnumerator DeviceEnumerator { get; } =
            System.Threading.Tasks.Task.Run(() => new MMDeviceEnumerator(Guid.NewGuid())).GetAwaiter().GetResult();
        public virtual ConcurrentDictionary<string, MMDevice> Devices { get; } = [];
        protected virtual DateTime LastDeviceScan { get; set; } = DateTime.MinValue;
        protected virtual int LastDeviceCount { get; set; } = 0;
        protected virtual int SessionCount => Devices.Sum(d => d.Value.AudioSessionManager2.Sessions.Count);
        protected virtual int LastSessionCount { get; set; } = 0;

        public event Action DevicesChanged;

        protected virtual void Add(Dictionary<string, MMDevice> devices)
        {
            foreach (var device in devices)
                Devices.Add(device.Key, device.Value);
        }

        public virtual void Clear()
        {
            Devices.Clear();
        }

        public virtual bool Scan(bool force)
        {
            bool result = false;
            bool countsChanged = false;

            try
            {
                if (force || DateTime.Now >= LastDeviceScan + TimeSpan.FromMilliseconds(Config.AudioDeviceCheckInterval))
                {
                    Logger.Debug($"Scanning Audio Devices");
                    var deviceList = EnumerateDevices(out int sessionCount);

                    countsChanged = LastDeviceCount != deviceList.Count || LastSessionCount != sessionCount;
                    if (countsChanged || force)
                    {
                        Logger.Debug($"Device Enumeration needed - DeviceCount {LastDeviceCount != deviceList.Count} | SessionCount {LastSessionCount != sessionCount}");
                        result = true;
                        Clear();
                        Add(deviceList);
                    }

                    LastSessionCount = SessionCount;
                    LastDeviceCount = Devices.Count;
                    LastDeviceScan = DateTime.Now;
                }

                // Only fire the public event when the actual device/session
                // topology changed. Forced rescans (e.g. mapping reset) still
                // return true to the caller so the audio service rebuilds
                // its session controls, but raising DevicesChanged on every
                // forced rescan was hammering the WPF AudioDevices binding
                // and snapping user combo edits back to source.
                if (countsChanged)
                    DevicesChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return result;
        }

        protected virtual Dictionary<string, MMDevice> EnumerateDevices(out int sessionCount)
        {
            Dictionary<string, MMDevice> devices = [];
            sessionCount = 0;
            MMDeviceCollection deviceList = null;
            try
            {
                deviceList = DeviceEnumerator.EnumerateAudioEndPoints(Config.AudioDeviceFlow, Config.AudioDeviceState);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            if (deviceList == null)
                return devices;

            foreach (var device in deviceList)
            {
                string deviceName;
                try { deviceName = device.DeviceFriendlyName; }
                catch (Exception ex) { Logger.LogException(ex); continue; }

                if (Config.AudioDeviceBlacklist.Where(d => d.StartsWith(deviceName, StringComparison.InvariantCultureIgnoreCase)).Any())
                {
                    Logger.Debug($"Ignoring Device '{deviceName}' (on Blacklist)");
                    continue;
                }

                // Sessions access (count + enumerate) goes via CoreAudio COM and
                // can throw transiently on a degraded audio stack. Wrap it so
                // one bad device doesn't abort the whole scan or get permanently
                // blacklisted on a transient hiccup — keep the device, just
                // skip its session count contribution this tick.
                int deviceSessionCount = 0;
                try
                {
                    if (Config.LogLevel == LogLevel.Verbose)
                    {
                        Logger.Verbose($"Testing Sessions on '{deviceName}'");
                        foreach (var session in device.AudioSessionManager2.Sessions)
                            Logger.Verbose($"Name: {session.DisplayName} | ID: {session.ProcessID} | SessionInstance: {session.SessionInstanceIdentifier}");
                    }
                    deviceSessionCount = device.AudioSessionManager2.Sessions.Count;
                }
                catch (Exception ex)
                {
                    Logger.Verbose($"Session enumeration failed for '{deviceName}': {ex.GetType().Name} - {ex.Message}");
                }
                sessionCount += deviceSessionCount;

                try { devices.Add(deviceName, device); }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                    Logger.Debug($"Ignoring Device '{deviceName}' (Add failed)");
                    Config.AudioDeviceBlacklist.Add(deviceName);
                }
            }

            return devices;
        }

        public virtual List<string> GetDeviceNames()
        {
            List<string> devices = [];
            MMDeviceCollection deviceList = null;
            try
            {
                deviceList = DeviceEnumerator.EnumerateAudioEndPoints(Config.AudioDeviceFlow, Config.AudioDeviceState);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            if (deviceList == null)
                return devices;

            foreach (var device in deviceList)
            {
                try
                {
                    string deviceName = device.DeviceFriendlyName;

                    // The session-iteration block is purely diagnostic (verbose
                    // logging only). Each Session.DisplayName / ProcessID read
                    // is a CoreAudio COM call and the Sessions enumeration
                    // creates RCWs that don't get disposed. Skipping this when
                    // logging is below Verbose makes GetDeviceNames cheap and
                    // avoids the audio stack degradation that used to wedge
                    // the Audio Settings tab after a few visits.
                    if (Config.LogLevel == LogLevel.Verbose)
                    {
                        Logger.Verbose($"Testing Sessions on '{deviceName}'");
                        foreach (var session in device.AudioSessionManager2.Sessions)
                            Logger.Verbose($"Name: {session.DisplayName} | ID: {session.ProcessID}");
                    }

                    devices.Add(deviceName);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                    Logger.Debug($"Device '{device.DeviceFriendlyName}' raised an Exception");
                }
            }

            return devices;
        }

        public virtual List<AudioSessionControl2> GetAudioSessions(AudioSession audioSession)
        {
            List<AudioSessionControl2> list = [];
            bool allDevices = string.IsNullOrWhiteSpace(audioSession.Device);
            string binaryMatch = $"{audioSession.Binary}.exe";
            bool onlyActive = audioSession.Mapping.OnlyActive;

            try
            {
                foreach (var device in Devices.Values)
                {
                    if (!allDevices && !device.DeviceFriendlyName.Equals(audioSession.Device, StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    // Iterate explicitly: a single property-throw on one
                    // session (ProcessID / SessionInstanceIdentifier / State
                    // can each fail independently when the audio stack is
                    // degraded) must not abort matching of its siblings on
                    // the same device.
                    int matchedThisDevice = 0;
                    IEnumerable<AudioSessionControl2> sessions;
                    try { sessions = device.AudioSessionManager2.Sessions; }
                    catch (Exception ex)
                    {
                        Logger.Verbose($"Sessions access failed on '{device.DeviceFriendlyName}': {ex.GetType().Name}");
                        continue;
                    }
                    if (sessions == null) continue;

                    foreach (var s in sessions)
                    {
                        bool match = false;
                        try
                        {
                            bool pidMatch = s.ProcessID == audioSession.ProcessId;
                            bool nameMatch = !pidMatch && (s.SessionInstanceIdentifier?.Contains(binaryMatch, StringComparison.InvariantCultureIgnoreCase) ?? false);
                            if (!pidMatch && !nameMatch) continue;

                            if (onlyActive && s.State != AudioSessionState.AudioSessionStateActive)
                                continue;

                            match = true;
                        }
                        catch (Exception ex)
                        {
                            Logger.Verbose($"Session property access failed on '{device.DeviceFriendlyName}': {ex.GetType().Name}");
                        }

                        if (match)
                        {
                            list.Add(s);
                            matchedThisDevice++;
                        }
                    }

                    if (matchedThisDevice > 0 && Config.LogLevel == LogLevel.Verbose)
                        Logger.Verbose($"Found {matchedThisDevice} Sessions on Device '{device.DeviceFriendlyName}'");
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            return list;
        }

        public virtual void WriteDebugInformation()
        {
            try
            {
                StringBuilder debugInfo = new();
                
                try
                {
                    debugInfo.AppendLine($"Configured Audio Mappings: {Config.AudioMappings.Count}");
                    int i = 0;
                    foreach (var mapping in Config.AudioMappings)
                        debugInfo.AppendLine($"\tMapping #{i++} - {mapping}");
                }
                catch (Exception ex)
                {
                    debugInfo.AppendLine($"Mapping Enumeration raised Exception: '{ex.GetType()}' - '{ex.Message}' - '{ex.TargetSite}' - {ex.StackTrace}");
                }

                try
                {
                    debugInfo.AppendLine("");
                    debugInfo.AppendLine("Process Enumeration ...");
                    int i = 0;
                    foreach (var mapping in Config.AudioMappings)
                    {
                        var proc = Sys.GetProcess(mapping.Binary);
                        var procId = proc?.Id ?? 0;
                        var procRunning = proc?.ProcessName == mapping.Binary;
                        debugInfo.AppendLine($"\tProcess for Mapping #{i++} - Binary '{mapping.Binary}' (Running: {procRunning} | ID: {procId})");
                    }
                }
                catch (Exception ex)
                {
                    debugInfo.AppendLine($"Process Enumeration raised Exception: '{ex.GetType()}' - '{ex.Message}' - '{ex.TargetSite}' - {ex.StackTrace}");
                }

                MMDeviceCollection deviceList = null;
                try
                {
                    debugInfo.AppendLine("");
                    deviceList = DeviceEnumerator.EnumerateAudioEndPoints(Config.AudioDeviceFlow, Config.AudioDeviceState);
                    debugInfo.AppendLine($"EnumerateAudioEndPoints(): Enumerated {deviceList.Count} Audio Devices (Flow: {Config.AudioDeviceFlow} | State: {Config.AudioDeviceState}).");
                }
                catch (Exception ex)
                {
                    debugInfo.AppendLine($"Device Enumeration raised Exception: '{ex.GetType()}' - '{ex.Message}' - '{ex.TargetSite}' - {ex.StackTrace}");
                }
                if (deviceList == null)
                    return;

                foreach (var device in deviceList)
                {
                    try
                    {
                        debugInfo.AppendLine($"Scanning Device '{device.DeviceFriendlyName}' (Sessions: {device?.AudioSessionManager2?.Sessions?.Count} | Blacklisted: {Config.AudioDeviceBlacklist.Where(d => d.StartsWith(device.DeviceFriendlyName, StringComparison.InvariantCultureIgnoreCase)).Any()})");
                        int i = 1;
                        foreach (var session in device.AudioSessionManager2.Sessions)
                            debugInfo.AppendLine($"\tSession #{i++} - Name: {session.DisplayName} | ID: {session.ProcessID} | State: {session.State} | SessionInstance: {session.SessionInstanceIdentifier}");
                    }
                    catch (Exception ex)
                    {
                        debugInfo.AppendLine($"Device raised Exception: '{ex.GetType()}' - '{ex.Message}' - '{ex.TargetSite}' - {ex.StackTrace}");
                    }
                }
            
                File.WriteAllText(Config.AudioDebugFile, debugInfo.ToString());
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }
    }
}
