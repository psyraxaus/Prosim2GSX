using CFIT.AppLogger;
using Prosim2GSX.AppConfig;
using Prosim2GSX.State;
using Prosim2GSX.Web.Contracts;
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
            _app.FlightStatus.MessageLog.CollectionChanged += OnMessageLogChanged;

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

        private void OnAudioChanged(object sender, PropertyChangedEventArgs e)
            => Broadcast(channel: "audio", e.PropertyName, sender);

        private void OnConfigChanged(object sender, PropertyChangedEventArgs e)
        {
            // Skip Config fields that aren't part of the AppSettingsDto wire
            // surface — internal/operational fields stay server-side.
            if (string.IsNullOrEmpty(e?.PropertyName)) return;
            if (!ConfigBroadcastWhitelist.Contains(e.PropertyName)) return;
            Broadcast(channel: "appSettings", e.PropertyName, sender);
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

        private void BroadcastBytes(byte[] frame)
        {
            // Best-effort enqueue per connection. If a writer is overwhelmed
            // the channel write may fail — drop and let the reader loop
            // detect the dead socket.
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
