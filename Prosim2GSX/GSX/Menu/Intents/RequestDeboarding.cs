using Prosim2GSX.GSX.Services;
using ProsimInterface;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Menu.Intents
{
    /// <summary>
    /// Gate-menu service intent: request GSX to begin passenger deboarding.
    /// Matches "Request Deboarding" on the live gate menu. When deboarding is
    /// already in progress the entry text mutates to a door-action prompt
    /// (e.g. "Waiting for your action: open Door cargo forward") — the
    /// resolver's door-action guard fires before this intent's regex matches,
    /// so the prior "Select(1) on the gate menu lands on a door toggle" footgun
    /// is structurally prevented.
    /// </summary>
    internal sealed class RequestDeboarding : KeywordIntent
    {
        public RequestDeboarding()
            : base(@"(?i)^request deboard")
        { }

        public override GsxMenuIntent ParentMenu => new OpenGateMenu();
        public override string ExpectedMenuTitlePrefix => GsxConstants.MenuGate;

        public override bool IsValidForPhase(AutomationState phase)
            => phase == AutomationState.Arrival || phase == AutomationState.TurnAround;

        public override bool ArePreconditionsSatisfied(GsxController controller)
        {
            var svc = IntentHelpers.GetService<GsxService>(controller, GsxServiceType.Deboarding);
            return svc != null && svc.State == GsxServiceState.Callable;
        }

        public override bool IsAlreadySatisfied(GsxController controller)
        {
            // Treat any state past Callable as "no menu action needed" —
            // Requested/Active mean the service is already in flight (GSX or
            // the user fired it pre-emptively), Completed means it's done.
            // Without this the resolver returns StatePreconditionFailed and
            // GsxAutomationController.RunArrival re-fires Call() forever.
            var svc = IntentHelpers.GetService<GsxService>(controller, GsxServiceType.Deboarding);
            if (svc == null) return false;
            var state = svc.State;
            return state == GsxServiceState.Requested
                || state == GsxServiceState.Active
                || state == GsxServiceState.Completed;
        }

        public override Task<bool> VerifyOutcomeAsync(GsxController controller, TimeSpan timeout, CancellationToken token)
        {
            return IntentHelpers.PollUntilAsync(
                () =>
                {
                    var svc = IntentHelpers.GetService<GsxService>(controller, GsxServiceType.Deboarding);
                    return svc != null
                        && (svc.State == GsxServiceState.Requested || svc.State == GsxServiceState.Active);
                },
                timeout,
                IntentHelpers.DefaultPollInterval,
                token);
        }

        public override string Describe() => "Request GSX to begin passenger deboarding";
    }
}
