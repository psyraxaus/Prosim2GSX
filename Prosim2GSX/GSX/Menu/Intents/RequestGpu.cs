using Prosim2GSX.GSX.Services;
using ProsimInterface;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.GSX.Menu.Intents
{
    /// <summary>
    /// Additional-services intent: request the Ground Power Unit on the
    /// Additional Services submenu. Unlike one-shot services
    /// (catering/refuel/etc.), GPU stays <see cref="GsxServiceState.Active"/>
    /// for the whole time it remains connected — so <see cref="IsAlreadySatisfied"/>
    /// short-circuits on both Active and Completed to avoid re-requesting
    /// an already-connected unit.
    /// </summary>
    internal sealed class RequestGpu : KeywordIntent
    {
        public RequestGpu()
            : base(@"(?i)^request gpu")
        { }

        public override GsxMenuIntent ParentMenu => new OpenAdditionalServicesMenu();
        public override string ExpectedMenuTitlePrefix => GsxConstants.MenuAdditionalServices;

        public override bool IsValidForPhase(AutomationState phase)
            => phase == AutomationState.Preparation
            || phase == AutomationState.Departure
            || phase == AutomationState.Arrival
            || phase == AutomationState.TurnAround;

        public override bool ArePreconditionsSatisfied(GsxController controller)
        {
            var svc = IntentHelpers.GetService<GsxService>(controller, GsxServiceType.GPU);
            return svc != null && svc.State == GsxServiceState.Callable;
        }

        public override bool IsAlreadySatisfied(GsxController controller)
        {
            // Requested/Active/Completed all mean "no menu action needed".
            // GPU stays Active while connected; Requested is the brief
            // intermediate state between Callable and Active.
            var svc = IntentHelpers.GetService<GsxService>(controller, GsxServiceType.GPU);
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
                    var svc = IntentHelpers.GetService<GsxService>(controller, GsxServiceType.GPU);
                    return svc != null
                        && (svc.State == GsxServiceState.Requested || svc.State == GsxServiceState.Active);
                },
                timeout,
                IntentHelpers.DefaultPollInterval,
                token);
        }

        public override string Describe() => "Request GSX Ground Power Unit";
    }
}
