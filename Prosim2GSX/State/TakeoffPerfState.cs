using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

namespace Prosim2GSX.State
{
    // Long-lived observable mirror of the TAKEOFF performance tab. Owned by
    // AppService for the app's lifetime; mutated by TakeoffPerfService in
    // response to user input (REST), runway/METAR loads, the explicit
    // Calculate button, and the FMS uplink action.
    //
    // Inputs persist across a web-client reconnect (other tabs flow through
    // the same store and would otherwise lose typed values) but are cleared
    // on the engines-off-on-ground rising edge (D6) so the next flight
    // cycle starts clean. The shutdown reset is driven from
    // TakeoffPerfService — same pattern as LoadsheetService.
    public partial class TakeoffPerfState : ObservableObject
    {
        // -----------------------------------------------------------------
        //  Airport & runway inputs
        // -----------------------------------------------------------------

        // Pre-populated from aircraft.fms.origin on the first SDK-connect
        // after a reset; user can override.
        [ObservableProperty] private string _Icao = "";
        [ObservableProperty] private string _RunwayId = "";
        [ObservableProperty] private string _IntersectionName = ""; // empty = full-length

        // "DRY" | "WET" — matches the gateway's `Surf` enum.
        [ObservableProperty] private string _Surface = "DRY";

        // -----------------------------------------------------------------
        //  Aircraft configuration inputs
        // -----------------------------------------------------------------

        // FlapVal stays LOWERCASE on the wire ("opt" | "1+F" | "2" | "3").
        // The gateway uppercases internally; we mirror the EFB's UI behaviour.
        [ObservableProperty] private string _Flap = "opt";

        // "OFF" | "ENG" | "ENG+WING"
        [ObservableProperty] private string _AntiIce = "OFF";

        // "OFF" | "ON"
        [ObservableProperty] private string _Packs = "ON";

        // Force TOGA selector. Maps to `TogaVal` "YES"/"NO" on the wire; the
        // calculator may still force TOGA via ForceTogaResult based on flex
        // bounds.
        [ObservableProperty] private bool _ForceToga;

        // -----------------------------------------------------------------
        //  Weights / environment inputs
        // -----------------------------------------------------------------

        // Stored in kg internally; UI shows tons. The "Sync Loadsheet"
        // button writes the loadsheet's TOW + MAC into these.
        [ObservableProperty] private double _TowKg;
        [ObservableProperty] private double _MactowPercent;

        [ObservableProperty] private int _OatC;
        [ObservableProperty] private int _QnhHpa = 1013;

        // Wind: "VRB" or numeric DDD as a 3-char string, plus magnitude kt.
        [ObservableProperty] private string _WindDir = "0";
        [ObservableProperty] private int _WindKt;

        // -----------------------------------------------------------------
        //  Resolved environment (read from datarefs, not user-editable)
        // -----------------------------------------------------------------

        // Resolved from system.config.Config.EPR. Falls back to "CFM" with a
        // log warning if the profile dataref isn't readable.
        [ObservableProperty] private string _EngineVariant = "CFM";

        // -----------------------------------------------------------------
        //  Cached lookups
        // -----------------------------------------------------------------

        [ObservableProperty] private List<RunwayOption> _Runways = new();
        [ObservableProperty] private string _MetarText = "";
        [ObservableProperty] private DateTime? _MetarFetchedAt;

        // -----------------------------------------------------------------
        //  Calculator result
        // -----------------------------------------------------------------

        [ObservableProperty] private bool _HasResult;

        // V1/VR/V2 rounded to int for display + dataref write.
        [ObservableProperty] private int _V1;
        [ObservableProperty] private int _VR;
        [ObservableProperty] private int _V2;

        // 1 ⇒ CONF 1+F, 2 ⇒ CONF 2, 3 ⇒ CONF 3. Maps directly to the
        // aircraft.fms.perf.takeOff.flaps dataref.
        [ObservableProperty] private int _FlapSettings;

        // 0 ⇒ TOGA forced. Otherwise °C.
        [ObservableProperty] private int _FlexOutputC;

        // Reconstructed signed THS: positive = nose-up, negative = nose-down.
        // Service composes this from (trimDir, trimOutput) before writing.
        [ObservableProperty] private double _ThsValue;

        // Display-only mirror of TrimDir ("UP" | "DN" | "").
        [ObservableProperty] private string _TrimDir = "";

        // Takeoff performance limit (TOPL) in kg. ToplLimited true when
        // current TOW > TOPL — drives the "***TOPL LIMITED***" banner.
        [ObservableProperty] private double _ToplKg;
        [ObservableProperty] private bool _ToplLimited;

        // Calculator-asserted TOGA (e.g. flex below CanFlexBeUsed bound).
        // Distinct from user-checkbox ForceToga so the banner reads
        // "***TOGA REQUIRED***" vs the user-selected case.
        [ObservableProperty] private bool _ForceTogaResult;

        // Signed: positive = headwind, negative = tailwind.
        [ObservableProperty] private int _HwCompKt;

        // Green-dot speed (engine-out clean), shown on the MCDU display
        // panel. Nullable on the wire — calculator only emits it inside
        // the 40–78 t TOW band.
        [ObservableProperty] private int? _GreenDot;

        // Runway shift in metres for intersection departures. 0 for
        // full-length. Writes to aircraft.fms.perf.takeOff.shift.
        [ObservableProperty] private int _ShiftM;

        // Calculator-side error (e.g. "TOPL data not found"). Surfaced as
        // a banner; result fields stay at zero in that case.
        [ObservableProperty] private string _CalculationError = "";

        // -----------------------------------------------------------------
        //  Status / lifecycle
        // -----------------------------------------------------------------

        // True while a calc / runway-load / METAR fetch is in flight.
        [ObservableProperty] private bool _IsBusy;

        // Input-validation or transport error. Distinct from
        // CalculationError (which is a successful 200-body with a problem).
        [ObservableProperty] private string _LastError = "";

        // True after a successful Send Uplink. Cleared automatically on
        // any input change so a stale "Uplink Sent" badge doesn't outlive
        // the values it wrote.
        [ObservableProperty] private bool _IsUplinked;
        [ObservableProperty] private DateTime? _UplinkedAt;

        // -----------------------------------------------------------------
        //  Reset
        // -----------------------------------------------------------------

        // Engines-off-on-ground edge; called from TakeoffPerfService.
        public void Reset()
        {
            Icao = "";
            RunwayId = "";
            IntersectionName = "";
            Surface = "DRY";
            Flap = "opt";
            AntiIce = "OFF";
            Packs = "ON";
            ForceToga = false;
            TowKg = 0;
            MactowPercent = 0;
            OatC = 0;
            QnhHpa = 1013;
            WindDir = "0";
            WindKt = 0;
            Runways = new List<RunwayOption>();
            MetarText = "";
            MetarFetchedAt = null;
            HasResult = false;
            V1 = 0; VR = 0; V2 = 0;
            FlapSettings = 0;
            FlexOutputC = 0;
            ThsValue = 0;
            TrimDir = "";
            ToplKg = 0;
            ToplLimited = false;
            ForceTogaResult = false;
            HwCompKt = 0;
            GreenDot = null;
            ShiftM = 0;
            CalculationError = "";
            IsBusy = false;
            LastError = "";
            IsUplinked = false;
            UplinkedAt = null;
        }
    }

    // Light projection of the SDK's RunwayResponse — only the fields the
    // perf panels actually consume. Lives next to the state classes (not
    // in the SDK) because the SDK DTO is the wire shape; this is the
    // view-shape and is allowed to drift independently.
    public class RunwayOption
    {
        public string RunwayId { get; set; } = "";
        public int LengthFt { get; set; }
        public int DtFt { get; set; }                   // parsed from RunwayResponse.Dt (string in the wire DTO)
        public int Qdm { get; set; }                    // first two chars of RunwayId × 10
        public List<RunwayIntersectionOption> Intersections { get; set; } = new();
    }

    public class RunwayIntersectionOption
    {
        public string Name { get; set; } = "";
        public int ToraFt { get; set; }
    }
}
