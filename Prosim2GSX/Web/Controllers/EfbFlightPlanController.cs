using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Prosim2GSX.Commands;
using Prosim2GSX.Web.Contracts;
using Prosim2GSX.Web.Contracts.Commands;
using System.Threading.Tasks;
using System.Windows;

namespace Prosim2GSX.Web.Controllers
{
    // EFB INIT tab REST surface.
    //   GET  /api/efb/flight-plan        — full snapshot
    //   POST /api/efb/fetch-ofp          — { departure, arrival, alternate, flightNumber }
    //   POST /api/efb/override           — { field, value }
    //   POST /api/efb/clear-override     — { field }
    //   POST /api/efb/clear-all-overrides
    //
    // POST endpoints delegate to the CommandRegistry — same dispatch helper
    // shape as OfpController so error mapping (validation → 400, missing
    // handler → 501) is identical.
    [ApiController]
    [Route("api/efb")]
    public class EfbFlightPlanController : ControllerBase
    {
        private readonly AppService _app;
        public EfbFlightPlanController(AppService app) => _app = app;

        [HttpGet("flight-plan")]
        public async Task<ActionResult<EfbFlightPlanDto>> GetFlightPlan()
        {
            var dto = await Application.Current.Dispatcher.InvokeAsync(() => EfbFlightPlanDto.From(_app));
            return Ok(dto);
        }

        [HttpPost("fetch-ofp")]
        public Task<IActionResult> FetchOfp([FromBody] FetchOfpRequest req)
            => Dispatch<FetchOfpRequest, EfbFlightPlanDto>("efb.fetchOfp", req);

        [HttpPost("override")]
        public Task<IActionResult> SetOverride([FromBody] OverrideRequest req)
            => Dispatch<OverrideRequest, EfbFlightPlanDto>("efb.setOverride", req);

        [HttpPost("clear-override")]
        public Task<IActionResult> ClearOverride([FromBody] ClearOverrideRequest req)
            => Dispatch<ClearOverrideRequest, EfbFlightPlanDto>("efb.clearOverride", req);

        [HttpPost("clear-all-overrides")]
        public Task<IActionResult> ClearAllOverrides()
            => Dispatch<ClearAllOverridesRequest, EfbFlightPlanDto>(
                "efb.clearAllOverrides", new ClearAllOverridesRequest());

        private async Task<IActionResult> Dispatch<TReq, TRes>(string name, TReq req)
        {
            if (req == null) return BadRequest("Missing request body.");
            try
            {
                var result = await _app.Commands.ExecuteAsync<TReq, TRes>(name, req, HttpContext.RequestAborted);
                return Ok(result);
            }
            catch (CommandValidationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (CommandNotFoundException)
            {
                return StatusCode(StatusCodes.Status501NotImplemented);
            }
        }
    }
}
