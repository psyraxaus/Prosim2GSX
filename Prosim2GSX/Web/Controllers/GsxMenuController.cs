using Microsoft.AspNetCore.Mvc;

namespace Prosim2GSX.Web.Controllers
{
    // Endpoints consumed by the in-sim GSX handler script (gsx_handler.py).
    // The script runs inside MSFS's Stackless Python and cannot present a
    // bearer token, so /api/gsxmenu/* is exempt from BearerTokenMiddleware
    // and read-only.
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
    }
}
