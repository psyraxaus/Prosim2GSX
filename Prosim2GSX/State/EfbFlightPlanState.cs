using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

namespace Prosim2GSX.State
{
    public enum OfpStatus
    {
        Empty,
        Loaded,
        Partial,
    }

    public enum OfpSource
    {
        None,
        SimbriefEfb,
        Mcdu,
        Manual,
    }

    // Long-lived observable store for the EFB Flight Planning (INIT) tab.
    // CurrentOfp is the raw OFP fetched from SimBrief; OverrideFlags +
    // OverrideValues layer pilot overrides on top. The "effective" value
    // for a field = override-if-set, else CurrentOfp[field]. The dicts are
    // replaced wholesale on mutation (not mutated in place) so the
    // [ObservableProperty] INPC fires on every change — same pattern the
    // rest of the state stores use to keep WS broadcasts cheap and
    // change-detection equality-based.
    public partial class EfbFlightPlanState : ObservableObject
    {
        [ObservableProperty] private OFPData _CurrentOfp;
        [ObservableProperty] private Dictionary<string, bool> _OverrideFlags = new();
        [ObservableProperty] private Dictionary<string, object> _OverrideValues = new();

        [ObservableProperty] private OfpStatus _Status = OfpStatus.Empty;
        [ObservableProperty] private OfpSource _Source = OfpSource.None;
        [ObservableProperty] private DateTime? _FetchedAt;

        // Empty when no fetch has failed since the last success. Drives the
        // "UPLINK FAILED" status line on both panels.
        [ObservableProperty] private string _LastFetchError = "";

        // True while an in-flight fetch is active. Drives the FETCH OFP
        // button's "SEARCHING..." state.
        [ObservableProperty] private bool _IsBusy;

        public bool IsOfpLoaded => Status != OfpStatus.Empty && CurrentOfp != null;
    }
}
