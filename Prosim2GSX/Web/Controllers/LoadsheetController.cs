using CFIT.AppLogger;
using Microsoft.AspNetCore.Mvc;
using Prosim2GSX.Web.Contracts;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace Prosim2GSX.Web.Controllers
{
    // Loadsheet REST surface.
    //   GET    /api/loadsheet/prelim — single prelim DTO
    //   GET    /api/loadsheet/final  — single final DTO
    //   POST   /api/loadsheet/resend — manual resend trigger (placeholder log only)
    //   DELETE /api/loadsheet        — reset both slots and broadcast
    //
    // The combined Prelim+Final snapshot rides on the WS "loadsheet" channel
    // (see StateWebSocketHandler.OnLoadsheetChanged). The split GET endpoints
    // exist per spec; new clients can fetch either independently for an
    // initial render and rely on WS for live deltas.
    [ApiController]
    [Route("api/loadsheet")]
    public class LoadsheetController : ControllerBase
    {
        private readonly AppService _app;

        public LoadsheetController(AppService app) => _app = app;

        [HttpGet("prelim")]
        public async Task<ActionResult<LoadsheetDto>> GetPrelim()
        {
            var dto = await Application.Current.Dispatcher.InvokeAsync(() => LoadsheetDto.FromPrelim(_app));
            return Ok(dto);
        }

        [HttpGet("final")]
        public async Task<ActionResult<LoadsheetDto>> GetFinal()
        {
            var dto = await Application.Current.Dispatcher.InvokeAsync(() => LoadsheetDto.FromFinal(_app));
            return Ok(dto);
        }

        // Resend the requested slot via the EFB SDK. ?slot=prelim |
        // final. Defaults to "prelim" for backward compatibility with any
        // caller that still hits the no-arg endpoint.
        [HttpPost("resend")]
        public async Task<IActionResult> Resend([FromQuery] string slot = "prelim")
        {
            var svc = _app?.LoadsheetService;
            if (svc == null)
            {
                Logger.Warning("Loadsheet resend requested but LoadsheetService is unavailable");
                return Ok(new { success = false, message = "Loadsheet service unavailable" });
            }

            // Hop onto the WPF dispatcher to start the async SDK call, then
            // await the inner task so the controller thread isn't blocked
            // through the ProSim REST round-trip.
            var taskOnUi = await Application.Current.Dispatcher.InvokeAsync(() => svc.ResendAsync(slot));
            var success = await taskOnUi;
            return Ok(new { success, slot });
        }

        [HttpDelete]
        public async Task<IActionResult> Reset()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var svc = _app?.LoadsheetService;
                var st = _app?.Loadsheet;
                if (svc != null && st != null) svc.ResetSlots(st);
            });
            Logger.Information("Loadsheet slots reset via web API");
            return Ok();
        }

        // GET /api/loadsheet/std — current effective STD with its source.
        // OFP-derived value wins when an OFP is loaded; manual override is
        // returned only as a fallback.
        [HttpGet("std")]
        public async Task<ActionResult<StdResponse>> GetStd()
        {
            var resp = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var ofpStd = _app?.EfbFlightPlan?.CurrentOfp?.Std;
                var manual = _app?.LoadsheetTimingService?.ResolveStd();
                if (ofpStd.HasValue) return new StdResponse { Std = ofpStd, Source = "ofp" };
                if (manual.HasValue) return new StdResponse { Std = manual, Source = "manual" };
                return new StdResponse { Std = null, Source = "none" };
            });
            return Ok(resp);
        }

        // POST /api/loadsheet/set-std — sets (or clears) the manual STD
        // override on the timing service. Body: { std: ISO-8601 | null }.
        // OFP-derived STD always wins; this is a fallback for OFP-less
        // workflows.
        [HttpPost("set-std")]
        public async Task<ActionResult<StdResponse>> SetStd([FromBody] SetStdRequest req)
        {
            if (req == null) return BadRequest("Missing body");

            var resp = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var svc = _app?.LoadsheetTimingService;
                if (svc == null) return new StdResponse { Std = null, Source = "none" };

                if (req.Std.HasValue) svc.SetStd(req.Std.Value);
                else svc.ClearManualStd();

                var ofpStd = _app?.EfbFlightPlan?.CurrentOfp?.Std;
                var manual = svc.ResolveStd();
                if (ofpStd.HasValue) return new StdResponse { Std = ofpStd, Source = "ofp" };
                if (manual.HasValue) return new StdResponse { Std = manual, Source = "manual" };
                return new StdResponse { Std = null, Source = "none" };
            });
            return Ok(resp);
        }

        public class SetStdRequest
        {
            public DateTime? Std { get; set; }
        }

        public class StdResponse
        {
            public DateTime? Std { get; set; }
            public string Source { get; set; } = "none"; // "ofp" | "manual" | "none"
        }
    }
}
