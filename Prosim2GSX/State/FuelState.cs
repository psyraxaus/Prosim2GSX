using CommunityToolkit.Mvvm.ComponentModel;

namespace Prosim2GSX.State
{
    // Long-lived observable mirror of the FUEL tab content. Owned by
    // AppService for the app's lifetime; populated each StateUpdateWorker
    // tick by FuelService. Same pattern as WeightBalanceState — every field
    // is [ObservableProperty] so per-property WS patches fire only on actual
    // change (the generator emits an equality compare-and-skip in the setter).
    //
    // Per-tank fields (Centre / Left / Right) are present in full because
    // ProsimDataref.csv exposes both .amount.kg and .capacity for each tank.
    // ACT1/ACT2 are intentionally NOT surfaced — A320-family aircraft load
    // them only in ACT-equipped variants; the spec scope is the three main
    // tanks, and adding ACT today would render a permanently-empty bar.
    public partial class FuelState : ObservableObject
    {
        // Planned ramp/block fuel from the EFB INIT cache (CurrentOfp).
        // Normalised to kg at parse time by EfbFlightPlanService — no
        // conversion needed here. Zero when no OFP has been loaded.
        [ObservableProperty] private double _PlannedRampKg;

        // Total fuel currently in tanks (read from RefFuelTotal, kg).
        [ObservableProperty] private double _FuelInTanksKg;

        // Per-tank current amounts (kg). Centre / Left / Right.
        [ObservableProperty] private double _FuelCentreKg;
        [ObservableProperty] private double _FuelLeftKg;
        [ObservableProperty] private double _FuelRightKg;

        // Total usable fuel capacity (RefFuelTotalCapacity).
        [ObservableProperty] private double _FuelCapacityKg;

        // Per-tank capacities (added to ProsimConstants alongside the
        // amount refs). Drive per-tank fill-ratio bars on the panels.
        [ObservableProperty] private double _FuelCentreCapacityKg;
        [ObservableProperty] private double _FuelLeftCapacityKg;
        [ObservableProperty] private double _FuelRightCapacityKg;

        // Derived: in-tanks minus planned. Positive = over-fuelled,
        // negative = under-fuelled. Computed by FuelService.Tick() so the
        // wire shape matches without the consumer having to do arithmetic.
        [ObservableProperty] private double _FuelDeltaKg;

        // Operator flags. Suppressed (always false) when PlannedRampKg == 0
        // so a freshly-loaded aircraft with no OFP doesn't falsely show as
        // "OVER-FUELLED" simply because tanks > 0 and planned = 0.
        [ObservableProperty] private bool _IsOverFuelled;
        [ObservableProperty] private bool _IsUnderFuelled;

        // Litre-conversion mirrors of the kg figures. Computed in the
        // service using SpecificGravity.
        [ObservableProperty] private double _PlannedRampLitres;
        [ObservableProperty] private double _FuelInTanksLitres;

        // Jet A-1 specific gravity. Hardcoded — matches the existing W&B
        // panel display ("SG: 0.80") and the wider industry default. Public
        // const so FuelDto and FuelService can read it without instance
        // access; a future per-airframe override would live on AircraftProfile.
        public const double SpecificGravity = 0.80;

        // Threshold below which the panel flags an UNDER-FUELLED warning.
        // 100 kg matches the spec; large enough to absorb the normal fuel
        // jitter during boarding (truck connect/disconnect can momentarily
        // toggle the value by single-digit kg).
        public const double UnderFuelThresholdKg = 100.0;
    }
}
