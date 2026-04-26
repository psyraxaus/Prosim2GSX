using Prosim2GSX.GSX;
using Prosim2GSX.GSX.Menu;
using Prosim2GSX.GSX.Services;
using ProsimInterface;

namespace Prosim2GSX.Web.Contracts
{
    // Live GSX runtime state — the bottom half of the Monitor (Flight Status)
    // tab. Embedded in FlightStatusDto rather than exposed at /api/gsx because
    // it's status, not configuration. /api/gsxsettings is the configuration
    // surface (GsxSettingsDto).
    public class GsxLiveDto
    {
        public bool GsxRunning { get; set; }
        public string GsxStarted { get; set; } = "";
        public bool GsxStartedValid { get; set; }
        public GsxMenuState GsxMenu { get; set; } = GsxMenuState.UNKNOWN;

        public int GsxPaxTarget { get; set; }
        public string GsxPaxTotal { get; set; } = "0 | 0";
        public string GsxCargoProgress { get; set; } = "0 | 0";

        public GsxServiceState ServiceReposition { get; set; }
        public GsxServiceState ServiceRefuel { get; set; }
        public GsxServiceState ServiceCatering { get; set; }
        public GsxServiceState ServiceLavatory { get; set; }
        public GsxServiceState ServiceWater { get; set; }
        public GsxServiceState ServiceCleaning { get; set; }

        public bool ServiceGpuConnected { get; set; }
        public bool ServiceGpuPhaseRelevant { get; set; }

        public GsxServiceState ServiceBoarding { get; set; }
        public GsxServiceState ServiceDeboarding { get; set; }
        public string ServicePushback { get; set; } = "";
        public GsxServiceState ServiceJetway { get; set; }
        public GsxServiceState ServiceStairs { get; set; }

        public AutomationState AppAutomationState { get; set; } = AutomationState.SessionStart;
        public string AppAutomationDepartureServices { get; set; } = "0 / 0";

        public static GsxLiveDto From(State.GsxState s) => new()
        {
            GsxRunning = s.GsxRunning,
            GsxStarted = s.GsxStarted,
            GsxStartedValid = s.GsxStartedValid,
            GsxMenu = s.GsxMenu,
            GsxPaxTarget = s.GsxPaxTarget,
            GsxPaxTotal = s.GsxPaxTotal,
            GsxCargoProgress = s.GsxCargoProgress,
            ServiceReposition = s.ServiceReposition,
            ServiceRefuel = s.ServiceRefuel,
            ServiceCatering = s.ServiceCatering,
            ServiceLavatory = s.ServiceLavatory,
            ServiceWater = s.ServiceWater,
            ServiceCleaning = s.ServiceCleaning,
            ServiceGpuConnected = s.ServiceGpuConnected,
            ServiceGpuPhaseRelevant = s.ServiceGpuPhaseRelevant,
            ServiceBoarding = s.ServiceBoarding,
            ServiceDeboarding = s.ServiceDeboarding,
            ServicePushback = s.ServicePushback,
            ServiceJetway = s.ServiceJetway,
            ServiceStairs = s.ServiceStairs,
            AppAutomationState = s.AppAutomationState,
            AppAutomationDepartureServices = s.AppAutomationDepartureServices,
        };
    }
}
