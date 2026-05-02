using Prosim2GSX.GSX;
using Prosim2GSX.SayIntentions;
using System;

namespace Prosim2GSX.Web.Contracts.Commands
{
    // Wire shapes for OFP command requests + responses. Each request has its
    // own type even when empty (SendNow, RefreshWeather) — keeps the
    // CommandRegistry's typed signatures clean and gives the controller layer
    // an explicit body to validate.

    public class ConfirmArrivalGateRequest
    {
        public string Gate { get; set; } = "";
    }

    public class SendNowRequest { }
    public class RefreshWeatherRequest { }

    public class SetPushbackPreferenceRequest
    {
        public PushbackPreference Preference { get; set; } = PushbackPreference.Straight;
    }

    // Response: the post-write snapshot of the gate-assignment workflow.
    public class GateAssignmentDto
    {
        public string PendingArrivalGate { get; set; } = "";
        public string GateAssignmentStatus { get; set; } = "";
        public string GsxAssignmentStatus { get; set; } = "";
    }

    public class WeatherSnapshotDto
    {
        public WeatherDto DepartureWeather { get; set; }
        public WeatherDto ArrivalWeather { get; set; }
        public string WeatherStatus { get; set; } = "";
        public bool IsRefreshingWeather { get; set; }
        public string CpdlcStation { get; set; } = "";
        public DateTimeOffset? WeatherFetchedAt { get; set; }
    }

    // Wire shape for SayIntentionsAirportWx — exposes only the fields the
    // panel will render. The internal type stays internal.
    public class WeatherDto
    {
        public string Airport { get; set; } = "";
        public string Atis { get; set; } = "";
        public string Metar { get; set; } = "";
        public string Taf { get; set; } = "";
        public string ActiveRunway { get; set; } = "";
        public int? WindDirection { get; set; }
        public int? WindSpeed { get; set; }

        public static WeatherDto From(SayIntentionsAirportWx src)
        {
            if (src == null) return null;
            return new WeatherDto
            {
                Airport = src.Airport ?? "",
                Atis = src.Atis ?? "",
                Metar = src.Metar ?? "",
                Taf = src.Taf ?? "",
                ActiveRunway = src.ActiveRunway ?? "",
                WindDirection = src.WindDirection,
                WindSpeed = src.WindSpeed,
            };
        }
    }

    public class PushbackPreferenceDto
    {
        public PushbackPreference Preference { get; set; }
    }
}
