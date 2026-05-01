using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Windows;

namespace Prosim2GSX.Web.Controllers
{
    // Read-only diagnostic surface mirroring the WPF Debug tab. Returns the
    // grouped Variable | Value snapshot from DebugDataService as JSON.
    //
    // Gated by AppConfig.ShowDebugTab — when the flag is off the endpoint
    // returns 404, matching the WPF tab's "must not be visible or instantiated
    // at all" contract.
    //
    // Bearer auth applies via BearerTokenMiddleware (handles all /api/* paths
    // except /api/gsxmenu and CORS preflight). The companion /debug HTML page
    // pulls the token from the URL fragment the same way the SPA does.
    //
    // Reads are marshalled onto the WPF dispatcher so the snapshot is
    // consistent with what the WPF tab would render at the same instant —
    // particularly important once any future group reads ObservableCollections
    // bound to the UI thread.
    [ApiController]
    [Route("api/debug")]
    public class DebugController : ControllerBase
    {
        private readonly AppService _app;

        public DebugController(AppService app) => _app = app;

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            if (_app?.Config?.ShowDebugTab != true)
                return NotFound();

            var service = _app.DebugData;
            if (service == null)
                return NotFound();

            var snapshot = await Application.Current.Dispatcher.InvokeAsync(() => service.GetSnapshot());
            return Ok(snapshot);
        }
    }
}
