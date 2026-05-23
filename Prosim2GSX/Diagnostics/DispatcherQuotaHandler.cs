using CFIT.AppLogger;
using System;
using System.ComponentModel;
using System.Threading;

namespace Prosim2GSX.Diagnostics
{
    /// <summary>
    /// Records the running count and first/last occurrence of dispatcher
    /// quota exceptions handled by <see cref="DispatcherQuotaHandler"/>.
    /// Snapshot returned by <see cref="DispatcherQuotaHandler.GetStats"/>;
    /// surfaced in the <c>ResourceDiagnosticsWorker</c> heartbeat.
    /// </summary>
    public readonly record struct QuotaExceptionStats(
        long HandledCount,
        DateTime FirstHandledUtc,
        DateTime LastHandledUtc);

    /// <summary>
    /// Resilience handler for the recurring WPF dispatcher
    /// <c>Win32Exception</c> with native error code 1816
    /// (<c>ERROR_NOT_ENOUGH_QUOTA</c>) from the render pipeline
    /// (<c>HwndTarget.UpdateWindowSettings → PostMessage</c>).
    ///
    /// <para>
    /// Phase 6.5.B's diagnostic data showed the app's own resource usage is
    /// healthy at the moment of these crashes (USER ~30, GDI ~23, dispatcher
    /// pending ~3), so the failure is almost certainly external/transient.
    /// The handler logs rich forensic context and lets the app continue.
    /// </para>
    ///
    /// <para>
    /// <see cref="HandleQuotaException"/> is invoked from
    /// <c>Prosim2GSX.UnhandledExceptionHandler</c> when the override
    /// recognises the specific exception. The handler MUST NEVER throw —
    /// any logging failure is swallowed so the caller can mark the
    /// exception handled and continue.
    /// </para>
    /// </summary>
    public sealed class DispatcherQuotaHandler
    {
        /// <summary>
        /// Native Win32 error code 1816 — <c>ERROR_NOT_ENOUGH_QUOTA</c>. The
        /// per-thread <c>PostMessage</c> budget (10,000 entries) is exhausted
        /// or transient OS-level resource pressure caused the dispatcher
        /// render call to fail.
        /// </summary>
        public const int ErrorNotEnoughQuota = 1816;

        private readonly ResourceDiagnosticsLog _resourceLog;
        private long _handledCount;
        private long _firstHandledUtcTicks;
        private long _lastHandledUtcTicks;

        public DispatcherQuotaHandler(ResourceDiagnosticsLog resourceLog)
        {
            _resourceLog = resourceLog;
        }

        /// <summary>
        /// Records the exception, snapshots resource state, and emits a WARN
        /// row to the main CFIT logger plus a richer row to
        /// <see cref="ResourceDiagnosticsLog"/>. Increments the per-process
        /// handled-count. Never throws.
        /// </summary>
        public void HandleQuotaException(Win32Exception ex)
        {
            try
            {
                var nowUtc = DateTime.UtcNow;
                var count = Interlocked.Increment(ref _handledCount);
                // First-handled timestamp is set once on the first occurrence;
                // last-handled is overwritten each time. CompareExchange on
                // the ticks field gives a lock-free atomic set.
                if (count == 1)
                    Interlocked.CompareExchange(ref _firstHandledUtcTicks, nowUtc.Ticks, 0L);
                Interlocked.Exchange(ref _lastHandledUtcTicks, nowUtc.Ticks);

                var snapshot = ResourceSnapshot.Capture();

                try
                {
                    Logger.Warning(
                        $"Caught dispatcher quota exception (handled occurrence #{count}): " +
                        $"USER={snapshot.UserObjects}, GDI={snapshot.GdiObjects}, " +
                        $"Handles={snapshot.Handles}, DispatcherPending={snapshot.DispatcherPending}, " +
                        $"IntentContext={snapshot.IntentContext ?? "<none>"}. " +
                        "App will continue. See ResourceDiagnosticsLog for full context.");
                }
                catch { /* main logger failure must not stop us from marking handled */ }

                try { _resourceLog?.LogQuotaException(count, snapshot, ex); }
                catch { /* dedicated log failure must not stop us */ }
            }
            catch
            {
                // Bulletproof guard — even if Interlocked or Capture fails,
                // the caller will still set e.Handled and keep the app alive.
            }
        }

        /// <summary>Returns the current handled-count + first/last occurrence timestamps.</summary>
        public QuotaExceptionStats GetStats()
        {
            var count = Interlocked.Read(ref _handledCount);
            var firstTicks = Interlocked.Read(ref _firstHandledUtcTicks);
            var lastTicks = Interlocked.Read(ref _lastHandledUtcTicks);
            return new QuotaExceptionStats(
                count,
                firstTicks > 0 ? new DateTime(firstTicks, DateTimeKind.Utc) : DateTime.MinValue,
                lastTicks > 0 ? new DateTime(lastTicks, DateTimeKind.Utc) : DateTime.MinValue);
        }
    }
}
