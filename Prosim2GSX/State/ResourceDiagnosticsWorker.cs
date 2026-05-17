using CFIT.AppLogger;
using Prosim2GSX.AppConfig;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Prosim2GSX.State
{
    // Always-on resource telemetry. Previously this lived on AppWindow and was
    // started in OnWindowLoaded — which never runs when the app is used
    // headless (window never shown). That headless path is exactly the one
    // that hit the recurring ERROR_NOT_ENOUGH_QUOTA crash, so there was zero
    // diagnostic data for it. Owned by AppService instead, it runs for the
    // whole process lifetime regardless of window state.
    //
    // Logs USER / GDI / handle counts + the CFIT log-queue depth at DBG every
    // tick, and escalates to WARN (rising-edge, so it doesn't spam) when USER
    // objects or the log queue cross a threshold — a steady climb in USER
    // objects points to a leak in this process; a flat count during a
    // session-wide quota crash points elsewhere (often the sim). Also keeps a
    // last-resort trim on Logger.Messages in case the drain worker ever wedges.
    public class ResourceDiagnosticsWorker
    {
        private const int IntervalMs = 60_000;

        // The per-process USER object limit defaults to 10,000. Warn well
        // before that so a climb is visible with headroom to react.
        private const uint UserObjectWarnThreshold = 8_000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetGuiResources(IntPtr hProcess, uint uiFlags);
        private const uint GR_GDIOBJECTS = 0;
        private const uint GR_USEROBJECTS = 1;

        private readonly Config _config;
        private readonly Timer _timer;
        private volatile bool _isTicking;
        private bool _errorReported;
        private bool _userWarned;
        private bool _queueWarned;

        public ResourceDiagnosticsWorker(Config config)
        {
            _config = config;
            _timer = new Timer(OnTick, null, Timeout.Infinite, Timeout.Infinite);
        }

        public virtual void Start()
            // Fire one immediately for a startup baseline, then every IntervalMs.
            => _timer.Change(0, IntervalMs);

        public virtual void Stop()
            => _timer.Change(Timeout.Infinite, Timeout.Infinite);

        protected virtual void OnTick(object? state)
        {
            if (_isTicking) return;
            _isTicking = true;
            try
            {
                uint user = 0, gdi = 0;
                int handles = 0;
                try
                {
                    using var proc = Process.GetCurrentProcess();
                    user = GetGuiResources(proc.Handle, GR_USEROBJECTS);
                    gdi = GetGuiResources(proc.Handle, GR_GDIOBJECTS);
                    handles = proc.HandleCount;
                }
                catch (Exception ex) when (!_errorReported)
                {
                    _errorReported = true;
                    Logger.LogException(ex);
                }

                int queueDepth = Logger.Messages.Count;
                Logger.Debug($"Resource heartbeat: USER={user}, GDI={gdi}, Handles={handles}, LogQueue={queueDepth}");

                // USER-object escalation (rising-edge).
                if (user >= UserObjectWarnThreshold && !_userWarned)
                {
                    _userWarned = true;
                    Logger.Warning($"USER object count {user} ≥ {UserObjectWarnThreshold} — approaching the per-process limit; possible UI-resource leak");
                }
                else if (user < UserObjectWarnThreshold / 2)
                {
                    _userWarned = false;
                }

                // CFIT log-queue escalation + last-resort trim. The drain
                // worker normally keeps this near-empty; a sustained high
                // depth here means the drain wedged, so trim to keep memory
                // bounded and surface it loudly.
                int warnThreshold = Math.Max(1, _config?.UiLogQueueWarnThreshold ?? 1000);
                int hardCap = Math.Max(1, _config?.UiLogMaxMessages ?? 200);
                if (queueDepth >= warnThreshold)
                {
                    if (!_queueWarned)
                    {
                        _queueWarned = true;
                        Logger.Warning($"CFIT log queue depth {queueDepth} ≥ {warnThreshold} — drain worker may be stalled; trimming to {hardCap}");
                    }
                    while (Logger.Messages.Count > hardCap)
                        Logger.Messages.TryDequeue(out _);
                }
                else if (queueDepth < warnThreshold / 2)
                {
                    _queueWarned = false;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            finally
            {
                _isTicking = false;
            }
        }
    }
}
