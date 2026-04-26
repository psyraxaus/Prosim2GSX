using Microsoft.AspNetCore.Mvc;

namespace Prosim2GSX.Web.Controllers
{
    // Tiny diagnostic surface for verifying that the host is up and that the
    // bearer token works end-to-end. Reachable only with a valid token (the
    // BearerTokenMiddleware enforces that for the /api prefix).
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        [HttpGet("info")]
        public IActionResult Info() => Ok(new { authenticated = true });
    }
}
