using System;
using System.Collections.Generic;
using System.Linq;

namespace Prosim2GSX.Web.Contracts
{
    // Snapshot of the Monitor (Flight Status) tab content — sim runtime, app
    // subsystem health, aircraft state, and a tail of recent log messages.
    // Read-only on the wire (no ApplyTo) — this surface is observe-only;
    // any writable controls live on the other three settings DTOs.
    public class FlightStatusDto
    {
        // Maximum messages returned in a single REST snapshot. The store keeps
        // 500 for durability; clients that need more can subscribe to the WS
        // delta stream (Phase 6) and reconstruct the running tail incrementally.
        public const int MessageLogTailSize = 200;

        // Sim runtime
        public bool SimRunning { get; set; }
        public bool SimConnected { get; set; }
        public bool SimSession { get; set; }
        public bool SimPaused { get; set; }
        public bool SimWalkaround { get; set; }
        public long CameraState { get; set; }
        public string SimVersion { get; set; } = "";
        public string AircraftString { get; set; } = "";

        // App subsystems
        public bool AppGsxController { get; set; }
        public bool AppAircraftBinary { get; set; }
        public bool AppAircraftInterface { get; set; }
        public bool AppProsimSdkConnected { get; set; }
        public bool AppAutomationController { get; set; }
        public bool AppAudioController { get; set; }

        // Aircraft / phase
        public bool AppOnGround { get; set; } = true;
        public bool AppEnginesRunning { get; set; }
        public bool AppInMotion { get; set; }
        public string AppProfile { get; set; } = "";
        public string AppAircraft { get; set; } = "";

        // Header strip (mirrors WPF HeaderBarControl) — read-only display.
        public string FlightNumber { get; set; } = "--------";
        public string UtcTime { get; set; } = "--:--Z";
        public string UtcDate { get; set; } = "------";

        // Live GSX runtime sub-section
        public GsxLiveDto Gsx { get; set; } = new();

        // Recent log tail (most recent last). Bounded by MessageLogTailSize.
        public List<string> MessageLog { get; set; } = new();

        // Reads from the long-lived FlightStatus + Gsx stores. The MessageLog
        // ObservableCollection is not thread-safe; Phase 6 controllers MUST
        // marshal calls to From() onto the WPF dispatcher before invoking,
        // or wrap with a defensive snapshot. The catch-and-empty fallback
        // here exists so a transient enumeration race never 500s the request.
        public static FlightStatusDto From(AppService app)
        {
            var fs = app.FlightStatus;
            return new FlightStatusDto
            {
                SimRunning = fs.SimRunning,
                SimConnected = fs.SimConnected,
                SimSession = fs.SimSession,
                SimPaused = fs.SimPaused,
                SimWalkaround = fs.SimWalkaround,
                CameraState = fs.CameraState,
                SimVersion = fs.SimVersion,
                AircraftString = fs.AircraftString,
                AppGsxController = fs.AppGsxController,
                AppAircraftBinary = fs.AppAircraftBinary,
                AppAircraftInterface = fs.AppAircraftInterface,
                AppProsimSdkConnected = fs.AppProsimSdkConnected,
                AppAutomationController = fs.AppAutomationController,
                AppAudioController = fs.AppAudioController,
                AppOnGround = fs.AppOnGround,
                AppEnginesRunning = fs.AppEnginesRunning,
                AppInMotion = fs.AppInMotion,
                AppProfile = fs.AppProfile,
                AppAircraft = fs.AppAircraft,
                FlightNumber = fs.FlightNumber,
                UtcTime = fs.UtcTime,
                UtcDate = fs.UtcDate,
                Gsx = GsxLiveDto.From(app.Gsx),
                MessageLog = SafeMessageLogTail(fs.MessageLog, MessageLogTailSize),
            };
        }

        private static List<string> SafeMessageLogTail(System.Collections.Generic.IEnumerable<string> log, int n)
        {
            try
            {
                // Materialise via ToList so we don't yield the underlying
                // ObservableCollection enumerator (which would throw if the
                // collection mutates mid-enumeration).
                var snapshot = log.ToList();
                return snapshot.Count <= n
                    ? snapshot
                    : snapshot.GetRange(snapshot.Count - n, n);
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }
    }
}
