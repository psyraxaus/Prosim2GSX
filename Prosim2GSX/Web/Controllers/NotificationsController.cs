using Microsoft.AspNetCore.Mvc;
using Prosim2GSX.Web.Contracts;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace Prosim2GSX.Web.Controllers
{
    // Notifications REST surface.
    //   GET  /api/notifications              — full snapshot
    //   POST /api/notifications/{id}/dismiss — soft-dismiss one entry
    //
    // Live updates ride on the WS "notifications" channel as a snapshot
    // envelope (see StateWebSocketHandler.OnNotificationsChanged).
    [ApiController]
    [Route("api/notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly AppService _app;

        public NotificationsController(AppService app) => _app = app;

        [HttpGet]
        public async Task<ActionResult<NotificationsSnapshotDto>> Get()
        {
            var dto = await Application.Current.Dispatcher.InvokeAsync(
                () => NotificationsSnapshotDto.From(_app));
            return Ok(dto);
        }

        [HttpPost("{id:guid}/dismiss")]
        public async Task<IActionResult> Dismiss(Guid id)
        {
            await Application.Current.Dispatcher.InvokeAsync(
                () => _app?.Notifications?.Dismiss(id));
            return Ok();
        }
    }
}
