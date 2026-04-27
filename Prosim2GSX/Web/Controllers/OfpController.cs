using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Prosim2GSX.Commands;
using Prosim2GSX.Web.Contracts;
using Prosim2GSX.Web.Contracts.Commands;
using System.Threading.Tasks;
using System.Windows;

namespace Prosim2GSX.Web.Controllers
{
    // OFP tab REST surface.
    //   GET  /api/ofp                        — full OfpDto snapshot
    //   POST /api/ofp/confirm-arrival-gate   — { gate: string }
    //   POST /api/ofp/send-now               — manually fire queued assignment
    //   POST /api/ofp/refresh-weather        — fetch SayIntentions weather
    //   POST /api/ofp/set-pushback-preference — { preference: enum }
    //
    // POST endpoints delegate to the CommandRegistry (8.0a/b). The Dispatch
    // helper centralises the catch chain so each endpoint stays a one-liner.
    [ApiController]
    [Route("api/ofp")]
    public class OfpController : ControllerBase
    {
        private readonly AppService _app;
        public OfpController(AppService app) => _app = app;

        [HttpGet]
        public async Task<ActionResult<OfpDto>> Get()
        {
            var dto = await Application.Current.Dispatcher.InvokeAsync(() => OfpDto.From(_app));
            return Ok(dto);
        }

        [HttpPost("confirm-arrival-gate")]
        public Task<IActionResult> ConfirmArrivalGate([FromBody] ConfirmArrivalGateRequest req)
            => Dispatch<ConfirmArrivalGateRequest, GateAssignmentDto>("ofp.confirmArrivalGate", req);

        [HttpPost("send-now")]
        public Task<IActionResult> SendNow()
            => Dispatch<SendNowRequest, GateAssignmentDto>("ofp.sendNow", new SendNowRequest());

        [HttpPost("refresh-weather")]
        public Task<IActionResult> RefreshWeather()
            => Dispatch<RefreshWeatherRequest, WeatherSnapshotDto>("ofp.refreshWeather", new RefreshWeatherRequest());

        [HttpPost("set-pushback-preference")]
        public Task<IActionResult> SetPushbackPreference([FromBody] SetPushbackPreferenceRequest req)
            => Dispatch<SetPushbackPreferenceRequest, PushbackPreferenceDto>("ofp.setPushbackPreference", req);

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
