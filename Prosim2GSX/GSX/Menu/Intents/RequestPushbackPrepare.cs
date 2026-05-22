using Prosim2GSX.GSX.Services;
using ProsimInterface;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Menu.Intents
{
    /// <summary>
    /// Gate-menu service intent: request GSX pushback preparation and
    /// departure. Matches "Prepare for Push-back and Departure" — pattern
    /// tolerates GSX's hyphenated/non-hyphenated variants on the entry text.
    /// Underlying GSX state lives on <c>FSDT_GSX_DEPARTURE_STATE</c>
    /// (<see cref="GsxConstants.VarServiceDeparture"/>), exposed via the
    /// <c>GsxServiceType.Pushback</c> service entry.
    /// </summary>
    internal sealed class RequestPushbackPrepare : KeywordIntent
    {
        public RequestPushbackPrepare()
            : base(@"(?i)^prepare for push.?back")
        { }

        public override GsxMenuIntent ParentMenu => new OpenGateMenu();
        public override string ExpectedMenuTitlePrefix => GsxConstants.MenuGate;

        public override bool IsValidForPhase(AutomationState phase)
            => phase == AutomationState.Departure;

        public override bool ArePreconditionsSatisfied(GsxController controller)
        {
            var svc = IntentHelpers.GetService<GsxService>(controller, GsxServiceType.Pushback);
            return svc != null && svc.State == GsxServiceState.Callable;
        }

        public override bool IsAlreadySatisfied(GsxController controller)
        {
            var svc = IntentHelpers.GetService<GsxService>(controller, GsxServiceType.Pushback);
            return svc != null && svc.State == GsxServiceState.Completed;
        }

        public override Task<bool> VerifyOutcomeAsync(GsxController controller, TimeSpan timeout, CancellationToken token)
        {
            return IntentHelpers.PollUntilAsync(
                () =>
                {
                    var svc = IntentHelpers.GetService<GsxService>(controller, GsxServiceType.Pushback);
                    return svc != null
                        && (svc.State == GsxServiceState.Requested || svc.State == GsxServiceState.Active);
                },
                timeout,
                IntentHelpers.DefaultPollInterval,
                token);
        }

        public override string Describe() => "Request GSX pushback preparation and departure";
    }
}
