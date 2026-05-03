using CFIT.AppLogger;
using CFIT.AppTools;
using Prosim2GSX.AppConfig;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Prosim2GSX.Audio
{
    public class SessionManager(AudioController controller)
    {
        protected virtual AudioController Controller { get; } = controller;
        protected virtual DeviceManager DeviceManager => Controller.DeviceManager;
        protected virtual Config Config => Controller.Config;
        protected virtual ConcurrentDictionary<AudioChannel, List<AudioSession>> MappedAudioSessions { get; } = [];
        public virtual bool HasEmptySearches => MappedAudioSessions.Any(c => c.Value.Any(s => s.SearchCounter > Config.AudioProcessMaxSearchCount));

        // CheckInactiveSessions iterates SessionControls[].State, which is a
        // CoreAudio COM call per session. Read twice per audio tick (DoRun
        // condition + log line) — cache so a degraded audio stack doesn't
        // multiply the call count.
        private static readonly TimeSpan InactiveCacheTtl = TimeSpan.FromSeconds(5);
        private bool _cachedInactive;
        private DateTime _inactiveCheckedAt = DateTime.MinValue;
        public virtual bool HasInactiveSessions
        {
            get
            {
                if (DateTime.UtcNow - _inactiveCheckedAt < InactiveCacheTtl)
                    return _cachedInactive;
                _cachedInactive = CheckInactiveSessions();
                _inactiveCheckedAt = DateTime.UtcNow;
                return _cachedInactive;
            }
        }

        public virtual List<Process> ProcessList { get; } = [];

        public virtual void RegisterMappings()
        {
            foreach (var mapping in Config.AudioMappings)
                RegisterMapping(mapping);
        }

        protected virtual void RegisterMapping(AudioMapping mapping)
        {
            if (!MappedAudioSessions.ContainsKey(mapping.Channel))
                MappedAudioSessions.Add(mapping.Channel, []);

            var session = new AudioSession(Controller, mapping);
            MappedAudioSessions[mapping.Channel].Add(session);
            Logger.Debug($"Registered AudioSession {session}");
        }

        public virtual void UnregisterMappings()
        {
            foreach (var channel in MappedAudioSessions)
                foreach (var session in channel.Value.ToList())
                    UnregisterMapping(session.Mapping);
        }

        protected virtual void UnregisterMapping(AudioMapping mapping)
        {
            if (!MappedAudioSessions.TryGetValue(mapping.Channel, out List<AudioSession>? sessionList))
                return;

            var list = sessionList.Where(s => s.Binary == mapping.Binary && s.Device == mapping.Device).ToList();
            foreach (var item in list)
            {
                try { item.RestoreVolumes(); } catch { }
                try { item.ClearSimSubscriptions(); } catch { }
                sessionList.Remove(item);
                Logger.Debug($"Removed AudioSession {item}");
            }
        }

        public virtual void Clear()
        {
            MappedAudioSessions.Clear();
            foreach (var p in ProcessList)
            {
                try { p.Dispose(); } catch { }
            }
            ProcessList.Clear();
        }

        protected virtual bool CheckInactiveSessions()
        {
            bool result;

            try
            {
                result = MappedAudioSessions.Any(c => c.Value.Any(s => s.Mapping.OnlyActive && s.SessionControls.Any(sc => sc.State != CoreAudio.AudioSessionState.AudioSessionStateActive)));
            }
            catch (Exception ex)
            {
                Logger.Warning($"'{ex.GetType().Name}' during Inactive Session Check");
                result = true;
            }

            return result;
        }

        public virtual bool CheckProcesses(bool force = false)
        {
            bool result = false;

            // Process objects each hold an OS handle that stays open until
            // the GC finalizer thread sweeps. Calling Clear() drops the
            // references but not the handles. Dispose the previous tick's
            // objects eagerly so we don't leak ~200–500 handles per tick
            // (which used to bring the system audio stack to its knees
            // after a few minutes of audio service runtime).
            foreach (var p in ProcessList)
            {
                try { p.Dispose(); } catch { }
            }
            ProcessList.Clear();
            ProcessList.AddRange(Process.GetProcesses());

            foreach (var channel in MappedAudioSessions)
                foreach (var session in channel.Value)
                    if (session.CheckProcess(force) != 0)
                        result = true;

            return result;
        }

        public virtual void RestoreVolumes()
        {
            foreach (var channel in MappedAudioSessions)
                foreach (var session in channel.Value)
                    session.RestoreVolumes();
        }

        public virtual void CheckSessions(bool force = false)
        {
            foreach (var channel in MappedAudioSessions)
            {
                foreach (var session in channel.Value)
                {
                    if (session.IsActive && (session.SessionControls.Count == 0 || force))
                    {
                        if (force)
                            Logger.Debug($"Query SessionControls for AudioSession {session}");
                        else
                            Logger.Verbose($"Query SessionControls for AudioSession {session}");
                        var sessions = DeviceManager.GetAudioSessions(session);
                        if (force || sessions.Count != session.SessionControls.Count)
                            session.SessionControls.Clear();

                        if (session.SessionControls.Count == 0 && sessions.Count != 0)
                        {
                            session.SetSessionList(sessions);
                            session.SynchControls();
                            Logger.Debug($"Added {sessions.Count} SessionControls to AudioSession {session}");
                        }
                        else if (session.SessionControls.Count == 0)
                            session.SearchCounter++;
                    }
                }
            }
        }

        public virtual void SynchControls()
        {
            foreach (var channel in MappedAudioSessions)
                foreach (var session in channel.Value)
                    session.SynchControls();
        }
    }
}