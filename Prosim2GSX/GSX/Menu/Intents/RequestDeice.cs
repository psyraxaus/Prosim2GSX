using Prosim2GSX.GSX.Services;
using ProsimInterface;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Menu.Intents
{
    /// <summary>
    /// Additional-services intent: request de-icing. Pattern tolerates GSX's
    /// "Request DeIce" and "Request De-Ice" variants. Note this intent only
    /// opens the de-ice request; fluid-type selection (the subsequent
    /// "Select de-icing type" menu) stays on the existing
    /// <see cref="GsxMenu.OnDeiceTypeSelect"/> callback path until that's
    /// migrated as its own intent.
    /// </summary>
    internal sealed class RequestDeice : KeywordIntent
    {
        public RequestDeice()
            : base(@"(?i)^request de.?ice")
        { }

        public override GsxMenuIntent ParentMenu => new OpenAdditionalServicesMenu();
        public override string ExpectedMenuTitlePrefix => GsxConstants.MenuAdditionalServices;

        public override bool IsValidForPhase(AutomationState phase)
            => phase == AutomationState.Departure;

        public override bool ArePreconditionsSatisfied(GsxController controller)
        {
            var svc = IntentHelpers.GetService<GsxService>(controller, GsxServiceType.Deice);
            return svc != null && svc.State == GsxServiceState.Callable;
        }

        public override bool IsAlreadySatisfied(GsxController controller)
        {
            var svc = IntentHelpers.GetService<GsxService>(controller, GsxServiceType.Deice);
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
                    var svc = IntentHelpers.GetService<GsxService>(controller, GsxServiceType.Deice);
                    return svc != null
                        && (svc.State == GsxServiceState.Requested || svc.State == GsxServiceState.Active);
                },
                timeout,
                IntentHelpers.DefaultPollInterval,
                token);
        }

        public override string Describe() => "Request GSX de-icing service";
    }
}
