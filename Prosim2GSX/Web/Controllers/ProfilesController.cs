using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Prosim2GSX.Commands;
using Prosim2GSX.Web.Contracts.Commands;
using System.Threading.Tasks;
using System.Windows;

namespace Prosim2GSX.Web.Controllers
{
    // Aircraft Profiles CRUD surface.
    //   GET  /api/profiles                        — full ProfilesListDto
    //   POST /api/profiles/set-active             — { name }
    //   POST /api/profiles/clone                  — { sourceName, newName? }
    //   POST /api/profiles/rename                 — { oldName, newName }
    //   POST /api/profiles/update-metadata        — { name, matchType, matchString }
    //   POST /api/profiles/delete                 — { name }
    //
    // All POST endpoints delegate to the CommandRegistry (handlers in
    // Prosim2GSX.Commands.Handlers.ProfileHandlers); the Dispatch helper
    // centralises the catch chain.
    [ApiController]
    [Route("api/profiles")]
    public class ProfilesController : ControllerBase
    {
        private readonly AppService _app;
        public ProfilesController(AppService app) => _app = app;

        [HttpGet]
        public async Task<ActionResult<ProfilesListDto>> Get()
        {
            var dto = await Application.Current.Dispatcher.InvokeAsync(() => ProfilesListDto.From(_app));
            return Ok(dto);
        }

        [HttpPost("set-active")]
        public Task<IActionResult> SetActive([FromBody] SetActiveProfileRequest req)
            => Dispatch<SetActiveProfileRequest, ProfilesListDto>("profiles.setActive", req);

        [HttpPost("clone")]
        public Task<IActionResult> Clone([FromBody] CloneProfileRequest req)
            => Dispatch<CloneProfileRequest, ProfilesListDto>("profiles.clone", req);

        [HttpPost("rename")]
        public Task<IActionResult> Rename([FromBody] RenameProfileRequest req)
            => Dispatch<RenameProfileRequest, ProfilesListDto>("profiles.rename", req);

        [HttpPost("update-metadata")]
        public Task<IActionResult> UpdateMetadata([FromBody] UpdateProfileMetadataRequest req)
            => Dispatch<UpdateProfileMetadataRequest, ProfilesListDto>("profiles.updateMetadata", req);

        [HttpPost("delete")]
        public Task<IActionResult> Delete([FromBody] DeleteProfileRequest req)
            => Dispatch<DeleteProfileRequest, ProfilesListDto>("profiles.delete", req);

        private async Task<IActionResult> Dispatch<TReq, TRes>(string name, TReq req)
        {
            if (req == null) return BadRequest("Missing request body.");
            try
            {
                var result = await _app.Commands.ExecuteAsync<TReq, TRes>(name, req, HttpContext.RequestAborted);
                return Ok(result);
            }
            catch (CommandValidationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (CommandNotFoundException)
            {
                return StatusCode(StatusCodes.Status501NotImplemented);
            }
        }
    }
}
