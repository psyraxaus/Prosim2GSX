using CFIT.AppLogger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prosim2GSX.AppConfig;
using Prosim2GSX.Web.Contracts;
using Prosim2GSX.Web.Middleware;
using System;
using System.ComponentModel;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Web
{
    // Embedded ASP.NET Core / Kestrel host. Wraps a WebApplication and exposes
    // Start/Stop methods that AppService and the App Settings tab call. Hot-
    // toggles in response to Config.WebServerEnabled / Port / BindAll changes
    // so the user can flip the checkbox at runtime.
    //
    // Lifecycle is intentionally separate from CFIT's SimApp host: this stays a
    // peer service hung off AppService rather than being mixed into the WPF
    // app's main composition root. That keeps the WPF startup path unchanged
    // and means web-host failures (e.g. port-in-use) only affect the web
    // surface, never the desktop UI.
    public class WebHostService
    {
        private readonly AppService _app;
        private readonly object _lock = new();

        // _webApp is non-null while the host is running. _runTask is the
        // host's RunAsync Task; we await it on stop so a port really has been
        // released before any subsequent Start tries to rebind.
        private WebApplication _webApp;
        private CancellationTokenSource _runCts;
        private Task _runTask;

        // Bumped by RegenerateToken — Phase 6C's WebSocket handler reads this
        // to detect when an open connection's auth token has been invalidated
        // and force-close the socket.
        private int _tokenGeneration;
        public int TokenGeneration => Volatile.Read(ref _tokenGeneration);

        // Raised after a successful RegenerateToken — StateWebSocketHandler
        // subscribes and kicks every active connection.
        public event Action TokenRotated;

        public bool IsRunning
        {
            get { lock (_lock) return _webApp != null; }
        }

        public WebHostService(AppService app)
        {
            _app = app;
            // Subscribe to Config so the App Settings checkbox / port / bindAll
            // takes effect live. Unsubscription happens implicitly when the
            // app process exits — WebHostService lives for the app's lifetime.
            _app.Config.PropertyChanged += OnConfigChanged;

            // Keep the in-sim Python handler's PROSIM2GSX_PORT line aligned
            // with the configured port — runs unconditionally on startup
            // regardless of WebServerEnabled, since the installer always
            // drops the script and the user may flip the toggle later.
            GsxHandlerSync.EnsurePort(_app.Config.WebServerPort);
        }

        // Start the host if Config.WebServerEnabled and not already running.
        // Generates an auth token on first start when none has been persisted.
        public void Start()
        {
            lock (_lock)
            {
                if (_webApp != null) return;

                if (string.IsNullOrEmpty(_app.Config.WebServerAuthToken))
                {
                    _app.Config.WebServerAuthToken = Guid.NewGuid().ToString("N");
                    _app.Config.SaveConfiguration();
                    Logger.Information("Web server auth token generated on first start.");
                }

                try
                {
                    DoStart();
                    Logger.Information(
                        $"Web server listening on http://{(_app.Config.WebServerBindAll ? "0.0.0.0" : "127.0.0.1")}:{_app.Config.WebServerPort}");
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to start web server.");
                    Logger.LogException(ex);
                    DoStopBestEffort();
                }
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                if (_webApp == null) return;
                DoStopBestEffort();
                Logger.Information("Web server stopped.");
            }
        }

        // Generates a new auth token, persists it, and bumps TokenGeneration so
        // the WebSocket handler will kick existing connections (Phase 6C).
        // Existing REST clients will start receiving 401 on their next request
        // because the middleware re-reads Config.WebServerAuthToken every call.
        public string RegenerateToken()
        {
            var newToken = Guid.NewGuid().ToString("N");
            _app.Config.WebServerAuthToken = newToken;
            _app.Config.SaveConfiguration();
            // Config's auto-property setter doesn't raise PropertyChanged, so
            // the WS handler's "appSettings" channel and any other Config
            // subscriber would otherwise miss this change. Raise it
            // explicitly to keep wire-side observers in sync with the WPF UI.
            _app.Config.NotifyPropertyChanged(nameof(Config.WebServerAuthToken));
            Interlocked.Increment(ref _tokenGeneration);
            try { TokenRotated?.Invoke(); } catch { }
            Logger.Information("Web server auth token regenerated; existing clients invalidated.");
            return newToken;
        }

        private void OnConfigChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e?.PropertyName)
            {
                case nameof(Config.WebServerEnabled):
                    if (_app.Config.WebServerEnabled)
                        Task.Run(Start);
                    else
                        Task.Run(Stop);
                    break;

                case nameof(Config.WebServerPort):
                case nameof(Config.WebServerBindAll):
                    if (e?.PropertyName == nameof(Config.WebServerPort))
                        GsxHandlerSync.EnsurePort(_app.Config.WebServerPort);
                    if (_app.Config.WebServerEnabled)
                    {
                        // Wrapped in try/catch so any future bug in Stop/Start
                        // can't escape as an unobserved Task exception (which
                        // would crash the app at the next finalizer pass).
                        Task.Run(() =>
                        {
                            try
                            {
                                Stop();
                                Start();
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Web server hot-restart failed.");
                                Logger.LogException(ex);
                            }
                        });
                    }
                    break;
            }
        }

        // Builds the ASP.NET Core pipeline. Must be called under _lock.
        private void DoStart()
        {
            // Pin ContentRoot/WebRoot to the exe directory so static-file
            // resolution works regardless of the process's current directory
            // at launch (CFIT may set this to anything). wwwroot is created
            // up-front so UseStaticFiles doesn't fail when Phase 7 hasn't
            // built the React bundle yet.
            var exeDir = System.AppContext.BaseDirectory;
            var webRoot = System.IO.Path.Combine(exeDir, "wwwroot");
            System.IO.Directory.CreateDirectory(webRoot);

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ContentRootPath = exeDir,
                WebRootPath = webRoot,
            });

            builder.WebHost.ConfigureKestrel(options =>
            {
                var address = _app.Config.WebServerBindAll ? IPAddress.Any : IPAddress.Loopback;
                options.Listen(address, _app.Config.WebServerPort);
            });

            // Make AppService and ourselves resolvable from controllers/handlers.
            builder.Services.AddSingleton(_app);
            builder.Services.AddSingleton(this);

            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                WebJsonOptions.Configure(options.JsonSerializerOptions);
            });

#if DEBUG
            // Vite dev server runs on 5173; allow it to talk to the API in
            // local development. Release builds serve the React app from the
            // same origin (wwwroot) so CORS isn't needed.
            builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
                p.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
                 .AllowAnyHeader()
                 .AllowAnyMethod()
                 .AllowCredentials()));
#endif

            _webApp = builder.Build();

#if DEBUG
            _webApp.UseCors();
#endif

            // WebSockets middleware MUST be registered BEFORE UseRouting /
            // MapControllers, otherwise the endpoint that handles /ws fires
            // before the upgrade middleware has populated HttpContext.WebSockets,
            // and the handshake fails (browser sees a generic
            // "WebSocket connection failed" with no useful detail).
            _webApp.UseWebSockets();

            // Static files next so wwwroot/index.html / wwwroot/assets/* are
            // served without going through the bearer gate. The HTML/JS bundle
            // is public — only /api/* needs the token, and the bearer
            // middleware enforces that explicitly by path prefix.
            _webApp.UseStaticFiles();

            // Bearer-token gate. Internally limits itself to /api/* paths.
            _webApp.UseMiddleware<BearerTokenMiddleware>();

            _webApp.UseRouting();
            _webApp.MapControllers();

            // WebSocket endpoint at /ws. Authentication is per-connection on
            // the first inbound frame (browsers can't send custom headers on
            // a WS upgrade and putting the token in the URL would leak it),
            // so the bearer-token middleware exempts /ws and the handler does
            // its own check.
            _webApp.Map("/ws", async context =>
            {
                if (!context.WebSockets.IsWebSocketRequest)
                {
                    context.Response.StatusCode = 400;
                    return;
                }

                var handler = _app?.WebSocketHandler;
                if (handler == null)
                {
                    context.Response.StatusCode = 503;
                    return;
                }

                using var socket = await context.WebSockets.AcceptWebSocketAsync();
                await handler.HandleAsync(socket, context.RequestAborted);
            });

            // SPA fallback: any non-API, non-WS, non-static path serves the
            // React app's index.html so client-side routing works (deep links
            // like /audio reload cleanly). When wwwroot is empty (Phase 6
            // state, before Phase 7 builds React) the response is a friendly
            // 404 message rather than letting the browser see an empty body.
            _webApp.MapFallback(async context =>
            {
                var path = context.Request.Path.Value ?? "";
                if (path.StartsWith("/api", System.StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith("/ws", System.StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = 404;
                    return;
                }

                var indexPath = System.IO.Path.Combine(webRoot, "index.html");
                if (!System.IO.File.Exists(indexPath))
                {
                    context.Response.StatusCode = 404;
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync(
                        "Web UI not deployed. Run `npm run build` in Prosim2GSX.Web/ to populate wwwroot.");
                    return;
                }
                context.Response.ContentType = "text/html";
                await context.Response.SendFileAsync(indexPath);
            });

            _runCts = new CancellationTokenSource();

            // RunAsync returns when the cancellation token fires (i.e. on Stop).
            // Capture the Task so Stop can await it, ensuring the port is fully
            // released before any restart re-binds.
            _runTask = _webApp.RunAsync(_runCts.Token);
        }

        // Best-effort shutdown — must be called under _lock. Swallows any
        // shutdown exception because we've already logged the start failure
        // path; trying to surface a stop failure on top would be noise.
        //
        // Active WebSocket connections will hold the host open until they
        // drain naturally, so we kick them upfront — otherwise Kestrel waits
        // for the browser's ws to close before completing shutdown, which
        // can stall a hot-toggle (port/bind change) for many seconds.
        private void DoStopBestEffort()
        {
            try
            {
                _app?.WebSocketHandler?.KickAll(
                    System.Net.WebSockets.WebSocketCloseStatus.EndpointUnavailable,
                    "host restart");
            }
            catch { }

            try { _runCts?.Cancel(); } catch { }
            // Short waits — if a peer refuses to close cleanly we'd rather
            // let DisposeAsync force-tear-down than block the UI thread for
            // 10 seconds. 1.5s is enough for an orderly graceful shutdown
            // on loopback / LAN.
            try { _runTask?.Wait(TimeSpan.FromSeconds(1.5)); } catch { }
            try { (_webApp as IAsyncDisposable)?.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(1.5)); } catch { }
            _webApp = null;
            _runCts?.Dispose();
            _runCts = null;
            _runTask = null;
        }
    }
}
