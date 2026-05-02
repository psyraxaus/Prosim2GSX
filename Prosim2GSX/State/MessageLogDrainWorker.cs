using CFIT.AppLogger;
using Prosim2GSX.AppConfig;
using System;
using System.Windows;
using System.Windows.Threading;

namespace Prosim2GSX.State
{
    // Drains CFIT Logger.Messages into FlightStatusState.MessageLog. Long-lived
    // worker owned by AppService — replaces the per-tab UpdateLog drain that used
    // to live on ModelMonitor, so the message stream is durable independent of
    // whether the WPF Monitor tab is open.
    //
    // Cap inside the store is StoreCap (500) — large enough for the web layer to
    // replay recent history; the WPF Monitor tab keeps its own visual-line trim
    // on top of this for its compact log panel.
    public class MessageLogDrainWorker
    {
        private const int StoreCap = 500;

        private readonly FlightStatusState _state;
        private readonly Config _config;
        private readonly DispatcherTimer _timer;
        private bool _queueOverflowWarned;

        public MessageLogDrainWorker(FlightStatusState state, Config config)
        {
            _state = state;
            _config = config;

            int interval = Math.Max(100, config?.UiRefreshInterval ?? 500);
            var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            _timer = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(interval),
            };
            _timer.Tick += OnTick;
        }

        public virtual void Start()
        {
            // Start can be called from any thread (e.g. AppService init); marshal
            // to the timer's dispatcher because DispatcherTimer.Start requires it.
            var d = _timer.Dispatcher;
            if (d.CheckAccess()) _timer.Start();
            else d.InvokeAsync(_timer.Start);
        }

        public virtual void Stop()
        {
            var d = _timer.Dispatcher;
            if (d.CheckAccess()) _timer.Stop();
            else d.InvokeAsync(_timer.Stop);
        }

        protected virtual void OnTick(object? sender, EventArgs e)
        {
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

                while (Logger.Messages.TryDequeue(out var msg))
                {
                    _state.MessageLog.Add(msg);
                    while (_state.MessageLog.Count > StoreCap)
                        _state.MessageLog.RemoveAt(0);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }
    }
}
