using CFIT.AppFramework.UI.ViewModels;
using CFIT.AppLogger;
using Prosim2GSX.State;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Prosim2GSX.UI.Views.Loadsheet
{
    // Notification adapter over LoadsheetState. The store is the source of
    // truth (written each StateUpdateWorker tick by LoadsheetService) and
    // this view-model is a pure projection — every public property reads
    // through to the store, and PropertyChanged is forwarded so XAML
    // bindings refresh without the view-model duplicating any state.
    //
    // Layout mirrors the React LoadsheetPanel: two cards (PRELIM / FINAL)
    // sharing identical bindings under different prefixes, plus a header
    // strip with RESEND / RESET buttons. Status / MacTow / TOW / Ident /
    // Received and the MAC% range bar all have web parity.
    public partial class ModelLoadsheet : ViewModelBase<AppService>
    {
        protected virtual AppService AppService => Source;
        protected virtual LoadsheetState State => AppService?.Loadsheet;

        // Range-bar geometry. Width matches the visual track length; the
        // marker dot's horizontal position is computed in pixels so the
        // binding can hit Canvas.Left directly without a value converter.
        public const double RangeBarWidth = 220;
        public const double RangeMarkerDiameter = 12;

        public ModelLoadsheet(AppService appService) : base(appService) { }

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
            // Prelim
            NotifyPropertyChanged(nameof(PrelimStatusText));
            NotifyPropertyChanged(nameof(PrelimStatusBrush));
            NotifyPropertyChanged(nameof(PrelimDataVisibility));
            NotifyPropertyChanged(nameof(PrelimEmptyText));
            NotifyPropertyChanged(nameof(PrelimEmptyVisibility));
            NotifyPropertyChanged(nameof(PrelimMacTowText));
            NotifyPropertyChanged(nameof(PrelimMacTowBrush));
            NotifyPropertyChanged(nameof(PrelimTowText));
            NotifyPropertyChanged(nameof(PrelimIdentText));
            NotifyPropertyChanged(nameof(PrelimReceivedText));
            NotifyPropertyChanged(nameof(PrelimRangeMarkerLeft));
            NotifyPropertyChanged(nameof(PrelimRangeMarkerBrush));

            // Final
            NotifyPropertyChanged(nameof(FinalStatusText));
            NotifyPropertyChanged(nameof(FinalStatusBrush));
            NotifyPropertyChanged(nameof(FinalDataVisibility));
            NotifyPropertyChanged(nameof(FinalEmptyText));
            NotifyPropertyChanged(nameof(FinalEmptyVisibility));
            NotifyPropertyChanged(nameof(FinalMacTowText));
            NotifyPropertyChanged(nameof(FinalMacTowBrush));
            NotifyPropertyChanged(nameof(FinalTowText));
            NotifyPropertyChanged(nameof(FinalIdentText));
            NotifyPropertyChanged(nameof(FinalReceivedText));
            NotifyPropertyChanged(nameof(FinalRangeMarkerLeft));
            NotifyPropertyChanged(nameof(FinalRangeMarkerBrush));
            NotifyPropertyChanged(nameof(FinalCardBorderBrush));
            NotifyPropertyChanged(nameof(FinalCardBorderThickness));

            // Range labels (constant per envelope but re-broadcast cheaply).
            NotifyPropertyChanged(nameof(MinMacTowText));
            NotifyPropertyChanged(nameof(MaxMacTowText));
        }

        // ── Static range labels ──────────────────────────────────────────────
        public virtual string MinMacTowText => (State?.MinMacTow ?? 10.5).ToString("F1", CultureInfo.InvariantCulture);
        public virtual string MaxMacTowText => (State?.MaxMacTow ?? 45.0).ToString("F1", CultureInfo.InvariantCulture);

        // ── PRELIM bindings ──────────────────────────────────────────────────
        public virtual string PrelimStatusText => StatusText(State?.PrelimStatus);
        public virtual Brush PrelimStatusBrush => StatusBrush(State?.PrelimStatus);
        public virtual Visibility PrelimDataVisibility =>
            State?.PrelimStatus == "received" ? Visibility.Visible : Visibility.Collapsed;
        public virtual string PrelimEmptyText => EmptyText(State?.PrelimStatus);
        public virtual Visibility PrelimEmptyVisibility =>
            State?.PrelimStatus == "received" ? Visibility.Collapsed : Visibility.Visible;
        public virtual string PrelimMacTowText =>
            (State?.PrelimMacTow ?? 0).ToString("F1", CultureInfo.InvariantCulture) + " %";
        public virtual Brush PrelimMacTowBrush =>
            (State?.PrelimMacTowError ?? false) ? Brushes.Red : Brushes.White;
        public virtual string PrelimTowText =>
            ((State?.PrelimTowKg ?? 0) / 1000.0).ToString("F1", CultureInfo.InvariantCulture) + " t";
        public virtual string PrelimIdentText =>
            string.IsNullOrEmpty(State?.PrelimLoadsheetIdent) ? "—" : State.PrelimLoadsheetIdent;
        public virtual string PrelimReceivedText => FormatReceived(State?.PrelimReceivedAt);
        public virtual double PrelimRangeMarkerLeft => RangeMarkerLeft(State?.PrelimMacTow ?? 0);
        public virtual Brush PrelimRangeMarkerBrush =>
            (State?.PrelimMacTowError ?? false)
                ? new SolidColorBrush(Color.FromRgb(0xDC, 0x40, 0x40))
                : new SolidColorBrush(Color.FromRgb(0x2D, 0xBE, 0x4D));

        // ── FINAL bindings ───────────────────────────────────────────────────
        public virtual string FinalStatusText => StatusText(State?.FinalStatus);
        public virtual Brush FinalStatusBrush => StatusBrush(State?.FinalStatus);
        public virtual Visibility FinalDataVisibility =>
            State?.FinalStatus == "received" ? Visibility.Visible : Visibility.Collapsed;
        public virtual string FinalEmptyText => EmptyText(State?.FinalStatus);
        public virtual Visibility FinalEmptyVisibility =>
            State?.FinalStatus == "received" ? Visibility.Collapsed : Visibility.Visible;
        public virtual string FinalMacTowText =>
            (State?.FinalMacTow ?? 0).ToString("F1", CultureInfo.InvariantCulture) + " %";
        public virtual Brush FinalMacTowBrush =>
            (State?.FinalMacTowError ?? false) ? Brushes.Red : Brushes.White;
        public virtual string FinalTowText =>
            ((State?.FinalTowKg ?? 0) / 1000.0).ToString("F1", CultureInfo.InvariantCulture) + " t";
        public virtual string FinalIdentText =>
            string.IsNullOrEmpty(State?.FinalLoadsheetIdent) ? "—" : State.FinalLoadsheetIdent;
        public virtual string FinalReceivedText => FormatReceived(State?.FinalReceivedAt);
        public virtual double FinalRangeMarkerLeft => RangeMarkerLeft(State?.FinalMacTow ?? 0);
        public virtual Brush FinalRangeMarkerBrush =>
            (State?.FinalMacTowError ?? false)
                ? new SolidColorBrush(Color.FromRgb(0xDC, 0x40, 0x40))
                : new SolidColorBrush(Color.FromRgb(0x2D, 0xBE, 0x4D));

        // Final-card border treatment per spec — red 2-thick border when
        // the final loadsheet's MacTow is out of envelope. Otherwise the
        // card borrows the standard SectionCardBorder look (1px theme border).
        public virtual Brush FinalCardBorderBrush =>
            (State?.FinalMacTowError ?? false) && State?.FinalStatus == "received"
                ? new SolidColorBrush(Color.FromRgb(0xDC, 0x40, 0x40))
                : (Brush)Application.Current?.TryFindResource("BorderBrush") ?? Brushes.Gray;
        public virtual Thickness FinalCardBorderThickness =>
            (State?.FinalMacTowError ?? false) && State?.FinalStatus == "received"
                ? new Thickness(2)
                : new Thickness(1);

        // ── Action handlers ──────────────────────────────────────────────────
        // Resend the most-recent slot via the EFB SDK. If the final has
        // been received, resending FINAL is the operationally meaningful
        // action (it's the latest authoritative loadsheet); otherwise
        // resend PRELIM. The web RESEND button mirrors this logic via
        // the same LoadsheetController.Resend?slot=… endpoint, so both
        // surfaces produce identical observable behaviour.
        public virtual async void OnResend()
        {
            try
            {
                var svc = AppService?.LoadsheetService;
                if (svc == null)
                {
                    Logger.Warning("Loadsheet resend requested but LoadsheetService is unavailable");
                    return;
                }
                string slot = State?.FinalStatus == "received" ? "final" : "prelim";
                bool ok = await svc.ResendAsync(slot);
                Logger.Information($"Loadsheet resend ({slot}) via WPF tab → success: {ok}");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        // Reset routes through the same service path the DELETE endpoint uses.
        public virtual void OnReset()
        {
            try
            {
                var svc = AppService?.LoadsheetService;
                var st = AppService?.Loadsheet;
                if (svc != null && st != null)
                {
                    svc.ResetSlots(st);
                    Logger.Information("Loadsheet slots reset via WPF tab");
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        protected static string StatusText(string status) => status switch
        {
            "received" => "RECEIVED",
            "error"    => "ERROR",
            _          => "PENDING",
        };

        protected static Brush StatusBrush(string status) => status switch
        {
            "received" => new SolidColorBrush(Color.FromRgb(0x2D, 0xBE, 0x4D)),
            "error"    => new SolidColorBrush(Color.FromRgb(0xDC, 0x40, 0x40)),
            _          => new SolidColorBrush(Color.FromRgb(0xF5, 0xA6, 0x23)),
        };

        protected static string EmptyText(string status) => status switch
        {
            "error" => "Failed to parse loadsheet JSON. See log.",
            _       => "Awaiting loadsheet from Dispatch.",
        };

        protected static string FormatReceived(DateTime? at)
        {
            if (!at.HasValue) return "—";
            return at.Value.ToLocalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        }

        // Pixel offset for the range marker dot's Canvas.Left. The dot is
        // 12px wide so its visual centre sits at offsetLeft + 6; here we
        // compute the centre and subtract half the diameter so the binding
        // can hit Canvas.Left directly without a converter.
        protected virtual double RangeMarkerLeft(double mac)
        {
            double min = State?.MinMacTow ?? 10.5;
            double max = State?.MaxMacTow ?? 45.0;
            double span = Math.Max(0.0001, max - min);
            double frac = Math.Max(0.0, Math.Min(1.0, (mac - min) / span));
            return (frac * RangeBarWidth) - (RangeMarkerDiameter / 2.0);
        }
    }
}
