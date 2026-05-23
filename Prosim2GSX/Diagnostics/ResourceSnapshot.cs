using CFIT.AppLogger;
using System;
using System.Diagnostics;

namespace Prosim2GSX.Diagnostics
{
    /// <summary>
    /// Point-in-time view of the same process-vital metrics
    /// <c>ResourceDiagnosticsWorker</c> emits each heartbeat tick. Used by
    /// <see cref="DispatcherQuotaHandler"/> to capture forensic context at
    /// the exact moment a dispatcher quota exception is caught.
    ///
    /// <para>
    /// Fast, exception-safe, and never blocks: any metric that fails to
    /// read is filled with -1 / 0 rather than throwing. Intent context is
    /// read from <see cref="UiMarshal.CurrentIntentContext"/> and may be
    /// <c>null</c> when no intent scope is active.
    /// </para>
    /// </summary>
    public readonly record struct ResourceSnapshot(
        DateTime TimestampUtc,
        uint UserObjects,
        uint GdiObjects,
        int Handles,
        int Threads,
        long ManagedBytes,
        long DispatcherPosted,
        long DispatcherCompleted,
        long DispatcherPending,
        int LogQueueDepth,
        string IntentContext)
    {
        /// <summary>
        /// Captures the current process state. Safe to call from any thread,
        /// including from inside an exception handler. All read paths are
        /// wrapped — a P/Invoke failure or transient access denial yields a
        /// sentinel value, not an exception.
        /// </summary>
        public static ResourceSnapshot Capture()
        {
            var now = DateTime.UtcNow;
            uint user = 0, gdi = 0;
            int handles = -1, threads = -1;
            long managed = -1;

            try
            {
                using var proc = Process.GetCurrentProcess();
                try { user = NativeMethods.GetGuiResources(proc.Handle, NativeMethods.GR_USEROBJECTS); } catch { }
                try { gdi = NativeMethods.GetGuiResources(proc.Handle, NativeMethods.GR_GDIOBJECTS); } catch { }
                try { handles = proc.HandleCount; } catch { }
                try { threads = proc.Threads.Count; } catch { }
            }
            catch { /* Process.GetCurrentProcess itself failed — leave sentinels */ }

            try { managed = GC.GetTotalMemory(forceFullCollection: false); } catch { }

            long posted = -1, completed = -1, pending = -1;
            try
            {
                var worker = AppService.Instance?.ResourceDiagnosticsWorker;
                if (worker != null)
                {
                    posted = worker.DispatcherPostedTotal;
                    completed = worker.DispatcherCompletedTotal;
                    pending = worker.DispatcherPending;
                }
            }
            catch { }

            int logQueue = -1;
            try { logQueue = Logger.Messages?.Count ?? -1; } catch { }

            string intent = null;
            try { intent = UiMarshal.CurrentIntentContext; } catch { }

            return new ResourceSnapshot(
                now, user, gdi, handles, threads, managed,
                posted, completed, pending, logQueue, intent);
        }
    }
}
