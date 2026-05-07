using Microsoft.AspNetCore.Mvc;
using Prosim2GSX.Web.Contracts;
using System.Threading.Tasks;
using System.Windows;

namespace Prosim2GSX.Web.Controllers
{
    // Passenger-simulation REST surface.
    //   POST /api/passengers/simulate   — { count? }   → result + manifest
    //   POST /api/passengers/clear                     → result
    //   GET  /api/passengers/manifest                  → last manifest (or empty)
    //
    // Dispatcher-marshalled to match the Fms / WeightBalance pattern: the
    // underlying SDK write is dispatcher-agnostic but the supporting state
    // reads (zone capacities) should be serialised against the
    // StateUpdateWorker tick.
    [ApiController]
    [Route("api/passengers")]
    public class PassengersController : ControllerBase
    {
        private readonly AppService _app;

        public PassengersController(AppService app) => _app = app;

        [HttpPost("simulate")]
        public async Task<ActionResult<PassengerSimulationResultDto>> Simulate(
            [FromBody] SimulatePassengersRequest req)
        {
            var svc = _app?.PassengerSimulationService;
            if (svc == null)
            {
                return Ok(new PassengerSimulationResultDto
                {
                    Success = false,
                    ErrorMessage = "Passenger simulation service not available",
                });
            }
            var count = req?.Count;
            var result = await Application.Current.Dispatcher.InvokeAsync(() => svc.Simulate(count));
            return Ok(result);
        }

        [HttpPost("clear")]
        public async Task<ActionResult<PassengerSimulationResultDto>> Clear()
        {
            var svc = _app?.PassengerSimulationService;
            if (svc == null)
            {
                return Ok(new PassengerSimulationResultDto
                {
                    Success = false,
                    ErrorMessage = "Passenger simulation service not available",
                });
            }
            var result = await Application.Current.Dispatcher.InvokeAsync(() => svc.Clear());
            return Ok(result);
        }

        [HttpGet("manifest")]
        public async Task<ActionResult<PassengerManifestDto>> GetManifest()
        {
            var svc = _app?.PassengerSimulationService;
            if (svc == null)
                return Ok(new PassengerManifestDto());
            var manifest = await Application.Current.Dispatcher.InvokeAsync(() => svc.GetManifest());
            return Ok(manifest);
        }
    }
}
