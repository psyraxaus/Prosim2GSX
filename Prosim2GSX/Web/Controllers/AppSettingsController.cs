using Microsoft.AspNetCore.Mvc;
using Prosim2GSX.Themes;
using Prosim2GSX.Web.Contracts;
using System.Threading.Tasks;
using System.Windows;

namespace Prosim2GSX.Web.Controllers
{
    // App Settings tab — read+write surface for user-tunable Config fields.
    // Plus two extra endpoints under the same prefix:
    //   GET  /api/appsettings/themes               — list available themes
    //   POST /api/appsettings/regenerate-token     — mint a new auth token
    //
    // The regenerate-token endpoint is privileged in the same way every other
    // endpoint is: the caller must already hold a valid token to invoke it.
    // After regeneration the old token is rejected on the very next request
    // (BearerTokenMiddleware reads Config.WebServerAuthToken on every call).
    // Phase 6C's WebSocket handler will additionally close existing connections
    // when WebHostService.TokenGeneration changes.
    [ApiController]
    [Route("api/appsettings")]
    public class AppSettingsController : ControllerBase
    {
        private readonly AppService _app;

        public AppSettingsController(AppService app) => _app = app;

        [HttpGet]
        public async Task<ActionResult<AppSettingsDto>> Get()
        {
            var dto = await Application.Current.Dispatcher.InvokeAsync(() => AppSettingsDto.From(_app));
            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<AppSettingsDto>> Post([FromBody] AppSettingsDto dto)
        {
            if (dto == null) return BadRequest("Missing body.");
            await Application.Current.Dispatcher.InvokeAsync(() => dto.ApplyTo(_app));
            var fresh = await Application.Current.Dispatcher.InvokeAsync(() => AppSettingsDto.From(_app));
            return Ok(fresh);
        }

        [HttpGet("themes")]
        public IActionResult Themes()
        {
            // ThemeManager.Instance.AvailableThemes is enumerated from disk on
            // the calling thread; calling it off the dispatcher is safe (no UI
            // bindings touched). Phase 5 decision project_phase5_dto_decisions:
            // themes ship as a plain endpoint, NOT promoted into AppSettingsDto.
            var themes = ThemeManager.Instance?.AvailableThemes ?? new System.Collections.Generic.List<string>();
            return Ok(themes);
        }

        [HttpPost("regenerate-token")]
        public IActionResult RegenerateToken()
        {
            var host = _app?.WebHost;
            if (host == null)
                return StatusCode(503, "Web host not initialised.");

            var newToken = host.RegenerateToken();
            return Ok(new { token = newToken });
        }
    }
}
