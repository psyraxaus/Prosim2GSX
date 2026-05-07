using Prosim2GSX.State;
using Prosim2GSX.Web.Contracts;
using Prosim2GSX.Web.Contracts.Commands;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Commands.Handlers
{
    // EFB Flight Planning command handlers. Mirrors OfpHandlers — every
    // mutation goes through the registry so the REST controller stays a
    // dispatch shell. All handlers return the post-write EfbFlightPlanDto so
    // the caller can update its local copy without a follow-up GET.
    public static class EfbFlightPlanHandlers
    {
        public static void Register(AppService app, CommandRegistry registry)
        {
            registry.Register<FetchOfpRequest, EfbFlightPlanDto>(
                "efb.fetchOfp",
                (req, ct) => FetchOfp(app, req, ct));

            registry.Register<OverrideRequest, EfbFlightPlanDto>(
                "efb.setOverride",
                (req, ct) => SetOverride(app, req, ct));

            registry.Register<ClearOverrideRequest, EfbFlightPlanDto>(
                "efb.clearOverride",
                (req, ct) => ClearOverride(app, req, ct));

            registry.Register<ClearAllOverridesRequest, EfbFlightPlanDto>(
                "efb.clearAllOverrides",
                (req, ct) => ClearAllOverrides(app, ct));

            registry.Register<ResetFlightRequest, EfbFlightPlanDto>(
                "efb.resetFlight",
                (req, ct) => ResetFlight(app, ct));
        }

        private static async Task<EfbFlightPlanDto> FetchOfp(AppService app, FetchOfpRequest req, CancellationToken ct)
        {
            var svc = app?.EfbFlightPlanService;
            if (svc == null)
                throw new CommandValidationException("EFB Flight Planning service not available.");
            await svc.FetchAsync(OfpSource.Manual, ct);
            return EfbFlightPlanDto.From(app);
        }

        private static Task<EfbFlightPlanDto> SetOverride(AppService app, OverrideRequest req, CancellationToken _)
        {
            var svc = app?.EfbFlightPlanService;
            if (svc == null)
                throw new CommandValidationException("EFB Flight Planning service not available.");
            if (string.IsNullOrWhiteSpace(req?.Field))
                throw new CommandValidationException("Override field must not be empty.");
            svc.SetOverride(req.Field, req.Value);
            return Task.FromResult(EfbFlightPlanDto.From(app));
        }

        private static Task<EfbFlightPlanDto> ClearOverride(AppService app, ClearOverrideRequest req, CancellationToken _)
        {
            var svc = app?.EfbFlightPlanService;
            if (svc == null)
                throw new CommandValidationException("EFB Flight Planning service not available.");
            if (string.IsNullOrWhiteSpace(req?.Field))
                throw new CommandValidationException("Override field must not be empty.");
            svc.ClearOverride(req.Field);
            return Task.FromResult(EfbFlightPlanDto.From(app));
        }

        private static Task<EfbFlightPlanDto> ClearAllOverrides(AppService app, CancellationToken _)
        {
            var svc = app?.EfbFlightPlanService;
            if (svc == null)
                throw new CommandValidationException("EFB Flight Planning service not available.");
            svc.ClearAllOverrides();
            return Task.FromResult(EfbFlightPlanDto.From(app));
        }

        private static Task<EfbFlightPlanDto> ResetFlight(AppService app, CancellationToken _)
        {
            var svc = app?.EfbFlightPlanService;
            if (svc == null)
                throw new CommandValidationException("EFB Flight Planning service not available.");
            svc.ResetFlight();
            return Task.FromResult(EfbFlightPlanDto.From(app));
        }
    }
}
