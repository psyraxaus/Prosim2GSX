using Microsoft.AspNetCore.Mvc;
using Prosim2GSX.Web.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Prosim2GSX.Web.Controllers
{
    // Audio Settings tab — full read+write mirror of the WPF tab. POST replaces
    // mappings and the device blacklist wholesale (preserves user-edit order)
    // and trips the AudioController's ResetMappings / ResetVolumes flags so
    // changes take effect without a tab being open.
    //
    // Note: this is the WEB controller — the audio-business-logic controller
    // lives at Prosim2GSX.Audio.AudioController. They share a class name but
    // never collide because they're in different namespaces and this file
    // doesn't import Prosim2GSX.Audio (DTO handles enum typing).
    [ApiController]
    [Route("api/audio")]
    public class AudioController : ControllerBase
    {
        private readonly AppService _app;

        public AudioController(AppService app) => _app = app;

        [HttpGet]
        public async Task<ActionResult<AudioDto>> Get()
        {
            var dto = await Application.Current.Dispatcher.InvokeAsync(() => AudioDto.From(_app));
            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<AudioDto>> Post([FromBody] AudioDto dto)
        {
            if (dto == null) return BadRequest("Missing body.");
            await Application.Current.Dispatcher.InvokeAsync(() => dto.ApplyTo(_app));
            var fresh = await Application.Current.Dispatcher.InvokeAsync(() => AudioDto.From(_app));
            return Ok(fresh);
        }

        // Suggestions for the mapping-binary autocomplete. Reads the same
        // AudioSessionRegistry the WPF typeahead uses — only processes with
        // a live CoreAudio session, deduped by ProcessName, IsAccessible
        // mirrors AudioSession.ProbeAccessible.
        [HttpGet("process-suggestions")]
        public ActionResult<List<AudioSessionSuggestionDto>> GetProcessSuggestions()
        {
            var snapshot = _app?.AudioService?.SessionRegistry?.Snapshot;
            if (snapshot == null) return Ok(new List<AudioSessionSuggestionDto>());
            return Ok(snapshot
                .Select(p => new AudioSessionSuggestionDto
                {
                    ProcessName = p.ProcessName,
                    IsAccessible = p.IsAccessible,
                })
                .ToList());
        }

        // VoiceMeeter strips/buses for the per-mapping combo. Returns an
        // empty list when VoiceMeeter is disabled, the DLL path is missing,
        // or the Remote API rejected Login. The web client renders a
        // warning in that case (mirrors WPF VoiceMeeterWarning).
        [HttpGet("/api/voicemeeter/strips")]
        public async Task<ActionResult<List<VoiceMeeterStripDto>>> GetVoiceMeeterStrips()
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var vm = _app?.AudioService?.VoiceMeeter;
                var cfg = _app?.Config;
                if (vm == null || cfg == null)
                    return (ActionResult<List<VoiceMeeterStripDto>>)Ok(new List<VoiceMeeterStripDto>());

                // Strip list is read-only metadata — gate on IsLoaded, not
                // IsAvailable, so suspend-state (CoreAudio mode) doesn't hide
                // the strips from a user who's about to flip back to VM.
                if (!vm.IsLoaded && !string.IsNullOrWhiteSpace(cfg.VoiceMeeterDllPath))
                    vm.Login(cfg.VoiceMeeterDllPath);

                if (!vm.IsLoaded)
                    return Ok(new List<VoiceMeeterStripDto>());

                var list = vm.GetStrips()
                    .Select(s => new VoiceMeeterStripDto
                    {
                        Index = s.Index,
                        IsBus = s.IsBus,
                        Label = s.Label ?? "",
                        DisplayName = s.DisplayName ?? "",
                    })
                    .ToList();
                return Ok(list);
            });
        }
    }
}
