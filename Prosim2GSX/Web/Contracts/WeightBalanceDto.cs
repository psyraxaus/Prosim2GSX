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
        public bool BulkCargoDoorOpen { get; set; }

        // Entry / overwing doors L1..R4 — drives the "Aircraft Status"
        // silhouette on the W&B tab. L1/R1 = forward pax, L2/R2 + L3/R3 =
        // overwing exits, L4/R4 = aft pax. Field naming mirrors
        // WeightBalanceState.Door{1..4}{L|R}Open and the
        // ProsimConstants.RefDoor{1..4}{L|R} dataref constants.
        public bool Door1LOpen { get; set; }
        public bool Door1ROpen { get; set; }
        public bool Door2LOpen { get; set; }
        public bool Door2ROpen { get; set; }
        public bool Door3LOpen { get; set; }
        public bool Door3ROpen { get; set; }
        public bool Door4LOpen { get; set; }
        public bool Door4ROpen { get; set; }

        // Mirror of !Aircraft.HasOpenDoors. Drives the W&B tab's
        // departure-readiness banner; covers all doors (entry + cargo).
        public bool AllDoorsClosed { get; set; } = true;

        public double MactowPercent { get; set; }
        public bool MacTowError { get; set; }

        // Resolution source for the displayed MACTOW: "final" | "prelim" |
        // "computed". Drives the chip next to the value and changes the
        // SYNC TO FMS button's label so the user knows which dataset
        // would actually be written.
        public string MacTowSource { get; set; } = "computed";

        // Loadsheet mirror — projected from the active slot (final → prelim)
        // so the W&B panel can render a "LOADSHEET" row beneath the LIVE
        // row. LoadsheetSource = "final" / "prelim" / "none" — "none" tells
        // the UI to grey the row and show dashes.
        public double LoadsheetZfwKg { get; set; }
        public double LoadsheetMaczfwPercent { get; set; }
        public double LoadsheetTowKg { get; set; }
        public double LoadsheetMactowPercent { get; set; }
        public string LoadsheetSource { get; set; } = "none";

        // FMS sync staleness signals. FmsSyncStale flips true when the
        // resolved values have drifted past operational thresholds OR the
        // loadsheet source has upgraded since the last sync. Last-synced
        // metadata is surfaced so the UI can show "last sync HH:MM:SS
        // (PRELIM)" alongside the RESYNC TO FMS button.
        public bool FmsSyncStale { get; set; }
        public System.DateTime? FmsLastSyncedAt { get; set; }
        public string FmsLastSyncedSource { get; set; } = "";

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
                BulkCargoDoorOpen = s.BulkCargoDoorOpen,
                Door1LOpen = s.Door1LOpen,
                Door1ROpen = s.Door1ROpen,
                Door2LOpen = s.Door2LOpen,
                Door2ROpen = s.Door2ROpen,
                Door3LOpen = s.Door3LOpen,
                Door3ROpen = s.Door3ROpen,
                Door4LOpen = s.Door4LOpen,
                Door4ROpen = s.Door4ROpen,
                AllDoorsClosed = s.AllDoorsClosed,
                MactowPercent = s.MactowPercent,
                MacTowError = s.MacTowError,
                MacTowSource = s.MacTowSource ?? "computed",
                LoadsheetZfwKg = s.LoadsheetZfwKg,
                LoadsheetMaczfwPercent = s.LoadsheetMaczfwPercent,
                LoadsheetTowKg = s.LoadsheetTowKg,
                LoadsheetMactowPercent = s.LoadsheetMactowPercent,
                LoadsheetSource = s.LoadsheetSource ?? "none",
                FmsSyncStale = s.FmsSyncStale,
                FmsLastSyncedAt = s.FmsLastSyncedAt,
                FmsLastSyncedSource = s.FmsLastSyncedSource ?? "",
                MinMacTow = app?.Loadsheet?.MinMacTow ?? 0.0,
                MaxMacTow = app?.Loadsheet?.MaxMacTow ?? 0.0,
                MtowLimitKg = s.MtowLimitKg,
                MlwLimitKg = s.MlwLimitKg,
                MzfwLimitKg = s.MzfwLimitKg,
            };
        }
    }
}
