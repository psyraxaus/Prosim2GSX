using Prosim2GSX.AppConfig;
using Prosim2GSX.Themes;
using ProsimInterface;

namespace Prosim2GSX.Web.Contracts
{
    // App Settings tab content. Scope is deliberately narrow: only the fields
    // currently bound on the WPF App Settings tab + the new web-server fields.
    // Operational/internal Config fields (binary names, conversion constants,
    // internal timer intervals, watchdog timeouts) stay server-side and are
    // NOT exposed on the wire — they're not user-tunable and shouldn't leak.
    //
    // Threading: ApplyTo writes to Config and calls SaveConfiguration. Phase 6
    // controllers must marshal onto the WPF dispatcher because Config raises
    // INPC events on the calling thread.
    public class AppSettingsDto
    {
        // Display unit
        public DisplayUnit DisplayUnitDefault { get; set; }
        public DisplayUnitSource DisplayUnitSource { get; set; }
        // Read-only echo of the currently effective unit (resolved by the app
        // based on DisplayUnitSource). Useful for the React UI's labels;
        // ApplyTo ignores this field.
        public DisplayUnit DisplayUnitCurrent { get; set; }

        // Fuel & weight (raw KG values — the React UI converts for display
        // using DisplayUnitCurrent, matching the wire-always-KG convention).
        public double ProsimWeightBag { get; set; }
        public double FuelResetDefaultKg { get; set; }
        public double FuelCompareVariance { get; set; }
        public bool FuelRoundUp100 { get; set; }

        // Audio cues
        public bool DingOnStartup { get; set; }
        public bool DingOnFinal { get; set; }

        // Cargo & doors
        public int CargoPercentChangePerSec { get; set; }
        public int DoorCargoDelay { get; set; }
        public int DoorCargoOpenDelay { get; set; }

        // GSX restart behaviour
        public bool ResetGsxStateVarsFlight { get; set; }
        public bool RestartGsxOnTaxiIn { get; set; }
        public bool RestartGsxStartupFail { get; set; }
        public int GsxMenuStartupMaxFail { get; set; }

        // Subsystem toggles
        public bool RunGsxService { get; set; }
        public bool RunAudioService { get; set; }
        public bool UseSayIntentions { get; set; }
        public bool OpenAppWindowOnStart { get; set; }

        // ProSim SDK
        public string ProSimSdkPath { get; set; } = "";

        // UI display
        public bool SolariAnimationEnabled { get; set; }
        public string CurrentTheme { get; set; } = "Light";

        // Web server (Phase 6 reads these to start/stop Kestrel and bind).
        // AuthToken is intentionally exposed both ways so the App Settings UI
        // can show the QR/copy-token panel; only "regenerate" actions in the
        // UI should write a NEW token via a dedicated endpoint, not via this
        // bulk POST. ApplyTo refuses to clobber a non-empty token with an
        // empty one (defensive — see ApplyTo).
        public bool WebServerEnabled { get; set; }
        public int WebServerPort { get; set; } = 5000;
        public bool WebServerBindAll { get; set; }
        public string WebServerAuthToken { get; set; } = "";

        public static AppSettingsDto From(AppService app)
        {
            var c = app.Config;
            if (c == null) return new AppSettingsDto();

            return new AppSettingsDto
            {
                DisplayUnitDefault = c.DisplayUnitDefault,
                DisplayUnitSource = c.DisplayUnitSource,
                DisplayUnitCurrent = c.DisplayUnitCurrent,

                ProsimWeightBag = c.ProsimWeightBag,
                FuelResetDefaultKg = c.FuelResetDefaultKg,
                FuelCompareVariance = c.FuelCompareVariance,
                FuelRoundUp100 = c.FuelRoundUp100,

                DingOnStartup = c.DingOnStartup,
                DingOnFinal = c.DingOnFinal,

                CargoPercentChangePerSec = c.CargoPercentChangePerSec,
                DoorCargoDelay = c.DoorCargoDelay,
                DoorCargoOpenDelay = c.DoorCargoOpenDelay,

                ResetGsxStateVarsFlight = c.ResetGsxStateVarsFlight,
                RestartGsxOnTaxiIn = c.RestartGsxOnTaxiIn,
                RestartGsxStartupFail = c.RestartGsxStartupFail,
                GsxMenuStartupMaxFail = c.GsxMenuStartupMaxFail,

                RunGsxService = c.RunGsxService,
                RunAudioService = c.RunAudioService,
                UseSayIntentions = c.UseSayIntentions,
                OpenAppWindowOnStart = c.OpenAppWindowOnStart,

                ProSimSdkPath = c.ProSimSdkPath ?? "",

                SolariAnimationEnabled = c.SolariAnimationEnabled,
                CurrentTheme = c.CurrentTheme ?? "Light",

                WebServerEnabled = c.WebServerEnabled,
                WebServerPort = c.WebServerPort,
                WebServerBindAll = c.WebServerBindAll,
                WebServerAuthToken = c.WebServerAuthToken ?? "",
            };
        }

        public void ApplyTo(AppService app)
        {
            var c = app.Config;
            if (c == null) return;

            // DisplayUnitCurrent is a derived runtime field — ignore inbound.

            c.DisplayUnitDefault = DisplayUnitDefault;
            c.DisplayUnitSource = DisplayUnitSource;

            c.ProsimWeightBag = ProsimWeightBag;
            c.FuelResetDefaultKg = FuelResetDefaultKg;
            c.FuelCompareVariance = FuelCompareVariance;
            c.FuelRoundUp100 = FuelRoundUp100;

            c.DingOnStartup = DingOnStartup;
            c.DingOnFinal = DingOnFinal;

            c.CargoPercentChangePerSec = CargoPercentChangePerSec;
            c.DoorCargoDelay = DoorCargoDelay;
            c.DoorCargoOpenDelay = DoorCargoOpenDelay;

            c.ResetGsxStateVarsFlight = ResetGsxStateVarsFlight;
            c.RestartGsxOnTaxiIn = RestartGsxOnTaxiIn;
            c.RestartGsxStartupFail = RestartGsxStartupFail;
            c.GsxMenuStartupMaxFail = GsxMenuStartupMaxFail;

            c.RunGsxService = RunGsxService;
            c.RunAudioService = RunAudioService;
            c.UseSayIntentions = UseSayIntentions;
            c.OpenAppWindowOnStart = OpenAppWindowOnStart;

            c.ProSimSdkPath = ProSimSdkPath ?? "";

            c.SolariAnimationEnabled = SolariAnimationEnabled;

            // Theme: persist the name AND tell the live WPF window to
            // restyle. Setting c.CurrentTheme alone only writes the value;
            // ThemeManager.ApplyTheme is what actually swaps the brushes
            // on the running window. ThemeManager handles same-name calls
            // gracefully so a no-op re-save is cheap.
            var requestedTheme = string.IsNullOrEmpty(CurrentTheme) ? "Light" : CurrentTheme;
            try
            {
                ThemeManager.Instance?.ApplyTheme(requestedTheme);
            }
            catch { }
            c.CurrentTheme = requestedTheme;

            // Capture old web-server values BEFORE writing so we can fire
            // INPC only on actual change (Config's auto-property setters
            // don't raise PropertyChanged themselves; without an explicit
            // raise, WebHostService never sees the toggle).
            var oldEnabled = c.WebServerEnabled;
            var oldPort = c.WebServerPort;
            var oldBindAll = c.WebServerBindAll;
            var oldToken = c.WebServerAuthToken ?? "";

            c.WebServerEnabled = WebServerEnabled;
            c.WebServerPort = WebServerPort;
            c.WebServerBindAll = WebServerBindAll;
            // Refuse to clear a populated token with an empty inbound value —
            // a misconfigured client could otherwise lock everyone out by
            // POSTing the settings form with the token field blank. The
            // dedicated regenerate endpoint (Phase 6) is the only legitimate
            // way to change the token.
            if (!string.IsNullOrEmpty(WebServerAuthToken))
                c.WebServerAuthToken = WebServerAuthToken;

            c.SaveConfiguration();

            // Raise PropertyChanged for the web-server fields only when they
            // actually changed — otherwise every Save would unnecessarily
            // restart Kestrel even if the user only edited an unrelated field.
            if (oldEnabled != WebServerEnabled)
                c.NotifyPropertyChanged(nameof(Config.WebServerEnabled));
            if (oldPort != WebServerPort)
                c.NotifyPropertyChanged(nameof(Config.WebServerPort));
            if (oldBindAll != WebServerBindAll)
                c.NotifyPropertyChanged(nameof(Config.WebServerBindAll));
            if (!string.IsNullOrEmpty(WebServerAuthToken) && WebServerAuthToken != oldToken)
                c.NotifyPropertyChanged(nameof(Config.WebServerAuthToken));

            // Trigger display-unit re-evaluation so the change takes effect
            // immediately (matches the WPF tab's DisplayUnitDefault setter).
            c.EvaluateDisplayUnit();
        }
    }
}
