using CFIT.AppLogger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
            Interlocked.Increment(ref _tokenGeneration);
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
                    if (_app.Config.WebServerEnabled)
                    {
                        Task.Run(() =>
                        {
                            Stop();
                            Start();
                        });
                    }
                    break;
            }
        }

        // Builds the ASP.NET Core pipeline. Must be called under _lock.
        private void DoStart()
        {
            var builder = WebApplication.CreateBuilder();

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

            // Bearer-token gate sits before routing so unauthorised requests
            // never reach controller code.
            _webApp.UseMiddleware<BearerTokenMiddleware>();

            _webApp.UseRouting();
            _webApp.MapControllers();

            _runCts = new CancellationTokenSource();

            // RunAsync returns when the cancellation token fires (i.e. on Stop).
            // Capture the Task so Stop can await it, ensuring the port is fully
            // released before any restart re-binds.
            _runTask = _webApp.RunAsync(_runCts.Token);
        }

        // Best-effort shutdown — must be called under _lock. Swallows any
        // shutdown exception because we've already logged the start failure
        // path; trying to surface a stop failure on top would be noise.
        private void DoStopBestEffort()
        {
            try { _runCts?.Cancel(); } catch { }
            try { _runTask?.Wait(TimeSpan.FromSeconds(5)); } catch { }
            try { (_webApp as IAsyncDisposable)?.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(5)); } catch { }
            _webApp = null;
            _runCts?.Dispose();
            _runCts = null;
            _runTask = null;
        }
    }
}
