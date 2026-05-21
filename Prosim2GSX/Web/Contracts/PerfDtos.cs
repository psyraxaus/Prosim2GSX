using Prosim2GSX.State;
using System;
using System.Collections.Generic;

namespace Prosim2GSX.Web.Contracts
{
    // Wire shapes for the takeoff + landing performance tabs.
    //
    // GET /api/perf/{takeoff|landing}   → state snapshot DTOs (this file)
    // POST .../inputs                   → partial-update *InputsDto (all fields nullable)
    // POST .../load-runways?icao=…      → no body
    // POST .../calculate                → no body
    // POST .../uplink                   → no body
    // POST .../sync-loadsheet           → no body (takeoff only)
    // POST .../reset                    → no body
    //
    // camelCase + null-omission come from WebJsonOptions.Default — these
    // classes are PascalCase C# props that the serializer rewrites.

    // ---------------------------------------------------------------------
    //  TAKEOFF
    // ---------------------------------------------------------------------

    public class TakeoffPerfStateDto
    {
        // Inputs
        public string Icao { get; set; } = "";
        public string RunwayId { get; set; } = "";
        public string IntersectionName { get; set; } = "";
        public string Surface { get; set; } = "DRY";
        public string Flap { get; set; } = "opt";
        public string AntiIce { get; set; } = "OFF";
        public string Packs { get; set; } = "ON";
        public bool ForceToga { get; set; }
        public double TowKg { get; set; }
        public double MactowPercent { get; set; }
        public int OatC { get; set; }
        public int QnhHpa { get; set; }
        public string WindDir { get; set; } = "0";
        public int WindKt { get; set; }
        public string EngineVariant { get; set; } = "CFM";

        // Lookups
        public List<RunwayDto> Runways { get; set; } = new();
        public string MetarText { get; set; } = "";
        public DateTime? MetarFetchedAt { get; set; }

        // Result
        public bool HasResult { get; set; }
        public int V1 { get; set; }
        public int Vr { get; set; }            // serializes as "vr"
        public int V2 { get; set; }
        public int FlapSettings { get; set; }
        public int FlexOutputC { get; set; }
        public double ThsValue { get; set; }
        public string TrimDir { get; set; } = "";
        public double ToplKg { get; set; }
        public bool ToplLimited { get; set; }
        public bool ForceTogaResult { get; set; }
        public int HwCompKt { get; set; }
        public int? GreenDot { get; set; }
        public int ShiftM { get; set; }
        public string CalculationError { get; set; } = "";

        // Status
        public bool IsBusy { get; set; }
        public string LastError { get; set; } = "";
        public bool IsUplinked { get; set; }
        public DateTime? UplinkedAt { get; set; }

        public static TakeoffPerfStateDto From(AppService app)
        {
            var s = app?.TakeoffPerf;
            if (s == null) return new TakeoffPerfStateDto();
            return new TakeoffPerfStateDto
            {
                Icao = s.Icao ?? "",
                RunwayId = s.RunwayId ?? "",
                IntersectionName = s.IntersectionName ?? "",
                Surface = s.Surface ?? "DRY",
                Flap = s.Flap ?? "opt",
                AntiIce = s.AntiIce ?? "OFF",
                Packs = s.Packs ?? "ON",
                ForceToga = s.ForceToga,
                TowKg = s.TowKg,
                MactowPercent = s.MactowPercent,
                OatC = s.OatC,
                QnhHpa = s.QnhHpa,
                WindDir = s.WindDir ?? "0",
                WindKt = s.WindKt,
                EngineVariant = s.EngineVariant ?? "CFM",
                Runways = RunwayDto.ProjectAll(s.Runways),
                MetarText = s.MetarText ?? "",
                MetarFetchedAt = s.MetarFetchedAt,
                HasResult = s.HasResult,
                V1 = s.V1,
                Vr = s.VR,
                V2 = s.V2,
                FlapSettings = s.FlapSettings,
                FlexOutputC = s.FlexOutputC,
                ThsValue = s.ThsValue,
                TrimDir = s.TrimDir ?? "",
                ToplKg = s.ToplKg,
                ToplLimited = s.ToplLimited,
                ForceTogaResult = s.ForceTogaResult,
                HwCompKt = s.HwCompKt,
                GreenDot = s.GreenDot,
                ShiftM = s.ShiftM,
                CalculationError = s.CalculationError ?? "",
                IsBusy = s.IsBusy,
                LastError = s.LastError ?? "",
                IsUplinked = s.IsUplinked,
                UplinkedAt = s.UplinkedAt,
            };
        }
    }

    // Partial-update payload — every field nullable so a client can send
    // only the fields they want to mutate. ApplyTo records which fields
    // arrived so the controller can clear the stale-uplink badge whenever
    // any input changes.
    public class TakeoffInputsDto
    {
        public string? Icao { get; set; }
        public string? RunwayId { get; set; }
        public string? IntersectionName { get; set; }
        public string? Surface { get; set; }
        public string? Flap { get; set; }
        public string? AntiIce { get; set; }
        public string? Packs { get; set; }
        public bool? ForceToga { get; set; }
        public double? TowKg { get; set; }
        public double? MactowPercent { get; set; }
        public int? OatC { get; set; }
        public int? QnhHpa { get; set; }
        public string? WindDir { get; set; }
        public int? WindKt { get; set; }

        // Returns true if at least one field was applied — caller uses this
        // to drop the IsUplinked flag (stale-input invalidation).
        public bool ApplyTo(TakeoffPerfState s)
        {
            if (s == null) return false;
            bool any = false;
            if (Icao             != null) { s.Icao             = Icao.ToUpperInvariant(); any = true; }
            if (RunwayId         != null) { s.RunwayId         = RunwayId; any = true; }
            if (IntersectionName != null) { s.IntersectionName = IntersectionName; any = true; }
            if (Surface          != null) { s.Surface          = Surface; any = true; }
            if (Flap             != null) { s.Flap             = Flap; any = true; }
            if (AntiIce          != null) { s.AntiIce          = AntiIce; any = true; }
            if (Packs            != null) { s.Packs            = Packs; any = true; }
            if (ForceToga      .HasValue) { s.ForceToga        = ForceToga.Value; any = true; }
            if (TowKg          .HasValue) { s.TowKg            = TowKg.Value; any = true; }
            if (MactowPercent  .HasValue) { s.MactowPercent    = MactowPercent.Value; any = true; }
            if (OatC           .HasValue) { s.OatC             = OatC.Value; any = true; }
            if (QnhHpa         .HasValue) { s.QnhHpa           = QnhHpa.Value; any = true; }
            if (WindDir          != null) { s.WindDir          = WindDir; any = true; }
            if (WindKt         .HasValue) { s.WindKt           = WindKt.Value; any = true; }
            return any;
        }
    }

    // ---------------------------------------------------------------------
    //  LANDING
    // ---------------------------------------------------------------------

    public class LandingPerfStateDto
    {
        // Inputs
        public string Icao { get; set; } = "";
        public string RunwayId { get; set; } = "";
        public int RwySurfaceCode { get; set; } = 6;
        public double LdgWeightTons { get; set; }
        public int? AircraftSpeedKt { get; set; }
        public string BrakeMode { get; set; } = "MED";
        public string RevMode { get; set; } = "max";
        public string AutolandMode { get; set; } = "manual";
        public string FlapConfig { get; set; } = "FULL";
        public string Athr { get; set; } = "1";
        public float OatC { get; set; }
        public float QnhHpa { get; set; } = 1013f;
        public string WindDir { get; set; } = "0";
        public float WindKt { get; set; }

        // Lookups
        public List<RunwayDto> Runways { get; set; } = new();
        public string MetarText { get; set; } = "";
        public DateTime? MetarFetchedAt { get; set; }

        // Result + derivations
        public bool HasResult { get; set; }
        public bool IsNoData { get; set; }
        public bool RetreatFlap { get; set; }
        public int LdrM { get; set; }
        public int Ldr15M { get; set; }
        public double LdaM { get; set; }
        public double HwKt { get; set; }
        public double XwKt { get; set; }
        public string HwClass { get; set; } = "normal";
        public string XwClass { get; set; } = "normal";
        public string VisualDistClass { get; set; } = "normal";

        // Status
        public bool IsBusy { get; set; }
        public string LastError { get; set; } = "";

        public static LandingPerfStateDto From(AppService app)
        {
            var s = app?.LandingPerf;
            if (s == null) return new LandingPerfStateDto();
            return new LandingPerfStateDto
            {
                Icao = s.Icao ?? "",
                RunwayId = s.RunwayId ?? "",
                RwySurfaceCode = s.RwySurfaceCode,
                LdgWeightTons = s.LdgWeightTons,
                AircraftSpeedKt = s.AircraftSpeedKt,
                BrakeMode = s.BrakeMode ?? "MED",
                RevMode = s.RevMode ?? "max",
                AutolandMode = s.AutolandMode ?? "manual",
                FlapConfig = s.FlapConfig ?? "FULL",
                Athr = s.Athr ?? "1",
                OatC = s.OatC,
                QnhHpa = s.QnhHpa,
                WindDir = s.WindDir ?? "0",
                WindKt = s.WindKt,
                Runways = RunwayDto.ProjectAll(s.Runways),
                MetarText = s.MetarText ?? "",
                MetarFetchedAt = s.MetarFetchedAt,
                HasResult = s.HasResult,
                IsNoData = s.IsNoData,
                RetreatFlap = s.RetreatFlap,
                LdrM = s.LdrM,
                Ldr15M = s.Ldr15M,
                LdaM = s.LdaM,
                HwKt = s.HwKt,
                XwKt = s.XwKt,
                HwClass = s.HwClass ?? "normal",
                XwClass = s.XwClass ?? "normal",
                VisualDistClass = s.VisualDistClass ?? "normal",
                IsBusy = s.IsBusy,
                LastError = s.LastError ?? "",
            };
        }
    }

    public class LandingInputsDto
    {
        public string? Icao { get; set; }
        public string? RunwayId { get; set; }
        public int? RwySurfaceCode { get; set; }
        public double? LdgWeightTons { get; set; }
        public int? AircraftSpeedKt { get; set; }       // explicit null to unset is not supported via this DTO
        public string? BrakeMode { get; set; }
        public string? RevMode { get; set; }
        public string? AutolandMode { get; set; }
        public string? FlapConfig { get; set; }
        public string? Athr { get; set; }
        public float? OatC { get; set; }
        public float? QnhHpa { get; set; }
        public string? WindDir { get; set; }
        public float? WindKt { get; set; }

        public bool ApplyTo(LandingPerfState s)
        {
            if (s == null) return false;
            bool any = false;
            if (Icao             != null) { s.Icao             = Icao.ToUpperInvariant(); any = true; }
            if (RunwayId         != null) { s.RunwayId         = RunwayId; any = true; }
            if (RwySurfaceCode .HasValue) { s.RwySurfaceCode   = RwySurfaceCode.Value; any = true; }
            if (LdgWeightTons  .HasValue) { s.LdgWeightTons    = LdgWeightTons.Value; any = true; }
            if (AircraftSpeedKt.HasValue) { s.AircraftSpeedKt  = AircraftSpeedKt.Value; any = true; }
            if (BrakeMode        != null) { s.BrakeMode        = BrakeMode; any = true; }
            if (RevMode          != null) { s.RevMode          = RevMode; any = true; }
            if (AutolandMode     != null) { s.AutolandMode     = AutolandMode; any = true; }
            if (FlapConfig       != null) { s.FlapConfig       = FlapConfig; any = true; }
            if (Athr             != null) { s.Athr             = Athr; any = true; }
            if (OatC           .HasValue) { s.OatC             = OatC.Value; any = true; }
            if (QnhHpa         .HasValue) { s.QnhHpa           = QnhHpa.Value; any = true; }
            if (WindDir          != null) { s.WindDir          = WindDir; any = true; }
            if (WindKt         .HasValue) { s.WindKt           = WindKt.Value; any = true; }
            return any;
        }
    }

    // ---------------------------------------------------------------------
    //  RUNWAY / METAR
    // ---------------------------------------------------------------------

    public class RunwayDto
    {
        public string RunwayId { get; set; } = "";
        public int LengthFt { get; set; }
        public int DtFt { get; set; }
        public int Qdm { get; set; }
        public List<RunwayIntersectionDto> Intersections { get; set; } = new();

        public static RunwayDto From(RunwayOption r) => new()
        {
            RunwayId = r.RunwayId,
            LengthFt = r.LengthFt,
            DtFt = r.DtFt,
            Qdm = r.Qdm,
            Intersections = RunwayIntersectionDto.ProjectAll(r.Intersections),
        };

        public static List<RunwayDto> ProjectAll(IEnumerable<RunwayOption> source)
        {
            var result = new List<RunwayDto>();
            if (source == null) return result;
            foreach (var r in source) result.Add(From(r));
            return result;
        }
    }

    public class RunwayIntersectionDto
    {
        public string Name { get; set; } = "";
        public int ToraFt { get; set; }

        public static List<RunwayIntersectionDto> ProjectAll(IEnumerable<RunwayIntersectionOption> source)
        {
            var result = new List<RunwayIntersectionDto>();
            if (source == null) return result;
            foreach (var i in source)
                result.Add(new RunwayIntersectionDto { Name = i.Name, ToraFt = i.ToraFt });
            return result;
        }
    }
}
