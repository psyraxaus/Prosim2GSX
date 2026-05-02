using CFIT.AppLogger;
using Prosim2GSX.SayIntentions;
using Prosim2GSX.State;
using Prosim2GSX.Web.Contracts.Commands;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Commands.Handlers
{
    // OFP command handlers. Each Register call adds one named operation to
    // the registry. Handlers mutate AppService.Ofp (the long-lived state
    // store) so changes survive tab churn and are visible to multiple
    // clients via the future WS broadcast (Phase 8B).
    //
    // CommandRegistry already marshals onto the WPF dispatcher, so handlers
    // can freely read from AircraftInterface / GsxController / Config without
    // additional thread juggling. SayIntentions calls (HTTP I/O) await
    // naturally; the marshal only governs where the handler STARTS.
    public static class OfpHandlers
    {
        public static void Register(AppService app, CommandRegistry registry)
        {
            registry.Register<ConfirmArrivalGateRequest, GateAssignmentDto>(
                "ofp.confirmArrivalGate",
                (req, ct) => ConfirmArrivalGate(app, req, ct));

            registry.Register<SendNowRequest, GateAssignmentDto>(
                "ofp.sendNow",
                (req, ct) => SendNow(app, ct));

            registry.Register<RefreshWeatherRequest, WeatherSnapshotDto>(
                "ofp.refreshWeather",
                (req, ct) => RefreshWeather(app, ct));

            registry.Register<SetPushbackPreferenceRequest, PushbackPreferenceDto>(
                "ofp.setPushbackPreference",
                (req, ct) => SetPushbackPreference(app, req, ct));
        }

        // ── confirmArrivalGate ──────────────────────────────────────────────

        private static Task<GateAssignmentDto> ConfirmArrivalGate(
            AppService app, ConfirmArrivalGateRequest req, CancellationToken _)
        {
            var ofp = app.Ofp;
            var ai = app.GsxService?.AircraftInterface;

            var gate = (req?.Gate ?? "").Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(gate))
                throw new CommandValidationException("Arrival gate must not be empty.");
            if (ai?.IsFlightPlanLoaded != true)
                throw new CommandValidationException("OFP not loaded — cannot assign gate yet.");

            ofp.PendingArrivalGate = gate;
            ofp.AutoFired = false;
            ofp.SayIntentionsSent = false;
            ofp.GsxSent = false;
            ofp.GateAssignmentStatus = app.SayIntentionsService?.IsActive == true
                ? "Queued — will be sent to ATC during cruise (or click 'Send Now')."
                : "SayIntentions not active — ATC assignment will be skipped.";
            ofp.GsxAssignmentStatus = "Queued — will be sent to GSX during cruise (or click 'Send Now').";

            return Task.FromResult(BuildGateAssignment(ofp));
        }

        // ── sendNow ─────────────────────────────────────────────────────────

        // Public so the OfpAutoSendService (and any other internal caller)
        // can fire the same gate-assignment workflow as the manual "Send Now"
        // button. The handler indirection still goes through the registry,
        // this overload just lets us bypass the dispatcher marshal when we
        // already know the call site is on a safe thread.
        internal static Task<GateAssignmentDto> SendNowAsync(AppService app)
            => SendNow(app, CancellationToken.None);

        private static async Task<GateAssignmentDto> SendNow(AppService app, CancellationToken _)
        {
            var ofp = app.Ofp;
            var gate = ofp.PendingArrivalGate;
            if (string.IsNullOrWhiteSpace(gate))
                return BuildGateAssignment(ofp);

            // SayIntentions step
            if (!ofp.SayIntentionsSent)
            {
                if (app.SayIntentionsService?.IsActive == true)
                {
                    ofp.GateAssignmentStatus = "Sending to ATC ...";
                    var arrivalIcao = ResolveArrivalIcao(app);
                    var result = await app.SayIntentionsService.AssignGateAsync(arrivalIcao, gate);
                    if (result.Ok)
                    {
                        ofp.GateAssignmentStatus = $"ATC assignment confirmed: {result.AssignedGate}";
                        ofp.SayIntentionsSent = true;
                    }
                    else
                    {
                        ofp.GateAssignmentStatus = $"ATC assignment failed: {result.Error}";
                    }
                }
                else
                {
                    // Skipped, don't retry forever.
                    ofp.GateAssignmentStatus = "SayIntentions not active — ATC assignment skipped.";
                    ofp.SayIntentionsSent = true;
                }
            }

            // GSX step
            if (!ofp.GsxSent)
            {
                var ctrl = app.GsxService;
                if (ctrl != null)
                {
                    ofp.GsxAssignmentStatus = $"Sending '{gate}' to GSX ...";
                    var ok = await ctrl.SetArrivalParkingAsync(gate);
                    if (ok)
                    {
                        ofp.GsxAssignmentStatus = $"GSX gate set: {gate}";
                        ofp.GsxSent = true;
                    }
                    else
                    {
                        ofp.GsxAssignmentStatus = $"GSX selection failed for '{gate}' — check the GSX log/menu.";
                    }
                }
                else
                {
                    ofp.GsxAssignmentStatus = "GSX not available.";
                    ofp.GsxSent = true;
                }
            }

            // Both done — clear the pending gate so the workflow is back to idle.
            if (ofp.SayIntentionsSent && ofp.GsxSent)
                ofp.PendingArrivalGate = "";

            return BuildGateAssignment(ofp);
        }

        // ── refreshWeather ──────────────────────────────────────────────────

        // Cache + debounce policy for the SayIntentions weather + CPDLC
        // pulls. Stops the WPF tab-flip and multi-browser-client patterns
        // from "smashing" the API:
        //   - TTL: cache entries ≤10 min old are served as-is.
        //   - Debounce: a forced refresh within 30s of the last successful
        //     fetch returns cache (with a friendly status). Protects against
        //     button-spam and concurrent client refreshes.
        //   - Dedupe: RefreshGate (SemaphoreSlim 1) means a second concurrent
        //     caller waits, then exits on the !isStale short-circuit and gets
        //     the first call's result for free.
        private static readonly TimeSpan WeatherCacheTtl = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan WeatherRefreshDebounce = TimeSpan.FromSeconds(30);

        private static async Task<WeatherSnapshotDto> RefreshWeather(AppService app, CancellationToken _)
        {
            var ofp = app.Ofp;

            if (app.SayIntentionsService?.IsActive != true)
            {
                ofp.WeatherStatus = "SayIntentions not active. Enable in App Settings and ensure flight.json is present.";
                ofp.DepartureWeather = null;
                ofp.ArrivalWeather = null;
                ofp.CpdlcStation = "";
                return BuildWeatherSnapshot(ofp);
            }

            await ofp.RefreshGate.WaitAsync();
            try
            {
                var now = DateTimeOffset.UtcNow;
                var hasCache = ofp.WeatherFetchedAt.HasValue;
                var isStale = !hasCache || (now - ofp.WeatherFetchedAt.Value) > WeatherCacheTtl;
                var sinceLastForced = ofp.LastForcedRefreshAt.HasValue
                    ? now - ofp.LastForcedRefreshAt.Value
                    : TimeSpan.MaxValue;
                var isDebounced = sinceLastForced < WeatherRefreshDebounce;

                // Cache fresh → serve as-is, no HTTP.
                if (!isStale)
                    return BuildWeatherSnapshot(ofp);

                // Stale but user clicked refresh too soon → do not re-fetch.
                if (isDebounced)
                {
                    ofp.WeatherStatus = $"Refresh limited — last update <{(int)WeatherRefreshDebounce.TotalSeconds}s ago.";
                    return BuildWeatherSnapshot(ofp);
                }

                var depIcao = ResolveDepartureIcao(app);
                var arrIcao = ResolveArrivalIcao(app);
                var icaos = new List<string>();
                if (!string.IsNullOrWhiteSpace(depIcao)) icaos.Add(depIcao);
                if (!string.IsNullOrWhiteSpace(arrIcao) && arrIcao != depIcao) icaos.Add(arrIcao);

                if (icaos.Count == 0)
                {
                    ofp.WeatherStatus = "No ICAOs available — load an OFP first.";
                    return BuildWeatherSnapshot(ofp);
                }

                ofp.IsRefreshingWeather = true;
                ofp.WeatherStatus = "";
                try
                {
                    // Fire weather + CPDLC in parallel — both are independent
                    // SayIntentions calls and share the same TTL window.
                    var weatherTask = app.SayIntentionsService.GetWeatherAsync(icaos);
                    var cpdlcTask = app.SayIntentionsService.GetCpdlcStationAsync();
                    await Task.WhenAll(weatherTask, cpdlcTask);

                    var results = weatherTask.Result;
                    SayIntentionsAirportWx dep = null, arr = null;
                    foreach (var wx in results)
                    {
                        if (string.Equals(wx.Airport, depIcao, StringComparison.OrdinalIgnoreCase)) dep = wx;
                        else if (string.Equals(wx.Airport, arrIcao, StringComparison.OrdinalIgnoreCase)) arr = wx;
                    }
                    ofp.DepartureWeather = dep;
                    ofp.ArrivalWeather = arr;
                    ofp.CpdlcStation = cpdlcTask.Result ?? "";

                    if (dep == null && arr == null)
                        ofp.WeatherStatus = "Weather request returned no data.";

                    ofp.WeatherFetchedAt = now;
                    ofp.LastForcedRefreshAt = now;
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                    ofp.WeatherStatus = $"Weather request failed: {ex.Message}";
                }
                finally
                {
                    ofp.IsRefreshingWeather = false;
                }

                return BuildWeatherSnapshot(ofp);
            }
            finally
            {
                ofp.RefreshGate.Release();
            }
        }

        // ── setPushbackPreference ───────────────────────────────────────────

        private static Task<PushbackPreferenceDto> SetPushbackPreference(
            AppService app, SetPushbackPreferenceRequest req, CancellationToken _)
        {
            var ctrl = app.GsxService;
            if (ctrl == null)
                throw new CommandValidationException("GSX service not available.");

            ctrl.PushbackPreference = req.Preference;
            return Task.FromResult(new PushbackPreferenceDto { Preference = ctrl.PushbackPreference });
        }

        // ── helpers ─────────────────────────────────────────────────────────

        private static string ResolveDepartureIcao(AppService app)
        {
            var ai = app.GsxService?.AircraftInterface;
            if (ai == null) return "";
            return ai.FmsOrigin ?? ai.LastSimbriefOfp?.Origin?.IcaoCode ?? "";
        }

        private static string ResolveArrivalIcao(AppService app)
        {
            var ai = app.GsxService?.AircraftInterface;
            if (ai == null) return "";
            return ai.FmsDestination ?? ai.LastSimbriefOfp?.Destination?.IcaoCode ?? "";
        }

        private static GateAssignmentDto BuildGateAssignment(OfpState ofp) => new()
        {
            PendingArrivalGate = ofp.PendingArrivalGate,
            GateAssignmentStatus = ofp.GateAssignmentStatus,
            GsxAssignmentStatus = ofp.GsxAssignmentStatus,
        };

        private static WeatherSnapshotDto BuildWeatherSnapshot(OfpState ofp) => new()
        {
            DepartureWeather = WeatherDto.From(ofp.DepartureWeather),
            ArrivalWeather = WeatherDto.From(ofp.ArrivalWeather),
            WeatherStatus = ofp.WeatherStatus,
            IsRefreshingWeather = ofp.IsRefreshingWeather,
            CpdlcStation = ofp.CpdlcStation ?? "",
            WeatherFetchedAt = ofp.WeatherFetchedAt,
        };
    }
}
