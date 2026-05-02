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
            NotifyPropertyChanged(nameof(FlightNumber));
            NotifyPropertyChanged(nameof(AlternateIcao));
            NotifyPropertyChanged(nameof(DeparturePlanRwy));
            NotifyPropertyChanged(nameof(ArrivalPlanRwy));
            NotifyPropertyChanged(nameof(CruiseAltitude));
            NotifyPropertyChanged(nameof(BlockFuelKg));
            NotifyPropertyChanged(nameof(BlockTimeFormatted));
            NotifyPropertyChanged(nameof(PaxCount));
            NotifyPropertyChanged(nameof(AirDistance));
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

        public virtual string DepartureIcao => AircraftInterface?.FmsOrigin ?? Ofp?.Origin?.IcaoCode ?? "";
        public virtual string ArrivalIcao => AircraftInterface?.FmsDestination ?? Ofp?.Destination?.IcaoCode ?? "";
        public virtual string AlternateIcao => Ofp?.Alternate?.IcaoCode ?? "";
        public virtual string FlightNumber
            => string.IsNullOrWhiteSpace(Ofp?.General?.FlightNumber) ? "" : $"{Ofp.General.IcaoAirline}{Ofp.General.FlightNumber}";
        public virtual string DeparturePlanRwy => Ofp?.Origin?.PlanRwy ?? "";
        public virtual string ArrivalPlanRwy => Ofp?.Destination?.PlanRwy ?? "";
        public virtual string CruiseAltitude => Ofp?.Atc?.InitialAlt ?? "";
        public virtual string BlockFuelKg => Ofp?.Fuel?.PlanRamp ?? "";
        public virtual string BlockTimeFormatted => FormatBlockSeconds(Ofp?.Times?.EstBlock);
        public virtual string PaxCount => Ofp?.Weights?.PaxCount ?? "";
        public virtual string AirDistance => Ofp?.General?.AirDistance ?? "";

        protected static string FormatBlockSeconds(string secondsStr)
        {
            if (long.TryParse(secondsStr, out long seconds) && seconds > 0)
            {
                var span = TimeSpan.FromSeconds(seconds);
                return $"{(int)span.TotalHours}h {span.Minutes:D2}m";
            }
            return "";
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
