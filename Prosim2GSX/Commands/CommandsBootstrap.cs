using Prosim2GSX.Commands.Handlers;

namespace Prosim2GSX.Commands
{
    // Single entry point for registering every command handler bundle. Called
    // from AppService.CreateServiceControllers after the registry is
    // constructed. Keeping it here means AppService doesn't grow a new
    // dependency every time a handler bundle is added — bundles plug in by
    // adding one line below.
    public static class CommandsBootstrap
    {
        public static void RegisterAll(AppService app, CommandRegistry registry)
        {
            OfpHandlers.Register(app, registry);
            ProfileHandlers.Register(app, registry);
            ChecklistHandlers.Register(app, registry);
        }
    }
}
