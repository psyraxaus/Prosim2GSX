using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Prosim2GSX.State
{
    // Long-lived observable mirror of the sim/app runtime status that the Monitor
    // tab presents. Owned by AppService for the app's lifetime — independent of
    // whether the WPF Monitor tab is currently active. The transient ModelMonitor
    // and the future web layer both observe this as the single source of truth.
    public partial class FlightStatusState : ObservableObject
    {
        [ObservableProperty] private bool _SimRunning;
        [ObservableProperty] private bool _SimConnected;
        [ObservableProperty] private bool _SimSession;
        [ObservableProperty] private bool _SimPaused;
        [ObservableProperty] private bool _SimWalkaround;
        [ObservableProperty] private long _CameraState;
        [ObservableProperty] private string _SimVersion = "";
        [ObservableProperty] private string _AircraftString = "";

        [ObservableProperty] private bool _AppGsxController;
        [ObservableProperty] private bool _AppAircraftBinary;
        [ObservableProperty] private bool _AppAircraftInterface;
        [ObservableProperty] private bool _AppProsimSdkConnected;
        [ObservableProperty] private bool _AppAutomationController;
        [ObservableProperty] private bool _AppAudioController;

        [ObservableProperty] private bool _AppOnGround = true;
        [ObservableProperty] private bool _AppEnginesRunning;
        [ObservableProperty] private bool _AppInMotion;
        [ObservableProperty] private string _AppProfile = "";
        [ObservableProperty] private string _AppAircraft = "Airline / Title / Registration";

        // Header-strip parity with the WPF HeaderBarControl. Read-only on the wire.
        [ObservableProperty] private string _FlightNumber = "--------";
        [ObservableProperty] private string _UtcTime = "--:--Z";
        [ObservableProperty] private string _UtcDate = "------";

        // Bounded ring buffer of recent log messages drained from CFIT Logger.Messages.
        // The drain worker is the only mutator; all reads/writes happen on the WPF
        // dispatcher (so existing WPF binding semantics are preserved).
        public ObservableCollection<string> MessageLog { get; } = [];
    }
}
