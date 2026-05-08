using CFIT.AppFramework.UI.ViewModels;
using CommunityToolkit.Mvvm.Input;
using Prosim2GSX.Services;
using Prosim2GSX.State;
using Prosim2GSX.Web.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace Prosim2GSX.UI.Views.WeightBalance
{
    // Notification adapter over WeightBalanceState. The store is the source of
    // truth (written each StateUpdateWorker tick by WeightBalanceService) and
    // this view-model is a pure projection — every public property reads
    // through to the store, and PropertyChanged is forwarded so XAML bindings
    // refresh without the view-model duplicating any state.
    //
    // Chart geometry mirrors the proportional layout used by the web panel.
    // The PNG occupies a fixed sub-rectangle of the chart Canvas; every
    // annotation, tick, and label is positioned via the same XForMac() /
    // YForT() helpers and the same ratio constants, so the WPF and web
    // charts render identically over the same envelope graphic.
    public partial class ModelWeightBalance : ViewModelBase<AppService>
    {
        protected virtual AppService AppService => Source;
        protected virtual WeightBalanceState State => AppService?.WeightBalance;

        // ── Canvas + PNG sub-rectangle ───────────────────────────────────────
        public const double CanvasW    = 600;
        public const double CanvasH    = 580;
        public const double PngLeft    = 80;
        public const double PngTop     = 30;
        public const double PngWidth   = 500;
        public const double PngHeight  = 435;

        // ── Axis ranges baked into the PNG ───────────────────────────────────
        // Top numbers run [21..38]; the endpoint values 20 and 39 form the
        // chart frame and aren't labelled. Left numbers run [35..75] in 5T
        // steps; ticks fill in the 1T steps between (and continue to 77T
        // since the envelope extends slightly past the last numeric label).
        public const double TopNumMin     = 20;
        public const double TopNumMax     = 39;
        public const double LeftNumMinKg  = 35;
        public const double LeftNumMaxKg  = 78;
        public const int    LeftKgStep    = 5;

        // MAC% range used for clamping the live CG dots and the gauge-bar
        // fills. The PNG's actual envelope is narrower than the labelled
        // axis, but these are the operational limits we surface to the user.
        public const double AxisMacMin = 21;
        public const double AxisMacMax = 38;

        // ── Bottom-of-chart UI offsets (gauges + summary + note) ─────────────
        // All inside the Canvas so they scale with the chart through the
        // outer Viewbox. Gauges sit immediately below the PNG, the four-cell
        // summary table below them, and the live-data note last.
        public const double GaugeZfwTop   = 470;
        public const double GaugeGwTop    = 478;
        public const double GaugeWidth    = PngWidth;
        public const double GaugeHeight   = 5;
        public const double SummaryTop    = 495;
        public const double SummaryHeight = 50;
        public const double NoteTop       = 558;

        public const double DotRadius = 11;

        // ── Cargo-doors silhouette ──────────────────────────────────────────
        // Cleaned A320 outline extracted from the user-supplied
        // top-down SVG (OUTLINE group only — engineering callouts and the
        // white background discarded). Units match the original 750×750
        // viewBox; the XAML binds Path.Data via x:Static so the React panel
        // and the WPF view share the same coordinate space and door
        // positions stay aligned across both surfaces.
        public const string A320OutlinePathData =
            "M350.995,207.883 c-2.094,0.125 -4.208,0.38 -6.244,0.899 c-1.419,0.366 -2.825,0.867 -4.099,1.598 c1.468,0.843 3.094,1.402 4.738,1.788 " +
            "c2.336,0.541 4.76,0.749 7.153,0.791 l13.673,44.816 c-2.119,0.121 -4.259,0.374 -6.319,0.895 c-1.434,0.367 -2.854,0.871 -4.141,1.609 " +
            "c1.481,0.85 3.121,1.412 4.781,1.798 c2.361,0.542 4.812,0.746 7.23,0.783 l7.46,24.453 v6.993 c-1.439,0.069 -2.933,0.125 -4.346,0.416 " +
            "c-0.343,0.077 -0.688,0.175 -0.998,0.343 c-0.112,0.063 -0.298,0.166 -0.306,0.308 c-0.002,0.065 0.036,0.123 0.079,0.169 " +
            "c0.115,0.117 0.259,0.202 0.405,0.274 c0.251,0.119 0.525,0.19 0.795,0.25 c1.418,0.286 2.93,0.363 4.372,0.458 v8.424 " +
            "c-2.384,0.078 -4.797,0.301 -7.124,0.842 c-1.587,0.374 -3.162,0.903 -4.581,1.716 c3.535,2.004 7.7,2.526 11.705,2.584 v37.773 H262.931 " +
            "l-2.155,0.011 c-1.1,0.002 -3.162,0.061 -4.25,0.085 c-4.462,0.131 -9.729,0.464 -14.195,0.866 c-11.896,1.072 -24.171,3.173 -35.871,5.588 " +
            "c0,0 -14.253,3.021 -14.253,3.021 c-0.141,0.027 -0.304,0.066 -0.446,0.085 c-0.398,0.062 -0.805,0.096 -1.208,0.079 " +
            "c-0.983,-0.03 -1.953,-0.382 -2.705,-1.021 c-0.378,-0.315 -0.708,-0.691 -0.989,-1.094 c-0.255,-0.326 -40.05,-63.595 -40.325,-63.983 " +
            "c-0.242,-0.361 -0.533,-0.69 -0.864,-0.972 c-0.872,-0.749 -1.994,-1.149 -3.117,-1.33 c-0.485,-0.079 -0.986,-0.121 -1.477,-0.135 " +
            "c-0.112,0.003 -0.285,-0.01 -0.399,-0.006 c0,0 -12.757,0 -12.757,0 l16.957,75.321 c-5.748,0.971 -11.661,2.223 -17.28,3.785 " +
            "c-4.318,1.222 -8.688,2.652 -12.737,4.597 v4.484 c4.049,1.945 8.42,3.375 12.737,4.597 c5.618,1.561 11.532,2.814 17.28,3.785 " +
            "l-16.957,75.321 h12.757 c0.126,-0.009 0.275,0.004 0.399,-0.006 c0.531,-0.016 1.069,-0.062 1.592,-0.154 " +
            "c1.083,-0.189 2.16,-0.587 3.003,-1.311 c0.33,-0.282 0.622,-0.611 0.863,-0.972 c0.335,-0.493 40.007,-63.535 40.325,-63.984 " +
            "c0.281,-0.403 0.611,-0.78 0.989,-1.094 c0.764,-0.649 1.756,-1.002 2.755,-1.022 c0.423,-0.014 0.851,0.026 1.268,0.099 " +
            "c0.097,0.023 0.239,0.041 0.336,0.067 c0,0 14.253,3.021 14.253,3.021 c11.698,2.418 23.974,4.516 35.871,5.588 " +
            "c5.213,0.475 11.082,0.817 16.31,0.919 c1.034,0.029 3.246,0.037 4.29,0.043 c0,0 112.297,0 112.297,0 v37.773 " +
            "c-4.005,0.058 -8.17,0.579 -11.705,2.584 c1.419,0.813 2.994,1.342 4.581,1.716 c2.327,0.541 4.74,0.764 7.124,0.842 v8.424 " +
            "c-1.443,0.095 -2.953,0.172 -4.372,0.458 c-0.28,0.062 -0.563,0.135 -0.821,0.263 c-0.138,0.072 -0.276,0.154 -0.384,0.267 " +
            "c-0.042,0.046 -0.078,0.104 -0.074,0.169 c0.013,0.141 0.195,0.24 0.306,0.303 c0.311,0.168 0.656,0.266 0.998,0.343 " +
            "c1.413,0.29 2.908,0.347 4.346,0.416 v6.993 l-7.46,24.453 c-2.419,0.037 -4.869,0.241 -7.23,0.783 c-1.66,0.386 -3.3,0.947 -4.781,1.798 " +
            "c1.287,0.738 2.707,1.242 4.141,1.609 c2.06,0.521 4.2,0.774 6.319,0.895 l-13.673,44.816 c-2.393,0.043 -4.818,0.25 -7.153,0.791 " +
            "c-1.644,0.385 -3.27,0.945 -4.738,1.788 c1.274,0.731 2.68,1.232 4.099,1.598 c2.035,0.519 4.15,0.774 6.244,0.899 l-20.316,66.594 " +
            "c-2.083,0.127 -4.269,0.242 -6.315,0.659 c-0.19,0.048 -0.414,0.092 -0.576,0.202 c-0.031,0.023 -0.059,0.054 -0.065,0.093 " +
            "c-0.004,0.023 0,0.047 0.007,0.069 c0.006,0.019 0.014,0.038 0.026,0.054 c0.04,0.052 0.103,0.077 0.163,0.099 " +
            "c0.32,0.099 0.676,0.115 1.009,0.145 c2.321,0.132 4.786,0.089 7.111,0.026 c4.108,-0.154 8.227,-0.435 12.15,-1.761 " +
            "c2.766,-0.914 5.4,-2.301 7.639,-4.173 c2.232,-1.852 4.063,-4.175 5.443,-6.721 l69.92,-135.423 c2.048,0.597 4.171,1.128 6.292,1.371 " +
            "c0.002,0.016 0.226,2.662 0.228,2.677 c0.006,0.048 0.014,0.098 0.028,0.145 c0.042,0.153 0.123,0.296 0.234,0.409 " +
            "c0.084,0.086 0.185,0.156 0.295,0.205 c0.04,0.017 0.082,0.033 0.124,0.044 c0.013,0.005 0.035,0.009 0.048,0.012 " +
            "c0,0 0.395,0.088 0.395,0.088 c8.169,1.692 16.665,1.798 24.978,1.66 c5.601,-0.16 11.426,-0.343 16.871,-1.746 " +
            "c0.365,-0.104 0.697,-0.321 0.937,-0.614 c0.255,-0.309 0.408,-0.702 0.428,-1.102 c0.117,-7.132 -0.011,-15.629 -0.252,-22.764 " +
            "c-0.019,-0.227 -0.08,-0.451 -0.179,-0.657 c-0.201,-0.422 -0.566,-0.763 -1,-0.935 c-0.09,-0.035 -0.185,-0.066 -0.28,-0.087 " +
            "c-1.009,-0.219 -2.187,-0.415 -3.211,-0.562 c-5.21,-0.717 -10.751,-0.622 -16.007,-0.568 c-6.402,0.149 -12.931,0.538 -19.235,1.709 " +
            "l18.319,-35.481 l16.758,-3.646 c7.797,-0.043 78.331,0.104 83.883,-0.11 c6.3,-0.155 13.327,-0.42 19.581,-1 " +
            "c6.726,-0.611 13.529,-1.614 20.062,-3.35 c8.054,-2.172 16.05,-5.148 23.581,-8.736 c3.828,-1.883 7.775,-3.924 11.089,-6.632 " +
            "c1.493,-1.265 2.926,-2.749 3.6,-4.623 c0.313,-0.857 0.393,-1.781 0.38,-2.688 c0.014,-0.928 -0.071,-1.875 -0.401,-2.749 " +
            "c-0.685,-1.846 -2.101,-3.311 -3.578,-4.562 c-3.313,-2.709 -7.261,-4.75 -11.089,-6.633 c-7.531,-3.588 -15.527,-6.564 -23.581,-8.736 " +
            "c-6.532,-1.735 -13.337,-2.739 -20.062,-3.35 c-6.248,-0.579 -13.297,-0.85 -19.581,-1 c-5.457,-0.195 -76.445,-0.084 -83.882,-0.11 l-16.758,-3.646 " +
            "l-18.319,-35.481 c6.303,1.171 12.833,1.561 19.235,1.709 c5.259,0.052 10.794,0.149 16.007,-0.568 " +
            "c1.022,-0.15 2.202,-0.343 3.211,-0.562 c0.105,-0.024 0.211,-0.058 0.31,-0.1 c0.432,-0.18 0.79,-0.526 0.984,-0.952 " +
            "c0.095,-0.207 0.153,-0.432 0.168,-0.66 c0.231,-7.139 0.373,-15.629 0.247,-22.764 c-0.026,-0.389 -0.177,-0.769 -0.426,-1.07 " +
            "c-0.241,-0.293 -0.572,-0.511 -0.937,-0.614 c-5.443,-1.403 -11.272,-1.587 -16.871,-1.746 c-8.314,-0.137 -16.809,-0.032 -24.978,1.66 " +
            "c0,0 -0.395,0.088 -0.395,0.088 l-0.016,0.004 c-0.03,0.008 -0.065,0.017 -0.094,0.028 c-0.045,0.017 -0.092,0.037 -0.135,0.061 " +
            "c-0.081,0.045 -0.157,0.102 -0.221,0.168 c-0.107,0.11 -0.187,0.246 -0.23,0.393 c-0.015,0.052 -0.027,0.107 -0.032,0.161 " +
            "c-0.004,0.014 -0.225,2.661 -0.228,2.677 c-2.12,0.243 -4.245,0.775 -6.292,1.371 l-69.92,-135.423 c-1.38,-2.546 -3.21,-4.87 -5.443,-6.721 " +
            "c-2.239,-1.871 -4.873,-3.258 -7.639,-4.173 c-3.923,-1.326 -8.042,-1.608 -12.15,-1.761 c-2.404,-0.061 -4.94,-0.117 -7.338,0.044 " +
            "c-0.263,0.027 -0.537,0.047 -0.79,0.13 c-0.062,0.023 -0.129,0.052 -0.164,0.111 c-0.01,0.018 -0.017,0.039 -0.022,0.059 " +
            "c-0.004,0.02 -0.004,0.04 0.001,0.06 c0.012,0.039 0.043,0.068 0.075,0.09 c0.185,0.114 0.42,0.157 0.63,0.208 " +
            "c2.025,0.401 4.189,0.52 6.249,0.644 L350.995,207.883 z";

        // Parsed once at class-load time. XAML binds Path.Data via x:Static
        // to *this* field, not the string above — Path.Data expects a
        // Geometry, and {x:Static} returns the raw field value without
        // running the GeometryConverter, so handing it the string would
        // throw a type mismatch on the setter. Parsing here also surfaces
        // any malformed path syntax at init rather than at first paint.
        public static readonly Geometry A320OutlineGeometry =
            Geometry.Parse(A320OutlinePathData);

        public List<ChartLabel> XAxisLabels { get; }
        public List<ChartLabel> YAxisLabels { get; }
        public List<ChartTick>  Ticks { get; }
        public List<ChartLabel> LimitAnnotations { get; }
        public List<ChartLabel> EnvelopeAnnotations { get; }

        // ── Seat overlay layout (source SVG coords, identical to web panel) ──
        // Cabin tube runs source x=216..552 (aft → forward), y=358..392 (port
        // → starboard) with a visual aisle gap at y=375. The -180° rotation
        // applied by the parent Canvas flips this so the displayed cabin
        // reads nose-LEFT — same orientation the cargo doors already use.
        // Zone breakdown is fixed by ProsimConstants.PaxZoneLimits {24, 30,
        // 36, 42}; six seats per row split 3+3 around the aisle.
        protected static readonly int[] SeatRowsPerZone = { 4, 5, 6, 7 };
        protected const double SeatRowPitch = 15;
        protected const double SeatRectW    = 11;
        protected const double SeatRectH    = 4;
        protected const double SeatXFwd     = 552;
        protected static readonly double[] SeatYByCol = { 358, 364, 370, 380, 386, 392 };

        // Seat overlay brushes. Filled seats use the same green as a closed
        // door for tonal consistency; empty seats use a darker neutral so the
        // cabin reads as "empty by default" rather than "broken / N/A".
        protected static readonly Brush SeatOccupiedBrush =
            new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
        protected static readonly Brush SeatEmptyBrush =
            new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A));
        protected static readonly Brush SeatStrokeBrush =
            new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A));

        public ModelWeightBalance(AppService appService) : base(appService)
        {
            XAxisLabels         = BuildXAxisLabels();
            YAxisLabels         = BuildYAxisLabels();
            Ticks               = BuildTicks();
            LimitAnnotations    = BuildLimitAnnotations();
            EnvelopeAnnotations = BuildEnvelopeAnnotations();
        }

        protected override void InitializeModel()
        {
            if (State != null)
                State.PropertyChanged += OnStatePropertyChanged;

            // Pull the cached manifest from the service so a tab close/reopen
            // (or a full WPF restart while the service kept its in-memory
            // cache) brings back the same names. Mirrors the React panel's
            // mount-time GET /api/passengers/manifest.
            var existing = AppService?.PassengerSimulationService?.GetManifest();
            if (existing != null && existing.TotalPassengers > 0)
            {
                Manifest = existing;
            }
        }

        protected virtual void OnStatePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Forward every store change as a refresh of all bound projections.
            // The set is small enough that property-by-property fan-out would
            // cost more in maintenance than the broadcast saves in CPU.
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
                dispatcher.BeginInvoke(new Action(NotifyAll));
            else
                NotifyAll();
        }

        protected virtual void NotifyAll()
        {
            NotifyPropertyChanged(nameof(ZfwKg));
            NotifyPropertyChanged(nameof(MaczfwPercent));
            NotifyPropertyChanged(nameof(GwKg));
            NotifyPropertyChanged(nameof(MacgwPercent));
            NotifyPropertyChanged(nameof(FuelPlannedKg));
            NotifyPropertyChanged(nameof(FuelInTanksKg));
            NotifyPropertyChanged(nameof(FuelCapacityKg));
            NotifyPropertyChanged(nameof(CargoFwdLoadedKg));
            NotifyPropertyChanged(nameof(CargoFwdCapacityKg));
            NotifyPropertyChanged(nameof(CargoAftLoadedKg));
            NotifyPropertyChanged(nameof(CargoAftCapacityKg));
            NotifyPropertyChanged(nameof(CargoBulkCapacityKg));
            NotifyPropertyChanged(nameof(CargoPlannedKg));
            NotifyPropertyChanged(nameof(CargoLoadedTotalKg));
            NotifyPropertyChanged(nameof(FwdCargoDoorOpen));
            NotifyPropertyChanged(nameof(AftCargoDoorOpen));
            NotifyPropertyChanged(nameof(BulkCargoDoorOpen));
            NotifyPropertyChanged(nameof(AllDoorsClosed));
            NotifyPropertyChanged(nameof(BulkFitted));
            NotifyPropertyChanged(nameof(BulkFittedVisibility));
            NotifyPropertyChanged(nameof(FwdDoorBrush));
            NotifyPropertyChanged(nameof(AftDoorBrush));
            NotifyPropertyChanged(nameof(BulkDoorBrush));
            NotifyPropertyChanged(nameof(FwdDoorStatusText));
            NotifyPropertyChanged(nameof(AftDoorStatusText));
            NotifyPropertyChanged(nameof(BulkDoorStatusText));
            NotifyPropertyChanged(nameof(Door1LBrush));
            NotifyPropertyChanged(nameof(Door1RBrush));
            NotifyPropertyChanged(nameof(Door2LBrush));
            NotifyPropertyChanged(nameof(Door2RBrush));
            NotifyPropertyChanged(nameof(Door3LBrush));
            NotifyPropertyChanged(nameof(Door3RBrush));
            NotifyPropertyChanged(nameof(Door4LBrush));
            NotifyPropertyChanged(nameof(Door4RBrush));
            NotifyPropertyChanged(nameof(SeatRects));
            NotifyPropertyChanged(nameof(ReadinessBannerText));
            NotifyPropertyChanged(nameof(ReadinessBannerBrush));
            NotifyPropertyChanged(nameof(PassengersPlanned));
            NotifyPropertyChanged(nameof(PassengersBoarded));
            NotifyPropertyChanged(nameof(PassengersTotalCapacity));
            NotifyPropertyChanged(nameof(Zone1Capacity));
            NotifyPropertyChanged(nameof(Zone2Capacity));
            NotifyPropertyChanged(nameof(Zone3Capacity));
            NotifyPropertyChanged(nameof(Zone4Capacity));
            NotifyPropertyChanged(nameof(MactowPercent));
            NotifyPropertyChanged(nameof(MacTowError));
            NotifyPropertyChanged(nameof(MacTowBrush));
            NotifyPropertyChanged(nameof(MacTowErrorRangeVisibility));
            NotifyPropertyChanged(nameof(MacTowRangeText));
            NotifyPropertyChanged(nameof(MacTowSource));
            NotifyPropertyChanged(nameof(MacTowSourceText));
            NotifyPropertyChanged(nameof(MacTowSourceBrush));
            NotifyPropertyChanged(nameof(MacTowSourceTooltip));
            NotifyPropertyChanged(nameof(FmsSyncStale));
            NotifyPropertyChanged(nameof(FmsSyncStaleVisibility));
            NotifyPropertyChanged(nameof(FmsSyncStaleText));
            NotifyPropertyChanged(nameof(SyncButtonText));
            NotifyPropertyChanged(nameof(IsSyncEnabled));
            SyncToFmsCommand?.NotifyCanExecuteChanged();

            NotifyPropertyChanged(nameof(ZfwDotLeft));
            NotifyPropertyChanged(nameof(ZfwDotTop));
            NotifyPropertyChanged(nameof(GwDotLeft));
            NotifyPropertyChanged(nameof(GwDotTop));
            NotifyPropertyChanged(nameof(ZfwDotVisibility));
            NotifyPropertyChanged(nameof(GwDotVisibility));
            NotifyPropertyChanged(nameof(ZfwBarFillWidth));
            NotifyPropertyChanged(nameof(GwBarFillWidth));
        }

        // ── Coordinate helpers (mirror the web panel exactly) ────────────────
        // X position for a MAC% value, anchored at the PNG's left edge.
        protected static double XForMac(double mac) =>
            PngLeft + PngWidth * (mac - TopNumMin) / (TopNumMax - TopNumMin);

        // Y position for a weight in tonnes, anchored at the PNG's top edge.
        // The −2 nudge matches the web panel — labels visually align with
        // the centre of their tick marks.
        protected static double YForT(double t) =>
            PngTop + PngHeight * (LeftNumMaxKg - t) / (LeftNumMaxKg - LeftNumMinKg) - 2;

        // Annotation position from a (xRatio, yRatio) pair anchored at the
        // PNG's top-left. Same ratio table the web panel uses, so labels
        // land in identical spots.
        protected static (double left, double top) AnnotPos(double xRatio, double yRatio) =>
            (PngLeft + xRatio * PngWidth, PngTop + yRatio * PngHeight);

        // ── Static collection builders ───────────────────────────────────────
        private static List<ChartLabel> BuildXAxisLabels()
        {
            var list = new List<ChartLabel>
            {
                // "%MAC" gutter label — top-left of the chart frame.
                new ChartLabel { CenterLeft = PngLeft - 10, CenterTop = PngTop - 10, Text = "%MAC" },
            };
            for (int m = (int)TopNumMin + 1; m < (int)TopNumMax; m++)
            {
                list.Add(new ChartLabel
                {
                    CenterLeft = XForMac(m),
                    CenterTop  = PngTop - 10,
                    Text       = m.ToString(CultureInfo.InvariantCulture),
                });
            }
            return list;
        }

        private static List<ChartLabel> BuildYAxisLabels()
        {
            var list = new List<ChartLabel>();
            for (double t = LeftNumMinKg; t <= 75; t += LeftKgStep)
            {
                list.Add(new ChartLabel
                {
                    CenterLeft = PngLeft - 8,
                    CenterTop  = YForT(t),
                    Text       = t.ToString("0", CultureInfo.InvariantCulture),
                });
            }
            return list;
        }

        private static List<ChartTick> BuildTicks()
        {
            var list = new List<ChartTick>();
            for (int t = (int)LeftNumMinKg; t < (int)LeftNumMaxKg; t++)
            {
                bool isLong = (t % 5) == 0;
                double w = isLong ? 10 : 5;
                list.Add(new ChartTick
                {
                    Left  = PngLeft - w,
                    Top   = YForT(t) - 0.5,  // 1px line — centre on yForT
                    Width = w,
                });
            }
            return list;
        }

        private static List<ChartLabel> BuildLimitAnnotations()
        {
            return new List<ChartLabel>
            {
                MakePillLabel(0.47, 0.125, "MTOW = 73,500KG"),
                MakePillLabel(0.43, 0.29,  "MLW = 64,500KG"),
                MakePillLabel(0.40, 0.37,  "MZFW = 61,000KG"),
            };
        }

        private static List<ChartLabel> BuildEnvelopeAnnotations()
        {
            // Casing matches the web panel for consistency.
            return new List<ChartLabel>
            {
                MakePillLabel(0.40, 0.56, "Operational Limits", scale: 0.8),
                MakePillLabel(0.69, 0.56, "Take-Off Limits",    angle: -43),
                MakePillLabel(0.70, 0.66, "Zfw limit",          angle: -60),
                MakePillLabel(0.16, 0.64, "Zfw limit",          angle: -85),
                MakePillLabel(0.12, 0.80, "Take-Off Limits",    angle: -110),
            };
        }

        private static ChartLabel MakePillLabel(double xRatio, double yRatio, string text,
                                                 double scale = 0.7, double angle = 0)
        {
            var (l, t) = AnnotPos(xRatio, yRatio);
            return new ChartLabel { CenterLeft = l, CenterTop = t, Text = text, Scale = scale, Angle = angle };
        }

        // ── Bottom-of-chart MAC% gauge bars ──────────────────────────────────
        // Width is the normalised MAC% position scaled to the gauge bar's
        // full width — bound directly to a Rectangle.Width so no value
        // converter is needed.
        public virtual double ZfwBarFillWidth => MacFraction(MaczfwPercent) * GaugeWidth;
        public virtual double GwBarFillWidth  => MacFraction(MacgwPercent)  * GaugeWidth;

        protected static double MacFraction(double mac)
        {
            if (double.IsNaN(mac) || mac <= 0) return 0;
            double clamped = Math.Clamp(mac, AxisMacMin, AxisMacMax);
            return (clamped - AxisMacMin) / (AxisMacMax - AxisMacMin);
        }

        // ── Raw values (forwarded from store) ────────────────────────────────
        public virtual double ZfwKg               => State?.ZfwKg ?? 0;
        public virtual double MaczfwPercent       => State?.MaczfwPercent ?? 0;
        public virtual double GwKg                => State?.GwKg ?? 0;
        public virtual double MacgwPercent        => State?.MacgwPercent ?? 0;
        public virtual double FuelPlannedKg       => State?.FuelPlannedKg ?? 0;
        public virtual double FuelInTanksKg       => State?.FuelInTanksKg ?? 0;
        public virtual double FuelCapacityKg      => State?.FuelCapacityKg ?? 0;
        public virtual double CargoFwdLoadedKg    => State?.CargoFwdLoadedKg ?? 0;
        public virtual double CargoFwdCapacityKg  => State?.CargoFwdCapacityKg ?? 0;
        public virtual double CargoAftLoadedKg    => State?.CargoAftLoadedKg ?? 0;
        public virtual double CargoAftCapacityKg  => State?.CargoAftCapacityKg ?? 0;
        public virtual double CargoBulkCapacityKg => State?.CargoBulkCapacityKg ?? 0;
        public virtual double CargoPlannedKg      => State?.CargoPlannedKg ?? 0;
        public virtual double CargoLoadedTotalKg  => CargoFwdLoadedKg + CargoAftLoadedKg;

        // ── Cargo door state + readiness ────────────────────────────────────
        // Door booleans forwarded from the store (polled each tick by
        // WeightBalanceService). BulkFitted gates the bulk indicator on
        // CargoBulkCapacityKg > 0 — there's no system.config flag for "bulk
        // hold present" in the SDK, so capacity is the reliable signal.
        public virtual bool FwdCargoDoorOpen  => State?.FwdCargoDoorOpen ?? false;
        public virtual bool AftCargoDoorOpen  => State?.AftCargoDoorOpen ?? false;
        public virtual bool BulkCargoDoorOpen => State?.BulkCargoDoorOpen ?? false;
        public virtual bool AllDoorsClosed    => State?.AllDoorsClosed ?? true;
        public virtual bool BulkFitted        => CargoBulkCapacityKg > 0;
        public virtual Visibility BulkFittedVisibility =>
            BulkFitted ? Visibility.Visible : Visibility.Collapsed;

        // Door colour: green when closed, amber when open. Bulk on a
        // non-fitted airframe is masked grey so the indicator still draws
        // (visibility is collapsed in the view, but the brush stays defined
        // for safety). Hex matches the React panel for visual parity.
        protected static readonly Brush DoorClosedBrush =
            new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));   // green
        protected static readonly Brush DoorOpenBrush =
            new SolidColorBrush(Color.FromRgb(0xF5, 0xA6, 0x23));   // amber
        protected static readonly Brush DoorNotFittedBrush =
            new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));   // grey

        public virtual Brush FwdDoorBrush => FwdCargoDoorOpen ? DoorOpenBrush : DoorClosedBrush;
        public virtual Brush AftDoorBrush => AftCargoDoorOpen ? DoorOpenBrush : DoorClosedBrush;
        public virtual Brush BulkDoorBrush =>
            !BulkFitted ? DoorNotFittedBrush : (BulkCargoDoorOpen ? DoorOpenBrush : DoorClosedBrush);

        public virtual string FwdDoorStatusText  => FwdCargoDoorOpen ? "OPEN" : "CLOSED";
        public virtual string AftDoorStatusText  => AftCargoDoorOpen ? "OPEN" : "CLOSED";
        public virtual string BulkDoorStatusText =>
            !BulkFitted ? "N/A" : (BulkCargoDoorOpen ? "OPEN" : "CLOSED");

        // Entry / overwing door brushes — green when closed, amber when open.
        // L1/R1 = forward pax, L2/R2 + L3/R3 = overwing exits, L4/R4 = aft
        // pax. Forwarded straight through from the W&B store (polled each
        // tick by WeightBalanceService).
        public virtual Brush Door1LBrush => (State?.Door1LOpen ?? false) ? DoorOpenBrush : DoorClosedBrush;
        public virtual Brush Door1RBrush => (State?.Door1ROpen ?? false) ? DoorOpenBrush : DoorClosedBrush;
        public virtual Brush Door2LBrush => (State?.Door2LOpen ?? false) ? DoorOpenBrush : DoorClosedBrush;
        public virtual Brush Door2RBrush => (State?.Door2ROpen ?? false) ? DoorOpenBrush : DoorClosedBrush;
        public virtual Brush Door3LBrush => (State?.Door3LOpen ?? false) ? DoorOpenBrush : DoorClosedBrush;
        public virtual Brush Door3RBrush => (State?.Door3ROpen ?? false) ? DoorOpenBrush : DoorClosedBrush;
        public virtual Brush Door4LBrush => (State?.Door4LOpen ?? false) ? DoorOpenBrush : DoorClosedBrush;
        public virtual Brush Door4RBrush => (State?.Door4ROpen ?? false) ? DoorOpenBrush : DoorClosedBrush;

        // Seat overlay — list of 132 SeatRect POCOs sized + positioned in
        // source coords, with Fill toggled per-seat off the comma-separated
        // seatOccupation string. Rebuilt on every state change (NotifyAll
        // refires SeatRects), so the ItemsControl in XAML rerenders cheaply.
        // 132 small allocations per tick is well within budget.
        public virtual List<SeatRect> SeatRects => BuildSeatRects(State?.SeatOccupation);

        protected static List<SeatRect> BuildSeatRects(string seatOccupation)
        {
            var occupied = ParseSeatOccupation(seatOccupation);
            var list = new List<SeatRect>(132);
            int rowOffset = 0;
            int seatIdx = 0;
            for (int zone = 0; zone < SeatRowsPerZone.Length; zone++)
            {
                int rowsInZone = SeatRowsPerZone[zone];
                for (int r = 0; r < rowsInZone; r++)
                {
                    int globalRow = rowOffset + r;
                    double x = SeatXFwd - globalRow * SeatRowPitch;
                    for (int col = 0; col < 6; col++)
                    {
                        bool isOccupied = seatIdx < occupied.Count && occupied[seatIdx];
                        list.Add(new SeatRect
                        {
                            Left   = x,
                            Top    = SeatYByCol[col] - SeatRectH / 2,
                            Width  = SeatRectW,
                            Height = SeatRectH,
                            Fill   = isOccupied ? SeatOccupiedBrush : SeatEmptyBrush,
                            Stroke = SeatStrokeBrush,
                        });
                        seatIdx++;
                    }
                }
                rowOffset += rowsInZone;
            }
            return list;
        }

        // Parse the comma-separated "true,false,..." seatOccupation string
        // into a bool list. Tolerates "1"/"0" and case-insensitive "true"
        // matching, mirroring WeightBalanceService.CountTrueChars but
        // preserving per-seat granularity.
        protected static List<bool> ParseSeatOccupation(string s)
        {
            var result = new List<bool>(132);
            if (string.IsNullOrEmpty(s)) return result;
            var parts = s.Split(',');
            foreach (var p in parts)
            {
                var v = p.Trim();
                result.Add(v.Equals("true", StringComparison.OrdinalIgnoreCase) || v == "1");
            }
            return result;
        }

        // Readiness banner mirrors !Aircraft.HasOpenDoors (covers entry +
        // cargo + bulk). Same predicate the GSX state machine uses to gate
        // CloseAllDoors() on final, so the banner reflects departure
        // reality — including pax doors that aren't drawn on the silhouette.
        public virtual string ReadinessBannerText =>
            AllDoorsClosed ? "ALL DOORS CLOSED" : "DOORS OPEN";

        public virtual Brush ReadinessBannerBrush =>
            AllDoorsClosed ? DoorClosedBrush : DoorOpenBrush;
        public virtual int    PassengersPlanned   => State?.PassengersPlanned ?? 0;
        public virtual int    PassengersBoarded   => State?.PassengersBoarded ?? 0;
        public virtual int    PassengersTotalCapacity =>
            (State?.Zone1Capacity ?? 0) + (State?.Zone2Capacity ?? 0)
            + (State?.Zone3Capacity ?? 0) + (State?.Zone4Capacity ?? 0);
        public virtual int    Zone1Capacity       => State?.Zone1Capacity ?? 0;
        public virtual int    Zone2Capacity       => State?.Zone2Capacity ?? 0;
        public virtual int    Zone3Capacity       => State?.Zone3Capacity ?? 0;
        public virtual int    Zone4Capacity       => State?.Zone4Capacity ?? 0;
        public virtual double MactowPercent       => State?.MactowPercent ?? 0;

        // ── Live dot positions ───────────────────────────────────────────────
        // Top-left of the 22×22 dot, so the bound (Canvas.Left, Canvas.Top)
        // plant the dot's centre on the data point.
        public virtual double ZfwDotLeft => MacToX(MaczfwPercent) - DotRadius;
        public virtual double ZfwDotTop  => WeightToY(ZfwKg / 1000.0) - DotRadius;
        public virtual double GwDotLeft  => MacToX(MacgwPercent) - DotRadius;
        public virtual double GwDotTop   => WeightToY(GwKg / 1000.0) - DotRadius;

        // Hide dots when the source values are unset — otherwise they'd
        // park in the bottom-left corner on first paint.
        public virtual Visibility ZfwDotVisibility =>
            (ZfwKg > 0 && MaczfwPercent > 0) ? Visibility.Visible : Visibility.Collapsed;
        public virtual Visibility GwDotVisibility =>
            (GwKg > 0 && MacgwPercent > 0) ? Visibility.Visible : Visibility.Collapsed;

        protected static double MacToX(double mac)
        {
            if (double.IsNaN(mac) || mac <= 0) return XForMac(AxisMacMin);
            double clamped = Math.Clamp(mac, AxisMacMin, AxisMacMax);
            return XForMac(clamped);
        }

        protected static double WeightToY(double t)
        {
            if (double.IsNaN(t) || t <= 0) return YForT(LeftNumMinKg);
            double clamped = Math.Clamp(t, LeftNumMinKg, 75);
            return YForT(clamped);
        }

        // ── MACTOW validation + FMS sync ────────────────────────────────────
        // MACTOW resolution and bounds checking are owned by
        // MactowValidationService; the view-model just projects what the
        // service exposes through the store and adds UI affordances on top.

        public virtual bool MacTowError => State?.MacTowError ?? false;

        // Green when MACTOW is in range (or unset — no error to show), red
        // when MacTowError is set. Hardcoded colours matching the React
        // panel's CSS to keep the two surfaces visually identical.
        public virtual Brush MacTowBrush => MacTowError
            ? new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35))   // red
            : new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));  // green

        // Bounds come from LoadsheetState (single source of truth — same
        // values the loadsheet parser uses to set PrelimMacTowError /
        // FinalMacTowError). Defaults of (0, 0) mean a degraded-mode read
        // shows "VALID RANGE: 0.0 – 0.0" rather than null-refing.
        public virtual double MinMacTow => AppService?.Loadsheet?.MinMacTow ?? 0.0;
        public virtual double MaxMacTow => AppService?.Loadsheet?.MaxMacTow ?? 0.0;

        public virtual string MacTowRangeText =>
            $"VALID RANGE: {MinMacTow:F1} – {MaxMacTow:F1}";

        public virtual Visibility MacTowErrorRangeVisibility =>
            MacTowError ? Visibility.Visible : Visibility.Collapsed;

        // ── Sync-to-FMS state machine ───────────────────────────────────────
        // Three transient UI signals layered on top of the persistent
        // MACTOW projection: IsSyncing (spinner + disable), FmsSyncFlash
        // ("idle" | "success" | "error" — drives a timed coloured flash on
        // the button background), and FmsSyncMessage (the text shown next
        // to the button after the attempt). Flash durations match the React
        // panel: 3 s on success, 5 s on failure.
        private readonly DispatcherTimer _flashTimer = new() { Interval = TimeSpan.FromSeconds(3) };
        private bool _isSyncing;
        private string _fmsSyncFlash = "idle";
        private string _fmsSyncMessage = "";

        public virtual bool IsSyncing
        {
            get => _isSyncing;
            protected set
            {
                if (_isSyncing == value) return;
                _isSyncing = value;
                NotifyPropertyChanged(nameof(IsSyncing));
                NotifyPropertyChanged(nameof(IsSyncEnabled));
                NotifyPropertyChanged(nameof(SyncButtonText));
                SyncToFmsCommand?.NotifyCanExecuteChanged();
            }
        }

        public virtual string FmsSyncFlash
        {
            get => _fmsSyncFlash;
            protected set
            {
                if (_fmsSyncFlash == value) return;
                _fmsSyncFlash = value;
                NotifyPropertyChanged(nameof(FmsSyncFlash));
                NotifyPropertyChanged(nameof(FmsSyncFlashBrush));
                NotifyPropertyChanged(nameof(FmsSyncMessageVisibility));
            }
        }

        public virtual string FmsSyncMessage
        {
            get => _fmsSyncMessage;
            protected set
            {
                if (_fmsSyncMessage == value) return;
                _fmsSyncMessage = value;
                NotifyPropertyChanged(nameof(FmsSyncMessage));
                NotifyPropertyChanged(nameof(FmsSyncMessageVisibility));
            }
        }

        public virtual Brush FmsSyncFlashBrush => FmsSyncFlash switch
        {
            "success" => new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)),  // green
            "error"   => new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35)),  // red
            _         => Brushes.Transparent,
        };

        public virtual Visibility FmsSyncMessageVisibility =>
            string.IsNullOrEmpty(FmsSyncMessage) ? Visibility.Collapsed : Visibility.Visible;

        public virtual bool IsSyncEnabled => !IsSyncing && !MacTowError;

        // Button text encodes both the busy state and the resolution
        // source so the user always knows what would be written. RESYNC
        // takes precedence when stale — it's the more important signal.
        public virtual string SyncButtonText
        {
            get
            {
                if (IsSyncing) return "SYNCING…";
                string verb = FmsSyncStale ? "RESYNC TO FMS" : "SYNC TO FMS";
                string suffix = MacTowSource switch
                {
                    "final" => " (FINAL)",
                    "prelim" => " (PRELIM)",
                    _ => " (COMPUTED)",
                };
                return verb + suffix;
            }
        }

        // ── Source chip ──────────────────────────────────────────────────────
        public virtual string MacTowSource => State?.MacTowSource ?? "computed";

        public virtual string MacTowSourceText => MacTowSource switch
        {
            "final" => "FINAL LS",
            "prelim" => "PRELIM LS",
            _ => "COMPUTED",
        };

        public virtual Brush MacTowSourceBrush => MacTowSource switch
        {
            "final" => new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)),  // green
            "prelim" => new SolidColorBrush(Color.FromRgb(0xF5, 0xA6, 0x23)), // amber
            _ => new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80)),        // grey
        };

        public virtual string MacTowSourceTooltip => MacTowSource switch
        {
            "final" => "MACTOW from final loadsheet (authoritative)",
            "prelim" => "MACTOW from preliminary loadsheet (will upgrade when final arrives)",
            _ => "No loadsheet received yet — value is live W&B computed mirror",
        };

        // ── Staleness indicator ──────────────────────────────────────────────
        public virtual bool FmsSyncStale => State?.FmsSyncStale ?? false;

        public virtual Visibility FmsSyncStaleVisibility =>
            FmsSyncStale ? Visibility.Visible : Visibility.Collapsed;

        public virtual string FmsSyncStaleText
        {
            get
            {
                var at = State?.FmsLastSyncedAt;
                var src = State?.FmsLastSyncedSource ?? "";
                if (at == null) return "FMS OUT OF DATE";
                var srcLabel = string.IsNullOrEmpty(src) ? "" : $" ({src.ToUpperInvariant()})";
                return $"FMS OUT OF DATE — last sync {at.Value.ToLocalTime():HH:mm:ss}{srcLabel}";
            }
        }

        // SDK writes happen on a background thread so the UI stays
        // responsive during the multi-write sequence. The service result
        // is consumed back on the dispatcher to update the flash state +
        // start the timer that clears it.
        [RelayCommand(CanExecute = nameof(IsSyncEnabled))]
        protected virtual async Task SyncToFmsAsync()
        {
            if (IsSyncing) return;

            var svc = AppService?.MactowValidationService;
            if (svc == null)
            {
                FmsSyncMessage = "MACTOW validation service unavailable";
                FmsSyncFlash = "error";
                StartFlashTimer(TimeSpan.FromSeconds(5));
                return;
            }

            IsSyncing = true;
            FmsSyncMessage = "";
            FmsSyncFlash = "idle";

            try
            {
                var result = await Task.Run(() => svc.SyncToFms());
                if (result?.Success == true)
                {
                    FmsSyncMessage = "FMS UPDATED";
                    FmsSyncFlash = "success";
                    StartFlashTimer(TimeSpan.FromSeconds(3));
                }
                else
                {
                    FmsSyncMessage = string.IsNullOrEmpty(result?.ErrorMessage)
                        ? "FMS sync failed"
                        : result.ErrorMessage;
                    FmsSyncFlash = "error";
                    StartFlashTimer(TimeSpan.FromSeconds(5));
                }
            }
            catch (Exception ex)
            {
                FmsSyncMessage = ex.Message ?? "FMS sync failed";
                FmsSyncFlash = "error";
                StartFlashTimer(TimeSpan.FromSeconds(5));
            }
            finally
            {
                IsSyncing = false;
            }
        }

        private void StartFlashTimer(TimeSpan duration)
        {
            _flashTimer.Stop();
            _flashTimer.Interval = duration;
            _flashTimer.Tick -= OnFlashTimerTick;
            _flashTimer.Tick += OnFlashTimerTick;
            _flashTimer.Start();
        }

        private void OnFlashTimerTick(object sender, EventArgs e)
        {
            _flashTimer.Stop();
            _flashTimer.Tick -= OnFlashTimerTick;
            FmsSyncFlash = "idle";
            FmsSyncMessage = "";
        }

        // ── Passenger simulation ────────────────────────────────────────────
        // Always-visible inline form on the WPF side: COUNT input + GENERATE
        // + CLEAR. The React panel keeps a SIMULATE/× toggle because flex
        // layout absorbs the horizontal width gracefully; the WPF column is
        // narrow enough that the toggle wasn't worth the extra states. SDK
        // write writes seatOccupation.string directly; the silhouette's seat
        // overlay updates through the W&B store tick. Manifest is cached
        // server-side so a tab close/reopen brings back the same name set.
        private string _simulateCount = "";
        private string _simulateFlash = "idle";
        private string _simulateMessage = "";
        private bool _isSimulating;
        private bool _isManifestOpen;
        private PassengerManifestDto _manifest;
        private readonly DispatcherTimer _simFlashTimer =
            new() { Interval = TimeSpan.FromSeconds(3) };

        // Two-way bound to the COUNT TextBox. Empty string means "fill to
        // capacity" — same convention as the web panel.
        public virtual string SimulateCount
        {
            get => _simulateCount;
            set
            {
                if (_simulateCount == value) return;
                _simulateCount = value ?? "";
                NotifyPropertyChanged(nameof(SimulateCount));
            }
        }

        public virtual bool IsSimulating
        {
            get => _isSimulating;
            protected set
            {
                if (_isSimulating == value) return;
                _isSimulating = value;
                NotifyPropertyChanged(nameof(IsSimulating));
                NotifyPropertyChanged(nameof(IsSimulateEnabled));
                NotifyPropertyChanged(nameof(GenerateButtonText));
                GenerateCommand?.NotifyCanExecuteChanged();
                ClearPaxCommand?.NotifyCanExecuteChanged();
            }
        }

        public virtual bool IsSimulateEnabled => !IsSimulating;

        public virtual string GenerateButtonText => IsSimulating ? "…" : "GENERATE";

        public virtual string SimulateFlash
        {
            get => _simulateFlash;
            protected set
            {
                if (_simulateFlash == value) return;
                _simulateFlash = value;
                NotifyPropertyChanged(nameof(SimulateFlash));
                NotifyPropertyChanged(nameof(SimulateFlashBrush));
                NotifyPropertyChanged(nameof(SimulateMessageBrush));
            }
        }

        public virtual string SimulateMessage
        {
            get => _simulateMessage;
            protected set
            {
                if (_simulateMessage == value) return;
                _simulateMessage = value;
                NotifyPropertyChanged(nameof(SimulateMessage));
                NotifyPropertyChanged(nameof(SimulateMessageVisibility));
            }
        }

        public virtual Visibility SimulateMessageVisibility =>
            string.IsNullOrEmpty(SimulateMessage) ? Visibility.Collapsed : Visibility.Visible;

        // Background colour for the GENERATE button during the flash window.
        // Idle = transparent (button keeps its default chrome), success = green,
        // error = red. Hex matches the React simulate flash classes.
        public virtual Brush SimulateFlashBrush => SimulateFlash switch
        {
            "success" => new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)),
            "error"   => new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35)),
            _         => Brushes.Transparent,
        };

        // Foreground for the inline status message text.
        public virtual Brush SimulateMessageBrush => SimulateFlash switch
        {
            "success" => new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)),
            "error"   => new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35)),
            _         => new SolidColorBrush(Color.FromRgb(0xCF, 0xD5, 0xE6)),
        };

        // Manifest panel state.
        public virtual PassengerManifestDto Manifest
        {
            get => _manifest;
            protected set
            {
                if (ReferenceEquals(_manifest, value)) return;
                _manifest = value;
                NotifyPropertyChanged(nameof(Manifest));
                NotifyPropertyChanged(nameof(ManifestPassengers));
                NotifyPropertyChanged(nameof(ManifestSectionVisibility));
                NotifyPropertyChanged(nameof(ManifestToggleText));
                NotifyPropertyChanged(nameof(ManifestWarnVisibility));
            }
        }

        public virtual List<PassengerEntryDto> ManifestPassengers =>
            _manifest?.Passengers ?? new List<PassengerEntryDto>();

        public virtual bool IsManifestOpen
        {
            get => _isManifestOpen;
            protected set
            {
                if (_isManifestOpen == value) return;
                _isManifestOpen = value;
                NotifyPropertyChanged(nameof(IsManifestOpen));
                NotifyPropertyChanged(nameof(ManifestTableVisibility));
                NotifyPropertyChanged(nameof(ManifestToggleText));
            }
        }

        public virtual Visibility ManifestSectionVisibility =>
            (_manifest != null && _manifest.TotalPassengers > 0)
                ? Visibility.Visible : Visibility.Collapsed;
        public virtual Visibility ManifestTableVisibility =>
            IsManifestOpen ? Visibility.Visible : Visibility.Collapsed;
        public virtual Visibility ManifestWarnVisibility =>
            (_manifest != null && _manifest.TotalPassengers > 0 && !_manifest.SeatOccupationWritten)
                ? Visibility.Visible : Visibility.Collapsed;

        public virtual string ManifestToggleText
        {
            get
            {
                int total = _manifest?.TotalPassengers ?? 0;
                string arrow = IsManifestOpen ? "▾" : "▸";
                return $"{arrow} MANIFEST ({total})";
            }
        }

        // ── Commands ─────────────────────────────────────────────────────────

        [RelayCommand]
        protected virtual void ToggleManifest() => IsManifestOpen = !IsManifestOpen;

        // The SDK write is fast (single dataref) but we still hop to a Task
        // to keep the call shape consistent with the FMS sync flow and to
        // avoid blocking the dispatcher if the SDK ever stalls.
        [RelayCommand(CanExecute = nameof(IsSimulateEnabled))]
        protected virtual async Task GenerateAsync()
        {
            if (IsSimulating) return;

            int? parsedCount = null;
            var trimmed = SimulateCount?.Trim() ?? "";
            if (trimmed.Length > 0)
            {
                if (!int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) || n < 0)
                {
                    StartSimFlash("error", "Invalid count", TimeSpan.FromSeconds(4));
                    return;
                }
                parsedCount = n;
            }

            var svc = AppService?.PassengerSimulationService;
            if (svc == null)
            {
                StartSimFlash("error", "Passenger simulation service unavailable", TimeSpan.FromSeconds(5));
                return;
            }

            IsSimulating = true;
            SimulateMessage = "";
            SimulateFlash = "idle";
            try
            {
                var result = await Task.Run(() => svc.Simulate(parsedCount));
                if (result?.Success == true && result.Manifest != null)
                {
                    Manifest = result.Manifest;
                    IsManifestOpen = true;
                    StartSimFlash("success", $"Generated {result.Manifest.TotalPassengers} pax", TimeSpan.FromSeconds(3));
                }
                else
                {
                    StartSimFlash("error",
                        string.IsNullOrEmpty(result?.ErrorMessage) ? "Generation failed" : result.ErrorMessage,
                        TimeSpan.FromSeconds(5));
                }
            }
            catch (Exception ex)
            {
                StartSimFlash("error", ex.Message ?? "Generation failed", TimeSpan.FromSeconds(5));
            }
            finally
            {
                IsSimulating = false;
            }
        }

        [RelayCommand(CanExecute = nameof(IsSimulateEnabled))]
        protected virtual async Task ClearPaxAsync()
        {
            if (IsSimulating) return;

            var svc = AppService?.PassengerSimulationService;
            if (svc == null)
            {
                StartSimFlash("error", "Passenger simulation service unavailable", TimeSpan.FromSeconds(5));
                return;
            }

            IsSimulating = true;
            SimulateMessage = "";
            SimulateFlash = "idle";
            try
            {
                var result = await Task.Run(() => svc.Clear());
                if (result?.Success == true)
                {
                    Manifest = null;
                    StartSimFlash("success", "Cabin cleared", TimeSpan.FromSeconds(2));
                }
                else
                {
                    StartSimFlash("error",
                        string.IsNullOrEmpty(result?.ErrorMessage) ? "Clear failed" : result.ErrorMessage,
                        TimeSpan.FromSeconds(5));
                }
            }
            catch (Exception ex)
            {
                StartSimFlash("error", ex.Message ?? "Clear failed", TimeSpan.FromSeconds(5));
            }
            finally
            {
                IsSimulating = false;
            }
        }

        private void StartSimFlash(string flash, string message, TimeSpan duration)
        {
            SimulateFlash = flash;
            SimulateMessage = message;
            _simFlashTimer.Stop();
            _simFlashTimer.Interval = duration;
            _simFlashTimer.Tick -= OnSimFlashTick;
            _simFlashTimer.Tick += OnSimFlashTick;
            _simFlashTimer.Start();
        }

        private void OnSimFlashTick(object sender, EventArgs e)
        {
            _simFlashTimer.Stop();
            _simFlashTimer.Tick -= OnSimFlashTick;
            SimulateFlash = "idle";
            SimulateMessage = "";
        }
    }

    // POCO for axis labels and pill-bordered annotations. CenterLeft/
    // CenterTop is the position the text's visual centre should land on;
    // the XAML template uses a 0×0 wrapper Grid so HorizontalAlignment="Center"
    // + VerticalAlignment="Center" centres the un-transformed text on that
    // anchor, then RenderTransform scales/rotates around the same centre.
    public class ChartLabel
    {
        public double CenterLeft { get; set; }
        public double CenterTop  { get; set; }
        public string Text       { get; set; } = string.Empty;
        public double Scale      { get; set; } = 0.7;
        public double Angle      { get; set; } = 0;
    }

    // POCO for Y-axis tick marks. Left/Top is the absolute top-left of the
    // 1-pixel-tall rectangle (no centring trick — ticks are drawn directly).
    public class ChartTick
    {
        public double Left  { get; set; }
        public double Top   { get; set; }
        public double Width { get; set; }
    }

    // POCO for one seat in the cabin overlay. Left/Top is the absolute
    // top-left in source SVG coords; Fill flips between occupied / empty
    // each time the seatOccupation string changes. Stroke is shared so the
    // 132 rects read as a faint grid even when the cabin is empty.
    public class SeatRect
    {
        public double Left   { get; set; }
        public double Top    { get; set; }
        public double Width  { get; set; }
        public double Height { get; set; }
        public Brush  Fill   { get; set; } = Brushes.Transparent;
        public Brush  Stroke { get; set; } = Brushes.Transparent;
    }

    // Converter used inside the chart label DataTemplates: takes a width or
    // height and returns its negation halved. Bound to Canvas.Left/Top on
    // the inner element with ElementName=Self, the result is a -W/2 / -H/2
    // offset that centres the element on its wrapper Canvas's origin —
    // which is itself positioned at (CenterLeft, CenterTop). Net effect:
    // the visual centre of the un-transformed text/pill lands on the data
    // anchor, and RenderTransformOrigin="0.5,0.5" then scales/rotates
    // around that same point.
    public class NegativeHalfConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d) return -d / 2.0;
            return 0.0;
        }
        public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
            => throw new System.NotSupportedException();
    }
}
