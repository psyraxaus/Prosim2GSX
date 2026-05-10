using CFIT.AppLogger;
using Prosim2GSX.AppConfig;
using Prosim2GSX.GSX;
using Prosim2GSX.State;
using Prosim2GSX.UI.Views.Checklists;
using Prosim2GSX.Web.Contracts;
using Prosim2GSX.Web.Contracts.Commands;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Prosim2GSX.Web
{
    // Long-lived WebSocket fan-out handler. Subscribes once to each store's
    // INotifyPropertyChanged + the FlightStatusState.MessageLog
    // CollectionChanged event, fans out deltas to every connected client. Auth
    // is per-connection on the first inbound frame ({ "auth": "<token>" });
    // token rotation closes every active connection synchronously via the
    // WebHostService.TokenRotated event.
    //
    // Owned by AppService (peer to WebHostService) so subscriptions are stable
    // across host Stop/Start cycles — restarting Kestrel doesn't recreate the
    // handler, only attaches its endpoint to the new pipeline.
    public class StateWebSocketHandler
    {
        // Wire-format envelope shapes:
        //   { "channel": "flightStatus" | "gsx" | "audio" | "appSettings",
        //     "patch": { propertyName: value } }
        //   { "channel": "flightStatus", "logAdded": "..." }
        // Property names are camelCase via WebJsonOptions; enums are string
        // names; TimeSpan rides as total-seconds.

        private readonly AppService _app;
        private readonly ConcurrentDictionary<Guid, Connection> _connections = new();

        // Whitelist for Config.PropertyChanged so we never leak internal /
        // operational fields to the wire (binary names, intervals, etc.).
        // Sourced from AppSettingsDto so the whitelist tracks the DTO without
        // a separate manual list.
        private static readonly HashSet<string> ConfigBroadcastWhitelist =
            typeof(AppSettingsDto).GetProperties()
                .Select(p => p.Name)
                .ToHashSet(StringComparer.Ordinal);

        public StateWebSocketHandler(AppService app)
        {
            _app = app;

            _app.FlightStatus.PropertyChanged += OnFlightStatusChanged;
            _app.Gsx.PropertyChanged += OnGsxChanged;
            _app.Audio.PropertyChanged += OnAudioChanged;
            _app.Config.PropertyChanged += OnConfigChanged;
            _app.Ofp.PropertyChanged += OnOfpChanged;
            _app.Checklist.PropertyChanged += OnChecklistChanged;
            _app.WeightBalance.PropertyChanged += OnWeightBalanceChanged;
            _app.Fuel.PropertyChanged += OnFuelChanged;
            _app.Loadsheet.PropertyChanged += OnLoadsheetChanged;
            _app.EfbFlightPlan.PropertyChanged += OnEfbFlightPlanChanged;
            _app.Notifications.PropertyChanged += OnNotificationsChanged;
            HookChecklistItems(_app.Checklist);
            _app.FlightStatus.MessageLog.CollectionChanged += OnMessageLogChanged;

            // GsxService is null in degraded mode (no SDK). Subscribe only when
            // present so a missing SDK doesn't NRE the handler ctor.
            if (_app.GsxService != null)
                _app.GsxService.PushbackPreferenceChanged += OnPushbackPreferenceChanged;

            // Connection kicking is wired from AppService once WebHost is
            // available — see AppService.CreateServiceControllers.
        }

        // Called by AppService after both WebHost and the handler exist; the
        // wiring is split so we don't have to make WebHost a constructor arg
        // (avoids a chicken-and-egg in the AppService init order).
        public void AttachToWebHost(WebHostService host)
        {
            host.TokenRotated += () => KickAll(WebSocketCloseStatus.PolicyViolation, "token rotated");
        }

        // Called from the /ws middleware after the WebSocket handshake. Owns
        // the socket from this point — caller does not Close/Dispose.
        public async Task HandleAsync(WebSocket socket, CancellationToken aborted)
        {
            if (!await TryAuthenticateAsync(socket, aborted))
            {
                await TryClose(socket, WebSocketCloseStatus.PolicyViolation, "auth required");
                return;
            }

            var conn = new Connection(socket);
            _connections.TryAdd(conn.Id, conn);
            try
            {
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(aborted, conn.Cts.Token);
                var writer = WriterLoopAsync(conn, linkedCts.Token);
                var reader = ReaderLoopAsync(conn, linkedCts.Token);

                // Send the full current state to this client BEFORE awaiting
                // either loop. Without this, slow-changing channels (Fuel,
                // W&B, Loadsheet, OFP, EfbFlightPlan, Notifications,
                // Checklists) would stay at their default values on the
                // client side because their per-property INPC events fire
                // only on real-world events (refuel, boarding, OFP import)
                // and the rising-edge BroadcastSnapshotAll only catches
                // clients that were already connected at the SDK transition.
                // Bytes are queued into conn.Outbound, the writer loop drains
                // them as soon as it starts (Channel<T> buffers — order
                // between this enqueue and writer-loop start doesn't matter).
                BroadcastSnapshotAll(target: conn);

                await Task.WhenAny(writer, reader);
                conn.Cts.Cancel();
                await Task.WhenAll(writer, reader);
            }
            finally
            {
                _connections.TryRemove(conn.Id, out _);
                await TryClose(socket, WebSocketCloseStatus.NormalClosure, "bye");
                conn.Dispose();
            }
        }

        // ── Auth ────────────────────────────────────────────────────────────

        private async Task<bool> TryAuthenticateAsync(WebSocket socket, CancellationToken aborted)
        {
            using var authCts = CancellationTokenSource.CreateLinkedTokenSource(aborted);
            authCts.CancelAfter(TimeSpan.FromSeconds(5));

            string presented;
            try
            {
                presented = ExtractAuthToken(await ReceiveTextAsync(socket, authCts.Token));
            }
            catch (OperationCanceledException) { return false; }
            catch { return false; }

            var expected = _app?.Config?.WebServerAuthToken;
            return !string.IsNullOrEmpty(presented)
                && !string.IsNullOrEmpty(expected)
                && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(presented),
                    Encoding.UTF8.GetBytes(expected));
        }

        private static string ExtractAuthToken(string firstFrame)
        {
            if (string.IsNullOrEmpty(firstFrame)) return null;
            try
            {
                using var doc = JsonDocument.Parse(firstFrame);
                if (doc.RootElement.TryGetProperty("auth", out var t) && t.ValueKind == JsonValueKind.String)
                    return t.GetString();
            }
            catch { }
            return null;
        }

        private static async Task<string> ReceiveTextAsync(WebSocket socket, CancellationToken ct)
        {
            var buffer = new byte[4096];
            using var ms = new System.IO.MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await socket.ReceiveAsync(buffer, ct);
                if (result.MessageType == WebSocketMessageType.Close) return null;
                ms.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        // ── Reader / writer loops ───────────────────────────────────────────

        private static async Task ReaderLoopAsync(Connection conn, CancellationToken ct)
        {
            var buffer = new byte[4096];
            try
            {
                while (!ct.IsCancellationRequested && conn.Socket.State == WebSocketState.Open)
                {
                    var result = await conn.Socket.ReceiveAsync(buffer, ct);
                    if (result.MessageType == WebSocketMessageType.Close)
                        break;
                    // Phase 6C ignores client messages beyond the auth frame.
                    // Future phases may add subscribe/unsubscribe controls here.
                }
            }
            catch { /* socket closed / cancelled */ }
        }

        private static async Task WriterLoopAsync(Connection conn, CancellationToken ct)
        {
            try
            {
                while (await conn.Outbound.Reader.WaitToReadAsync(ct))
                {
                    while (conn.Outbound.Reader.TryRead(out var frame))
                    {
                        if (conn.Socket.State != WebSocketState.Open) return;
                        await conn.Socket.SendAsync(frame, WebSocketMessageType.Text, endOfMessage: true, ct);
                    }
                }
            }
            catch { /* socket closed / cancelled */ }
        }

        // ── Store change → broadcast ────────────────────────────────────────

        private void OnFlightStatusChanged(object sender, PropertyChangedEventArgs e)
            => Broadcast(channel: "flightStatus", e.PropertyName, sender);

        private void OnGsxChanged(object sender, PropertyChangedEventArgs e)
            => Broadcast(channel: "gsx", e.PropertyName, sender);

        // Probe + broadcast handlers for the per-property channels. The
        // Logger.Debug calls at the top of each are deliberate: in March 2026
        // we hit a regression where fuel/weightBalance/audio (and ofp,
        // loadsheet via the custom handlers below) silently stopped firing
        // INPC because services were reading from a stale SDK cache. The
        // probe makes the next occurrence visible in DEBUG logs without
        // adding any cost in INFO-level operation. Gate on connection count
        // so the log stays clean when the web UI isn't open.
        private void OnAudioChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!_connections.IsEmpty) Logger.Debug($"WS audio: {e.PropertyName}");
            Broadcast(channel: "audio", e.PropertyName, sender);
        }

        private void OnWeightBalanceChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!_connections.IsEmpty) Logger.Debug($"WS weightBalance: {e.PropertyName}");
            Broadcast(channel: "weightBalance", e.PropertyName, sender);
        }

        private void OnFuelChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!_connections.IsEmpty) Logger.Debug($"WS fuel: {e.PropertyName}");
            Broadcast(channel: "fuel", e.PropertyName, sender);
        }

        // Loadsheet broadcasts the full Prelim+Final pair on every property
        // change rather than per-property patches — the React panel renders
        // both cards atomically (each card is one card, not 8 fields), and a
        // single combined patch is cheaper to reason about than 16 individual
        // PropertyChanged broadcasts. Cost is small: the DTO pair is ~20
        // primitives + 2 strings, fired only when something actually moved.
        private void OnLoadsheetChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_connections.IsEmpty) return;
            Logger.Debug($"WS loadsheet: {e.PropertyName}");
            try
            {
                var snap = LoadsheetSnapshotDto.From(_app);
                var envelope = new
                {
                    channel = "loadsheet",
                    patch = new Dictionary<string, object>
                    {
                        ["prelim"] = snap.Prelim,
                        ["final"] = snap.Final,
                    },
                };
                BroadcastBytes(JsonSerializer.SerializeToUtf8Bytes(envelope, WebJsonOptions.Default));
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        private void OnConfigChanged(object sender, PropertyChangedEventArgs e)
        {
            // Skip Config fields that aren't part of the AppSettingsDto wire
            // surface — internal/operational fields stay server-side.
            if (string.IsNullOrEmpty(e?.PropertyName)) return;
            if (!ConfigBroadcastWhitelist.Contains(e.PropertyName)) return;
            Broadcast(channel: "appSettings", e.PropertyName, sender);
        }

        // OFP-specific broadcast: skips internal-only flags
        // (AutoFired/SayIntentionsSent/GsxSent are workflow plumbing, not
        // wire surface) and projects the cached SayIntentionsAirportWx into
        // the WeatherDto wire shape so the internal type doesn't leak.
        private void OnOfpChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_connections.IsEmpty) return;
            var name = e?.PropertyName ?? "";
            if (string.IsNullOrEmpty(name)) return;
            if (sender is not OfpState ofp) return;
            Logger.Debug($"WS ofp: {name}");

            object value;
            switch (name)
            {
                case nameof(OfpState.AutoFired):
                case nameof(OfpState.SayIntentionsSent):
                case nameof(OfpState.GsxSent):
                    return; // internal flags, not on the wire
                case nameof(OfpState.DepartureWeather):
                    value = WeatherDto.From(ofp.DepartureWeather);
                    break;
                case nameof(OfpState.ArrivalWeather):
                    value = WeatherDto.From(ofp.ArrivalWeather);
                    break;
                default:
                    var prop = typeof(OfpState).GetProperty(name);
                    if (prop == null) return;
                    value = prop.GetValue(ofp);
                    break;
            }

            var camel = JsonNamingPolicy.CamelCase.ConvertName(name);
            try
            {
                var envelope = new
                {
                    channel = "ofp",
                    patch = new Dictionary<string, object> { [camel] = value },
                };
                BroadcastBytes(JsonSerializer.SerializeToUtf8Bytes(envelope, WebJsonOptions.Default));
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        // Notifications broadcast: full-snapshot per change. The list is
        // replaced wholesale on every Add/Dismiss, so a single snapshot is
        // the natural envelope — same shape the React panel needs to
        // recompute "most recent non-dismissed".
        private void OnNotificationsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_connections.IsEmpty) return;
            try
            {
                var dto = NotificationsSnapshotDto.From(_app);
                var envelope = new
                {
                    channel = "notifications",
                    snapshot = dto,
                };
                BroadcastBytes(JsonSerializer.SerializeToUtf8Bytes(envelope, WebJsonOptions.Default));
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        // EFB Flight Planning broadcast: full-snapshot per change. CurrentOfp
        // is a complex object (not a primitive) and the override dicts move
        // together with status, so per-property patches buy nothing — the
        // panel renders all fields atomically. Snapshot-only matches the
        // checklists + fmsSync convention.
        private void OnEfbFlightPlanChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_connections.IsEmpty) return;
            Logger.Debug($"WS efbFlightPlan: {e.PropertyName}");
            try
            {
                var dto = EfbFlightPlanDto.From(_app);
                var envelope = new
                {
                    channel = "efbFlightPlan",
                    snapshot = dto,
                };
                BroadcastBytes(JsonSerializer.SerializeToUtf8Bytes(envelope, WebJsonOptions.Default));
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        // ── Checklists channel ───────────────────────────────────────────────
        // The checklist wire surface is whole-snapshot per change. Per-item
        // IsChecked changes happen on individual ChecklistItemRuntime objects
        // (not on the store itself), so the simplest correct fan-out is to
        // re-serialise the full ChecklistDto whenever ANY relevant change
        // fires. Definitions are short (~80 items max) and changes are
        // human-paced (clicks + occasional dataref edges) so the bandwidth
        // cost is negligible compared to the simplicity of the contract.

        private readonly HashSet<ChecklistItemRuntime> _hookedItems = new();

        private void OnChecklistChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is ChecklistState cl && e?.PropertyName == nameof(ChecklistState.Definition))
                HookChecklistItems(cl);
            BroadcastChecklistSnapshot();
        }

        private void OnChecklistItemChanged(object sender, PropertyChangedEventArgs e)
            => BroadcastChecklistSnapshot();

        private void HookChecklistItems(ChecklistState cl)
        {
            if (cl == null) return;
            // Detach from any previously-hooked runtimes (LoadDefinition
            // replaces them all wholesale).
            foreach (var r in _hookedItems)
                r.PropertyChanged -= OnChecklistItemChanged;
            _hookedItems.Clear();

            foreach (var kvp in cl.ItemsBySection)
            {
                foreach (var rt in kvp.Value)
                {
                    rt.PropertyChanged += OnChecklistItemChanged;
                    _hookedItems.Add(rt);
                }
            }
        }

        private void BroadcastChecklistSnapshot(Connection target = null)
        {
            if (target == null && _connections.IsEmpty) return;
            try
            {
                var dto = ChecklistDto.From(_app);
                var envelope = new
                {
                    channel = "checklists",
                    snapshot = dto,
                };
                BroadcastBytes(JsonSerializer.SerializeToUtf8Bytes(envelope, WebJsonOptions.Default), target);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        // Full-state snapshot broadcast for every channel. Two callers:
        //   1. StateUpdateWorker rising-edge detection (target == null) —
        //      fans out to every connected client when the SDK transitions
        //      disconnected → connected. Catches clients that subscribed
        //      during the disconnected window before state stores were
        //      populated.
        //   2. HandleAsync on a fresh WebSocket connection (target == conn) —
        //      sends the full current state to ONE newly-connected client
        //      so its tabs render with real values immediately, instead of
        //      sitting at defaults until the next per-property INPC fires.
        //      Required because slow-changing stores (Fuel, W&B, Loadsheet,
        //      OFP, EfbFlightPlan, Notifications, Checklists) update only
        //      on real-world events (refuel, boarding, OFP import) so a
        //      client that connected mid-session would otherwise never see
        //      their values until such an event happened to fire.
        //
        // Patch-style channels (weightBalance, fuel, flightStatus, audio,
        // ofp, appSettings, gsx) ship as { channel, patch: <full DTO> } so
        // the existing client-side reducer's merge path picks them up
        // unchanged. Snapshot-style channels (loadsheet, efbFlightPlan,
        // notifications, checklists) re-use their existing snapshot
        // helpers so the wire shape is identical to a normal change.
        //
        // Idempotent and safe to call repeatedly; no-op when target is null
        // and no clients are connected.
        public void BroadcastSnapshotAll() => BroadcastSnapshotAll(null);

        private void BroadcastSnapshotAll(Connection target)
        {
            if (target == null && _connections.IsEmpty) return;
            Logger.Information(target == null
                ? "WS snapshot broadcast — pushing full state to all connected clients (SDK-connect rising edge)"
                : "WS snapshot — sending initial state to newly-connected client");

            // Patch-style channels with a dedicated DTO.
            BroadcastDtoAsPatch("weightBalance", WeightBalanceDto.From(_app), target);
            BroadcastDtoAsPatch("fuel", FuelDto.From(_app), target);
            BroadcastDtoAsPatch("flightStatus", FlightStatusDto.From(_app), target);
            BroadcastDtoAsPatch("audio", AudioDto.From(_app), target);
            BroadcastDtoAsPatch("ofp", OfpDto.From(_app), target);
            BroadcastDtoAsPatch("appSettings", AppSettingsDto.From(_app), target);

            // Patch-style "gsx" channel — no DTO (per-property INPC fan-out
            // works directly off GsxState). Reflect over the state object
            // and ship every public scalar property in one envelope so the
            // client gets the same key set it would receive from a tick's
            // worth of per-property patches.
            BroadcastStateAsPatch("gsx", _app?.Gsx, target);

            // Snapshot-style channels — re-use the existing helpers so the
            // wire shape stays identical to a property-driven snapshot.
            BroadcastChecklistSnapshot(target);

            try
            {
                var lsSnap = LoadsheetSnapshotDto.From(_app);
                var lsEnvelope = new
                {
                    channel = "loadsheet",
                    patch = new Dictionary<string, object>
                    {
                        ["prelim"] = lsSnap.Prelim,
                        ["final"] = lsSnap.Final,
                    },
                };
                BroadcastBytes(Serialize(lsEnvelope), target);
            }
            catch (Exception ex) { Logger.LogException(ex); }

            try
            {
                var notifEnvelope = new
                {
                    channel = "notifications",
                    snapshot = NotificationsSnapshotDto.From(_app),
                };
                BroadcastBytes(Serialize(notifEnvelope), target);
            }
            catch (Exception ex) { Logger.LogException(ex); }

            try
            {
                var efbEnvelope = new
                {
                    channel = "efbFlightPlan",
                    snapshot = EfbFlightPlanDto.From(_app),
                };
                BroadcastBytes(Serialize(efbEnvelope), target);
            }
            catch (Exception ex) { Logger.LogException(ex); }
        }

        // Wraps a DTO as a { channel, patch: <dto> } envelope. JsonSerializer
        // applies WebJsonOptions camelCase naming so the patch keys match
        // the per-property broadcast shape and the client's reducer merges
        // them into the existing channel state without any special handling.
        // When `target` is non-null, sends to that connection only.
        private void BroadcastDtoAsPatch(string channel, object dto, Connection target = null)
        {
            if (dto == null) return;
            try
            {
                var envelope = new { channel, patch = dto };
                BroadcastBytes(Serialize(envelope), target);
            }
            catch (Exception ex) { Logger.LogException(ex); }
        }

        // Reflection-based snapshot for channels that don't have a dedicated
        // DTO (currently just "gsx"). Mirrors what an exhaustive sequence of
        // per-property INPC broadcasts would produce: every public readable
        // scalar, camelCased, packed into one patch envelope. When `target`
        // is non-null, sends to that connection only.
        private void BroadcastStateAsPatch(string channel, object stateObject, Connection target = null)
        {
            if (stateObject == null) return;
            try
            {
                var dict = new Dictionary<string, object>();
                foreach (var prop in stateObject.GetType().GetProperties())
                {
                    if (prop.GetIndexParameters().Length > 0) continue;
                    if (!prop.CanRead) continue;
                    try
                    {
                        var camel = JsonNamingPolicy.CamelCase.ConvertName(prop.Name);
                        dict[camel] = prop.GetValue(stateObject);
                    }
                    catch { }
                }
                var envelope = new { channel, patch = dict };
                BroadcastBytes(Serialize(envelope), target);
            }
            catch (Exception ex) { Logger.LogException(ex); }
        }

        // FMS sync result → "fmsSync" channel snapshot. Fired only after a
        // POST /api/fms/sync attempt — this is a one-shot broadcast (not
        // per-property), so listeners can drive a flash + label transition
        // without subscribing to W&B deltas. Caller is FmsSyncService.
        public void BroadcastFmsSync(FmsSyncResultDto result)
        {
            if (_connections.IsEmpty || result == null) return;
            try
            {
                var envelope = new
                {
                    channel = "fmsSync",
                    snapshot = result,
                };
                BroadcastBytes(JsonSerializer.SerializeToUtf8Bytes(envelope, WebJsonOptions.Default));
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        // GsxController.PushbackPreferenceChanged → "ofp" channel patch.
        // Lives outside the OFP store because the preference itself is on
        // GsxController (in-memory, session-scoped); the WS broadcast keeps
        // multiple clients synchronised regardless of who wrote it.
        private void OnPushbackPreferenceChanged(PushbackPreference pref)
        {
            if (_connections.IsEmpty) return;
            try
            {
                var envelope = new
                {
                    channel = "ofp",
                    patch = new Dictionary<string, object> { ["pushbackPreference"] = pref },
                };
                BroadcastBytes(JsonSerializer.SerializeToUtf8Bytes(envelope, WebJsonOptions.Default));
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        private void OnMessageLogChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add || e.NewItems == null) return;
            foreach (var item in e.NewItems)
            {
                if (item is not string msg) continue;
                BroadcastBytes(SerializeLogAdd(msg));
            }
        }

        // Reads the property value from the sender via reflection and
        // serialises a { channel, patch } envelope. Property naming policy is
        // applied so the wire name is camelCase even though the source
        // property is PascalCase.
        private void Broadcast(string channel, string propertyName, object sender)
        {
            if (_connections.IsEmpty) return;
            if (string.IsNullOrEmpty(propertyName)) return;

            try
            {
                var prop = sender.GetType().GetProperty(propertyName);
                if (prop == null) return;
                var value = prop.GetValue(sender);
                var camel = JsonNamingPolicy.CamelCase.ConvertName(propertyName);

                var envelope = new
                {
                    channel,
                    patch = new Dictionary<string, object> { [camel] = value },
                };
                BroadcastBytes(Serialize(envelope));
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        private static byte[] Serialize<T>(T envelope)
            => JsonSerializer.SerializeToUtf8Bytes(envelope, WebJsonOptions.Default);

        private static byte[] SerializeLogAdd(string msg)
            => JsonSerializer.SerializeToUtf8Bytes(new { channel = "flightStatus", logAdded = msg }, WebJsonOptions.Default);

        private void BroadcastBytes(byte[] frame, Connection target = null)
        {
            // Best-effort enqueue. Two modes:
            //   target == null → fan out to every connected client
            //   target != null → send to that one connection only (used by
            //                    the on-connect initial-snapshot path)
            // If a writer is overwhelmed the channel write may fail — drop
            // and let the reader loop detect the dead socket.
            if (target != null)
            {
                if (!target.Outbound.Writer.TryWrite(frame))
                    target.Cts.Cancel();
                return;
            }
            foreach (var conn in _connections.Values)
            {
                if (!conn.Outbound.Writer.TryWrite(frame))
                    conn.Cts.Cancel();
            }
        }

        // ── Token-rotation kick ─────────────────────────────────────────────

        public void KickAll(WebSocketCloseStatus status, string reason)
        {
            foreach (var conn in _connections.Values)
            {
                try { conn.Cts.Cancel(); } catch { }
                // Route through TryClose (which catches internally) instead of
                // a bare _ = Socket.CloseAsync(...). The bare form's Task can
                // fault when the socket has raced into the Aborted state; the
                // fault is unobserved (no awaiter, no .Exception read), and a
                // later finalizer pass re-throws it as AggregateException —
                // which the CFIT unhandled-exception handler treats as an app
                // crash. TryClose's internal catch ensures the returned Task
                // never faults, so the discard is safe.
                _ = TryClose(conn.Socket, status, reason);
            }
        }

        private static async Task TryClose(WebSocket socket, WebSocketCloseStatus status, string reason)
        {
            try
            {
                if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
                    await socket.CloseAsync(status, reason, CancellationToken.None);
            }
            catch { }
        }

        // ── Connection ──────────────────────────────────────────────────────

        private sealed class Connection : IDisposable
        {
            public Guid Id { get; } = Guid.NewGuid();
            public WebSocket Socket { get; }
            public Channel<byte[]> Outbound { get; }
            public CancellationTokenSource Cts { get; } = new();

            public Connection(WebSocket socket)
            {
                Socket = socket;
                // Bounded so a slow client cannot blow up server memory; new
                // frames are dropped (and the connection is killed) when the
                // queue is saturated.
                Outbound = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(256)
                {
                    SingleReader = true,
                    SingleWriter = false,
                    FullMode = BoundedChannelFullMode.DropWrite,
                });
            }

            public void Dispose()
            {
                try { Cts.Cancel(); } catch { }
                Cts.Dispose();
                Outbound.Writer.TryComplete();
            }
        }
    }
}
