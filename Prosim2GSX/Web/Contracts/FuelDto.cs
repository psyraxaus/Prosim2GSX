using Prosim2GSX.State;

namespace Prosim2GSX.Web.Contracts
{
    // Read-only Fuel snapshot. Wire-shape mirror of FuelState (which is the
    // long-lived store written by FuelService each tick). The React panel
    // and the WPF Model both consume this DTO via GET /api/fuel + WS "fuel"
    // patches.
    //
    // SpecificGravity and UnderFuelThresholdKg are surfaced on the wire so
    // the panel can render the SG label and decide UI thresholds (delta
    // colours, under-fuel warning) without a second config fetch.
    public class FuelDto
    {
        public double PlannedRampKg { get; set; }
        public double FuelInTanksKg { get; set; }
        public double FuelCentreKg { get; set; }
        public double FuelLeftKg { get; set; }
        public double FuelRightKg { get; set; }
        public double FuelLeftOuterKg { get; set; }
        public double FuelLeftInnerKg { get; set; }
        public double FuelRightInnerKg { get; set; }
        public double FuelRightOuterKg { get; set; }
        public double FuelCapacityKg { get; set; }
        public double FuelCentreCapacityKg { get; set; }
        public double FuelLeftCapacityKg { get; set; }
        public double FuelRightCapacityKg { get; set; }
        public double FuelLeftOuterCapacityKg { get; set; }
        public double FuelLeftInnerCapacityKg { get; set; }
        public double FuelRightInnerCapacityKg { get; set; }
        public double FuelRightOuterCapacityKg { get; set; }
        public double FuelDeltaKg { get; set; }
        public bool IsOverFuelled { get; set; }
        public bool IsUnderFuelled { get; set; }
        public double SpecificGravity { get; set; }
        public double PlannedRampLitres { get; set; }
        public double FuelInTanksLitres { get; set; }
        public double UnderFuelThresholdKg { get; set; }

        public static FuelDto From(AppService app)
        {
            var s = app?.Fuel;
            if (s == null)
            {
                return new FuelDto
                {
                    SpecificGravity = FuelState.SpecificGravity,
                    UnderFuelThresholdKg = FuelState.UnderFuelThresholdKg,
                };
            }

            return new FuelDto
            {
                PlannedRampKg = s.PlannedRampKg,
                FuelInTanksKg = s.FuelInTanksKg,
                FuelCentreKg = s.FuelCentreKg,
                FuelLeftKg = s.FuelLeftKg,
                FuelRightKg = s.FuelRightKg,
                FuelLeftOuterKg = s.FuelLeftOuterKg,
                FuelLeftInnerKg = s.FuelLeftInnerKg,
                FuelRightInnerKg = s.FuelRightInnerKg,
                FuelRightOuterKg = s.FuelRightOuterKg,
                FuelCapacityKg = s.FuelCapacityKg,
                FuelCentreCapacityKg = s.FuelCentreCapacityKg,
                FuelLeftCapacityKg = s.FuelLeftCapacityKg,
                FuelRightCapacityKg = s.FuelRightCapacityKg,
                FuelLeftOuterCapacityKg = s.FuelLeftOuterCapacityKg,
                FuelLeftInnerCapacityKg = s.FuelLeftInnerCapacityKg,
                FuelRightInnerCapacityKg = s.FuelRightInnerCapacityKg,
                FuelRightOuterCapacityKg = s.FuelRightOuterCapacityKg,
                FuelDeltaKg = s.FuelDeltaKg,
                IsOverFuelled = s.IsOverFuelled,
                IsUnderFuelled = s.IsUnderFuelled,
                SpecificGravity = FuelState.SpecificGravity,
                PlannedRampLitres = s.PlannedRampLitres,
                FuelInTanksLitres = s.FuelInTanksLitres,
                UnderFuelThresholdKg = FuelState.UnderFuelThresholdKg,
            };
        }
    }
}
