using CFIT.AppLogger;
using Microsoft.AspNetCore.Mvc;
using Prosim2GSX.Themes;
using Prosim2GSX.Web.Contracts;
using System;
using System.IO;
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

        // Returns the raw JSON content of a single theme file from
        // <exe-dir>/Themes/<name>.json. The React app applies the colors
        // object as CSS custom properties so the web UI matches whichever
        // theme is active in the WPF window.
        [HttpGet("theme/{name}")]
        public IActionResult Theme(string name)
        {
            if (!IsSafeThemeName(name))
                return BadRequest("Invalid theme name.");

            var file = Path.Combine(AppContext.BaseDirectory, "Themes", name + ".json");
            if (!System.IO.File.Exists(file))
                return NotFound();

            try
            {
                var json = System.IO.File.ReadAllText(file);
                return Content(json, "application/json; charset=utf-8");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return StatusCode(500, "Failed to read theme file.");
            }
        }

        // Whitelist alphanumeric + dash + underscore so the {name} segment
        // can't traverse out of the Themes directory (no '..', no path
        // separators, no extensions).
        private static bool IsSafeThemeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            foreach (var c in name)
            {
                if (!char.IsLetterOrDigit(c) && c != '-' && c != '_')
                    return false;
            }
            return true;
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
