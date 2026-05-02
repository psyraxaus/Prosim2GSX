using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Prosim2GSX.Commands;
using Prosim2GSX.Web.Contracts;
using Prosim2GSX.Web.Contracts.Commands;
using System.Threading.Tasks;
using System.Windows;

namespace Prosim2GSX.Web.Controllers
{
    // Checklists tab REST surface.
    //   GET  /api/checklists                — full ChecklistDto snapshot
    //   POST /api/checklists/select         — { name: string }
    //   POST /api/checklists/select-section — { sectionIndex: int }
    //   POST /api/checklists/toggle         — { sectionIndex, itemIndex }
    //   POST /api/checklists/reset-section  — { sectionIndex }
    //   POST /api/checklists/complete       — empty body
    //
    // POST endpoints delegate to the CommandRegistry. The Dispatch helper
    // mirrors OfpController.Dispatch — same error mapping (400/501).
    [ApiController]
    [Route("api/checklists")]
    public class ChecklistController : ControllerBase
    {
        private readonly AppService _app;
        public ChecklistController(AppService app) => _app = app;

        [HttpGet]
        public async Task<ActionResult<ChecklistDto>> Get()
        {
            var dto = await Application.Current.Dispatcher.InvokeAsync(() => ChecklistDto.From(_app));
            return Ok(dto);
        }

        [HttpPost("select")]
        public Task<IActionResult> Select([FromBody] SelectChecklistRequest req)
            => Dispatch<SelectChecklistRequest, ChecklistCommandResponse>("checklists.select", req);

        [HttpPost("select-section")]
        public Task<IActionResult> SelectSection([FromBody] SelectSectionRequest req)
            => Dispatch<SelectSectionRequest, ChecklistCommandResponse>("checklists.selectSection", req);

        [HttpPost("toggle")]
        public Task<IActionResult> Toggle([FromBody] ToggleItemRequest req)
            => Dispatch<ToggleItemRequest, ChecklistCommandResponse>("checklists.toggleItem", req);

        [HttpPost("reset-section")]
        public Task<IActionResult> ResetSection([FromBody] ResetSectionRequest req)
            => Dispatch<ResetSectionRequest, ChecklistCommandResponse>("checklists.resetSection", req);

        [HttpPost("complete")]
        public Task<IActionResult> Complete()
            => Dispatch<CompleteSectionRequest, ChecklistCommandResponse>("checklists.completeSection", new CompleteSectionRequest());

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
