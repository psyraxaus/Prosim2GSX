using CFIT.AppLogger;
using Prosim2GSX.AppConfig;
using Prosim2GSX.Diagnostics;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace Prosim2GSX.State
{
    // Always-on resource telemetry. Previously this lived on AppWindow and was
    // started in OnWindowLoaded — which never runs when the app is used
    // headless (window never shown). That headless path is exactly the one
    // that hit the recurring ERROR_NOT_ENOUGH_QUOTA crash, so there was zero
    // diagnostic data for it. Owned by AppService instead, it runs for the
    // whole process lifetime regardless of window state.
    //
    // Phase 6.5.B upgrade: heartbeat cadence changed from 1 minute to 5
    // minutes (30s under StressMode). Per-tick rows now go ONLY to the
    // dedicated ResourceDiagnosticsLog (CMTrace-format), keeping the main
    // CFIT app log focused on app behaviour. Rising-edge WARN escalations
    // still emit to both sinks because they're actionable and rare; the
    // StressMode startup marker and the "dispatcher hooks unavailable"
    // first-tick notice also go to the main log because they're one-shot
    // context the operator needs to interpret subsequent rows.
    //
    // The heartbeat row collects:
    //   - USER / GDI / handle / thread counts (process vitals)
    //   - CFIT log queue depth (drain-worker health)
    //   - WPF dispatcher posted / completed / pending counters
    //     (the immediate signal the ERROR_NOT_ENOUGH_QUOTA hypothesis needs)
    //   - Managed-heap size + per-generation GC collection deltas
    //     (allocation pressure correlated with the producer)
    //   - Thread-pool availability + pending work item count
    //     (Kestrel back-pressure indicator)
    //   - Web request total / delta / active count
    //     (directly tests the audit's web-controller hypothesis)
    //   - WebSocket connection count
    //     (context for the request/dispatcher correlation)
    //   - Process uptime
    //     (trajectory context — pending=4500 after 6h ≠ after 30s)
    public class ResourceDiagnosticsWorker
    {
        private const int NormalIntervalMs = 300_000;     // 5 minutes — default cadence
        private const int StressIntervalMs = 30_000;       // 30 seconds — StressMode cadence

        // The per-process USER object limit defaults to 10,000. Warn well
        // before that so a climb is visible with headroom to react.
        private const uint UserObjectWarnThreshold = 8_000;

        // The per-thread Win32 PostMessage budget is 10,000. Half of that
        // (5,000) is the rising-edge warn threshold; clear the warn state
        // when pending falls below 2,500 (hysteresis).
        private const long DispatcherPendingWarnHigh = 5_000;
        private const long DispatcherPendingWarnLow = 2_500;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetGuiResources(IntPtr hProcess, uint uiFlags);
        private const uint GR_GDIOBJECTS = 0;
        private const uint GR_USEROBJECTS = 1;

        private readonly AppService _app;
        private readonly Config _config;
        private readonly ResourceDiagnosticsLog _log;
        private readonly Timer _timer;
        private readonly int _intervalMs;
        private readonly bool _stressMode;
        private readonly DateTime _startedAtUtc = DateTime.UtcNow;
        private volatile bool _isTicking;
        private bool _errorReported;
        private bool _userWarned;
        private bool _queueWarned;
        private bool _dispatcherWarned;
        private bool _stressModeAnnounced;
        private bool _dispatcherHooksUnavailableAnnounced;

        // Dispatcher hook counters. Totals are process-lifetime cumulative
        // (only ever increment); per-tick deltas are computed by subtracting
        // the previous tick's snapshot. Pending count at any moment is
        // posted - completed.
        private long _dispatcherPostedTotal;
        private long _dispatcherCompletedTotal;
        private long _dispatcherPostedAtLastTick;
        private long _dispatcherCompletedAtLastTick;
        private bool _hooksSubscribed;

        // GC + web metric snapshots from the previous tick, used for delta
        // computation. Totals themselves are not stored — we re-read each
        // tick because GC.CollectionCount and WebRequestMetrics.* are
        // monotonic process-lifetime counters.
        private int _gen0AtLastTick;
        private int _gen1AtLastTick;
        private int _gen2AtLastTick;
        private long _webRequestsAtLastTick;

        public ResourceDiagnosticsWorker(AppService app, Config config, ResourceDiagnosticsLog log)
        {
            _app = app;
            _config = config;
            _log = log;
            _stressMode = config?.StressMode == true;
            _intervalMs = _stressMode ? StressIntervalMs : NormalIntervalMs;
            _timer = new Timer(OnTick, null, Timeout.Infinite, Timeout.Infinite);
        }

        public virtual void Start()
        {
            // Subscribe to DispatcherHooks as early as possible; the first
            // OnTick re-attempts if Application.Current.Dispatcher was not
            // yet available at this point.
            TrySubscribeDispatcherHooks();
            // Fire one immediately for a startup baseline, then every interval.
            _timer.Change(0, _intervalMs);
        }

        public virtual void Stop()
            => _timer.Change(Timeout.Infinite, Timeout.Infinite);

        private void TrySubscribeDispatcherHooks()
        {
            if (_hooksSubscribed) return;
            try
            {
                var dispatcher = Application.Current?.Dispatcher;
                if (dispatcher == null) return;
                dispatcher.Hooks.OperationPosted += OnDispatcherOperationPosted;
                dispatcher.Hooks.OperationCompleted += OnDispatcherOperationCompleted;
                _hooksSubscribed = true;
            }
            catch (Exception ex)
            {
                Logger.Warning($"ResourceDiagnosticsWorker: failed to subscribe DispatcherHooks — {ex.Message}");
            }
        }

        private void OnDispatcherOperationPosted(object sender, DispatcherHookEventArgs e)
            => Interlocked.Increment(ref _dispatcherPostedTotal);

        private void OnDispatcherOperationCompleted(object sender, DispatcherHookEventArgs e)
            => Interlocked.Increment(ref _dispatcherCompletedTotal);

        protected virtual void OnTick(object? state)
        {
            if (_isTicking) return;
            _isTicking = true;
            try
            {
                // StressMode marker — emit once (first tick) on both sinks.
                if (_stressMode && !_stressModeAnnounced)
                {
                    _stressModeAnnounced = true;
                    Logger.Information($"StressMode active: diagnostics heartbeat at {_intervalMs}ms, window forced hidden");
                    _log?.LogStressModeStart(_intervalMs);
                }

                // Re-attempt the DispatcherHooks subscription if we missed it
                // at Start(); announce unavailability once if still missing
                // on the first tick.
                if (!_hooksSubscribed)
                {
                    TrySubscribeDispatcherHooks();
                    if (!_hooksSubscribed && !_dispatcherHooksUnavailableAnnounced)
                    {
                        _dispatcherHooksUnavailableAnnounced = true;
                        Logger.Information("ResourceDiagnosticsWorker: WPF Dispatcher hooks unavailable — heartbeats will not include dispatcher counters this session");
                    }
                }

                uint user = 0, gdi = 0;
                int handles = 0;
                int threads = 0;
                try
                {
                    using var proc = Process.GetCurrentProcess();
                    user = GetGuiResources(proc.Handle, GR_USEROBJECTS);
                    gdi = GetGuiResources(proc.Handle, GR_GDIOBJECTS);
                    handles = proc.HandleCount;
                    threads = proc.Threads.Count;
                }
                catch (Exception ex) when (!_errorReported)
                {
                    _errorReported = true;
                    Logger.LogException(ex);
                }

                int queueDepth = Logger.Messages.Count;

                // Dispatcher deltas — atomic reads of the lifetime totals,
                // subtract last-tick snapshot to get the window delta. Pending
                // is the lifetime difference between posted and completed
                // (instantaneous queue depth signal).
                long postedTotal = Interlocked.Read(ref _dispatcherPostedTotal);
                long completedTotal = Interlocked.Read(ref _dispatcherCompletedTotal);
                long postedDelta = postedTotal - _dispatcherPostedAtLastTick;
                long completedDelta = completedTotal - _dispatcherCompletedAtLastTick;
                long pending = postedTotal - completedTotal;
                _dispatcherPostedAtLastTick = postedTotal;
                _dispatcherCompletedAtLastTick = completedTotal;

                // GC / managed memory — total managed bytes (no forced
                // collection) and per-generation collection deltas. A rising
                // Gen-2 delta correlated with rising DispatcherPending points
                // at delegate/closure churn from the producer.
                long managedBytes = 0;
                int gen0Delta = 0, gen1Delta = 0, gen2Delta = 0;
                try
                {
                    managedBytes = GC.GetTotalMemory(forceFullCollection: false);
                    int gen0 = GC.CollectionCount(0);
                    int gen1 = GC.CollectionCount(1);
                    int gen2 = GC.CollectionCount(2);
                    gen0Delta = gen0 - _gen0AtLastTick;
                    gen1Delta = gen1 - _gen1AtLastTick;
                    gen2Delta = gen2 - _gen2AtLastTick;
                    _gen0AtLastTick = gen0;
                    _gen1AtLastTick = gen1;
                    _gen2AtLastTick = gen2;
                }
                catch { /* GC reads are infallible in practice; guard for paranoia */ }

                // Thread-pool availability and pending work item count.
                // Kestrel borrows worker threads; if availability drops to
                // near zero and pending climbs, the pool is starved (often
                // upstream of the dispatcher saturation).
                int tpWorkerAvailable = 0, tpIoAvailable = 0;
                long tpPending = 0;
                try
                {
                    ThreadPool.GetAvailableThreads(out tpWorkerAvailable, out tpIoAvailable);
                    tpPending = ThreadPool.PendingWorkItemCount;
                }
                catch { }

                // Web request metrics — total since process start, delta for
                // this window, and active count (in-flight requests). The
                // delta directly tests the audit's "always-on producer at
                // process start" hypothesis.
                long webTotal = WebRequestMetrics.TotalRequests;
                long webActive = WebRequestMetrics.ActiveRequests;
                long webDelta = webTotal - _webRequestsAtLastTick;
                _webRequestsAtLastTick = webTotal;

                int wsConnections = 0;
                try { wsConnections = _app?.WebSocketHandler?.ConnectionCount ?? 0; }
                catch { }

                double uptimeSec = (DateTime.UtcNow - _startedAtUtc).TotalSeconds;

                // Per-tick row goes to the dedicated CMTrace-formatted
                // ResourceDiagnosticsLog only — the main CFIT app log stays
                // focused on app behaviour. WARN escalations below still hit
                // both sinks so the user sees them in the main log they
                // already monitor.
                _log?.LogHeartbeat(
                    user, gdi, handles, threads, queueDepth,
                    postedDelta, completedDelta, pending,
                    managedBytes, gen0Delta, gen1Delta, gen2Delta,
                    tpWorkerAvailable, tpIoAvailable, tpPending,
                    webTotal, webDelta, webActive,
                    wsConnections, uptimeSec);

                // Top-N attributed posters via UiMarshal.SnapshotTop. The
                // snapshot is atomic — counters reset for the next window at
                // the same instant they're read.
                try
                {
                    var top = UiMarshal.SnapshotTop(5);
                    _log?.LogTopPosters(top);
                }
                catch (Exception ex) { Logger.LogException(ex); }

                // USER-object escalation (rising-edge with hysteresis).
                if (user >= UserObjectWarnThreshold && !_userWarned)
                {
                    _userWarned = true;
                    var msg = $"USER object count {user} ≥ {UserObjectWarnThreshold} — approaching the per-process limit; possible UI-resource leak";
                    Logger.Warning(msg);
                    _log?.LogWarning("resource-warn-user-objects", msg);
                }
                else if (user < UserObjectWarnThreshold / 2)
                {
                    _userWarned = false;
                }

                // Dispatcher-pending escalation (rising-edge with hysteresis).
                // Only meaningful when the hooks are subscribed; with no
                // subscription pending stays at 0 and this branch never fires.
                if (pending >= DispatcherPendingWarnHigh && !_dispatcherWarned)
                {
                    _dispatcherWarned = true;
                    var msg = $"Dispatcher pending operations {pending} ≥ {DispatcherPendingWarnHigh} — approaching the per-thread PostMessage limit; message queue saturation imminent";
                    Logger.Warning(msg);
                    _log?.LogWarning("resource-warn-dispatcher-pending", msg);
                }
                else if (pending < DispatcherPendingWarnLow)
                {
                    _dispatcherWarned = false;
                }

                // CFIT log-queue escalation + last-resort trim. Unchanged
                // semantics from the pre-Phase 6.5.B implementation.
                int warnThreshold = Math.Max(1, _config?.UiLogQueueWarnThreshold ?? 1000);
                int hardCap = Math.Max(1, _config?.UiLogMaxMessages ?? 200);
                if (queueDepth >= warnThreshold)
                {
                    if (!_queueWarned)
                    {
                        _queueWarned = true;
                        var msg = $"CFIT log queue depth {queueDepth} ≥ {warnThreshold} — drain worker may be stalled; trimming to {hardCap}";
                        Logger.Warning(msg);
                        _log?.LogWarning("resource-warn-log-queue", msg);
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
