using CFIT.AppLogger;
using CoreAudio;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace Prosim2GSX.Audio
{
    public sealed class AudioSessionProcess
    {
        public string ProcessName { get; init; }
        public bool IsAccessible { get; init; }
    }

    // Snapshot of CoreAudio sessions currently visible to us, keyed by
    // ProcessName. Refreshed on the audio service tick via Refresh(); UI code
    // reads Snapshot directly off the cached list (no COM, no Process.GetProcesses).
    //
    // The dedupe-by-ProcessName step means multiple instances of the same
    // binary collapse to a single suggestion. IsAccessible mirrors the same
    // ProbeAccessible check AudioSession uses for elevated-process exclusion;
    // the dropdown annotates inaccessible entries with " — elevated".
    public class AudioSessionRegistry
    {
        private readonly AudioController _controller;
        private IReadOnlyList<AudioSessionProcess> _snapshot = Array.Empty<AudioSessionProcess>();

        public AudioSessionRegistry(AudioController controller)
        {
            _controller = controller;
        }

        public virtual IReadOnlyList<AudioSessionProcess> Snapshot => _snapshot;

        public virtual void Refresh()
        {
            var byName = new Dictionary<string, AudioSessionProcess>(StringComparer.InvariantCultureIgnoreCase);
            try
            {
                foreach (var device in _controller.DeviceManager.Devices.Values)
                {
                    IEnumerable<AudioSessionControl2> sessions;
                    try { sessions = device.AudioSessionManager2.Sessions; }
                    catch { continue; }
                    if (sessions == null) continue;

                    foreach (var s in sessions)
                    {
                        uint pid;
                        try { pid = s.ProcessID; }
                        catch { continue; }
                        if (pid == 0) continue;

                        string name;
                        bool accessible;
                        Process proc = null;
                        try
                        {
                            proc = Process.GetProcessById((int)pid);
                            name = proc.ProcessName;
                            accessible = ProbeAccessible(proc);
                        }
                        catch (ArgumentException) { continue; /* process exited */ }
                        catch (Exception ex)
                        {
                            Logger.Verbose($"AudioSessionRegistry: process lookup failed pid={pid}: {ex.GetType().Name}");
                            continue;
                        }
                        finally
                        {
                            try { proc?.Dispose(); } catch { }
                        }

                        if (string.IsNullOrEmpty(name)) continue;

                        // First entry wins; if any duplicate is more permissive
                        // (accessible) than what we have, prefer that one so a
                        // single elevated instance doesn't poison the suggestion.
                        if (!byName.TryGetValue(name, out var existing))
                            byName[name] = new AudioSessionProcess { ProcessName = name, IsAccessible = accessible };
                        else if (accessible && !existing.IsAccessible)
                            byName[name] = new AudioSessionProcess { ProcessName = name, IsAccessible = true };
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            var list = new List<AudioSessionProcess>(byName.Values);
            list.Sort((a, b) => string.Compare(a.ProcessName, b.ProcessName, StringComparison.InvariantCultureIgnoreCase));
            _snapshot = list;
        }

        public virtual void Clear() => _snapshot = Array.Empty<AudioSessionProcess>();

        // Same probe as AudioSession.ProbeAccessible — duplicated here rather
        // than reaching into AudioSession because the registry is process-level
        // (no per-mapping AudioSession exists for unmapped binaries).
        private static bool ProbeAccessible(Process proc)
        {
            try
            {
                _ = proc.MainModule?.FileName;
                return true;
            }
            catch (Win32Exception) { return false; }
            catch (InvalidOperationException) { return false; }
            catch { return false; }
        }
    }
}
