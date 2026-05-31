using ProsimInterface;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Menu.Intents
{
    /// <summary>
    /// Menu-callback intent: auto-answer "Yes" to the GSX deboard-crew
    /// prompt. Replaces <see cref="GsxMenu.OnDeboardCrew"/>'s body — gating
    /// on <c>AircraftProfile.SkipCrewQuestion</c> moves to the migrated
    /// callback (Phase 4); the intent is the pure "select yes on the
    /// deboard-crew menu" action.
    /// </summary>
    internal sealed class OnDeboardCrewIntent : KeywordIntent
    {
        public OnDeboardCrewIntent()
            : base(@"(?i)^yes")
        { }

        public override GsxMenuIntent ParentMenu => null;
        public override string ExpectedMenuTitlePrefix => GsxConstants.MenuDeboardCrew;

        public override bool IsValidForPhase(AutomationState phase)
            => phase == AutomationState.Arrival
            || phase == AutomationState.TurnAround;

        public override bool ArePreconditionsSatisfied(GsxController controller) => true;

        public override Task<bool> VerifyOutcomeAsync(GsxController controller, TimeSpan timeout, CancellationToken token)
        {
            return IntentHelpers.PollUntilAsync(
                () => controller?.Menu != null
                      && !controller.Menu.MatchTitle(GsxConstants.MenuDeboardCrew),
                timeout,
                IntentHelpers.DefaultPollInterval,
                token);
        }

        public override string Describe() => "Auto-answer 'yes' to the deboard-crew prompt";
    }
}
