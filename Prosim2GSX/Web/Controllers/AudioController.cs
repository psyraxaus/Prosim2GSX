using Microsoft.AspNetCore.Mvc;
using Prosim2GSX.Web.Contracts;
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
    }
}
