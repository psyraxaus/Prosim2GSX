namespace Prosim2GSX.Web.Contracts.Commands
{
    // Wire shapes for EFB Flight Planning command requests. Each request has
    // its own type (even when empty) so the CommandRegistry signatures stay
    // typed and the controller layer has an explicit body to validate.

    // Pre-fetch context entered on the INIT tab. The actual SimBrief fetch
    // keys off the SimBrief user dataref (efb.simbrief.id), not these
    // fields — they're echoed back for display + future server-side
    // validation that the OFP returned matches what the pilot entered.
    public class FetchOfpRequest
    {
        public string Departure { get; set; } = "";
        public string Arrival { get; set; } = "";
        public string Alternate { get; set; } = "";
        public string FlightNumber { get; set; } = "";
    }

    public class OverrideRequest
    {
        public string Field { get; set; } = "";
        public object Value { get; set; }
    }

    public class ClearOverrideRequest
    {
        public string Field { get; set; } = "";
    }

    public class ClearAllOverridesRequest { }
}
