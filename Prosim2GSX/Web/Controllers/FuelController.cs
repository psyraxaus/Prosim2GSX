using Microsoft.AspNetCore.Mvc;
using Prosim2GSX.Web.Contracts;
using System.Threading.Tasks;
using System.Windows;

namespace Prosim2GSX.Web.Controllers
{
    // Read-only Fuel snapshot. Reads FuelState (which is populated each
    // StateUpdateWorker tick by FuelService) and returns a wire-shape DTO.
    // WS deltas on the "fuel" channel keep the React panel current between
    // fetches.
    [ApiController]
    [Route("api/fuel")]
    public class FuelController : ControllerBase
    {
        private readonly AppService _app;

        public FuelController(AppService app) => _app = app;

        [HttpGet]
        public async Task<ActionResult<FuelDto>> Get()
        {
            // Marshal to the WPF dispatcher to match the WeightBalance/
            // FlightStatus pattern — the store is written from a background
            // Task.Run inside the worker tick and read here from a request
            // thread. FuelDto is all primitives so the dispatcher hop is
            // purely conventional, not strictly required.
            var dto = await Application.Current.Dispatcher.InvokeAsync(() => FuelDto.From(_app));
            return Ok(dto);
        }
    }
}
