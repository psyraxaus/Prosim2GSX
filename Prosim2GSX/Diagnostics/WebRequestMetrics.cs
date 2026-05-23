using System.Threading;

namespace Prosim2GSX.Diagnostics
{
    /// <summary>
    /// Process-lifetime counters for inbound web requests. Populated by a
    /// pass-through middleware registered in <c>WebHostService.DoStart</c>;
    /// read by <c>ResourceDiagnosticsWorker</c> each heartbeat so the
    /// resource log can correlate dispatcher / GC / handle trends with
    /// the web request rate that's putting load on them.
    ///
    /// <para>
    /// Counters never reset; deltas between heartbeats are computed by the
    /// worker. <see cref="ActiveRequests"/> rises on enter, falls on exit,
    /// so its instantaneous value is the count of requests currently
    /// inside the pipeline. A growing <see cref="ActiveRequests"/> while
    /// <see cref="CompletedRequests"/> stagnates is a tell that something
    /// (typically a slow dispatcher hop) is back-pressuring Kestrel.
    /// </para>
    /// </summary>
    public static class WebRequestMetrics
    {
        private static long _totalRequests;
        private static long _completedRequests;
        private static long _activeRequests;

        public static long TotalRequests => Interlocked.Read(ref _totalRequests);
        public static long CompletedRequests => Interlocked.Read(ref _completedRequests);
        public static long ActiveRequests => Interlocked.Read(ref _activeRequests);

        /// <summary>Called from the metrics middleware on request entry.</summary>
        public static void OnRequestStart()
        {
            Interlocked.Increment(ref _totalRequests);
            Interlocked.Increment(ref _activeRequests);
        }

        /// <summary>Called from the metrics middleware on request exit (success or fault).</summary>
        public static void OnRequestEnd()
        {
            Interlocked.Increment(ref _completedRequests);
            Interlocked.Decrement(ref _activeRequests);
        }
    }
}
