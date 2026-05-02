using CommunityToolkit.Mvvm.ComponentModel;
using Prosim2GSX.SayIntentions;
using System;
using System.Threading;

namespace Prosim2GSX.State
{
    // Long-lived observable mirror of the OFP-tab workflow state. Owned by
    // AppService — independent of whether the WPF OFP tab is currently
    // active, so the gate-assignment flow (Confirm → SendNow) and the
    // weather cache survive tab close + reopen and are visible to web
    // clients.
    //
    // The previous home for these fields was ModelOfp (transient per-tab),
    // which meant a Confirm Arrival Gate clicked while the WPF tab was
    // open was lost the moment the user switched away. Phase 8.0b promotes
    // them up here as part of the OFP web-stack groundwork.
    public partial class OfpState : ObservableObject
    {
        // Gate-assignment workflow.
        // PendingArrivalGate is set by the Confirm command and consumed by
        // SendNow (manual click) or the future auto-fire on phase=Flight.
        // The two status strings drive the UI; the three flags prevent
        // double-fire / retry-forever once a sub-step has succeeded.
        [ObservableProperty] private string _PendingArrivalGate = "";
        [ObservableProperty] private string _GateAssignmentStatus = "";
        [ObservableProperty] private string _GsxAssignmentStatus = "";
        [ObservableProperty] private bool _AutoFired;
        [ObservableProperty] private bool _SayIntentionsSent;
        [ObservableProperty] private bool _GsxSent;

        // GSX SetGate readback — formatted display string ("C3", "Gate 12", or
        // "" when unassigned). Mirrored on GsxState for the Monitor live view;
        // both are written by StateUpdateWorker from the same SimConnect read.
        [ObservableProperty] private string _AssignedArrivalGate = "";

        // Weather cache (most recent SayIntentions response). Held as the
        // raw service type so OfpDto.From can project to a wire-safe shape
        // in Phase 8B without OfpState having to know about WeatherDto.
        [ObservableProperty] private SayIntentionsAirportWx _DepartureWeather;
        [ObservableProperty] private SayIntentionsAirportWx _ArrivalWeather;
        [ObservableProperty] private string _WeatherStatus = "";
        [ObservableProperty] private bool _IsRefreshingWeather;

        // CPDLC station from SayIntentions getCurrentFrequencies. Single
        // value because the endpoint reports whatever airport SayIntentions
        // currently considers active (departure → arrival on its own).
        [ObservableProperty] private string _CpdlcStation = "";

        // Refresh-cache metadata. Not [ObservableProperty] — pure server-
        // side bookkeeping; the React/WPF panels only ever read the
        // observable fields above.
        public DateTimeOffset? WeatherFetchedAt { get; set; }
        public DateTimeOffset? LastForcedRefreshAt { get; set; }
        // Single in-flight guard for refresh — second concurrent caller
        // waits then exits via the !isStale short-circuit.
        public SemaphoreSlim RefreshGate { get; } = new SemaphoreSlim(1, 1);
    }
}
