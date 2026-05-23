using CFIT.AppLogger;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Prosim2GSX.Diagnostics
{
    /// <summary>
    /// Diagnostic verbosity for <see cref="ResourceDiagnosticsLog"/>. The
    /// enum mirrors <c>GsxMenuDiagnosticLog</c>'s level pattern for
    /// future expansion. Today only Off (file never created) and Normal
    /// (full emission of heartbeat / top-poster / WARN / stress-mode
    /// rows) are observable behaviours; Verbose is reserved for future
    /// per-tick raw dispatcher-event capture.
    /// </summary>
    public enum ResourceDiagnosticsLevel
    {
        Off = 0,
        Normal = 1,
        Verbose = 2,
    }

    /// <summary>
    /// CMTrace-format dedicated log for resource telemetry. Captures
    /// per-tick heartbeats (USER/GDI/handle counts, log queue depth,
    /// dispatcher posted/completed/pending counters), top-N attributed
    /// posters, rising-edge WARN escalations, and the StressMode
    /// startup marker. Lives alongside <c>GsxMenuDiagnosticLog</c> in
    /// the same log directory; one session = one file; oldest sessions
    /// pruned at startup.
    /// </summary>
    public sealed class ResourceDiagnosticsLog : IDisposable
    {
        private const string FilenamePrefix = "Prosim2GSX-Resource-";
        private const string FilenameExtension = ".log";
        private const int RetainSessions = 10;

        private readonly object _writeLock = new();
        private readonly string _logFilePath;
        private readonly bool _enabled;
        private bool _disposed;

        /// <summary>Resolved absolute path of the session log file (or <c>null</c> when disabled).</summary>
        public string LogFilePath => _logFilePath;

        /// <summary>True if the log file was successfully created and writes will be attempted.</summary>
        public bool IsEnabled => _enabled;

        /// <summary>Effective verbosity at construction time.</summary>
        public ResourceDiagnosticsLevel Level { get; }

        /// <summary>
        /// Constructs the logger. Resolves the file path, prunes old
        /// sessions, and writes the header row. Off-level → no file is
        /// created; all subsequent log calls no-op. Failures during file
        /// creation degrade silently to a CFIT logger warning so the main
        /// app keeps starting.
        /// </summary>
        public ResourceDiagnosticsLog(string appLogDirectory, ResourceDiagnosticsLevel level = ResourceDiagnosticsLevel.Normal)
        {
            Level = level;

            if (Level == ResourceDiagnosticsLevel.Off)
            {
                _enabled = false;
                _logFilePath = null;
                return;
            }

            try
            {
                var directory = string.IsNullOrWhiteSpace(appLogDirectory)
                    ? Directory.GetCurrentDirectory()
                    : appLogDirectory;
                Directory.CreateDirectory(directory);

                var timestamp = DateTime.Now.ToString("yyyy-MM-dd-HHmmss", CultureInfo.InvariantCulture);
                _logFilePath = Path.Combine(directory, FilenamePrefix + timestamp + FilenameExtension);

                WriteHeader();
                PruneOldSessions(directory, RetainSessions);
                _enabled = true;
            }
            catch (Exception ex)
            {
                Logger.Warning($"ResourceDiagnosticsLog: failed to initialise — {ex.Message}");
                _enabled = false;
                _logFilePath = null;
            }
        }

        private void WriteHeader()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
            var sb = new StringBuilder();
            sb.AppendLine("=== Prosim2GSX Resource Diagnostic Log ===");
            sb.AppendLine($"=== Session start: {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ} ===");
            sb.AppendLine($"=== Verbosity: {Level} ===");
            sb.AppendLine($"=== App version: {version} ===");
            File.AppendAllText(_logFilePath, sb.ToString(), Encoding.UTF8);
        }

        private void PruneOldSessions(string directory, int retainCount)
        {
            try
            {
                var keep = retainCount > 0 ? retainCount : 1;
                var files = new DirectoryInfo(directory)
                    .GetFiles(FilenamePrefix + "*" + FilenameExtension)
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .Skip(keep)
                    .ToList();
                foreach (var f in files)
                {
                    try { f.Delete(); }
                    catch (Exception ex) { Logger.Debug($"ResourceDiagnosticsLog: failed to prune '{f.Name}' — {ex.Message}"); }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug($"ResourceDiagnosticsLog: prune scan failed — {ex.Message}");
            }
        }

        /// <summary>
        /// One row per <c>ResourceDiagnosticsWorker</c> tick. Carries all
        /// process-wide vitals captured for this window:
        /// USER/GDI/handle counts, thread count, CFIT log-queue depth,
        /// dispatcher post/complete deltas + lifetime pending,
        /// GC managed bytes + collection-count deltas per generation,
        /// thread-pool worker / IO availability + pending work item count,
        /// web request total / delta / active, WebSocket connection count,
        /// and process uptime in seconds.
        /// </summary>
        public void LogHeartbeat(
            uint user, uint gdi, int handles, int threads, int logQueue,
            long dispatcherPosted, long dispatcherCompleted, long dispatcherPending,
            long managedBytes, int gen0Delta, int gen1Delta, int gen2Delta,
            int tpWorkerAvailable, int tpIoAvailable, long tpPending,
            long webRequestsTotal, long webRequestsDelta, long webRequestsActive,
            int wsConnections, double uptimeSeconds)
        {
            if (!_enabled) return;
            var sb = new StringBuilder();
            sb.Append("[resource-heartbeat] ")
              .Append("UptimeSec=").Append(uptimeSeconds.ToString("F0", CultureInfo.InvariantCulture))
              .Append(", USER=").Append(user)
              .Append(", GDI=").Append(gdi)
              .Append(", Handles=").Append(handles)
              .Append(", Threads=").Append(threads)
              .Append(", LogQueue=").Append(logQueue)
              .Append(", DispatcherPosted=").Append(dispatcherPosted)
              .Append(", DispatcherCompleted=").Append(dispatcherCompleted)
              .Append(", DispatcherPending=").Append(dispatcherPending)
              .Append(", ManagedKB=").Append(managedBytes / 1024)
              .Append(", GC0=").Append(gen0Delta)
              .Append(", GC1=").Append(gen1Delta)
              .Append(", GC2=").Append(gen2Delta)
              .Append(", TPWorkerAvail=").Append(tpWorkerAvailable)
              .Append(", TPIoAvail=").Append(tpIoAvailable)
              .Append(", TPPending=").Append(tpPending)
              .Append(", WebTotal=").Append(webRequestsTotal)
              .Append(", WebDelta=").Append(webRequestsDelta)
              .Append(", WebActive=").Append(webRequestsActive)
              .Append(", WsClients=").Append(wsConnections);
            Write(sb.ToString(), severity: 1);
        }

        /// <summary>
        /// One row per entry in the top-N list so each can be filtered
        /// individually in CMTrace. <paramref name="topPosters"/> is the
        /// list returned by <c>UiMarshal.SnapshotTop</c>.
        /// </summary>
        public void LogTopPosters(IReadOnlyList<(string Site, long Count)> topPosters)
        {
            if (!_enabled || topPosters == null) return;
            for (int i = 0; i < topPosters.Count; i++)
            {
                var entry = topPosters[i];
                var sb = new StringBuilder();
                sb.Append("[resource-top-poster] ")
                  .Append(i + 1).Append(". ")
                  .Append(entry.Site ?? "<null>")
                  .Append(" => ").Append(entry.Count);
                Write(sb.ToString(), severity: 1);
            }
        }

        /// <summary>
        /// Rising-edge WARN. <paramref name="category"/> should be one
        /// of the prompt's canonical category strings
        /// (<c>resource-warn-user-objects</c>,
        /// <c>resource-warn-dispatcher-pending</c>,
        /// <c>resource-warn-log-queue</c>) so operators can filter on it
        /// in CMTrace.
        /// </summary>
        public void LogWarning(string category, string message)
        {
            if (!_enabled) return;
            var sb = new StringBuilder();
            sb.Append('[').Append(string.IsNullOrEmpty(category) ? "resource-warn" : category)
              .Append("] ").Append(message ?? "");
            Write(sb.ToString(), severity: 2);
        }

        /// <summary>
        /// Emitted once at the first heartbeat when StressMode is active
        /// so the log carries explicit context that the cadence + headless
        /// window forcing are intentional.
        /// </summary>
        public void LogStressModeStart(int intervalMs)
        {
            if (!_enabled) return;
            var sb = new StringBuilder();
            sb.Append("[resource-stress-mode] StressMode active: diagnostics heartbeat at ")
              .Append(intervalMs).Append("ms, window forced hidden");
            Write(sb.ToString(), severity: 1);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            // No persistent handle — Write opens/appends/closes per row —
            // so nothing to release. Method exists for using-statement parity
            // with GsxMenuDiagnosticLog.
        }

        private void Write(string message, int severity)
        {
            if (!_enabled) return;
            var now = DateTime.Now;
            // CMTrace expects the timezone offset in minutes; mirror
            // GsxMenuDiagnosticLog's "+000" placeholder so both logs open
            // identically in the viewer.
            var line = string.Format(
                CultureInfo.InvariantCulture,
                "<![LOG[{0}]LOG]!><time=\"{1:HH:mm:ss.fff}+000\" date=\"{1:MM-dd-yyyy}\" component=\"Resource\" context=\"\" type=\"{2}\" thread=\"{3}\" file=\"\">",
                message,
                now,
                severity,
                Thread.CurrentThread.ManagedThreadId);

            lock (_writeLock)
            {
                try
                {
                    File.AppendAllText(_logFilePath, line + Environment.NewLine, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    Logger.Debug($"ResourceDiagnosticsLog write failed — {ex.Message}");
                }
            }
        }
    }
}
