using Prosim2GSX.State;
using System;

namespace Prosim2GSX.Web.Contracts
{
    // Read-only loadsheet snapshot for a single slot (Prelim or Final).
    // Wire-shape mirror of the matching half of LoadsheetState. Both halves
    // ride together inside LoadsheetSnapshotDto for the snapshot endpoint
    // and the WS broadcast.
    public class LoadsheetDto
    {
        public string Type { get; set; } = "none";        // "none" | "prelim" | "final"
        public string Status { get; set; } = "pending";   // "pending" | "received" | "error"
        public double MacTow { get; set; }
        public bool MacTowError { get; set; }
        public double MinMacTow { get; set; }
        public double MaxMacTow { get; set; }
        public string LoadsheetIdent { get; set; } = "";
        public double TowKg { get; set; }
        public string RawJson { get; set; } = "";
        public DateTime? ReceivedAt { get; set; }

        public static LoadsheetDto FromPrelim(AppService app)
        {
            var s = app?.Loadsheet;
            if (s == null) return new LoadsheetDto();
            return new LoadsheetDto
            {
                Type = s.PrelimType ?? "none",
                Status = s.PrelimStatus ?? "pending",
                MacTow = s.PrelimMacTow,
                MacTowError = s.PrelimMacTowError,
                MinMacTow = s.MinMacTow,
                MaxMacTow = s.MaxMacTow,
                LoadsheetIdent = s.PrelimLoadsheetIdent ?? "",
                TowKg = s.PrelimTowKg,
                RawJson = s.PrelimRawJson ?? "",
                ReceivedAt = s.PrelimReceivedAt,
            };
        }

        public static LoadsheetDto FromFinal(AppService app)
        {
            var s = app?.Loadsheet;
            if (s == null) return new LoadsheetDto();
            return new LoadsheetDto
            {
                Type = s.FinalType ?? "none",
                Status = s.FinalStatus ?? "pending",
                MacTow = s.FinalMacTow,
                MacTowError = s.FinalMacTowError,
                MinMacTow = s.MinMacTow,
                MaxMacTow = s.MaxMacTow,
                LoadsheetIdent = s.FinalLoadsheetIdent ?? "",
                TowKg = s.FinalTowKg,
                RawJson = s.FinalRawJson ?? "",
                ReceivedAt = s.FinalReceivedAt,
            };
        }
    }

    // Combined snapshot — Prelim + Final ride together so both panel cards
    // refresh atomically. Used for both the WS "loadsheet" channel patch
    // and the future snapshot endpoint if added.
    public class LoadsheetSnapshotDto
    {
        public LoadsheetDto Prelim { get; set; } = new();
        public LoadsheetDto Final { get; set; } = new();

        public static LoadsheetSnapshotDto From(AppService app) => new()
        {
            Prelim = LoadsheetDto.FromPrelim(app),
            Final = LoadsheetDto.FromFinal(app),
        };
    }
}
