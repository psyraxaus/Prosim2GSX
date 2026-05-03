using CFIT.AppLogger;
using CoreAudio;
using Prosim2GSX.AppConfig;
using ProsimInterface;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Prosim2GSX.Audio
{
    public class AudioSession(AudioController controller, AudioMapping mapping)
    {
        protected virtual AudioController Controller { get; } = controller;
        protected virtual SessionManager Manager => Controller.SessionManager;
        public virtual AudioMapping Mapping { get; } = mapping;
        public virtual AudioChannel Channel => Mapping.Channel;
        public virtual string Device => Mapping.Device;
        public virtual string Binary => Mapping.Binary;
        public virtual bool UseLatch => Mapping.UseLatch;
        public virtual bool OnlyActive => Mapping.OnlyActive;
        public virtual uint ProcessId { get; protected set; } = 0;
        public virtual int ProcessCount { get; protected set; } = 0;
        public virtual bool IsActive => ProcessId > 0 && Controller.HasInitialized && Controller.IsExecutionAllowed;
        public virtual bool IsRunning => Manager?.ProcessList?.Any(p => p.ProcessName.Equals(Binary, StringComparison.InvariantCultureIgnoreCase)) == true;
        public virtual int SearchCounter { get; set; } = 0;
        public virtual ConcurrentDictionary<string, float> SavedVolumes { get; } = [];
        public virtual ConcurrentDictionary<string, bool> SavedMutes { get; } = [];
        public virtual ConcurrentDictionary<string, bool> SynchedSessionsVolume { get; } = [];
        public virtual ConcurrentDictionary<string, bool> SynchedSessionsMute { get; } = [];
        public virtual List<AudioSessionControl2> SessionControls { get; } = [];

        // Cached current values from ProSim. Subscribe handlers feed these and
        // forward to ApplyVolume/ApplyMute so a fresh SetSessionList can reseed
        // newly discovered AudioSessionControl2s without waiting for the next
        // knob movement.
        protected virtual float CurrentVolume { get; set; } = 1f;
        protected virtual bool CurrentMute { get; set; } = false;

        // Coalescing write pipeline. ProSim SDK callbacks fire on the SDK
        // polling thread shared by every dataref subscription in the app.
        // CoreAudio SimpleAudioVolume.MasterVolume/Mute writes can take tens
        // of ms (and seconds when an elevated process holds a session on the
        // same device), so writing inline would stall every other dataref
        // update. Callbacks now record the latest value under a lock and a
        // single per-session worker drains them on the thread pool. Rapid
        // knob movement collapses to the most recent pending value.
        private float? _pendingVolume;
        private bool? _pendingMute;
        private bool _writerActive;
        private readonly object _writeLock = new();

        protected virtual Action<string, dynamic, dynamic> VolumeHandler { get; set; }
        protected virtual Action<string, dynamic, dynamic> MuteHandler { get; set; }
        protected virtual AcpSide SubscribedSide { get; set; }

        public override string ToString()
        {
            return $"{Channel}: '{Binary}' @ '{(string.IsNullOrWhiteSpace(Device) ? "all" : Device)}'";
        }

        public virtual int CheckProcess(bool force = false)
        {
            try
            {
                bool running = IsRunning;
                int result = 0;

                if (!running && ProcessId != 0 || force)
                {
                    if (!force)
                        Logger.Debug($"Binary '{Binary}' stopped");
                    else
                        Logger.Verbose($"Binary '{Binary}' stopped");
                    ClearSimSubscriptions();
                    ProcessId = 0;
                    SessionControls.Clear();
                    result = -1;
                }

                if (running && ProcessId == 0)
                {
                    ProcessId = (uint)Manager.ProcessList.Where(p => p.ProcessName.Equals(Binary, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault().Id;
                    if (ProcessId != 0)
                    {
                        Logger.Debug($"Binary '{Binary}' started (PID: {ProcessId})");
                        SetSimSubscriptions();
                        result = 1;
                    }
                }
                else if (running && ProcessId > 0)
                {
                    int count = Manager?.ProcessList?.Where(p => p.ProcessName.Equals(Binary, StringComparison.InvariantCultureIgnoreCase)).Count() ?? 0;
                    if (ProcessCount != count)
                    {
                        result = 1;
                        Logger.Debug($"Process Count changed");
                    }
                    ProcessCount = count;
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return 0;
        }

        public virtual void RestoreVolumes()
        {
            var savedVolumes = SavedVolumes.ToArray();
            foreach (var ident in savedVolumes)
            {
                var query = SessionControls.Where(s => s.SessionInstanceIdentifier == ident.Key);
                if (query.Any())
                {
                    Logger.Debug($"Restore Volume for Instance '{ident.Key}' to {ident.Value} (AudioSession {this})");
                    try
                    {
                        query.First().SimpleAudioVolume.MasterVolume = ident.Value;
                        SavedVolumes.TryRemove(ident.Key, out _);
                    }
                    catch { }
                }
            }
            SavedVolumes.Clear();

            var savedMutes = SavedMutes.ToArray();
            foreach (var ident in savedMutes)
            {
                var query = SessionControls.Where(s => s.SessionInstanceIdentifier == ident.Key);
                if (query.Any())
                {
                    Logger.Debug($"Restore Mute for Instance '{ident.Key}' to {ident.Value} (AudioSession {this})");
                    try
                    {
                        query.First().SimpleAudioVolume.Mute = ident.Value;
                        SavedMutes.TryRemove(ident.Key, out _);
                    }
                    catch { }
                }
            }
            SavedMutes.Clear();

            SynchedSessionsVolume.Clear();
            SynchedSessionsMute.Clear();
        }

        public virtual void SetSessionList(List<AudioSessionControl2> list)
        {
            SessionControls.Clear();
            SearchCounter = 0;

            foreach (var item in list)
            {
                SavedVolumes.TryAdd(item.SessionInstanceIdentifier, item.SimpleAudioVolume.MasterVolume);
                SavedMutes.TryAdd(item.SessionInstanceIdentifier, item.SimpleAudioVolume.Mute);
                SessionControls.Add(item);
            }
        }

        public virtual void SetSimSubscriptions()
        {
            var audio = Controller.AudioInterface;
            if (audio == null)
            {
                Logger.Warning($"AudioInterface not available — skipping subscriptions for {this}");
                return;
            }

            ClearSimSubscriptions();

            string channelName = Channel.ToString();
            int side = (int)Controller.Config.AudioAcpSide;
            SubscribedSide = Controller.Config.AudioAcpSide;

            VolumeHandler = audio.SubscribeToVolume(channelName, side, OnVolumeValue);
            MuteHandler = audio.SubscribeToMute(channelName, side, OnMuteValue);

            // Seed current state immediately so newly discovered sessions match
            // the live knob position without waiting for the next movement.
            try
            {
                CurrentVolume = NormaliseVolume(audio.ReadVolume(channelName, side));
                CurrentMute = UseLatch && !audio.ReadMute(channelName, side);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Seeding initial ACP state for {this}");
            }
        }

        public virtual void ClearSimSubscriptions()
        {
            var audio = Controller.AudioInterface;
            if (audio == null)
            {
                VolumeHandler = null;
                MuteHandler = null;
                return;
            }

            string channelName = Channel.ToString();
            int side = (int)SubscribedSide;
            try { if (VolumeHandler != null) audio.UnsubscribeVolume(channelName, side, VolumeHandler); } catch { }
            try { if (MuteHandler != null) audio.UnsubscribeMute(channelName, side, MuteHandler); } catch { }
            VolumeHandler = null;
            MuteHandler = null;
        }

        public virtual void SynchControls()
        {
            // Re-seed CurrentVolume/CurrentMute from ProSim and push them to
            // every SessionControl so newly discovered controls match the live
            // knob position without needing the user to move the knob.
            var audio = Controller.AudioInterface;
            if (audio != null)
            {
                try
                {
                    string channelName = Channel.ToString();
                    int side = (int)Controller.Config.AudioAcpSide;
                    CurrentVolume = NormaliseVolume(audio.ReadVolume(channelName, side));
                    CurrentMute = UseLatch && !audio.ReadMute(channelName, side);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, $"SynchControls read for {this}");
                }
            }

            // Route through the same worker as live callbacks so the
            // AudioController.DoRun thread is not held up by a slow
            // CoreAudio write either.
            bool kick;
            lock (_writeLock)
            {
                _pendingVolume = CurrentVolume;
                if (UseLatch) _pendingMute = CurrentMute;
                kick = !_writerActive;
                if (kick) _writerActive = true;
            }
            if (kick) _ = Task.Run(DrainAsync);
        }

        protected virtual float NormaliseVolume(float raw)
        {
            float v = raw / (float)ProsimAudioInterface.VolumeMax;
            if (v < 0f) v = 0f;
            if (v > 1f) v = 1f;
            return v;
        }

        protected virtual void OnVolumeValue(float raw)
        {
            float value = NormaliseVolume(raw);
            CurrentVolume = value;
            if (!IsActive || SessionControls.Count == 0) return;

            bool kick;
            lock (_writeLock)
            {
                _pendingVolume = value;
                kick = !_writerActive;
                if (kick) _writerActive = true;
            }
            if (kick) _ = Task.Run(DrainAsync);
        }

        protected virtual void OnMuteValue(bool unmuted)
        {
            if (!UseLatch) { CurrentMute = false; return; }
            bool mute = !unmuted;
            CurrentMute = mute;
            if (!IsActive || SessionControls.Count == 0) return;

            bool kick;
            lock (_writeLock)
            {
                _pendingMute = mute;
                kick = !_writerActive;
                if (kick) _writerActive = true;
            }
            if (kick) _ = Task.Run(DrainAsync);
        }

        // Drains the latest pending volume/mute pair off the SDK polling
        // thread. Producer-consumer with a single worker: any callback that
        // arrives while a write is in flight overwrites the pending field
        // (intermediate knob positions are dropped). The empty-pending check
        // and _writerActive flip happen under the same lock the producer
        // takes when setting pending, so a producer cannot lose its update.
        private async Task DrainAsync()
        {
            try
            {
                while (true)
                {
                    float? v;
                    bool? m;
                    lock (_writeLock)
                    {
                        v = _pendingVolume; _pendingVolume = null;
                        m = _pendingMute; _pendingMute = null;
                        if (v == null && m == null)
                        {
                            _writerActive = false;
                            return;
                        }
                    }

                    if (IsActive && SessionControls.Count > 0)
                    {
                        if (v.HasValue) ApplyVolume(v.Value);
                        if (m.HasValue) ApplyMute(m.Value);
                    }

                    await Task.Yield();
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                lock (_writeLock) { _writerActive = false; }
            }
        }

        // Live updates fire on actual dataref change — always write to every
        // session control. The SynchedSessionsVolume/Mute tracking is kept
        // for diagnostic visibility (and so RestoreVolumes can still clear
        // them), but is no longer used to gate writes the way the old
        // LVAR-poller flow needed.
        protected virtual void ApplyVolume(float value)
        {
            try
            {
                foreach (var ctrl in SessionControls)
                {
                    try
                    {
                        ctrl.SimpleAudioVolume.MasterVolume = value;
                        SynchedSessionsVolume.TryAdd(ctrl.SessionInstanceIdentifier, true);
                    }
                    catch (Exception ex)
                    {
                        Logger.Verbose($"ApplyVolume: session write skipped for {this}: {ex.GetType().Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual void ApplyMute(bool mute)
        {
            try
            {
                foreach (var ctrl in SessionControls)
                {
                    try
                    {
                        ctrl.SimpleAudioVolume.Mute = mute;
                        SynchedSessionsMute.TryAdd(ctrl.SessionInstanceIdentifier, true);
                    }
                    catch (Exception ex)
                    {
                        Logger.Verbose($"ApplyMute: session write skipped for {this}: {ex.GetType().Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }
    }
}
