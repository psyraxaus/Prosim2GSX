using Microsoft.AspNetCore.Mvc;
using Prosim2GSX.Web.Contracts;
using System.Threading.Tasks;
using System.Windows;

namespace Prosim2GSX.Web.Controllers
{
    // Read-only Weight & Balance snapshot. Reads WeightBalanceState (which is
    // populated each StateUpdateWorker tick by WeightBalanceService) and
    // returns a wire-shape DTO. WS deltas on the "weightBalance" channel keep
    // the React panel current between fetches.
    [ApiController]
    [Route("api/weightbalance")]
    public class WeightBalanceController : ControllerBase
    {
        private readonly AppService _app;

        public WeightBalanceController(AppService app) => _app = app;

        [HttpGet]
        public async Task<ActionResult<WeightBalanceDto>> Get()
        {
            // Marshal to the WPF dispatcher to match the FlightStatus pattern —
            // the store is written from a background Task.Run inside the
            // worker tick and read here from a request thread. WeightBalanceDto
            // is all primitives (no observable collections), so the dispatcher
            // hop is purely conventional, not strictly required.
            var dto = await Application.Current.Dispatcher.InvokeAsync(() => WeightBalanceDto.From(_app));
            return Ok(dto);
        }
    }
}
