using Microsoft.AspNetCore.Mvc;
using Prosim2GSX.Web.Contracts;
using System.Threading.Tasks;
using System.Windows;

namespace Prosim2GSX.Web.Controllers
{
    // GSX Settings (Automation) tab — full read+write surface against the
    // active AircraftProfile + the app-wide auto-deice fields on Config.
    // Both verbs marshal onto the WPF dispatcher: GET so the From snapshot is
    // consistent, POST because ApplyTo writes to Config and raises INPC events
    // that existing WPF bindings expect on the UI thread.
    //
    // POST returns the freshly-projected state so clients see exactly what was
    // persisted (silent ApplyTo rejections of invalid min/max pairs etc. show
    // through this echo).
    [ApiController]
    [Route("api/gsxsettings")]
    public class GsxSettingsController : ControllerBase
    {
        private readonly AppService _app;

        public GsxSettingsController(AppService app) => _app = app;

        [HttpGet]
        public async Task<ActionResult<GsxSettingsDto>> Get()
        {
            var dto = await Application.Current.Dispatcher.InvokeAsync(() => GsxSettingsDto.From(_app));
            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<GsxSettingsDto>> Post([FromBody] GsxSettingsDto dto)
        {
            if (dto == null) return BadRequest("Missing body.");
            await Application.Current.Dispatcher.InvokeAsync(() => dto.ApplyTo(_app));
            var fresh = await Application.Current.Dispatcher.InvokeAsync(() => GsxSettingsDto.From(_app));
            return Ok(fresh);
        }
    }
}
