using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Prosim2GSX.State
{
    // Long-lived observable mirror of the Weight & Balance tab content. Owned
    // by AppService for the app's lifetime; populated each tick by
    // WeightBalanceService via StateUpdateWorker. The [ObservableProperty]
    // generator gives equality-based compare-and-skip on every setter, which
    // is the project-wide convention for keeping WS broadcasts cheap (no
    // throttle layer — INPC only fires on actual change).
    //
    // Aircraft envelope limits are hardcoded constants on this store. They
    // are A320 family values; per-variant differences (CEO vs NEO vs ACT
    // configs) would warrant moving them onto AircraftProfile, but that's a
    // future refinement once additional airframes are flown.
    public partial class WeightBalanceState : ObservableObject
    {
        // Live aircraft state — kg / % MAC.
        [ObservableProperty] private double _ZfwKg;
        [ObservableProperty] private double _MaczfwPercent;
        [ObservableProperty] private double _GwKg;
        [ObservableProperty] private double _MacgwPercent;

        // Fuel — planned (from SimBrief OFP) + in tanks + tank capacity.
        // FuelCapacityKg comes from aircraft.fuel.total.capacity so the React
        // panel's "CAPACITY USABLE" header label adapts to the airframe.
        [ObservableProperty] private double _FuelPlannedKg;
        [ObservableProperty] private double _FuelInTanksKg;
        [ObservableProperty] private double _FuelCapacityKg;

        // Cargo — current loaded amounts vs hold capacities. There is no
        // aircraft.cargo.bulk.amount dataref in ProsimDataref.csv, so the bulk
        // hold's loaded weight is intentionally NOT exposed here (the field
        // was dropped per Phase 1 archaeology). CargoPlannedKg comes from the
        // efb.plannedCargoKg dataref.
        [ObservableProperty] private double _CargoFwdLoadedKg;
        [ObservableProperty] private double _CargoFwdCapacityKg;
        [ObservableProperty] private double _CargoAftLoadedKg;
        [ObservableProperty] private double _CargoAftCapacityKg;
        [ObservableProperty] private double _CargoBulkCapacityKg;
        [ObservableProperty] private double _CargoPlannedKg;

        // Passengers — planned (efb.passengerStatistics) vs boarded (count of
        // 'T' characters in seatOccupation). Zone capacities come from
        // aircraft.passengers.zone[1..4].capacity for the SVG cabin layout.
        [ObservableProperty] private int _PassengersPlanned;
        [ObservableProperty] private int _PassengersBoarded;
        [ObservableProperty] private int _Zone1Capacity;
        [ObservableProperty] private int _Zone2Capacity;
        [ObservableProperty] private int _Zone3Capacity;
        [ObservableProperty] private int _Zone4Capacity;
        [ObservableProperty] private string _SeatOccupation = "";

        // Cargo door state — drives door-open warnings on the chart.
        [ObservableProperty] private bool _FwdCargoDoorOpen;
        [ObservableProperty] private bool _AftCargoDoorOpen;

        // Take-off MAC% — resolved each tick by MactowValidationService from
        // the loadsheet (final → prelim → live computed) so both UIs see the
        // same authoritative value. MacTowSource records which path produced
        // the value so the panel can show "FINAL LS" / "PRELIM LS" /
        // "COMPUTED" chips and adjust the SYNC TO FMS button label
        // accordingly. MacTowError is the envelope check against
        // LoadsheetState.MinMacTow / MaxMacTow.
        [ObservableProperty] private double _MactowPercent;
        [ObservableProperty] private bool _MacTowError;
        [ObservableProperty] private string _MacTowSource = "computed";

        // FMS sync staleness. Set by MactowValidationService on every tick:
        // true when a prior sync exists AND any of MACTOW (>0.1 %MAC),
        // ZFW (>100 kg), or block-fuel (>200 kg) have drifted past their
        // operational tolerance, OR the loadsheet source has upgraded
        // (prelim → final). Drives the "RESYNC TO FMS" button label and a
        // chip that calls out the staleness reason.
        [ObservableProperty] private bool _FmsSyncStale;
        [ObservableProperty] private DateTime? _FmsLastSyncedAt;
        [ObservableProperty] private string _FmsLastSyncedSource = "";

        // Hardcoded A320-family envelope limits. Public so WeightBalanceDto
        // can project them onto the wire without reflection.
        public double MtowLimitKg => 73500.0;
        public double MlwLimitKg => 64500.0;
        public double MzfwLimitKg => 61000.0;

        // Index step into ZfwcgAdjArray. The array stores 200 paired entries
        // (each unique value repeated twice consecutively), giving an
        // effective 200kg per unique value while still keeping the natural
        // index = floor(fuel / 100) calculation. Confirmed against the EFB
        // source — do NOT collapse the pairs; the pairing is intentional and
        // produces flat segments that match how the EFB renders the curve.
        public const double FuelStepKg = 100.0;

        // ZFW CG adjustment lookup, indexed by floor(FuelInTanksKg / FuelStepKg)
        // and clamped to [0, 199]. MACGW = MACZFW + ZfwcgAdjArray[index]. Values
        // are negative (fuel forward of CG drives MAC% aft as fuel burns).
        public static readonly double[] ZfwcgAdjArray = new double[]
        {
            0, 0, -0.0611692667008015, -0.0611692667008015,
            -0.1217812299729, -0.1217812299729, -0.181853771209703,
            -0.181853771209703, -0.241386890411402, -0.241386890411402,
            -0.3003895282746, -0.3003895282746, -0.358882546424901,
            -0.358882546424901, -0.4168510437012, -0.4168510437012,
            -0.474315881729201, -0.474315881729201, -0.531265139579801,
            -0.531265139579801, -0.5877315998078, -0.5877315998078,
            -0.643709301948601, -0.643709301948601, -0.6992042064667,
            -0.6992042064667, -0.754219293594399, -0.754219293594399,
            -0.808769464492801, -0.808769464492801, -0.8628606796265,
            -0.8628606796265, -0.916483998298702, -0.916483998298702,
            -0.969660282135003, -0.969660282135003, -1.0223835706711,
            -1.0223835706711, -1.0746657848358, -1.0746657848358,
            -1.1265188455582, -1.1265188455582, -1.1779397726059,
            -1.1779397726059, -1.2289375066757, -1.2289375066757,
            -1.2795120477677, -1.2795120477677, -1.3296782970429,
            -1.3296782970429, -1.3794332742691, -1.3794332742691,
            -1.4287739992142, -1.4287739992142, -1.4777272939682,
            -1.4777272939682, -1.5262752771378, -1.5262752771378,
            -1.5744417905808, -1.5744417905808, -1.5744417905808,
            -1.6222298145294, -1.6696184873581, -1.6696184873581,
            -1.7166495323181, -1.7166495323181, -1.7632991075516,
            -1.7632991075516, -1.8095850944519, -1.8095850944519,
            -1.8555045127869, -1.8555045127869, -1.9010722637177,
            -1.9010722637177, -1.9462764263153, -1.9462764263153,
            -1.9911348819733, -1.9911348819733, -2.0356476306916,
            -2.0356476306916, -2.0798236131668, -2.0798236131668,
            -2.1236568689347, -2.1236568689347, -2.1671563386917,
            -2.1671563386917, -2.2103130817414, -2.2103130817414,
            -2.2531598806381, -2.2531598806381, -2.2956669330597,
            -2.2956669330597, -2.2956669330597, -2.3378670215607,
            -2.3797512054444, -2.3797512054444, -2.4213135242462,
            -2.4213135242462, -2.4625778198242, -2.4625778198242,
            -2.5035381317139, -2.5035381317139, -2.5441855192185,
            -2.5441855192185, -2.5845408439636, -2.5845408439636,
            -2.624598145485, -2.624598145485, -2.6643633842469,
            -2.6643633842469, -2.7038455009461, -2.7038455009461,
            -2.7430355548859, -2.7430355548859, -2.781942486763,
            -2.781942486763, -2.8205648064614, -2.8205648064614,
            -2.858929336071, -2.858929336071, -2.858929336071,
            -2.8969958424568, -2.9348060488701, -2.9348060488701,
            -2.9723450541497, -2.9723450541497, -2.9723450541497,
            -3.0413046479225, -3.1658902764321, -3.1658902764321,
            -3.2896220684052, -3.2896220684052, -3.4124657511711,
            -3.4124657511711, -3.5344690084458, -3.5344690084458,
            -3.6556199193001, -3.6556199193001, -3.7759393453598,
            -3.7759393453598, -3.8954108953476, -3.8954108953476,
            -4.0140777826309, -4.0140777826309, -4.1319265961647,
            -4.1319265961647, -4.2489781975746, -4.2489781975746,
            -4.3652310967446, -4.3652310967446, -4.4806927442551,
            -4.4806927442551, -4.5953720808029, -4.5953720808029,
            -4.7092869877816, -4.7092869877816, -4.822438955307,
            -4.822438955307, -4.9348339438439, -4.9348339438439,
            -4.9348339438439, -5.0464734435082, -5.0464734435082,
            -5.157370865345, -5.2675396203995, -5.2675396203995,
            -5.3769871592522, -5.3769871592522, -5.3769871592522,
            -5.4856985807419, -5.5937111377716, -5.5937111377716,
            -5.7010143995285, -5.7010143995285, -5.8076098561287,
            -5.8076098561287, -5.9135258197785, -5.9135258197785,
            -6.0187488794327, -6.0187488794327, -6.0187488794327,
            -6.123298406601, -6.2271744012833, -6.2271744012833,
            -6.3303917646408, -6.3303917646408, -6.4329296350479,
            -6.4329296350479, -6.5348356962204, -6.5348356962204,
            -6.636081635952, -6.636081635952,
        };
    }
}
