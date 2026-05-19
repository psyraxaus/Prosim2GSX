namespace Prosim2GSX.Web.Contracts
{
    // Canonical flight identity for the in-sim handler script to render on
    // the gate's VDGS display via addVdgsMessage(). Sourced from the EFB
    // INIT canonical OFP (EfbFlightPlanState.CurrentOfp) with a live-FMS
    // fallback for origin/destination, mirroring OfpDto's resolution so the
    // VDGS agrees with the rest of the app rather than GSX's own getSimbrief.
    //
    // Consumed only by gsx_handler.py over GET /api/gsxmenu/flight-info
    // (loopback, unauthenticated by necessity — same exemption as
    // pending-gate/events). The controller returns null when nothing is
    // loaded so the handler clears the VDGS message.
    public class FlightInfoDto
    {
        public string Callsign { get; set; } = "";
        public string FlightNumber { get; set; } = "";
        public string Airline { get; set; } = "";
        public string Origin { get; set; } = "";
        public string Destination { get; set; } = "";

        // True only when there is enough identity to display something
        // meaningful — otherwise the controller returns null.
        public bool HasContent =>
            !string.IsNullOrWhiteSpace(Callsign)
            || !string.IsNullOrWhiteSpace(FlightNumber)
            || (!string.IsNullOrWhiteSpace(Origin) && !string.IsNullOrWhiteSpace(Destination));

        public static FlightInfoDto From(AppService app)
        {
            var ai = app?.GsxService?.AircraftInterface;
            var ofp = app?.EfbFlightPlan?.CurrentOfp;

            string origin = !string.IsNullOrWhiteSpace(ai?.FmsOrigin)
                ? ai.FmsOrigin
                : ofp?.DepartureIcao ?? "";
            string destination = !string.IsNullOrWhiteSpace(ai?.FmsDestination)
                ? ai.FmsDestination
                : ofp?.ArrivalIcao ?? "";

            return new FlightInfoDto
            {
                Callsign = ofp?.Callsign ?? "",
                FlightNumber = ofp?.FlightNumber ?? "",
                Airline = ofp?.AirlineIcao ?? "",
                Origin = (origin ?? "").Trim().ToUpperInvariant(),
                Destination = (destination ?? "").Trim().ToUpperInvariant(),
            };
        }
    }
}
