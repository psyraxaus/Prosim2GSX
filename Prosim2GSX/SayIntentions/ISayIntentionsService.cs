using System.Collections.Generic;
using System.Threading.Tasks;

namespace Prosim2GSX.SayIntentions
{
    public class SayIntentionsAirportWx
    {
        public string Airport { get; set; } = "";
        public string Atis { get; set; } = "";
        public string Metar { get; set; } = "";
        public string Taf { get; set; } = "";
        public string ActiveRunway { get; set; } = "";
        public int? WindDirection { get; set; }
        public int? WindSpeed { get; set; }
    }

    public class SayIntentionsAssignResult
    {
        public bool Ok { get; init; }
        public string AssignedGate { get; init; } = "";
        public string Error { get; init; } = "";
    }

    public interface ISayIntentionsService
    {
        bool IsActive { get; }
        Task<SayIntentionsAssignResult> AssignGateAsync(string airportIcao, string gate);
        Task<IReadOnlyList<SayIntentionsAirportWx>> GetWeatherAsync(IEnumerable<string> icaos);
        // Returns the CPDLC logon address (e.g. "EFIN") for whatever airport
        // SayIntentions currently considers active (GPS + flight plan); empty
        // string when SayIntentions is inactive, the response has no CPDLC
        // entry, or the call fails. Endpoint takes no airport parameter — it
        // tracks departure → arrival on its own as the flight progresses.
        Task<string> GetCpdlcStationAsync();
    }
}
