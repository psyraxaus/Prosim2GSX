using Prosim2GSX.GSX.Services;
using ProsimInterface;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Menu.Intents
{
    /// <summary>
    /// Additional-services intent: request lavatory service.
    /// </summary>
    internal sealed class RequestLavatory : KeywordIntent
    {
        public RequestLavatory()
            : base(@"(?i)^request lavatory")
        { }

        public override GsxMenuIntent ParentMenu => new OpenAdditionalServicesMenu();
        public override string ExpectedMenuTitlePrefix => GsxConstants.MenuAdditionalServices;

        public override bool IsValidForPhase(AutomationState phase)
            => phase == AutomationState.Preparation
            || phase == AutomationState.Departure
            || phase == AutomationState.TurnAround;

        public override bool ArePreconditionsSatisfied(GsxController controller)
        {
            var svc = IntentHelpers.GetService<GsxService>(controller, GsxServiceType.Lavatory);
            return svc != null && svc.State == GsxServiceState.Callable;
        }

        public override bool IsAlreadySatisfied(GsxController controller)
        {
            var svc = IntentHelpers.GetService<GsxService>(controller, GsxServiceType.Lavatory);
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
                    var svc = IntentHelpers.GetService<GsxService>(controller, GsxServiceType.Lavatory);
                    return svc != null
                        && (svc.State == GsxServiceState.Requested || svc.State == GsxServiceState.Active);
                },
                timeout,
                IntentHelpers.DefaultPollInterval,
                token);
        }

        public override string Describe() => "Request GSX lavatory service";
    }
}
