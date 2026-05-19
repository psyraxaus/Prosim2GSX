using Prosim2GSX.GSX;

namespace Prosim2GSX.Web.Contracts
{
    // Deice holdover-time card. Nested in FlightStatusDto and pushed on the
    // "deiceHoldover" WS channel (patch-only, nests under
    // flightStatus.deiceHoldover client-side — same shape as the "gsx"
    // channel). Precip/OatC are crew inputs set via /api/deice.
    public class DeiceHoldoverDto
    {
        public bool Active { get; set; }
        public bool Expired { get; set; }
        public int FluidType { get; set; }
        public string FluidLabel { get; set; } = "";
        public int Concentration { get; set; }

        // Crew inputs.
        public HotPrecip Precip { get; set; } = HotPrecip.None;
        public double OatC { get; set; }
        public bool OatUserSet { get; set; }

        public double LowMinutes { get; set; }
        public double HighMinutes { get; set; }
        public int RemainingLowSeconds { get; set; }
        public int RemainingHighSeconds { get; set; }
        public string Status { get; set; } = "";

        public static DeiceHoldoverDto From(State.DeiceHoldoverState s) => new()
        {
            Active = s.Active,
            Expired = s.Expired,
            FluidType = s.FluidType,
            FluidLabel = s.FluidLabel,
            Concentration = s.Concentration,
            Precip = s.Precip,
            OatC = s.OatC,
            OatUserSet = s.OatUserSet,
            LowMinutes = s.LowMinutes,
            HighMinutes = s.HighMinutes,
            RemainingLowSeconds = s.RemainingLowSeconds,
            RemainingHighSeconds = s.RemainingHighSeconds,
            Status = s.Status,
        };
    }
}
