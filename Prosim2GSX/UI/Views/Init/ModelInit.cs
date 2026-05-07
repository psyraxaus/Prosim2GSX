using CFIT.AppFramework.UI.ViewModels;
using CFIT.AppLogger;
using Prosim2GSX.State;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Prosim2GSX.UI.Views.Init
{
    // Notification adapter over EfbFlightPlanState. Same pattern as
    // ModelLoadsheet — every public property reads through to the store and
    // PropertyChanged is forwarded so XAML bindings refresh without the
    // view-model duplicating any state.
    //
    // Inline override editing is intentionally NOT exposed in this v1 of
    // the WPF tab — the React panel owns the per-row edit UX. The WPF tab
    // shows the effective values (override-if-set else OFP) with an amber
    // tint when overridden, plus the four action buttons.
    public partial class ModelInit : ViewModelBase<AppService>
    {
        protected virtual AppService AppService => Source;
        protected virtual EfbFlightPlanState State => AppService?.EfbFlightPlan;
        protected virtual OFPData Ofp => State?.CurrentOfp;

        // FMS palette — defined here so the bindings can reference brushes
        // by name without the consumer having to know hex codes.
        public static readonly SolidColorBrush FmsCyan   = Freeze(0x00, 0xD4, 0xE8);
        public static readonly SolidColorBrush FmsGreen  = Freeze(0x39, 0xFF, 0x6E);
        public static readonly SolidColorBrush FmsAmber  = Freeze(0xFF, 0xAA, 0x00);
        public static readonly SolidColorBrush FmsWhite  = Freeze(0xD8, 0xDC, 0xE8);
        public static readonly SolidColorBrush FmsGrey   = Freeze(0x4A, 0x52, 0x68);
        public static readonly SolidColorBrush FmsWarn   = Freeze(0xFF, 0x44, 0x44);
        public static readonly SolidColorBrush FmsOverride = Freeze(0xFF, 0x8C, 0x00);

        public ModelInit(AppService appService) : base(appService) { }

        protected override void InitializeModel()
        {
            if (State != null)
                State.PropertyChanged += OnStatePropertyChanged;
        }

        protected virtual void OnStatePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
                dispatcher.BeginInvoke(new Action(NotifyAll));
            else
                NotifyAll();
        }

        protected virtual void NotifyAll()
        {
            // Status + source + busy
            NotifyPropertyChanged(nameof(StatusText));
            NotifyPropertyChanged(nameof(StatusBrush));
            NotifyPropertyChanged(nameof(SourceText));
            NotifyPropertyChanged(nameof(BusyText));
            NotifyPropertyChanged(nameof(LastFetchError));
            NotifyPropertyChanged(nameof(HasFetchError));
            NotifyPropertyChanged(nameof(IsLoadedVisibility));
            NotifyPropertyChanged(nameof(IsEmptyVisibility));

            // Flight info
            NotifyPropertyChanged(nameof(FlightNumberText));
            NotifyPropertyChanged(nameof(DepartureText));
            NotifyPropertyChanged(nameof(ArrivalText));
            NotifyPropertyChanged(nameof(AlternateText));
            NotifyPropertyChanged(nameof(CallsignText));
            NotifyPropertyChanged(nameof(CruiseFlText));
            NotifyPropertyChanged(nameof(CostIndexText));
            NotifyPropertyChanged(nameof(RouteText));
            NotifyPropertyChanged(nameof(DepartureRwyText));
            NotifyPropertyChanged(nameof(ArrivalRwyText));
            NotifyPropertyChanged(nameof(StdText));
            NotifyPropertyChanged(nameof(EtaText));
            NotifyPropertyChanged(nameof(AircraftTypeText));
            NotifyPropertyChanged(nameof(AircraftRegText));

            // Weights / fuel — effective values + override brushes
            NotifyPropertyChanged(nameof(ZfwText));
            NotifyPropertyChanged(nameof(ZfwBrush));
            NotifyPropertyChanged(nameof(FuelRampText));
            NotifyPropertyChanged(nameof(FuelRampBrush));
            NotifyPropertyChanged(nameof(FuelTripText));
            NotifyPropertyChanged(nameof(FuelMinText));
            NotifyPropertyChanged(nameof(FuelExtraText));
            NotifyPropertyChanged(nameof(FuelContingencyText));
            NotifyPropertyChanged(nameof(FuelAlternateText));
            NotifyPropertyChanged(nameof(FuelReserveText));
            NotifyPropertyChanged(nameof(FuelTaxiText));
            NotifyPropertyChanged(nameof(PaxText));
            NotifyPropertyChanged(nameof(PaxBrush));
            NotifyPropertyChanged(nameof(CargoText));
            NotifyPropertyChanged(nameof(CargoBrush));
            NotifyPropertyChanged(nameof(OewText));

            // Footer
            NotifyPropertyChanged(nameof(FetchedAtText));
            NotifyPropertyChanged(nameof(HasOverrides));
            NotifyPropertyChanged(nameof(SyncEnabled));
            NotifyPropertyChanged(nameof(ClearOverridesEnabled));
        }

        // ── Status / busy / source ──────────────────────────────────────────

        public virtual string StatusText
        {
            get
            {
                if (State?.IsBusy == true) return "FETCHING…";
                if (HasFetchError) return "UPLINK FAILED";
                return State?.Status switch
                {
                    OfpStatus.Loaded => "OFP LOADED",
                    OfpStatus.Partial => "OFP PARTIAL",
                    _ => "AWAITING OFP",
                };
            }
        }

        public virtual Brush StatusBrush
        {
            get
            {
                if (State?.IsBusy == true) return FmsCyan;
                if (HasFetchError) return FmsWarn;
                return State?.Status == OfpStatus.Loaded ? FmsGreen : FmsAmber;
            }
        }

        public virtual string SourceText => State?.Source switch
        {
            OfpSource.SimbriefEfb => "SIMBRIEF",
            OfpSource.Mcdu => "MCDU",
            OfpSource.Manual => "MANUAL",
            _ => "—",
        };

        public virtual string BusyText => State?.IsBusy == true ? "SEARCHING…" : "";

        public virtual string LastFetchError => State?.LastFetchError ?? "";
        public virtual bool HasFetchError => !string.IsNullOrEmpty(State?.LastFetchError);

        public virtual Visibility IsLoadedVisibility =>
            State?.IsOfpLoaded == true ? Visibility.Visible : Visibility.Collapsed;
        public virtual Visibility IsEmptyVisibility =>
            State?.IsOfpLoaded == true ? Visibility.Collapsed : Visibility.Visible;

        // ── Flight info ─────────────────────────────────────────────────────

        public virtual string FlightNumberText => Ofp?.FlightNumber.NullIfEmpty() ?? "—";
        public virtual string DepartureText => Ofp?.DepartureIcao.NullIfEmpty() ?? "—";
        public virtual string ArrivalText => Ofp?.ArrivalIcao.NullIfEmpty() ?? "—";
        public virtual string AlternateText => Ofp?.AlternateIcao.NullIfEmpty() ?? "—";
        public virtual string CallsignText => Ofp?.Callsign.NullIfEmpty() ?? "—";
        public virtual string CruiseFlText =>
            (Ofp?.CruiseFlightLevel ?? 0) > 0 ? $"FL{Ofp.CruiseFlightLevel:D3}" : "FL---";
        public virtual string CostIndexText =>
            (Ofp?.CostIndex ?? 0) > 0 ? Ofp.CostIndex.ToString(CultureInfo.InvariantCulture) : "—";
        public virtual string RouteText => Ofp?.Route.NullIfEmpty() ?? "—";
        public virtual string DepartureRwyText => Ofp?.DeparturePlanRwy.NullIfEmpty() ?? "—";
        public virtual string ArrivalRwyText => Ofp?.ArrivalPlanRwy.NullIfEmpty() ?? "—";
        public virtual string StdText => FormatZulu(Ofp?.Std);
        public virtual string EtaText => FormatZulu(Ofp?.Eta);
        public virtual string AircraftTypeText => Ofp?.AircraftType.NullIfEmpty() ?? "—";
        public virtual string AircraftRegText => Ofp?.AircraftReg.NullIfEmpty() ?? "—";

        // ── Weights / fuel — effective + override brush ─────────────────────

        public virtual string ZfwText => FormatKg(EffectiveDouble("zfwKg", Ofp?.ZfwKg ?? 0));
        public virtual Brush ZfwBrush => OverrideBrush("zfwKg");

        public virtual string FuelRampText => FormatKg(EffectiveDouble("fuelRampKg", Ofp?.FuelRampKg ?? 0));
        public virtual Brush FuelRampBrush => OverrideBrush("fuelRampKg");

        public virtual string FuelTripText => FormatKg(Ofp?.FuelTripKg ?? 0);
        public virtual string FuelMinText => FormatKg(Ofp?.FuelMinimumKg ?? 0);
        public virtual string FuelExtraText => FormatKg(Ofp?.FuelExtraKg ?? 0);
        public virtual string FuelContingencyText => FormatKg(Ofp?.FuelContingencyKg ?? 0);
        public virtual string FuelAlternateText => FormatKg(Ofp?.FuelAlternateKg ?? 0);
        public virtual string FuelReserveText => FormatKg(Ofp?.FuelReserveKg ?? 0);
        public virtual string FuelTaxiText => FormatKg(Ofp?.FuelTaxiKg ?? 0);

        public virtual string PaxText
        {
            get
            {
                int p = (int)EffectiveDouble("passengerCount", Ofp?.PassengerCount ?? 0);
                return p > 0 ? p.ToString(CultureInfo.InvariantCulture) : "—";
            }
        }
        public virtual Brush PaxBrush => OverrideBrush("passengerCount");

        public virtual string CargoText => FormatKg(EffectiveDouble("cargoKg", Ofp?.CargoKg ?? 0));
        public virtual Brush CargoBrush => OverrideBrush("cargoKg");

        public virtual string OewText => FormatKg(Ofp?.OewKg ?? 0);

        // ── Footer + button gating ─────────────────────────────────────────

        public virtual string FetchedAtText
        {
            get
            {
                var t = State?.FetchedAt;
                if (!t.HasValue) return "";
                return $"FETCHED {t.Value.ToLocalTime():HH:mm:ss}";
            }
        }

        public virtual bool HasOverrides => (State?.OverrideFlags?.Count ?? 0) > 0;
        public virtual bool SyncEnabled => State?.IsOfpLoaded == true && State?.IsBusy != true;
        public virtual bool ClearOverridesEnabled => HasOverrides;

        // ── Button actions ─────────────────────────────────────────────────

        public virtual async void OnFetchOfp()
        {
            try
            {
                var svc = AppService?.EfbFlightPlanService;
                if (svc != null)
                    await svc.FetchAsync(OfpSource.Manual, default);
            }
            catch (Exception ex) { Logger.LogException(ex); }
        }

        public virtual void OnSyncToFms()
        {
            try { AppService?.EfbFlightPlanService?.SyncToFms(); }
            catch (Exception ex) { Logger.LogException(ex); }
        }

        public virtual void OnClearOverrides()
        {
            try { AppService?.EfbFlightPlanService?.ClearAllOverrides(); }
            catch (Exception ex) { Logger.LogException(ex); }
        }

        public virtual void OnResetFlight()
        {
            try { AppService?.EfbFlightPlanService?.ResetFlight(); }
            catch (Exception ex) { Logger.LogException(ex); }
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private double EffectiveDouble(string field, double fallback)
        {
            if (State?.OverrideFlags == null || State?.OverrideValues == null) return fallback;
            if (!State.OverrideFlags.TryGetValue(field, out var on) || !on) return fallback;
            if (!State.OverrideValues.TryGetValue(field, out var v)) return fallback;
            return v switch
            {
                double d => d,
                float f => f,
                int i => i,
                long l => l,
                string s when double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var sd) => sd,
                System.Text.Json.JsonElement je when je.ValueKind == System.Text.Json.JsonValueKind.Number => je.GetDouble(),
                _ => fallback,
            };
        }

        private Brush OverrideBrush(string field)
        {
            if (State?.OverrideFlags == null) return FmsGreen;
            return State.OverrideFlags.TryGetValue(field, out var on) && on ? FmsOverride : FmsGreen;
        }

        private static string FormatKg(double kg) =>
            kg <= 0 ? "—" : $"{kg:N0} KG";

        private static string FormatZulu(DateTime? t)
        {
            if (!t.HasValue) return "—";
            return t.Value.ToString("HHmm", CultureInfo.InvariantCulture) + "Z";
        }

        private static SolidColorBrush Freeze(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }
    }

    internal static class StringExt
    {
        public static string NullIfEmpty(this string s) => string.IsNullOrWhiteSpace(s) ? null : s;
    }
}
