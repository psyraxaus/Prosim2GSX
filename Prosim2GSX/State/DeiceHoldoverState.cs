using CommunityToolkit.Mvvm.ComponentModel;
using Prosim2GSX.GSX;
using System;

namespace Prosim2GSX.State
{
    // Holdover-time state for the deice HOT card. Owned by AppService;
    // written by DeiceHoldoverService and projected to the web (and a
    // compact WPF Monitor readout).
    //
    // Two fields are user inputs (the crew supplies them, as on a real HOT
    // card — Prosim2GSX has no reliable precip/at-aircraft-OAT dataref):
    // Precip and OatC. The rest is computed when GSX deicing completes and
    // ticked down each StateUpdateWorker pass.
    public partial class DeiceHoldoverState : ObservableObject
    {
        // ── User inputs (persist across deice cycles) ────────────────────
        // Selected precipitation condition for the HOT lookup.
        [ObservableProperty] private HotPrecip _Precip = HotPrecip.None;
        // OAT (°C) used for the lookup. Prefilled from TakeoffPerfState.OatC
        // when the user has set it there; overridable from the HOT card.
        [ObservableProperty] private double _OatC;
        // True once the user has explicitly set OatC, so the prefill stops
        // clobbering a manual value.
        [ObservableProperty] private bool _OatUserSet;

        // ── Computed at deice completion ─────────────────────────────────
        [ObservableProperty] private bool _Active;
        [ObservableProperty] private int _FluidType;          // 1..4 (0 = none)
        [ObservableProperty] private string _FluidLabel = "";  // "Type IV 75%"
        [ObservableProperty] private int _Concentration;       // 100 / 75 / 50
        [ObservableProperty] private double _LowMinutes;
        [ObservableProperty] private double _HighMinutes;

        // ── Ticked countdown ─────────────────────────────────────────────
        [ObservableProperty] private int _RemainingLowSeconds;
        [ObservableProperty] private int _RemainingHighSeconds;
        [ObservableProperty] private bool _Expired;
        // Human-readable status for the WPF Monitor one-liner and the web
        // card subtitle (e.g. "Type IV 75% · light snow · 18–34 min left").
        [ObservableProperty] private string _Status = "";

        // Start of the holdover window. Plain property — server-side
        // bookkeeping, not projected to the wire (Remaining*/Status are).
        public DateTime? StartedUtc { get; set; }
    }
}
