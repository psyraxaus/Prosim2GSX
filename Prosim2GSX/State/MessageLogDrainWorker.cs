using CFIT.AppLogger;
using Prosim2GSX.AppConfig;
using System;
using System.Threading;

namespace Prosim2GSX.State
{
    // Drains CFIT Logger.Messages into FlightStatusState.MessageLog. Long-lived
    // worker owned by AppService — replaces the per-tab UpdateLog drain that used
    // to live on ModelMonitor, so the message stream is durable independent of
    // whether the WPF Monitor tab is open.
    //
    // Runs on a System.Threading.Timer (thread-pool callback), NOT a
    // DispatcherTimer. The earlier DispatcherTimer implementation ran this drain
    // — and the synchronous WS log fan-out it triggers via
    // FlightStatusState.MessageLog.CollectionChanged — on the WPF UI thread for
    // the whole process lifetime, including the long headless stretches the
    // window spends hidden to the tray. With a chatty logger that starved the
    // UI thread's shared Win32 message queue until the per-thread PostMessage
    // limit was hit (ERROR_NOT_ENOUGH_QUOTA crash on the next window restore).
    // The store and the WS handler have no UI affinity; the only UI consumer
    // (ModelMonitor) marshals to its own dispatcher.
    //
    // Cap inside the store is StoreCap (500) — large enough for the web layer to
    // replay recent history; the WPF Monitor tab keeps its own visual-line trim
    // on top of this for its compact log panel.
    public class MessageLogDrainWorker
    {
        private const int StoreCap = 500;

        // Hard ceiling on how many messages a single tick will move out of the
        // CFIT queue into the store. At the ~500 ms cadence this is far above
        // any sane sustained log rate; it exists purely so a pathological burst
        // can't make one tick run unbounded. Anything still queued above this
        // is caught by the overflow/hardCap trim below on subsequent ticks.
        private const int MaxDrainPerTick = 1000;

        private readonly FlightStatusState _state;
        private readonly Config _config;
        private readonly Timer _timer;
        private readonly int _intervalMs;
        private volatile bool _isDraining;
        private bool _queueOverflowWarned;

        public MessageLogDrainWorker(FlightStatusState state, Config config)
        {
            _state = state;
            _config = config;

            _intervalMs = Math.Max(100, config?.UiRefreshInterval ?? 500);
            _timer = new Timer(OnTick, null, Timeout.Infinite, Timeout.Infinite);
        }

        public virtual void Start()
            => _timer.Change(_intervalMs, _intervalMs);

        public virtual void Stop()
            => _timer.Change(Timeout.Infinite, Timeout.Infinite);

        protected virtual void OnTick(object? state)
        {
            if (_isDraining) return;
            _isDraining = true;
            try
            {
                if (Logger.Messages.IsEmpty)
                    return;

                int warnThreshold = Math.Max(1, _config?.UiLogQueueWarnThreshold ?? 1000);
                int hardCap = Math.Max(1, _config?.UiLogMaxMessages ?? 200);

                int queueDepth = Logger.Messages.Count;
                if (queueDepth >= warnThreshold && !_queueOverflowWarned)
                {
                    _queueOverflowWarned = true;
                    Logger.Warning($"UI log buffer overflow detected: queue depth {queueDepth} ≥ {warnThreshold}; dropping oldest entries");
                    while (Logger.Messages.Count > hardCap)
                        Logger.Messages.TryDequeue(out _);
                }
                else if (queueDepth < warnThreshold / 2)
                {
                    _queueOverflowWarned = false;
                }

                int drained = 0;
                while (drained < MaxDrainPerTick && Logger.Messages.TryDequeue(out var msg))
                {
                    _state.MessageLog.Add(msg);
                    while (_state.MessageLog.Count > StoreCap)
                        _state.MessageLog.RemoveAt(0);
                    drained++;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            finally
            {
                _isDraining = false;
            }
        }
    }
}
