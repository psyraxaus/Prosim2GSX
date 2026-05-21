using Microsoft.AspNetCore.Mvc;
using Prosim2GSX.GSX;
using Prosim2GSX.Web.Contracts;
using System.Threading.Tasks;
using System.Windows;

namespace Prosim2GSX.Web.Controllers
{
    // Crew inputs for the deice holdover (HOT) card. Bearer-gated like the
    // rest of /api (the React UI sends the token; only the in-sim handler
    // script's /api/gsxmenu/* is exempt). State changes propagate back to
    // all clients via the "deiceHoldover" WS channel.
    [ApiController]
    [Route("api/deice")]
    public class DeiceController : ControllerBase
    {
        private readonly AppService _app;
        public DeiceController(AppService app) => _app = app;

        [HttpPost("set-precip")]
        public async Task<ActionResult<DeiceHoldoverDto>> SetPrecip([FromBody] SetPrecipRequest req)
        {
            if (req == null) return BadRequest("Missing body");
            var dto = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _app?.DeiceHoldoverService?.SetPrecip(req.Precip);
                return DeiceHoldoverDto.From(_app.DeiceHoldover);
            });
            return Ok(dto);
        }

        [HttpPost("set-oat")]
        public async Task<ActionResult<DeiceHoldoverDto>> SetOat([FromBody] SetOatRequest req)
        {
            if (req == null) return BadRequest("Missing body");
            var dto = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _app?.DeiceHoldoverService?.SetOat(req.OatC);
                return DeiceHoldoverDto.From(_app.DeiceHoldover);
            });
            return Ok(dto);
        }

        public class SetPrecipRequest
        {
            public HotPrecip Precip { get; set; } = HotPrecip.None;
        }

        public class SetOatRequest
        {
            public double OatC { get; set; }
        }
    }
}
