namespace Prosim2GSX.Web.Contracts.Commands
{
    // Wire shapes for Checklist command requests + responses. The handlers
    // are registered in CommandsBootstrap and dispatched by ChecklistController.

    public class SelectChecklistRequest
    {
        public string Name { get; set; } = "";
    }

    public class SelectSectionRequest
    {
        public int SectionIndex { get; set; }
    }

    public class ToggleItemRequest
    {
        public int SectionIndex { get; set; }
        public int ItemIndex { get; set; }
    }

    public class ResetSectionRequest
    {
        public int SectionIndex { get; set; }
    }

    public class CompleteSectionRequest { }

    // The command handlers all return the post-mutation full Checklist snapshot
    // so the calling client picks up section / item / current pointer changes
    // without having to merge multiple WebSocket patches.
    public class ChecklistCommandResponse
    {
        public ChecklistDto Snapshot { get; set; }
    }
}
