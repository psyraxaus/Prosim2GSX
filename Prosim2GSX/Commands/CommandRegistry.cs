using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Prosim2GSX.Commands
{
    // Centralised dispatcher for named, typed command handlers. Both REST
    // controllers (Phase 8B+) and — eventually — WPF RelayCommands invoke
    // operations through this registry instead of directly poking services,
    // so dispatcher marshalling, audit logging, and validation live in one
    // place.
    //
    // Naming convention (project memory): dotted, lowercase namespace head,
    // camelCase tail. e.g. "ofp.confirmArrivalGate", "profiles.setActive",
    // "audio.setMute".
    //
    // Thread-safety: registration via ConcurrentDictionary so handlers can
    // be added from any thread during startup; execution is lock-free.
    public class CommandRegistry
    {
        private readonly ConcurrentDictionary<string, Delegate> _handlers
            = new(StringComparer.Ordinal);

        // Register a handler with a typed request and response. The Func
        // receives a CancellationToken so REST endpoints can pipe
        // HttpContext.RequestAborted through. Throws if the same name is
        // registered twice — handler bundles are expected to be wired once
        // at app startup, double-registration is a bug.
        public virtual void Register<TReq, TRes>(
            string name,
            Func<TReq, CancellationToken, Task<TRes>> handler)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Command name required.", nameof(name));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (!_handlers.TryAdd(name, handler))
                throw new InvalidOperationException(
                    $"Command '{name}' is already registered.");
        }

        // Execute a previously-registered command.
        //
        // Marshal-by-default: most commands write to Config, mutate INPC
        // sources, or touch ObservableCollection<T> — all of which require
        // the WPF dispatcher. The registry hops onto Application.Current.Dispatcher
        // unless the caller is already on it. Handlers that need to do
        // long-running off-UI work (HTTP fetches, etc.) should kick off
        // Task.Run inside the handler body — the marshalling here only
        // affects where the handler STARTS, not where it spends its time.
        public virtual async Task<TRes> ExecuteAsync<TReq, TRes>(
            string name,
            TReq req,
            CancellationToken ct = default)
        {
            if (!_handlers.TryGetValue(name, out var del))
                throw new CommandNotFoundException(name);

            if (del is not Func<TReq, CancellationToken, Task<TRes>> handler)
                throw new InvalidOperationException(
                    $"Command '{name}' is registered with a different signature than requested.");

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
                return await handler(req, ct).ConfigureAwait(false);

            // dispatcher.InvokeAsync<Task<TRes>>(...) returns
            // DispatcherOperation<Task<TRes>>; .Task is Task<Task<TRes>>.
            // Await once for the dispatcher to schedule + start the
            // handler, then await the inner Task for the handler to
            // complete and surface its TRes / exception.
            var op = dispatcher.InvokeAsync(() => handler(req, ct));
            var innerTask = await op.Task.ConfigureAwait(false);
            return await innerTask.ConfigureAwait(false);
        }

        // True when a handler with the given name is registered. Useful
        // for diagnostics and conditional UI rendering.
        public virtual bool IsRegistered(string name) => _handlers.ContainsKey(name);
    }
}
