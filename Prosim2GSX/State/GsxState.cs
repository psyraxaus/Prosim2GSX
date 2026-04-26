using CommunityToolkit.Mvvm.ComponentModel;
using Prosim2GSX.GSX;
using Prosim2GSX.GSX.Menu;
using Prosim2GSX.GSX.Services;
using ProsimInterface;

namespace Prosim2GSX.State
{
    // Long-lived observable mirror of the GSX live runtime status that the Monitor
    // tab presents. Owned by AppService for the app's lifetime; independent of any
    // tab being open.
    public partial class GsxState : ObservableObject
    {
        [ObservableProperty] private bool _GsxRunning;
        [ObservableProperty] private string _GsxStarted = "";
        // GsxStartedValid is the bool the Monitor tab uses to colour the GsxStarted
        // indicator — kept here so view-side colour computation stays a pure
        // projection of state.
        [ObservableProperty] private bool _GsxStartedValid;
        [ObservableProperty] private GsxMenuState _GsxMenu = GsxMenuState.UNKNOWN;
        [ObservableProperty] private int _GsxPaxTarget;
        [ObservableProperty] private string _GsxPaxTotal = "0 | 0";
        [ObservableProperty] private string _GsxCargoProgress = "0 | 0";

        [ObservableProperty] private GsxServiceState _ServiceReposition = GsxServiceState.Unknown;
        [ObservableProperty] private GsxServiceState _ServiceRefuel = GsxServiceState.Unknown;
        [ObservableProperty] private GsxServiceState _ServiceCatering = GsxServiceState.Unknown;
        [ObservableProperty] private GsxServiceState _ServiceLavatory = GsxServiceState.Unknown;
        [ObservableProperty] private GsxServiceState _ServiceWater = GsxServiceState.Unknown;
        [ObservableProperty] private GsxServiceState _ServiceCleaning = GsxServiceState.Unknown;

        [ObservableProperty] private bool _ServiceGpuConnected;
        // The Monitor tab greys the GPU indicator when the current automation phase
        // is not one in which GPU connection is meaningful (Preparation/Departure/
        // Arrival/TurnAround). Surfacing the gate as state lets both UIs decide.
        [ObservableProperty] private bool _ServiceGpuPhaseRelevant;

        [ObservableProperty] private GsxServiceState _ServiceBoarding = GsxServiceState.Unknown;
        [ObservableProperty] private GsxServiceState _ServiceDeboarding = GsxServiceState.Unknown;
        [ObservableProperty] private string _ServicePushback = $"{GsxServiceState.Unknown} (0)";
        [ObservableProperty] private GsxServiceState _ServiceJetway = GsxServiceState.Unknown;
        [ObservableProperty] private GsxServiceState _ServiceStairs = GsxServiceState.Unknown;

        [ObservableProperty] private AutomationState _AppAutomationState = AutomationState.SessionStart;
        [ObservableProperty] private string _AppAutomationDepartureServices = "0 / 0";
    }
}
