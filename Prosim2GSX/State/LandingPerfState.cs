using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

namespace Prosim2GSX.State
{
    // Long-lived observable mirror of the LANDING performance tab. Owned by
    // AppService; mutated by LandingPerfService.
    //
    // Unlike the takeoff tab there's no FMS uplink target — Airbus FMGCs
    // don't accept a landing-distance write. The store therefore carries
    // only inputs + cached lookups + computed outputs + status.
    //
    // The store reuses RunwayOption / RunwayIntersectionOption from
    // TakeoffPerfState (both panels render the same options shape).
    public partial class LandingPerfState : ObservableObject
    {
        // -----------------------------------------------------------------
        //  Airport & runway inputs
        // -----------------------------------------------------------------

        // Pre-populated from aircraft.fms.destination on the first
        // SDK-connect after reset; user can override.
        [ObservableProperty] private string _Icao = "";
        [ObservableProperty] private string _RunwayId = "";

        // Runway condition / friction code, 1–6 (6=Dry … 1=Poor). Sent to
        // the gateway as `BreakAction` (note: misspelled on the wire — the
        // gateway's CalcLdrRequest binder reads "Break*", not "Brake*").
        [ObservableProperty] private int _RwySurfaceCode = 6;

        // -----------------------------------------------------------------
        //  Aircraft configuration inputs
        // -----------------------------------------------------------------

        // Landing weight in **tons** (66 t is the gateway's reference).
        // CalcLdrRequest.LdgW expects tons, not kg — easy mistake to make.
        [ObservableProperty] private double _LdgWeightTons;

        // Optional VAPP override. Null ⇒ defer to gateway default.
        [ObservableProperty] private int? _AircraftSpeedKt;

        // "LOW" | "MED" | "MAX"
        [ObservableProperty] private string _BrakeMode = "MED";

        // "idle" | "max" — only "idle" is special-cased by the gateway
        // (suppresses the reverse credit).
        [ObservableProperty] private string _RevMode = "max";

        // "auto" | "manual" — UI-side mode. Maps to "1"/"0" on the wire.
        // Also flips the crosswind class threshold (auto uses the surface-
        // indexed limitArray; manual is a flat 20-kt cap).
        [ObservableProperty] private string _AutolandMode = "manual";

        // "FULL" | "3"
        [ObservableProperty] private string _FlapConfig = "FULL";

        // "0" | "1" — autothrust engaged. Adds a 5-kt floor to the speed
        // correction inside the gateway.
        [ObservableProperty] private string _Athr = "1";

        // -----------------------------------------------------------------
        //  Weather inputs (floats — match the gateway DTO)
        // -----------------------------------------------------------------

        [ObservableProperty] private float _OatC;
        [ObservableProperty] private float _QnhHpa = 1013f;
        [ObservableProperty] private string _WindDir = "0"; // "VRB" or "DDD"
        [ObservableProperty] private float _WindKt;

        // -----------------------------------------------------------------
        //  Cached lookups
        // -----------------------------------------------------------------

        // Landing tab pulls runways without intersections — the EFB doesn't
        // offer intersection landings.
        [ObservableProperty] private List<RunwayOption> _Runways = new();
        [ObservableProperty] private string _MetarText = "";
        [ObservableProperty] private DateTime? _MetarFetchedAt;

        // -----------------------------------------------------------------
        //  Calculator output + derivations
        // -----------------------------------------------------------------

        [ObservableProperty] private bool _HasResult;

        // True ⇒ gateway returned ldr == 0 (no data for the lookup). UI
        // dashes everything.
        [ObservableProperty] private bool _IsNoData;

        // Defensive: gateway A320 path never emits -2, but if it ever did
        // (retreat-flap-config), surface it so the UI can warn the pilot.
        [ObservableProperty] private bool _RetreatFlap;

        // Landing distance required (m).
        [ObservableProperty] private int _LdrM;

        // 1.15 × LdrM (regulatory margin), rounded.
        [ObservableProperty] private int _Ldr15M;

        // Landing distance available (m), computed from runway lengthFt − dt.
        [ObservableProperty] private double _LdaM;

        // Signed head/cross wind components, kt. Service rounds to int for
        // display; the doubles preserve the raw value for the class check.
        [ObservableProperty] private double _HwKt;
        [ObservableProperty] private double _XwKt;

        // "red" / "normal" / "red-margin" colour codes computed server-side
        // so the WPF + React UIs render identically.
        [ObservableProperty] private string _HwClass = "normal";
        [ObservableProperty] private string _XwClass = "normal";
        [ObservableProperty] private string _VisualDistClass = "normal";

        // -----------------------------------------------------------------
        //  Status / lifecycle
        // -----------------------------------------------------------------

        [ObservableProperty] private bool _IsBusy;
        [ObservableProperty] private string _LastError = "";

        // -----------------------------------------------------------------
        //  Reset
        // -----------------------------------------------------------------

        public void Reset()
        {
            Icao = "";
            RunwayId = "";
            RwySurfaceCode = 6;
            LdgWeightTons = 0;
            AircraftSpeedKt = null;
            BrakeMode = "MED";
            RevMode = "max";
            AutolandMode = "manual";
            FlapConfig = "FULL";
            Athr = "1";
            OatC = 0f;
            QnhHpa = 1013f;
            WindDir = "0";
            WindKt = 0f;
            Runways = new List<RunwayOption>();
            MetarText = "";
            MetarFetchedAt = null;
            HasResult = false;
            IsNoData = false;
            RetreatFlap = false;
            LdrM = 0;
            Ldr15M = 0;
            LdaM = 0;
            HwKt = 0;
            XwKt = 0;
            HwClass = "normal";
            XwClass = "normal";
            VisualDistClass = "normal";
            IsBusy = false;
            LastError = "";
        }
    }
}
