namespace Prosim2GSX.Web.Contracts
{
    // Read-only Weight & Balance snapshot. Wire-shape mirror of WeightBalanceState
    // (which is the long-lived store written by WeightBalanceService each tick).
    // The React panel and the WPF Model both consume this DTO via the standard
    // GET /api/weightbalance route + WS "weightBalance" patches.
    public class WeightBalanceDto
    {
        public double ZfwKg { get; set; }
        public double MaczfwPercent { get; set; }
        public double GwKg { get; set; }
        public double MacgwPercent { get; set; }
        public double FuelPlannedKg { get; set; }
        public double FuelInTanksKg { get; set; }
        public double FuelCapacityKg { get; set; }

        public double CargoFwdLoadedKg { get; set; }
        public double CargoFwdCapacityKg { get; set; }
        public double CargoAftLoadedKg { get; set; }
        public double CargoAftCapacityKg { get; set; }
        public double CargoBulkCapacityKg { get; set; }
        public double CargoPlannedKg { get; set; }

        public int PassengersPlanned { get; set; }
        public int PassengersBoarded { get; set; }
        public int Zone1Capacity { get; set; }
        public int Zone2Capacity { get; set; }
        public int Zone3Capacity { get; set; }
        public int Zone4Capacity { get; set; }
        public string SeatOccupation { get; set; } = "";

        public bool FwdCargoDoorOpen { get; set; }
        public bool AftCargoDoorOpen { get; set; }

        public double MactowPercent { get; set; }
        public bool MacTowError { get; set; }

        // MACTOW envelope bounds. Sourced from LoadsheetState (10.5 / 45.0
        // for the A320) so the loadsheet validator and the W&B panel share
        // a single source of truth. Surfaced on the wire so the React panel
        // can render "VALID RANGE" without a second fetch.
        public double MinMacTow { get; set; }
        public double MaxMacTow { get; set; }

        // Aircraft envelope limits — A320 family hardcoded constants. Surfaced on
        // the wire so the React panel can render the chart axes without a second
        // config fetch and so a future variant change only needs touching one
        // place (WeightBalanceState).
        public double MtowLimitKg { get; set; }
        public double MlwLimitKg { get; set; }
        public double MzfwLimitKg { get; set; }

        public static WeightBalanceDto From(AppService app)
        {
            var s = app?.WeightBalance;
            if (s == null) return new WeightBalanceDto();

            return new WeightBalanceDto
            {
                ZfwKg = s.ZfwKg,
                MaczfwPercent = s.MaczfwPercent,
                GwKg = s.GwKg,
                MacgwPercent = s.MacgwPercent,
                FuelPlannedKg = s.FuelPlannedKg,
                FuelInTanksKg = s.FuelInTanksKg,
                FuelCapacityKg = s.FuelCapacityKg,
                CargoFwdLoadedKg = s.CargoFwdLoadedKg,
                CargoFwdCapacityKg = s.CargoFwdCapacityKg,
                CargoAftLoadedKg = s.CargoAftLoadedKg,
                CargoAftCapacityKg = s.CargoAftCapacityKg,
                CargoBulkCapacityKg = s.CargoBulkCapacityKg,
                CargoPlannedKg = s.CargoPlannedKg,
                PassengersPlanned = s.PassengersPlanned,
                PassengersBoarded = s.PassengersBoarded,
                Zone1Capacity = s.Zone1Capacity,
                Zone2Capacity = s.Zone2Capacity,
                Zone3Capacity = s.Zone3Capacity,
                Zone4Capacity = s.Zone4Capacity,
                SeatOccupation = s.SeatOccupation ?? "",
                FwdCargoDoorOpen = s.FwdCargoDoorOpen,
                AftCargoDoorOpen = s.AftCargoDoorOpen,
                MactowPercent = s.MactowPercent,
                MacTowError = s.MacTowError,
                MinMacTow = app?.Loadsheet?.MinMacTow ?? 0.0,
                MaxMacTow = app?.Loadsheet?.MaxMacTow ?? 0.0,
                MtowLimitKg = s.MtowLimitKg,
                MlwLimitKg = s.MlwLimitKg,
                MzfwLimitKg = s.MzfwLimitKg,
            };
        }
    }
}
