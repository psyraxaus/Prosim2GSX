using Microsoft.AspNetCore.Mvc;
using Prosim2GSX.Web.Contracts;
using System.Threading.Tasks;
using System.Windows;

namespace Prosim2GSX.Web.Controllers
{
    // Read-only Monitor surface. The FlightStatusDto snapshot reads from the
    // FlightStatusState / GsxState stores plus a tail of FlightStatusState.MessageLog
    // (which is an ObservableCollection<string> bound to the WPF dispatcher),
    // so the read is marshalled there to keep enumeration safe.
    [ApiController]
    [Route("api/flightstatus")]
    public class FlightStatusController : ControllerBase
    {
        private readonly AppService _app;

        public FlightStatusController(AppService app) => _app = app;

        [HttpGet]
        public async Task<ActionResult<FlightStatusDto>> Get()
        {
            var dto = await Application.Current.Dispatcher.InvokeAsync(() => FlightStatusDto.From(_app));
            return Ok(dto);
        }
    }
}
