using System.Threading.Tasks;

namespace Prosim2GSX.Services
{
    public interface IAcarsService
    {
        string Callsign { get; set; }
        string AcarsNetworkUrl { get; set; }
        string LogonSecret { get; set; }

        bool Initialize(string flightNumber);
        Task SendMessageAsync(string toCallsign, string messageType, string packetData);
        Task SendPreliminaryLoadsheetAsync(string flightNumber, (string time, string flightNumber, string tailNumber, string day, string date, string orig, string dest, double est_zfw, double max_zfw, double est_tow, double max_tow, double est_law, double max_law, int paxInfants, int paxAdults, double macZfw, double macTow, int paxZoneA, int paxZoneB, int paxZoneC, double fuelInTanks) loadsheetData);
        Task SendFinalLoadsheetAsync(string flightNumber, (string time, string flightNumber, string tailNumber, string day, string date, string orig, string dest, double est_zfw, double max_zfw, double est_tow, double max_tow, double est_law, double max_law, int paxInfants, int paxAdults, double macZfw, double macTow, int paxZoneA, int paxZoneB, int paxZoneC, double fuelInTanks) loadsheetData, (double zfw, double tow, int pax, double macZfw, double macTow, double fuel) preliminaryData);
        string FlightCallsignToOpsCallsign(string flightNumber);
    }
}
