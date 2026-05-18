using Microsoft.AspNetCore.Mvc;

namespace Prosim2GSX.Web.Controllers
{
    // Endpoints for the in-sim GSX handler script (gsx_handler.py). The
    // script runs inside MSFS's Couatl/Stackless Python and cannot present a
    // bearer token, so /api/gsxmenu/* is exempt from BearerTokenMiddleware.
    // The contract is handler-script I/O: a GET pull (pending-gate) and a
    // GET-encoded event push (events). It is not authenticated by necessity;
    // the server is loopback-only by default and payloads are whitelisted
    // scalars, never anything that mutates security-relevant state.
    [ApiController]
    [Route("api/gsxmenu")]
    public class GsxMenuController : ControllerBase
    {
        private readonly AppService _app;
        public GsxMenuController(AppService app) => _app = app;

        // Returns the user-confirmed pending arrival gate (e.g. "C3") or
        // JSON null when none is queued. The handler script's selectGate()
        // call accepts the bare string. This endpoint never clears the
        // pending value — that lives in the SendNow command flow.
        [HttpGet("pending-gate")]
        public ActionResult<string> PendingGate()
        {
            var gate = _app?.Ofp?.PendingArrivalGate;
            if (string.IsNullOrWhiteSpace(gate))
                return Ok((string)null);
            return Ok(gate);
        }

        // Event sink for the in-sim handler script's lifecycle/service hooks.
        // The Couatl Python sandbox has no HTTP POST primitive, so events are
        // delivered as a GET with query params and the body is always JSON
        // null — fetchJson() on the handler side just needs valid JSON back.
        //   e  — event name (whitelisted in GsxHandlerEventSink)
        //   r  — optional reason (gateReset: user_revoked / user_changed / …)
        //   ts — optional handler-side timestamp, accepted but unused here
        // Always 200; never throws — a failure must not propagate into the
        // GSX tasklet that invoked the handler hook. Unauthenticated by
        // necessity (same /api/gsxmenu exemption as pending-gate); loopback
        // by default; payloads are whitelisted scalars only.
        [HttpGet("events")]
        public ActionResult<string> Events(
            [FromQuery] string e,
            [FromQuery] string r = null,
            [FromQuery] string ts = null)
        {
            GsxHandlerEventSink.Process(_app, e, r);
            return Ok((string)null);
        }
    }
}
