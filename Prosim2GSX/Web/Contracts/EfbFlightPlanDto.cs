using Prosim2GSX.State;
using System;
using System.Collections.Generic;

namespace Prosim2GSX.Web.Contracts
{
    // Read-only EFB INIT tab snapshot. Mirrors EfbFlightPlanState. The panel
    // renders directly from this shape, computing each field's "effective"
    // value as override-if-set else CurrentOfp.<field>. The override dicts
    // are projected as new copies so wire serialisation never observes a
    // dict mid-mutation on the server.
    public class EfbFlightPlanDto
    {
        public bool IsOfpLoaded { get; set; }
        public OfpStatus Status { get; set; } = OfpStatus.Empty;
        public OfpSource Source { get; set; } = OfpSource.None;
        public OFPData Ofp { get; set; }
        public Dictionary<string, bool> OverrideFlags { get; set; } = new();
        public Dictionary<string, object> OverrideValues { get; set; } = new();
        public DateTime? FetchedAt { get; set; }
        public string LastFetchError { get; set; } = "";
        public bool IsBusy { get; set; }

        // Behaviour flags from Config — projected so the panel renders the
        // correct UX (locked vs editable fields, auto-sync hint, etc.)
        // without a second round-trip to /api/app-settings.
        public bool AutoSyncToFmsOnFetch { get; set; }
        public bool PreferEfbFlightPlan { get; set; }
        public bool LockFieldsFromOfp { get; set; }

        public static EfbFlightPlanDto From(AppService app)
        {
            var s = app?.EfbFlightPlan;
            var c = app?.Config;
            if (s == null) return new EfbFlightPlanDto();
            return new EfbFlightPlanDto
            {
                IsOfpLoaded = s.IsOfpLoaded,
                Status = s.Status,
                Source = s.Source,
                Ofp = s.CurrentOfp,
                OverrideFlags = new Dictionary<string, bool>(s.OverrideFlags),
                OverrideValues = new Dictionary<string, object>(s.OverrideValues),
                FetchedAt = s.FetchedAt,
                LastFetchError = s.LastFetchError ?? "",
                IsBusy = s.IsBusy,
                AutoSyncToFmsOnFetch = c?.EfbAutoSyncToFmsOnFetch == true,
                PreferEfbFlightPlan = c?.EfbPreferEfbFlightPlan == true,
                LockFieldsFromOfp = c?.EfbLockFieldsFromOfp != false,
            };
        }
    }
}
