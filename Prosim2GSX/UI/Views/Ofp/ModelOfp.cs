using CFIT.AppFramework.UI.ViewModels;
using CFIT.AppLogger;
using CommunityToolkit.Mvvm.Input;
using Prosim2GSX.Aircraft;
using Prosim2GSX.AppConfig;
using Prosim2GSX.GSX;
using Prosim2GSX.SayIntentions;
using Prosim2GSX.State;
using Prosim2GSX.Web.Contracts.Commands;
using ProsimInterface;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Prosim2GSX.UI.Views.Ofp
{
    public partial class ModelOfp : ViewModelBase<AppService>
    {
        protected virtual AppService AppService => Source;
        protected virtual Config Config => AppService?.Config;
        protected virtual GsxController GsxController => AppService?.GsxService;
        protected virtual AircraftInterface AircraftInterface => GsxController?.AircraftInterface;
        protected virtual ISayIntentionsService SayIntentions => AppService?.SayIntentionsService;
        protected virtual OfpState OfpState => AppService?.Ofp;
        protected virtual EfbFlightPlanState EfbFlightPlan => AppService?.EfbFlightPlan;

        public ModelOfp(AppService appService) : base(appService)
        {
        }

        protected override void InitializeModel()
        {
            if (AircraftInterface != null)
                AircraftInterface.OnFlightPlanChanged += OnFlightPlanChanged;
            if (Config != null)
                Config.PropertyChanged += OnConfigPropertyChanged;
            if (GsxController?.AutomationController != null)
                GsxController.AutomationController.OnStateChange += OnAutomationStateChanged;
            // Subscribe to PushbackPreference mutations from anywhere (web
            // command handler, future cross-client sync) so the WPF tab's
            // radio buttons reflect external changes rather than going stale.
            if (GsxController != null)
                GsxController.PushbackPreferenceChanged += OnPushbackPreferenceChanged;
            // Mirror OfpState.AssignedArrivalGate (written by StateUpdateWorker
            // each tick) onto a local notifier so the WPF Tab's binding stays
            // in sync without the panel having to know about the store.
            if (OfpState != null)
                OfpState.PropertyChanged += OnOfpStateChanged;
            // Drive the FLIGHT PLAN summary card from EfbFlightPlanState. The
            // card is read-only — overrides + fetching live on the web INIT
            // page; this surface just shows whatever OFP is currently loaded
            // so the WPF user has parity awareness with the web side.
            if (EfbFlightPlan != null)
                EfbFlightPlan.PropertyChanged += OnFlightPlanStateChanged;
        }

        protected virtual void OnOfpStateChanged(object sender, PropertyChangedEventArgs e)
        {
            // Route OfpState property changes onto the corresponding view-model
            // properties so the WPF tab's bindings stay in sync. AssignedArrivalGate
            // and the weather fields all live on OfpState (the long-lived store)
            // so the view-model is purely a notification adapter for them.
            var dispatcher = Application.Current?.Dispatcher;
            Action notify = e?.PropertyName switch
            {
                nameof(State.OfpState.AssignedArrivalGate) => () =>
                {
                    NotifyPropertyChanged(nameof(AssignedArrivalGate));
                    NotifyPropertyChanged(nameof(HasAssignedArrivalGate));
                },
                nameof(State.OfpState.DepartureWeather) => () =>
                {
                    NotifyPropertyChanged(nameof(DepartureWeather));
                    NotifyPropertyChanged(nameof(HasDepartureWeather));
                    NotifyPropertyChanged(nameof(HasNoDepartureWeather));
                },
                nameof(State.OfpState.ArrivalWeather) => () =>
                {
                    NotifyPropertyChanged(nameof(ArrivalWeather));
                    NotifyPropertyChanged(nameof(HasArrivalWeather));
                    NotifyPropertyChanged(nameof(HasNoArrivalWeather));
                },
                nameof(State.OfpState.WeatherStatus) => () =>
                {
                    NotifyPropertyChanged(nameof(WeatherStatus));
                    NotifyPropertyChanged(nameof(HasWeatherStatus));
                },
                nameof(State.OfpState.IsRefreshingWeather) => () =>
                {
                    NotifyPropertyChanged(nameof(IsRefreshingWeather));
                    RefreshWeatherCommand.NotifyCanExecuteChanged();
                },
                nameof(State.OfpState.CpdlcStation) => () =>
                {
                    NotifyPropertyChanged(nameof(CpdlcStation));
                    NotifyPropertyChanged(nameof(HasCpdlcStation));
                },
                _ => null,
            };
            if (notify == null) return;
            if (dispatcher != null && !dispatcher.CheckAccess())
                dispatcher.BeginInvoke(notify);
            else
                notify();
        }

        protected virtual void OnFlightPlanStateChanged(object sender, PropertyChangedEventArgs e)
        {
            // CurrentOfp is replaced wholesale on every fetch (and cleared on
            // RESET FLIGHT) so a single notify-everything fan-out per change
            // is the simplest correct shape — no per-field tracking needed.
            // Status changes are forwarded too because HasFlightPlan reads
            // through it.
            if (e?.PropertyName != nameof(EfbFlightPlanState.CurrentOfp)
                && e?.PropertyName != nameof(EfbFlightPlanState.Status))
                return;

            var dispatcher = Application.Current?.Dispatcher;
            Action notify = NotifyFlightPlanProperties;
            if (dispatcher != null && !dispatcher.CheckAccess())
                dispatcher.BeginInvoke(notify);
            else
                notify();
        }

        protected virtual void NotifyFlightPlanProperties()
        {
            NotifyPropertyChanged(nameof(HasFlightPlan));
            NotifyPropertyChanged(nameof(HasNoFlightPlan));
            NotifyPropertyChanged(nameof(FlightNumber));
            NotifyPropertyChanged(nameof(Callsign));
            NotifyPropertyChanged(nameof(FpDeparture));
            NotifyPropertyChanged(nameof(FpArrival));
            NotifyPropertyChanged(nameof(FpAlternate));
            NotifyPropertyChanged(nameof(DeparturePlanRwy));
            NotifyPropertyChanged(nameof(ArrivalPlanRwy));
            NotifyPropertyChanged(nameof(AircraftType));
            NotifyPropertyChanged(nameof(AircraftReg));
            NotifyPropertyChanged(nameof(CruiseFlText));
            NotifyPropertyChanged(nameof(Route));
            NotifyPropertyChanged(nameof(StdZulu));
            NotifyPropertyChanged(nameof(EtaZulu));
            NotifyPropertyChanged(nameof(ZfwKgText));
            NotifyPropertyChanged(nameof(BlockFuelKgText));
            NotifyPropertyChanged(nameof(PaxCountText));
            NotifyPropertyChanged(nameof(CargoKgText));
            NotifyPropertyChanged(nameof(FetchedAtText));
        }

        protected virtual void OnPushbackPreferenceChanged(PushbackPreference _)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
                dispatcher.BeginInvoke(new Action(NotifyPushbackPropertiesChanged));
            else
                NotifyPushbackPropertiesChanged();
        }

        protected virtual void NotifyPushbackPropertiesChanged()
        {
            NotifyPropertyChanged(nameof(PushbackPrefIsStraight));
            NotifyPropertyChanged(nameof(PushbackPrefIsTailLeft));
            NotifyPropertyChanged(nameof(PushbackPrefIsTailRight));
        }

        protected virtual void OnFlightPlanChanged()
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
                dispatcher.BeginInvoke(new Action(NotifyOfpProperties));
            else
                NotifyOfpProperties();
        }

        protected virtual void OnConfigPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e?.PropertyName == nameof(Config.UseSayIntentions))
                NotifyPropertyChanged(nameof(UseSayIntentions));
        }

        protected virtual void NotifyOfpProperties()
        {
            NotifyPropertyChanged(nameof(IsOfpLoaded));
            NotifyPropertyChanged(nameof(IsOfpNotLoaded));
            NotifyPropertyChanged(nameof(DepartureIcao));
            NotifyPropertyChanged(nameof(ArrivalIcao));
            ConfirmArrivalGateCommand.NotifyCanExecuteChanged();
            SendNowCommand.NotifyCanExecuteChanged();
            RefreshWeatherCommand.NotifyCanExecuteChanged();
        }

        protected virtual void OnAutomationStateChanged(AutomationState state)
        {
            // Auto-fire on phase=Flight is now handled by OfpAutoSendService
            // (backend) so the WPF tab does not need to listen for this any
            // more. Hook retained as a no-op to keep the subscription
            // wiring above harmless if the service is added back later.
        }

        // OFP state
        public virtual bool IsOfpLoaded => AircraftInterface?.IsFlightPlanLoaded == true;
        public virtual bool IsOfpNotLoaded => !IsOfpLoaded;

        protected virtual SimbriefResponse Ofp => AircraftInterface?.LastSimbriefOfp;

        // Retained for gate-assignment (SayIntentions.AssignGateAsync needs the
        // ICAO) and the weather card titles. The FLIGHT PLAN summary card uses
        // its own Fp* projections sourced from EfbFlightPlanState below — that
        // path is exclusively SimBrief-fed, while these two also fall back to
        // the FMS origin/destination from the live aircraft for the gate flow.
        public virtual string DepartureIcao => AircraftInterface?.FmsOrigin ?? Ofp?.Origin?.IcaoCode ?? "";
        public virtual string ArrivalIcao => AircraftInterface?.FmsDestination ?? Ofp?.Destination?.IcaoCode ?? "";

        // ── FLIGHT PLAN summary card (read-only, sourced from EfbFlightPlanState) ──
        // The web INIT page is the only place to fetch / manage overrides; this
        // card just reflects whichever OFP is currently loaded so the desktop
        // user has the same situational awareness without launching a browser.
        // All values come from CurrentOfp directly — overrides are intentionally
        // not applied here. If the user has tweaked PAX/FUEL/etc on the web INIT
        // page, the W&B and Loadsheet panels (also web-only) show the resulting
        // numbers; this card stays as the as-fetched OFP record.
        protected virtual OFPData CurrentOfp => EfbFlightPlan?.CurrentOfp;
        public virtual bool HasFlightPlan =>
            EfbFlightPlan != null
            && EfbFlightPlan.Status != OfpStatus.Empty
            && CurrentOfp != null;
        public virtual bool HasNoFlightPlan => !HasFlightPlan;

        public virtual string FlightNumber => CurrentOfp?.FlightNumber ?? "";
        public virtual string Callsign => CurrentOfp?.Callsign ?? "";
        public virtual string FpDeparture => CurrentOfp?.DepartureIcao ?? "";
        public virtual string FpArrival => CurrentOfp?.ArrivalIcao ?? "";
        public virtual string FpAlternate => CurrentOfp?.AlternateIcao ?? "";
        public virtual string DeparturePlanRwy => CurrentOfp?.DeparturePlanRwy ?? "";
        public virtual string ArrivalPlanRwy => CurrentOfp?.ArrivalPlanRwy ?? "";
        public virtual string AircraftType => CurrentOfp?.AircraftType ?? "";
        public virtual string AircraftReg => CurrentOfp?.AircraftReg ?? "";
        public virtual string Route => CurrentOfp?.Route ?? "";

        // FL370 / FL280 — short, scannable. Empty when no plan or zero level.
        public virtual string CruiseFlText
        {
            get
            {
                var fl = CurrentOfp?.CruiseFlightLevel ?? 0;
                return fl > 0 ? $"FL{fl}" : "";
            }
        }

        // STD = scheduled out (off blocks); ETA = wheels-down per the Airbus FMS
        // convention. Both shown as Zulu HH:MM with a "Z" suffix to match the
        // INIT panel formatting.
        public virtual string StdZulu => FormatZulu(CurrentOfp?.Std);
        public virtual string EtaZulu => FormatZulu(CurrentOfp?.Eta);

        public virtual string ZfwKgText => FormatKg(CurrentOfp?.ZfwKg);
        public virtual string BlockFuelKgText => FormatKg(CurrentOfp?.FuelRampKg);
        public virtual string CargoKgText => FormatKg(CurrentOfp?.CargoKg);
        public virtual string PaxCountText
        {
            get
            {
                var n = CurrentOfp?.PassengerCount ?? 0;
                return n > 0 ? n.ToString(System.Globalization.CultureInfo.InvariantCulture) : "";
            }
        }

        public virtual string FetchedAtText
        {
            get
            {
                var at = CurrentOfp?.FetchedAt;
                if (!at.HasValue || at.Value == default) return "";
                return $"FETCHED {at.Value.ToLocalTime():HH:mm:ss}";
            }
        }

        private static string FormatZulu(System.DateTime? dt)
        {
            if (!dt.HasValue || dt.Value == default) return "";
            return dt.Value.ToUniversalTime().ToString("HH:mm") + "Z";
        }

        private static string FormatKg(double? kg)
        {
            if (!kg.HasValue || kg.Value <= 0) return "";
            return kg.Value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " KG";
        }

        // Arrival Gate input + Confirm
        protected string _arrivalGate = "";
        public virtual string ArrivalGate
        {
            get => _arrivalGate;
            set
            {
                if (_arrivalGate == value) return;
                _arrivalGate = value ?? "";
                NotifyPropertyChanged(nameof(ArrivalGate));
                ConfirmArrivalGateCommand.NotifyCanExecuteChanged();
            }
        }

        protected string _gateAssignmentStatus = "";
        public virtual string GateAssignmentStatus
        {
            get => _gateAssignmentStatus;
            set
            {
                if (_gateAssignmentStatus == value) return;
                _gateAssignmentStatus = value ?? "";
                NotifyPropertyChanged(nameof(GateAssignmentStatus));
                NotifyPropertyChanged(nameof(HasGateAssignmentStatus));
            }
        }
        public virtual bool HasGateAssignmentStatus => !string.IsNullOrWhiteSpace(GateAssignmentStatus);

        protected string _gsxAssignmentStatus = "";
        public virtual string GsxAssignmentStatus
        {
            get => _gsxAssignmentStatus;
            set
            {
                if (_gsxAssignmentStatus == value) return;
                _gsxAssignmentStatus = value ?? "";
                NotifyPropertyChanged(nameof(GsxAssignmentStatus));
                NotifyPropertyChanged(nameof(HasGsxAssignmentStatus));
            }
        }
        public virtual bool HasGsxAssignmentStatus => !string.IsNullOrWhiteSpace(GsxAssignmentStatus);

        protected string _pendingArrivalGate = "";
        protected bool _autoFired = false;
        protected bool _sayIntentionsSent = false;
        protected bool _gsxSent = false;
        public virtual string PendingArrivalGate
        {
            get => _pendingArrivalGate;
            protected set
            {
                if (_pendingArrivalGate == value) return;
                _pendingArrivalGate = value ?? "";
                NotifyPropertyChanged(nameof(PendingArrivalGate));
                NotifyPropertyChanged(nameof(HasPendingArrivalGate));
                SendNowCommand.NotifyCanExecuteChanged();
            }
        }
        public virtual bool HasPendingArrivalGate => !string.IsNullOrWhiteSpace(PendingArrivalGate);

        // GSX SetGate readback — view-only mirror of OfpState.AssignedArrivalGate,
        // populated by StateUpdateWorker.UpdateAssignedGate each tick.
        public virtual string AssignedArrivalGate => OfpState?.AssignedArrivalGate ?? "";
        public virtual bool HasAssignedArrivalGate => !string.IsNullOrWhiteSpace(AssignedArrivalGate);

        public virtual bool UseSayIntentions => Config?.UseSayIntentions == true;

        // Pushback direction preference (in-memory, session-scoped, lives on GsxController)
        public virtual bool PushbackPrefIsStraight
        {
            get => GsxController?.PushbackPreference == PushbackPreference.Straight;
            set { if (value) SetPushbackPreference(PushbackPreference.Straight); }
        }
        public virtual bool PushbackPrefIsTailLeft
        {
            get => GsxController?.PushbackPreference == PushbackPreference.TailLeft;
            set { if (value) SetPushbackPreference(PushbackPreference.TailLeft); }
        }
        public virtual bool PushbackPrefIsTailRight
        {
            get => GsxController?.PushbackPreference == PushbackPreference.TailRight;
            set { if (value) SetPushbackPreference(PushbackPreference.TailRight); }
        }
        protected virtual void SetPushbackPreference(PushbackPreference preference)
        {
            if (GsxController == null || GsxController.PushbackPreference == preference) return;
            GsxController.PushbackPreference = preference;
            NotifyPropertyChanged(nameof(PushbackPrefIsStraight));
            NotifyPropertyChanged(nameof(PushbackPrefIsTailLeft));
            NotifyPropertyChanged(nameof(PushbackPrefIsTailRight));
        }

        [RelayCommand(CanExecute = nameof(CanConfirmArrivalGate))]
        private Task ConfirmArrivalGateAsync()
        {
            var gate = (ArrivalGate ?? "").Trim().ToUpperInvariant();
            ArrivalGate = gate;

            if (string.IsNullOrWhiteSpace(gate))
            {
                GateAssignmentStatus = "Please enter an arrival gate.";
                GsxAssignmentStatus = "";
                return Task.CompletedTask;
            }

            PendingArrivalGate = gate;
            _autoFired = false;
            _sayIntentionsSent = false;
            _gsxSent = false;

            GateAssignmentStatus = SayIntentions?.IsActive == true
                ? "Queued — will be sent to ATC during cruise (or click 'Send Now')."
                : "SayIntentions not active — ATC assignment will be skipped.";
            GsxAssignmentStatus = "Queued — will be sent to GSX during cruise (or click 'Send Now').";
            return Task.CompletedTask;
        }

        private bool CanConfirmArrivalGate()
            => IsOfpLoaded && !string.IsNullOrWhiteSpace(ArrivalGate);

        [RelayCommand(CanExecute = nameof(CanSendNow))]
        private async Task SendNowAsync()
        {
            var gate = PendingArrivalGate;
            if (string.IsNullOrWhiteSpace(gate)) return;

            // ATC (SayIntentions)
            if (!_sayIntentionsSent)
            {
                if (SayIntentions?.IsActive == true)
                {
                    GateAssignmentStatus = "Sending to ATC ...";
                    var result = await SayIntentions.AssignGateAsync(ArrivalIcao, gate);
                    if (result.Ok)
                    {
                        GateAssignmentStatus = $"ATC assignment confirmed: {result.AssignedGate}";
                        _sayIntentionsSent = true;
                    }
                    else
                    {
                        GateAssignmentStatus = $"ATC assignment failed: {result.Error}";
                    }
                }
                else
                {
                    GateAssignmentStatus = "SayIntentions not active — ATC assignment skipped.";
                    _sayIntentionsSent = true; // skipped, don't retry forever
                }
            }

            // GSX
            if (!_gsxSent)
            {
                if (GsxController != null)
                {
                    GsxAssignmentStatus = $"Sending '{gate}' to GSX ...";
                    var ok = await GsxController.SetArrivalParkingAsync(gate);
                    if (ok)
                    {
                        GsxAssignmentStatus = $"GSX gate set: {gate}";
                        _gsxSent = true;
                    }
                    else
                    {
                        GsxAssignmentStatus = $"GSX selection failed for '{gate}' — check the GSX log/menu.";
                    }
                }
                else
                {
                    GsxAssignmentStatus = "GSX not available.";
                    _gsxSent = true;
                }
            }

            if (_sayIntentionsSent && _gsxSent)
                PendingArrivalGate = "";
        }

        private bool CanSendNow() => HasPendingArrivalGate;

        // Weather (ATIS / METAR / wind / runway) and CPDLC station all
        // live on OfpState — view-model is a pure notification adapter.
        // The OfpHandlers.RefreshWeather command owns the cache + debounce
        // policy, so both the WPF tab and the web panel share the same
        // SayIntentions rate-limiting.
        public virtual SayIntentionsAirportWx DepartureWeather => OfpState?.DepartureWeather;
        public virtual SayIntentionsAirportWx ArrivalWeather => OfpState?.ArrivalWeather;
        public virtual bool HasDepartureWeather => DepartureWeather != null;
        public virtual bool HasNoDepartureWeather => DepartureWeather == null;
        public virtual bool HasArrivalWeather => ArrivalWeather != null;
        public virtual bool HasNoArrivalWeather => ArrivalWeather == null;

        public virtual string WeatherStatus => OfpState?.WeatherStatus ?? "";
        public virtual bool HasWeatherStatus => !string.IsNullOrWhiteSpace(WeatherStatus);

        public virtual bool IsRefreshingWeather => OfpState?.IsRefreshingWeather ?? false;

        public virtual string CpdlcStation => OfpState?.CpdlcStation ?? "";
        public virtual bool HasCpdlcStation => !string.IsNullOrWhiteSpace(CpdlcStation);

        [RelayCommand(CanExecute = nameof(CanRefreshWeather))]
        private async Task RefreshWeatherAsync()
        {
            if (IsRefreshingWeather) return;

            try
            {
                await AppService.Commands.ExecuteAsync<RefreshWeatherRequest, WeatherSnapshotDto>(
                    "ofp.refreshWeather", new RefreshWeatherRequest(), CancellationToken.None);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        private bool CanRefreshWeather() => !IsRefreshingWeather;

        public virtual void OnTabActivated()
        {
            // Fetch weather when the tab becomes visible if SayIntentions is on and we have ICAOs.
            if (SayIntentions?.IsActive == true && (!string.IsNullOrWhiteSpace(DepartureIcao) || !string.IsNullOrWhiteSpace(ArrivalIcao)))
                _ = RefreshWeatherAsync();
        }
    }
}
