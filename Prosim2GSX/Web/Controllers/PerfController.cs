using CFIT.AppLogger;
using Microsoft.AspNetCore.Mvc;
using Prosim2GSX.Web.Contracts;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace Prosim2GSX.Web.Controllers
{
    // REST surface for the TAKEOFF + LANDING performance tabs. Both branches
    // hop onto the WPF dispatcher for state reads/writes so they serialise
    // against the StateUpdateWorker tick — same pattern as WeightBalance,
    // Loadsheet, Fms, EfbFlightPlan controllers.
    //
    // Authentication runs in BearerTokenMiddleware (one layer up); errors
    // surface as { error: "…" } 4xx bodies.
    [ApiController]
    [Route("api/perf")]
    public class PerfController : ControllerBase
    {
        private readonly AppService _app;
        public PerfController(AppService app) => _app = app;

        // =================================================================
        //  TAKEOFF
        // =================================================================

        [HttpGet("takeoff")]
        public async Task<ActionResult<TakeoffPerfStateDto>> GetTakeoff()
        {
            var dto = await Application.Current.Dispatcher.InvokeAsync(() => TakeoffPerfStateDto.From(_app));
            return Ok(dto);
        }

        // Partial update — any non-null field in the payload is applied to
        // the takeoff state. Mutation clears the IsUplinked badge so the
        // "Uplink Sent" indicator doesn't outlive the values it wrote.
        [HttpPost("takeoff/inputs")]
        public async Task<ActionResult<TakeoffPerfStateDto>> PostTakeoffInputs([FromBody] TakeoffInputsDto inputs)
        {
            if (inputs == null) return BadRequest(new { error = "Missing body" });

            var dto = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var s = _app?.TakeoffPerf;
                if (s == null) return new TakeoffPerfStateDto();
                if (inputs.ApplyTo(s))
                {
                    s.IsUplinked = false;
                    s.UplinkedAt = null;
                }
                return TakeoffPerfStateDto.From(_app);
            });
            return Ok(dto);
        }

        // Runs the runway + METAR fetch on a background thread (gateway
        // round-trip), then projects the resulting state on the dispatcher.
        // The async path runs OFF the dispatcher — only the final snapshot
        // read hops onto it, so the perf calls don't block the UI thread.
        [HttpPost("takeoff/load-runways")]
        public async Task<ActionResult<TakeoffPerfStateDto>> LoadTakeoffRunways([FromQuery] string icao)
        {
            if (string.IsNullOrWhiteSpace(icao))
                return BadRequest(new { error = "icao query parameter is required" });

            var svc = _app?.TakeoffPerfService;
            if (svc == null) return ServiceUnavailable("Takeoff perf service unavailable");

            await svc.LoadRunwaysAsync(icao, HttpContext.RequestAborted);
            var dto = await Application.Current.Dispatcher.InvokeAsync(() => TakeoffPerfStateDto.From(_app));
            return Ok(dto);
        }

        [HttpPost("takeoff/sync-loadsheet")]
        public async Task<ActionResult<TakeoffPerfStateDto>> SyncLoadsheet()
        {
            var svc = _app?.TakeoffPerfService;
            if (svc == null) return ServiceUnavailable("Takeoff perf service unavailable");

            var dto = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                svc.SyncFromLoadsheet();
                // Pulling new weights from the loadsheet invalidates a
                // prior uplink — drop the badge so the user re-sends.
                var s = _app?.TakeoffPerf;
                if (s != null) { s.IsUplinked = false; s.UplinkedAt = null; }
                return TakeoffPerfStateDto.From(_app);
            });
            return Ok(dto);
        }

        [HttpPost("takeoff/calculate")]
        public async Task<ActionResult<TakeoffPerfStateDto>> CalculateTakeoff()
        {
            var svc = _app?.TakeoffPerfService;
            if (svc == null) return ServiceUnavailable("Takeoff perf service unavailable");

            await svc.CalculateAsync(HttpContext.RequestAborted);
            var dto = await Application.Current.Dispatcher.InvokeAsync(() => TakeoffPerfStateDto.From(_app));
            return Ok(dto);
        }

        [HttpPost("takeoff/uplink")]
        public async Task<ActionResult<TakeoffPerfStateDto>> UplinkTakeoff()
        {
            var svc = _app?.TakeoffPerfService;
            if (svc == null) return ServiceUnavailable("Takeoff perf service unavailable");

            var dto = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                svc.SendUplinkToFms();
                return TakeoffPerfStateDto.From(_app);
            });
            return Ok(dto);
        }

        [HttpPost("takeoff/reset")]
        public async Task<ActionResult<TakeoffPerfStateDto>> ResetTakeoff()
        {
            var dto = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _app?.TakeoffPerfService?.Reset();
                return TakeoffPerfStateDto.From(_app);
            });
            Logger.Information("TakeoffPerfState reset via web API");
            return Ok(dto);
        }

        // =================================================================
        //  LANDING
        // =================================================================

        [HttpGet("landing")]
        public async Task<ActionResult<LandingPerfStateDto>> GetLanding()
        {
            var dto = await Application.Current.Dispatcher.InvokeAsync(() => LandingPerfStateDto.From(_app));
            return Ok(dto);
        }

        [HttpPost("landing/inputs")]
        public async Task<ActionResult<LandingPerfStateDto>> PostLandingInputs([FromBody] LandingInputsDto inputs)
        {
            if (inputs == null) return BadRequest(new { error = "Missing body" });

            var dto = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var s = _app?.LandingPerf;
                if (s != null) inputs.ApplyTo(s);
                return LandingPerfStateDto.From(_app);
            });
            return Ok(dto);
        }

        [HttpPost("landing/load-runways")]
        public async Task<ActionResult<LandingPerfStateDto>> LoadLandingRunways([FromQuery] string icao)
        {
            if (string.IsNullOrWhiteSpace(icao))
                return BadRequest(new { error = "icao query parameter is required" });

            var svc = _app?.LandingPerfService;
            if (svc == null) return ServiceUnavailable("Landing perf service unavailable");

            await svc.LoadRunwaysAsync(icao, HttpContext.RequestAborted);
            var dto = await Application.Current.Dispatcher.InvokeAsync(() => LandingPerfStateDto.From(_app));
            return Ok(dto);
        }

        [HttpPost("landing/calculate")]
        public async Task<ActionResult<LandingPerfStateDto>> CalculateLanding()
        {
            var svc = _app?.LandingPerfService;
            if (svc == null) return ServiceUnavailable("Landing perf service unavailable");

            await svc.CalculateAsync(HttpContext.RequestAborted);
            var dto = await Application.Current.Dispatcher.InvokeAsync(() => LandingPerfStateDto.From(_app));
            return Ok(dto);
        }

        [HttpPost("landing/reset")]
        public async Task<ActionResult<LandingPerfStateDto>> ResetLanding()
        {
            var dto = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _app?.LandingPerfService?.Reset();
                return LandingPerfStateDto.From(_app);
            });
            Logger.Information("LandingPerfState reset via web API");
            return Ok(dto);
        }

        private ActionResult ServiceUnavailable(string message)
            => StatusCode(503, new { error = message });
    }
}
