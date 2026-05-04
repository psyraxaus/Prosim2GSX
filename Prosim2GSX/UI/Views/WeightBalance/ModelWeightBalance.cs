using CFIT.AppFramework.UI.ViewModels;
using Prosim2GSX.State;
using System;
using System.ComponentModel;
using System.Windows;

namespace Prosim2GSX.UI.Views.WeightBalance
{
    // Notification adapter over WeightBalanceState. The store is the source of
    // truth (written each StateUpdateWorker tick by WeightBalanceService) and
    // this view-model is a pure projection — every public property reads
    // through to the store, and PropertyChanged is forwarded so XAML bindings
    // refresh without the view-model duplicating any state.
    //
    // The chart-coordinate properties (ZfwDotLeft/Top, GwDotLeft/Top) live
    // here so the XAML can bind Canvas.Left/Top directly without a converter
    // chain — the math fits cleanly with the rest of the projection.
    public partial class ModelWeightBalance : ViewModelBase<AppService>
    {
        protected virtual AppService AppService => Source;
        protected virtual WeightBalanceState State => AppService?.WeightBalance;

        // Chart canvas dimensions and axis ranges. Kept here (not on the
        // store) because they are pure presentation: the envelope chart is
        // a WPF rendering choice. The React panel uses its own SVG geometry.
        // Calibration adopted verbatim from the ProsimEFB W&B page so the
        // wandb.png envelope graphics align with our dot positions without
        // any scaling. The label positions in the EFB markup map to:
        //   MAC 21  → label left=57, line at x=62  (label centred on line)
        //   MAC 38  → label left=346, line at x=351
        //   75T     → label top=42.07, line at y=47
        //   35T     → label top=323, line at y=328
        // From which: 17 px / 1% MAC, 7.023 px / 1T.
        public const double ChartWidth = 380;
        public const double ChartHeight = 340;
        public const double PlotLeft = 62;
        public const double PlotRight = 351;
        public const double PlotTop = 47;
        public const double PlotBottom = 328;
        public const double AxisMacMin = 21;
        public const double AxisMacMax = 38;
        public const double AxisWeightMinT = 35;
        public const double AxisWeightMaxT = 75;
        public const double DotRadius = 11;
        // 380 logical px × the chart's LayoutTransform scale (1.25) — keeps
        // the gauge bars visually aligned with the rendered chart underneath
        // and ensures the chart fits inside the SectionCardBorder's content
        // area (510 col − 24 padding = 486 available; 380 × 1.25 = 475 fits).
        public const double GaugeBarWidth = 475;

        public ModelWeightBalance(AppService appService) : base(appService)
        {
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

            NotifyPropertyChanged(nameof(ZfwDotLeft));
            NotifyPropertyChanged(nameof(ZfwDotTop));
            NotifyPropertyChanged(nameof(GwDotLeft));
            NotifyPropertyChanged(nameof(GwDotTop));
            NotifyPropertyChanged(nameof(MtowLineTop));
            NotifyPropertyChanged(nameof(MlwLineTop));
            NotifyPropertyChanged(nameof(MzfwLineTop));
            NotifyPropertyChanged(nameof(ZfwBarFillWidth));
            NotifyPropertyChanged(nameof(GwBarFillWidth));
        }

        // Bottom-of-chart MAC% gauge bars. Width is the normalised MAC%
        // position scaled to the gauge bar's full width — bound directly to
        // a Rectangle.Width so no value converter is needed.
        public virtual double ZfwBarFillWidth => MacFraction(MaczfwPercent) * GaugeBarWidth;
        public virtual double GwBarFillWidth => MacFraction(MacgwPercent) * GaugeBarWidth;

        protected static double MacFraction(double mac)
        {
            if (double.IsNaN(mac) || mac <= 0) return 0;
            double clamped = Math.Clamp(mac, AxisMacMin, AxisMacMax);
            return (clamped - AxisMacMin) / (AxisMacMax - AxisMacMin);
        }

        // ── Raw values ──────────────────────────────────────────────────────
        public virtual double ZfwKg => State?.ZfwKg ?? 0;
        public virtual double MaczfwPercent => State?.MaczfwPercent ?? 0;
        public virtual double GwKg => State?.GwKg ?? 0;
        public virtual double MacgwPercent => State?.MacgwPercent ?? 0;
        public virtual double FuelPlannedKg => State?.FuelPlannedKg ?? 0;
        public virtual double FuelInTanksKg => State?.FuelInTanksKg ?? 0;
        public virtual double FuelCapacityKg => State?.FuelCapacityKg ?? 0;
        public virtual double CargoFwdLoadedKg => State?.CargoFwdLoadedKg ?? 0;
        public virtual double CargoFwdCapacityKg => State?.CargoFwdCapacityKg ?? 0;
        public virtual double CargoAftLoadedKg => State?.CargoAftLoadedKg ?? 0;
        public virtual double CargoAftCapacityKg => State?.CargoAftCapacityKg ?? 0;
        public virtual double CargoBulkCapacityKg => State?.CargoBulkCapacityKg ?? 0;
        public virtual double CargoPlannedKg => State?.CargoPlannedKg ?? 0;
        public virtual double CargoLoadedTotalKg => CargoFwdLoadedKg + CargoAftLoadedKg;
        public virtual int PassengersPlanned => State?.PassengersPlanned ?? 0;
        public virtual int PassengersBoarded => State?.PassengersBoarded ?? 0;
        public virtual int PassengersTotalCapacity =>
            (State?.Zone1Capacity ?? 0) + (State?.Zone2Capacity ?? 0)
            + (State?.Zone3Capacity ?? 0) + (State?.Zone4Capacity ?? 0);
        public virtual int Zone1Capacity => State?.Zone1Capacity ?? 0;
        public virtual int Zone2Capacity => State?.Zone2Capacity ?? 0;
        public virtual int Zone3Capacity => State?.Zone3Capacity ?? 0;
        public virtual int Zone4Capacity => State?.Zone4Capacity ?? 0;
        public virtual double MactowPercent => State?.MactowPercent ?? 0;

        // ── Chart geometry ──────────────────────────────────────────────────
        // Convert (MAC%, weight kg) to Canvas pixel coordinates. The dot
        // properties subtract DotRadius so the bound (Canvas.Left, Canvas.Top)
        // plant the dot's centre on the data point.
        public virtual double ZfwDotLeft => MacToX(MaczfwPercent) - DotRadius;
        public virtual double ZfwDotTop => WeightToY(ZfwKg / 1000.0) - DotRadius;
        public virtual double GwDotLeft => MacToX(MacgwPercent) - DotRadius;
        public virtual double GwDotTop => WeightToY(GwKg / 1000.0) - DotRadius;

        // Limit lines — Y positions for the dashed MTOW/MLW/MZFW horizontals.
        // Limits are A320 family constants on WeightBalanceState.
        public virtual double MtowLineTop => WeightToY((State?.MtowLimitKg ?? 73500) / 1000.0);
        public virtual double MlwLineTop => WeightToY((State?.MlwLimitKg ?? 64500) / 1000.0);
        public virtual double MzfwLineTop => WeightToY((State?.MzfwLimitKg ?? 61000) / 1000.0);

        protected static double MacToX(double macPercent)
        {
            if (double.IsNaN(macPercent) || macPercent <= 0) return PlotLeft;
            double clamped = Math.Clamp(macPercent, AxisMacMin, AxisMacMax);
            double frac = (clamped - AxisMacMin) / (AxisMacMax - AxisMacMin);
            return PlotLeft + frac * (PlotRight - PlotLeft);
        }

        protected static double WeightToY(double weightT)
        {
            if (double.IsNaN(weightT) || weightT <= 0) return PlotBottom;
            double clamped = Math.Clamp(weightT, AxisWeightMinT, AxisWeightMaxT);
            double frac = (AxisWeightMaxT - clamped) / (AxisWeightMaxT - AxisWeightMinT);
            return PlotTop + frac * (PlotBottom - PlotTop);
        }
    }
}
