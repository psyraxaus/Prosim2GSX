using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace Prosim2GSX.Diagnostics
{
    /// <summary>
    /// Dispatcher-post helper with layered attribution. Two services:
    /// <list type="number">
    /// <item><description><see cref="Post"/> / <see cref="PostBackground"/> wrap the common
    /// <c>Application.Current.Dispatcher.BeginInvoke</c> pattern and record the
    /// caller's file:line for per-tick top-N reporting.</description></item>
    /// <item><description><see cref="BeginIntentContext"/> + <see cref="SnapshotTop"/> let
    /// <c>ResourceDiagnosticsWorker</c> identify which intent (or which call site)
    /// is dominating dispatcher traffic since the last heartbeat tick.</description></item>
    /// </list>
    ///
    /// <para>
    /// Existing raw <c>Dispatcher.BeginInvoke</c> call sites are deliberately NOT
    /// migrated in Phase 6.5.B — the comparison of "DispatcherPending growth via
    /// hooks" vs "top-N attributed via UiMarshal" is itself diagnostic data:
    /// if pending climbs but the top-N list is short, the producer is routing
    /// through raw <c>BeginInvoke</c> somewhere and is the next thing to migrate.
    /// </para>
    /// </summary>
    public static class UiMarshal
    {
        private static readonly AsyncLocal<string> _currentIntentContext = new();
        private static ConcurrentDictionary<string, long> _counters = new(StringComparer.Ordinal);

        /// <summary>
        /// Posts <paramref name="action"/> onto the WPF dispatcher at
        /// <see cref="DispatcherPriority.Normal"/>. Increments the per-site
        /// (or per-intent) counter before dispatching. When the dispatcher
        /// is unavailable (headless test / non-WPF host) the action runs
        /// inline on the caller's thread.
        /// </summary>
        public static void Post(
            Action action,
            [CallerMemberName] string member = null,
            [CallerFilePath] string file = null,
            [CallerLineNumber] int line = 0)
        {
            if (action == null) return;
            Track(file, line);
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null) { action(); return; }
            dispatcher.BeginInvoke(DispatcherPriority.Normal, action);
        }

        /// <summary>
        /// Posts <paramref name="action"/> onto the WPF dispatcher at
        /// <see cref="DispatcherPriority.Background"/>. Same fallback as
        /// <see cref="Post"/>.
        /// </summary>
        public static void PostBackground(
            Action action,
            [CallerMemberName] string member = null,
            [CallerFilePath] string file = null,
            [CallerLineNumber] int line = 0)
        {
            if (action == null) return;
            Track(file, line);
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null) { action(); return; }
            dispatcher.BeginInvoke(DispatcherPriority.Background, action);
        }

        /// <summary>
        /// Records <paramref name="intentName"/> in an AsyncLocal slot for
        /// the lifetime of the returned scope. Posts that occur within the
        /// scope are attributed to the intent (<c>"intent:{name}"</c>)
        /// instead of the file:line caller. Nested scopes save and restore
        /// the previous value so parent/child intent chains (ExecuteIntent
        /// navigating its ParentMenu) attribute correctly.
        /// </summary>
        public static IDisposable BeginIntentContext(string intentName)
        {
            var previous = _currentIntentContext.Value;
            _currentIntentContext.Value = intentName;
            return new IntentContextScope(previous);
        }

        /// <summary>
        /// Atomically snapshots the per-attribution counters, swapping in a
        /// fresh empty dictionary, then returns the top <paramref name="n"/>
        /// entries by count (descending). Counters reset for the next tick
        /// at the same instant they're read, so concurrent posts during
        /// the swap are credited to the next tick's window (no double-count,
        /// no lost increments).
        /// </summary>
        public static IReadOnlyList<(string Site, long Count)> SnapshotTop(int n)
        {
            var snapshot = Interlocked.Exchange(
                ref _counters,
                new ConcurrentDictionary<string, long>(StringComparer.Ordinal));
            return snapshot
                .Select(kvp => (Site: kvp.Key, Count: kvp.Value))
                .OrderByDescending(t => t.Count)
                .Take(Math.Max(0, n))
                .ToList();
        }

        private static void Track(string file, int line)
        {
            var current = _currentIntentContext.Value;
            string key = !string.IsNullOrEmpty(current)
                ? "intent:" + current
                : (Path.GetFileName(file ?? "<unknown>") ?? "<unknown>") + ":" + line;
            _counters.AddOrUpdate(key, 1, (_, v) => v + 1);
        }

        private sealed class IntentContextScope : IDisposable
        {
            private readonly string _previous;
            private bool _disposed;

            public IntentContextScope(string previous)
            {
                _previous = previous;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _currentIntentContext.Value = _previous;
            }
        }
    }
}
