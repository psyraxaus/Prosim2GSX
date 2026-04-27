namespace Prosim2GSX.Commands
{
    // Single entry point for registering every command handler bundle. Called
    // from AppService.CreateServiceControllers after the registry is
    // constructed. Keeping it here means AppService doesn't grow a new
    // dependency every time a handler bundle is added — bundles plug in by
    // adding one line below.
    //
    // Empty in Phase 8.0a; 8.0b adds OfpHandlers.Register(app, registry);
    // 8C adds ProfileHandlers.Register(app, registry).
    public static class CommandsBootstrap
    {
        public static void RegisterAll(AppService app, CommandRegistry registry)
        {
            // Intentionally empty — populated in subsequent Phase 8 commits.
        }
    }
}
