using CFIT.AppFramework.UI.ViewModels;
using Prosim2GSX.State;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Prosim2GSX.UI.Views.Fuel
{
    // Notification adapter over FuelState. The store is the source of truth
    // (written each StateUpdateWorker tick by FuelService) and this
    // view-model is a pure projection — every public property reads through
    // to the store, and PropertyChanged is forwarded so XAML bindings refresh
    // without the view-model duplicating any state.
    //
    // Layout mirrors the React FuelPanel: capacity bar with colour banding,
    // PLANNED vs IN TANKS in KG and L, delta indicator with OVER/UNDER tag,
    // and three tank-breakdown bars (CENTRE / LEFT / RIGHT). Thresholds are
    // hardcoded as on the React side — server doesn't gate them, both panels
    // apply the same fixed cutoffs so the two surfaces render identically.
    public partial class ModelFuel : ViewModelBase<AppService>
    {
        protected virtual AppService AppService => Source;
        protected virtual FuelState State => AppService?.Fuel;

        // Capacity-bar colour bands. Wider tolerance than the delta indicator
        // because the bar shows physical fuel quantity (100 kg over plan is
        // fine to look "green"); the delta indicator is the operator-facing
        // alert. Mirrors the React panel constants exactly.
        public const double CapacityOverAmberKg = 200.0;
        public const double CapacityUnderRedKg  = 100.0;

        // Delta-indicator colour bands. Tighter — any deviation past the
        // spec's 100 kg threshold flips the indicator amber/red.
        public const double DeltaAmberKg = 100.0;
        public const double DeltaRedKg   = 100.0;

        // Brushes built once and reused for every state transition. Hardcoded
        // hex matches the React panel CSS so the two surfaces look identical.
        private static readonly Brush GreenBrush  = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
        private static readonly Brush AmberBrush  = new SolidColorBrush(Color.FromRgb(0xF5, 0xA6, 0x23));
        private static readonly Brush RedBrush    = new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35));
        private static readonly Brush NeutralBrush = new SolidColorBrush(Color.FromRgb(0x4F, 0x6F, 0x8F));

        public ModelFuel(AppService appService) : base(appService) { }

        protected override void InitializeModel()
        {
            if (State != null)
                State.PropertyChanged += OnStatePropertyChanged;
        }

        protected virtual void OnStatePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Forward every store change as a refresh of all bound projections.
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
                dispatcher.BeginInvoke(new Action(NotifyAll));
            else
                NotifyAll();
        }

        protected virtual void NotifyAll()
        {
            // Header / capacity
            NotifyPropertyChanged(nameof(CapacityHeaderText));
            NotifyPropertyChanged(nameof(CapacityRatioPercent));
            NotifyPropertyChanged(nameof(CapacityFillBrush));
            NotifyPropertyChanged(nameof(CapacityLabelText));

            // Summary grid
            NotifyPropertyChanged(nameof(PlannedKgText));
            NotifyPropertyChanged(nameof(InTanksKgText));
            NotifyPropertyChanged(nameof(PlannedLitresText));
            NotifyPropertyChanged(nameof(InTanksLitresText));

            // Delta row
            NotifyPropertyChanged(nameof(DeltaValueText));
            NotifyPropertyChanged(nameof(DeltaValueBrush));
            NotifyPropertyChanged(nameof(DeltaTagText));
            NotifyPropertyChanged(nameof(DeltaTagVisibility));

            // Tanks
            NotifyPropertyChanged(nameof(CentreFillPercent));
            NotifyPropertyChanged(nameof(CentreLabelText));
            NotifyPropertyChanged(nameof(LeftFillPercent));
            NotifyPropertyChanged(nameof(LeftLabelText));
            NotifyPropertyChanged(nameof(RightFillPercent));
            NotifyPropertyChanged(nameof(RightLabelText));
        }

        // ── Header text ─────────────────────────────────────────────────────
        public virtual string CapacityHeaderText
        {
            get
            {
                double cap = State?.FuelCapacityKg ?? 0;
                double sg = FuelState.SpecificGravity;
                return $"CAPACITY USABLE {FormatKg(cap)} KG — SG: {sg.ToString("F2", CultureInfo.InvariantCulture)}";
            }
        }

        // ── Capacity bar ────────────────────────────────────────────────────
        // ProgressBar.Value sits in a 0–100 range. Computed from in-tanks /
        // capacity, clamped — a refuel target above usable (simulator quirk)
        // shouldn't blow the visual.
        public virtual double CapacityRatioPercent
        {
            get
            {
                double cap = State?.FuelCapacityKg ?? 0;
                if (cap <= 0) return 0;
                double r = (State?.FuelInTanksKg ?? 0) / cap;
                if (r < 0) r = 0;
                if (r > 1) r = 1;
                return r * 100.0;
            }
        }

        // Colour-banded fill. Neutral when no plan loaded so the panel
        // doesn't mislead by going red/amber on a deliberately empty
        // aircraft. Bands match the React panel.
        public virtual Brush CapacityFillBrush
        {
            get
            {
                double planned = State?.PlannedRampKg ?? 0;
                if (planned <= 0) return NeutralBrush;
                double delta = State?.FuelDeltaKg ?? 0;
                if (delta > CapacityOverAmberKg) return AmberBrush;
                if (delta < -CapacityUnderRedKg) return RedBrush;
                return GreenBrush;
            }
        }

        public virtual string CapacityLabelText
        {
            get
            {
                double now = State?.FuelInTanksKg ?? 0;
                double cap = State?.FuelCapacityKg ?? 0;
                return $"{FormatKg(now)} / {FormatKg(cap)} KG";
            }
        }

        // ── Summary grid ────────────────────────────────────────────────────
        public virtual string PlannedKgText     => FormatKg(State?.PlannedRampKg ?? 0);
        public virtual string InTanksKgText     => FormatKg(State?.FuelInTanksKg ?? 0);
        public virtual string PlannedLitresText => FormatKg(State?.PlannedRampLitres ?? 0);
        public virtual string InTanksLitresText => FormatKg(State?.FuelInTanksLitres ?? 0);

        // ── Delta row ───────────────────────────────────────────────────────
        public virtual string DeltaValueText
        {
            get
            {
                double planned = State?.PlannedRampKg ?? 0;
                if (planned <= 0) return "— (no flight plan)";
                double d = State?.FuelDeltaKg ?? 0;
                string sign = d >= 0 ? "+" : "";
                return $"{sign}{FormatKg(d)} KG";
            }
        }

        public virtual Brush DeltaValueBrush
        {
            get
            {
                double planned = State?.PlannedRampKg ?? 0;
                if (planned <= 0) return NeutralBrush;
                double d = State?.FuelDeltaKg ?? 0;
                if (d > DeltaAmberKg) return AmberBrush;
                if (d < -DeltaRedKg) return RedBrush;
                return GreenBrush;
            }
        }

        public virtual string DeltaTagText
        {
            get
            {
                if ((State?.PlannedRampKg ?? 0) <= 0) return "";
                if (State?.IsOverFuelled == true) return "OVER";
                if (State?.IsUnderFuelled == true) return "UNDER";
                return "OK";
            }
        }

        public virtual Visibility DeltaTagVisibility =>
            (State?.PlannedRampKg ?? 0) > 0 ? Visibility.Visible : Visibility.Collapsed;

        // ── Tank rows ───────────────────────────────────────────────────────
        public virtual double CentreFillPercent => TankPercent(State?.FuelCentreKg ?? 0, State?.FuelCentreCapacityKg ?? 0);
        public virtual string CentreLabelText   => TankLabel(State?.FuelCentreKg ?? 0, State?.FuelCentreCapacityKg ?? 0);
        public virtual double LeftFillPercent   => TankPercent(State?.FuelLeftKg ?? 0, State?.FuelLeftCapacityKg ?? 0);
        public virtual string LeftLabelText     => TankLabel(State?.FuelLeftKg ?? 0, State?.FuelLeftCapacityKg ?? 0);
        public virtual double RightFillPercent  => TankPercent(State?.FuelRightKg ?? 0, State?.FuelRightCapacityKg ?? 0);
        public virtual string RightLabelText    => TankLabel(State?.FuelRightKg ?? 0, State?.FuelRightCapacityKg ?? 0);

        // ── Helpers ─────────────────────────────────────────────────────────
        protected static double TankPercent(double kg, double cap)
        {
            if (cap <= 0) return 0;
            double r = kg / cap;
            if (r < 0) r = 0;
            if (r > 1) r = 1;
            return r * 100.0;
        }

        protected static string TankLabel(double kg, double cap) =>
            $"{FormatKg(kg)} / {FormatKg(cap)} KG";

        protected static string FormatKg(double kg) =>
            kg.ToString("N0", CultureInfo.InvariantCulture);
    }
}
