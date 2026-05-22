using CFIT.AppLogger;
using Prosim2GSX.AppConfig;
using Prosim2GSX.GSX.Menu.Intents;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Prosim2GSX.GSX.Menu
{
    /// <summary>
    /// Diagnostic verbosity for <see cref="GsxMenuDiagnosticLog"/>. The serialised
    /// setting lives on <see cref="Config.GsxMenuDiagnosticLevel"/>.
    /// </summary>
    public enum GsxMenuDiagnosticLevel
    {
        /// <summary>File is never created; all log methods no-op.</summary>
        Off = 0,
        /// <summary>One row per intent execution. Success carries matched-entry text only; non-success carries the full snapshot menu dump.</summary>
        Normal = 1,
        /// <summary>Every row carries the full snapshot. Adds rows for Open/Hide navigation and per-poll verification observations.</summary>
        Verbose = 2,
    }

    /// <summary>
    /// CMTrace-format diagnostic logger for the new intent-based menu engine.
    /// Separate from <c>CFIT.AppLogger.Logger</c> so the main app log stays terse
    /// while this file accumulates the per-intent, per-menu, per-resolution detail
    /// needed to debug bad selections after the fact. One session = one file;
    /// older sessions are pruned at startup.
    /// </summary>
    public sealed class GsxMenuDiagnosticLog : IDisposable
    {
        private const string FilenamePrefix = "Prosim2GSX-GsxMenu-";
        private const string FilenameExtension = ".log";

        private readonly object _writeLock = new();
        private readonly Config _config;
        private readonly string _logFilePath;
        private readonly bool _enabled;
        private bool _disposed;

        /// <summary>Resolved absolute path of the session log file (or <c>null</c> when disabled).</summary>
        public string LogFilePath => _logFilePath;

        /// <summary>True if the log file was successfully created and writes will be attempted.</summary>
        public bool IsEnabled => _enabled;

        /// <summary>Effective verbosity at construction time. Re-reading the Config at emit time would race with the Settings UI.</summary>
        public GsxMenuDiagnosticLevel Level { get; }

        /// <summary>
        /// Constructs the logger. Resolves the file path, prunes old sessions, and
        /// writes the header row. Off-level → no file is created; all subsequent
        /// log calls no-op. Failures during file creation degrade silently to a
        /// CFIT logger warning so the main app keeps starting.
        /// </summary>
        public GsxMenuDiagnosticLog(Config config, string appLogDirectory)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            Level = config.GsxMenuDiagnosticLevel;

            if (Level == GsxMenuDiagnosticLevel.Off)
            {
                _enabled = false;
                _logFilePath = null;
                return;
            }

            try
            {
                var directory = ResolveDirectory(config, appLogDirectory);
                Directory.CreateDirectory(directory);

                var timestamp = DateTime.Now.ToString("yyyy-MM-dd-HHmmss", CultureInfo.InvariantCulture);
                _logFilePath = Path.Combine(directory, FilenamePrefix + timestamp + FilenameExtension);

                WriteHeader();
                PruneOldSessions(directory, config.GsxMenuDiagnosticRetainSessions);
                _enabled = true;
            }
            catch (Exception ex)
            {
                Logger.Warning($"GsxMenuDiagnosticLog: failed to initialise — {ex.Message}");
                _enabled = false;
                _logFilePath = null;
            }
        }

        private static string ResolveDirectory(Config config, string appLogDirectory)
        {
            var custom = config.GsxMenuDiagnosticPath;
            if (string.IsNullOrWhiteSpace(custom))
                return appLogDirectory;

            // Relative paths resolve against ProductPath, matching the AudioDebugFile
            // convention. Absolute paths are honoured as-is.
            // Definition is a static singleton on AppConfigBase<> — accessed via
            // the type name rather than the config instance.
            if (Path.IsPathRooted(custom))
                return custom;

            var productPath = Config.Definition?.ProductPath ?? string.Empty;
            return Path.Combine(productPath, custom);
        }

        private void WriteHeader()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
            var sb = new StringBuilder();
            sb.AppendLine("=== Prosim2GSX GSX Menu Diagnostic Log ===");
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
                    catch (Exception ex) { Logger.Debug($"GsxMenuDiagnosticLog: failed to prune '{f.Name}' — {ex.Message}"); }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug($"GsxMenuDiagnosticLog: prune scan failed — {ex.Message}");
            }
        }

        /// <summary>
        /// Records the outcome of one <see cref="GsxMenu.ExecuteIntent"/> invocation.
        /// Type (Info/Warning/Error) is derived from <see cref="GsxMenuResult.Outcome"/>;
        /// menu-dump verbosity is gated on <see cref="Level"/> and whether the
        /// outcome was successful.
        /// </summary>
        public void LogIntentExecution(GsxMenuResult result)
        {
            if (!_enabled || result == null) return;

            var type = ClassifyOutcome(result.Outcome);
            var includeFullDump = Level == GsxMenuDiagnosticLevel.Verbose || !result.IsSuccess;

            var sb = new StringBuilder();
            sb.Append("Intent ").Append(result.Intent?.IntentName ?? "<null>")
              .Append(" => ").Append(result.Outcome);
            if (result.Duration.HasValue)
                sb.Append(" (").Append((int)result.Duration.Value.TotalMilliseconds).Append("ms)");
            sb.AppendLine();
            sb.Append("  Phase: ").Append(result.PhaseAtExecution).AppendLine();
            if (!string.IsNullOrWhiteSpace(result.Reason))
                sb.Append("  Reason: ").AppendLine(result.Reason);
            if (result.ResolvedIndex.HasValue)
                sb.Append("  ResolvedIndex: ").Append(result.ResolvedIndex.Value)
                  .Append(" => '").Append(result.MatchedEntryText ?? "").Append("'").AppendLine();
            if (!string.IsNullOrWhiteSpace(result.PostWriteStateObservation))
                sb.Append("  PostWriteState: ").AppendLine(result.PostWriteStateObservation);
            if (!string.IsNullOrWhiteSpace(result.MenuTitleObserved))
                sb.Append("  MenuTitle: '").Append(result.MenuTitleObserved).Append("'").AppendLine();

            if (includeFullDump && result.MenuLinesObserved != null)
            {
                sb.Append("  MenuLines (").Append(result.MenuLinesObserved.Count).AppendLine("):");
                for (int i = 0; i < result.MenuLinesObserved.Count; i++)
                    sb.Append("    [").Append(i + 1).Append("] ").AppendLine(result.MenuLinesObserved[i]);
            }

            if (result.Exception != null)
                sb.Append("  Exception: ").AppendLine(result.Exception.ToString());

            Write(sb.ToString(), type);
        }

        /// <summary>
        /// Records that a write to <c>FSDT_GSX_MENU_CHOICE</c> arrived without our
        /// <c>Select()</c> initiating it — i.e. the user clicked a menu item
        /// manually. Always emitted regardless of <see cref="Level"/> (except Off).
        /// </summary>
        public void LogManualOverride(string menuTitle, int choiceObserved, string menuLineText)
        {
            if (!_enabled) return;

            var sb = new StringBuilder();
            sb.Append("Manual override: menu '").Append(menuTitle ?? "")
              .Append("', choice index ").Append(choiceObserved)
              .Append(" => '").Append(menuLineText ?? "").Append("'");
            Write(sb.ToString(), severity: 1);
        }

        /// <summary>
        /// Records a navigation step (Open / Hide / OpenHide / Timeout). Only emits
        /// at <see cref="GsxMenuDiagnosticLevel.Verbose"/>.
        /// </summary>
        public void LogNavigation(string action, string menuTitleBefore, string menuTitleAfter, bool success)
        {
            if (!_enabled || Level != GsxMenuDiagnosticLevel.Verbose) return;

            var sb = new StringBuilder();
            sb.Append("Navigation ").Append(action ?? "")
              .Append(": '").Append(menuTitleBefore ?? "").Append("' => '")
              .Append(menuTitleAfter ?? "").Append("' (")
              .Append(success ? "ok" : "failed").Append(")");
            Write(sb.ToString(), severity: success ? 1 : 2);
        }

        /// <summary>
        /// Free-form diagnostic emission for Phase 5 instrumentation
        /// (pushback-direction / gate-selection). Category is prepended to the
        /// message; severity defaults to Info, callers may pass an outcome to
        /// classify Warning/Error.
        /// </summary>
        public void LogDiagnostic(string category, string message, MenuOutcome? severity = null)
        {
            if (!_enabled) return;

            var sb = new StringBuilder();
            sb.Append('[').Append(category ?? "diag").Append("] ").Append(message ?? "");
            var type = severity.HasValue ? ClassifyOutcome(severity.Value) : 1;
            Write(sb.ToString(), type);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            // No persistent handle — we open/append/close per Write — so nothing
            // to release. Method exists so callers can use a using-statement-style
            // lifecycle without surprises.
        }

        private static int ClassifyOutcome(MenuOutcome outcome) => outcome switch
        {
            MenuOutcome.Success => 1,
            MenuOutcome.StatePreconditionSatisfiedAlready => 1,
            MenuOutcome.ItemNotAvailable => 1,
            MenuOutcome.DoorActionPrompt => 2,
            MenuOutcome.PhaseMismatch => 2,
            MenuOutcome.StatePreconditionFailed => 2,
            MenuOutcome.MenuTitleMismatch => 2,
            MenuOutcome.NavigationFailed => 2,
            MenuOutcome.GsxNoResponse => 2,
            MenuOutcome.Timeout => 2,
            MenuOutcome.AmbiguousMatch => 3,
            MenuOutcome.GsxError => 3,
            _ => 1,
        };

        private void Write(string message, int severity)
        {
            if (!_enabled) return;

            var now = DateTime.Now;
            // CMTrace expects the timezone offset in minutes; use +000 as a neutral
            // placeholder per the spec example. The wall-clock time itself is local.
            var line = string.Format(
                CultureInfo.InvariantCulture,
                "<![LOG[{0}]LOG]!><time=\"{1:HH:mm:ss.fff}+000\" date=\"{1:MM-dd-yyyy}\" component=\"GsxMenu\" context=\"\" type=\"{2}\" thread=\"{3}\" file=\"\">",
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
                    // Don't keep retrying; just record once-per-write to the main log.
                    Logger.Debug($"GsxMenuDiagnosticLog write failed — {ex.Message}");
                }
            }
        }
    }
}
