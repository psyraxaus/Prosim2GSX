using ProsimInterface;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Menu.Intents
{
    /// <summary>
    /// Menu-callback intent: auto-answer "Yes" to the GSX board-crew prompt.
    /// Replaces <see cref="GsxMenu.OnBoardCrew"/>'s body — gating on
    /// <c>AircraftProfile.SkipCrewQuestion</c> moves to the migrated callback
    /// (Phase 4); the intent itself is the pure "select yes on the board-crew
    /// menu" action.
    /// </summary>
    internal sealed class OnBoardCrewIntent : KeywordIntent
    {
        public OnBoardCrewIntent()
            : base(@"(?i)^yes")
        { }

        public override GsxMenuIntent ParentMenu => null;
        public override string ExpectedMenuTitlePrefix => GsxConstants.MenuBoardCrew;

        public override bool IsValidForPhase(AutomationState phase)
            => phase == AutomationState.Preparation
            || phase == AutomationState.Departure;

        public override bool ArePreconditionsSatisfied(GsxController controller) => true;

        public override Task<bool> VerifyOutcomeAsync(GsxController controller, TimeSpan timeout, CancellationToken token)
        {
            return IntentHelpers.PollUntilAsync(
                () => controller?.Menu != null
                      && !controller.Menu.MatchTitle(GsxConstants.MenuBoardCrew),
                timeout,
                IntentHelpers.DefaultPollInterval,
                token);
        }

        public override string Describe() => "Auto-answer 'yes' to the board-crew prompt";
    }
}
