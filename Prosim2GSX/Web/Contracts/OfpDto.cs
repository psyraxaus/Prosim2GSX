using Prosim2GSX.GSX;
using Prosim2GSX.Web.Contracts.Commands;
using System;

namespace Prosim2GSX.Web.Contracts
{
    // Read-only OFP tab snapshot. The OFP tab now owns gate / weather /
    // pushback only — flight info (dep/arr/alt/flightNumber/runways/fuel/
    // time/pax/distance) lives on the INIT tab via EfbFlightPlanDto.
    // DepartureIcao + ArrivalIcao are retained because the gate
    // assignment and weather cards still need them for context.
    //
    // The OFP panel REST-loads this on mount and on tab activation; WS
    // deltas on the "ofp" channel keep it current between fetches.
    public class OfpDto
    {
        // OFP metadata (live) — kept for gate-assignment + weather context.
        public bool IsOfpLoaded { get; set; }
        public string DepartureIcao { get; set; } = "";
        public string ArrivalIcao { get; set; } = "";

        // Workflow state
        public string PendingArrivalGate { get; set; } = "";
        public string GateAssignmentStatus { get; set; } = "";
        public string GsxAssignmentStatus { get; set; } = "";
        // GSX SetGate readback — formatted display ("C3", "Gate 12", or "").
        public string AssignedArrivalGate { get; set; } = "";

        // Weather
        public WeatherDto DepartureWeather { get; set; }
        public WeatherDto ArrivalWeather { get; set; }
        public string WeatherStatus { get; set; } = "";
        public bool IsRefreshingWeather { get; set; }
        // CPDLC logon address from SayIntentions getCurrentFrequencies
        // (single airport — endpoint tracks dep→arr automatically).
        public string CpdlcStation { get; set; } = "";
        // Last successful weather/CPDLC fetch — clients consult this to
        // decide whether to skip an auto-refresh on tab activation.
        public DateTimeOffset? WeatherFetchedAt { get; set; }

        // Pushback preference (in-memory on GsxController)
        public PushbackPreference PushbackPreference { get; set; } = PushbackPreference.Straight;

        // SayIntentions context — surfaced so the React panel can dim the
        // ATC-side controls / weather when the service is inactive.
        public bool UseSayIntentions { get; set; }
        public bool SayIntentionsActive { get; set; }

        public static OfpDto From(AppService app)
        {
            var ai = app?.GsxService?.AircraftInterface;
            var ofp = ai?.LastSimbriefOfp;
            var ofpState = app?.Ofp;
            var gsx = app?.GsxService;
            var config = app?.Config;
            var sayIntentions = app?.SayIntentionsService;

            return new OfpDto
            {
                IsOfpLoaded = ai?.IsFlightPlanLoaded == true,
                DepartureIcao = ai?.FmsOrigin ?? ofp?.Origin?.IcaoCode ?? "",
                ArrivalIcao = ai?.FmsDestination ?? ofp?.Destination?.IcaoCode ?? "",

                PendingArrivalGate = ofpState?.PendingArrivalGate ?? "",
                GateAssignmentStatus = ofpState?.GateAssignmentStatus ?? "",
                GsxAssignmentStatus = ofpState?.GsxAssignmentStatus ?? "",
                AssignedArrivalGate = ofpState?.AssignedArrivalGate ?? "",

                DepartureWeather = WeatherDto.From(ofpState?.DepartureWeather),
                ArrivalWeather = WeatherDto.From(ofpState?.ArrivalWeather),
                WeatherStatus = ofpState?.WeatherStatus ?? "",
                IsRefreshingWeather = ofpState?.IsRefreshingWeather ?? false,
                CpdlcStation = ofpState?.CpdlcStation ?? "",
                WeatherFetchedAt = ofpState?.WeatherFetchedAt,

                PushbackPreference = gsx?.PushbackPreference ?? PushbackPreference.Straight,
                UseSayIntentions = config?.UseSayIntentions == true,
                SayIntentionsActive = sayIntentions?.IsActive == true,
            };
        }

    }
}
