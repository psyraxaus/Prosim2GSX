using CFIT.AppFramework.UI.ViewModels;
using CommunityToolkit.Mvvm.Input;
using Prosim2GSX.Services;
using Prosim2GSX.State;
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

        public List<ChartLabel> XAxisLabels { get; }
        public List<ChartLabel> YAxisLabels { get; }
        public List<ChartTick>  Ticks { get; }
        public List<ChartLabel> LimitAnnotations { get; }
        public List<ChartLabel> EnvelopeAnnotations { get; }

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
