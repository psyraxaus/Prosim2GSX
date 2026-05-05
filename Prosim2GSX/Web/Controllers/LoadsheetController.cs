using CFIT.AppLogger;
using Microsoft.AspNetCore.Mvc;
using Prosim2GSX.Web.Contracts;
using System.Threading.Tasks;
using System.Windows;

namespace Prosim2GSX.Web.Controllers
{
    // Loadsheet REST surface.
    //   GET    /api/loadsheet/prelim — single prelim DTO
    //   GET    /api/loadsheet/final  — single final DTO
    //   POST   /api/loadsheet/resend — manual resend trigger (placeholder log only)
    //   DELETE /api/loadsheet        — reset both slots and broadcast
    //
    // The combined Prelim+Final snapshot rides on the WS "loadsheet" channel
    // (see StateWebSocketHandler.OnLoadsheetChanged). The split GET endpoints
    // exist per spec; new clients can fetch either independently for an
    // initial render and rely on WS for live deltas.
    [ApiController]
    [Route("api/loadsheet")]
    public class LoadsheetController : ControllerBase
    {
        private readonly AppService _app;

        public LoadsheetController(AppService app) => _app = app;

        [HttpGet("prelim")]
        public async Task<ActionResult<LoadsheetDto>> GetPrelim()
        {
            var dto = await Application.Current.Dispatcher.InvokeAsync(() => LoadsheetDto.FromPrelim(_app));
            return Ok(dto);
        }

        [HttpGet("final")]
        public async Task<ActionResult<LoadsheetDto>> GetFinal()
        {
            var dto = await Application.Current.Dispatcher.InvokeAsync(() => LoadsheetDto.FromFinal(_app));
            return Ok(dto);
        }

        // Placeholder per spec — logs the action; no actual EFB resend call.
        // The Prosim SDK's ProsimEfbInterface.ProsimLoadsheetTask("/resend",
        // HttpMethod.Post) is the wire-up point if/when this is promoted to a
        // real command.
        [HttpPost("resend")]
        public IActionResult Resend()
        {
            Logger.Information("Loadsheet resend requested via web API (placeholder — no EFB call wired)");
            return Ok();
        }

        [HttpDelete]
        public async Task<IActionResult> Reset()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var svc = _app?.LoadsheetService;
                var st = _app?.Loadsheet;
                if (svc != null && st != null) svc.ResetSlots(st);
            });
            Logger.Information("Loadsheet slots reset via web API");
            return Ok();
        }
    }
}
