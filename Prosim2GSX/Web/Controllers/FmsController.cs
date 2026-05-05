using CFIT.AppLogger;
using Microsoft.AspNetCore.Mvc;
using Prosim2GSX.Web.Contracts;
using System.Threading.Tasks;
using System.Windows;

namespace Prosim2GSX.Web.Controllers
{
    // FMS sync endpoint. Hops onto the WPF dispatcher to match the
    // WeightBalance / Loadsheet controllers — the underlying SDK writes
    // are dispatcher-agnostic but the supporting state reads should be
    // serialised against the StateUpdateWorker tick.
    [ApiController]
    [Route("api/fms")]
    public class FmsController : ControllerBase
    {
        private readonly AppService _app;

        public FmsController(AppService app) => _app = app;

        [HttpPost("sync")]
        public async Task<ActionResult<FmsSyncResultDto>> Sync()
        {
            var svc = _app?.MactowValidationService;
            if (svc == null)
            {
                Logger.Warning("FMS sync requested but MactowValidationService is unavailable");
                return Ok(new FmsSyncResultDto
                {
                    Success = false,
                    ErrorMessage = "MACTOW validation service not available",
                });
            }

            var result = await Application.Current.Dispatcher.InvokeAsync(() => svc.SyncToFms());
            return Ok(result);
        }
    }
}
