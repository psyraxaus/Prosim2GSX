using CFIT.AppLogger;
using Prosim2GSX.State;
using ProsimInterface;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Services
{
    // Owns the EFB Flight Planning (INIT) tab workflow.
    //
    //   1. Manual fetch (REST POST /api/efb/fetch-ofp + WPF FETCH OFP button)
    //      calls the SDK's SimbriefService.FetchAndImportSimbriefOfp, then
    //      projects the parsed response into OFPData. Source = Manual.
    //
    //   2. MCDU-triggered observation (Tick from StateUpdateWorker) watches
    //      AircraftInterface.LastSimbriefOfp for a new RequestId. The SDK
    //      auto-fetches when MCDU dep/arr is entered (its own MCDU watcher);
    //      we just pick up the result. Source = Mcdu.
    //
    //   3. Overrides — SetOverride/ClearOverride/ClearAllOverrides mutate
    //      the override dicts. The downstream ProSim dataref writes belong
    //      to a later slice; this service only owns the in-memory map.
    //
    // RequestId-based change detection means both fetch paths converge on
    // ApplyOfp. After a manual fetch our cache's OfpId equals what the SDK
    // has, so the next Tick is a no-op.
    public class EfbFlightPlanService
    {
        private readonly AppService _app;

        public EfbFlightPlanService(AppService app)
        {
            _app = app;
        }

        // Polled from StateUpdateWorker on the background tick. Cheap when
        // there is no change. Skipped while a manual fetch is in flight so
        // the two paths can't race on the same LastResponse.
        public virtual void Tick()
        {
            try
            {
                var state = _app?.EfbFlightPlan;
                if (state == null) return;
                if (state.IsBusy) return;

                var ai = _app?.GsxService?.AircraftInterface;
                var ofp = ai?.LastSimbriefOfp;
                if (ofp == null) return;

                var newId = ofp.Params?.RequestId ?? "";
                var currentId = state.CurrentOfp?.OfpId ?? "";
                if (string.IsNullOrEmpty(newId) ||
                    string.Equals(newId, currentId, StringComparison.Ordinal))
                    return;

                ApplyOfp(ofp, OfpSource.Mcdu);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        // Manual fetch entry point used by the FETCH OFP button. Source is
        // an arg so the same path serves the manual REST call and any
        // future programmatic re-fetch.
        public virtual async Task<bool> FetchAsync(OfpSource source, CancellationToken ct)
        {
            var state = _app?.EfbFlightPlan;
            if (state == null) return false;

            var ai = _app?.GsxService?.AircraftInterface;
            var simbrief = ai?.ProsimInterface?.SimbriefService;
            var user = ai?.SimbriefUser ?? "";

            if (simbrief == null)
            {
                state.LastFetchError = "SimBrief service not available — ProSim SDK not connected.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(user))
            {
                state.LastFetchError = "SimBrief user not configured — set the user ID on ProSim's EFB.";
                return false;
            }

            state.IsBusy = true;
            state.LastFetchError = "";

            try
            {
                var (success, _) = await simbrief.FetchAndImportSimbriefOfp(user);
                if (!success)
                {
                    state.LastFetchError = "SimBrief fetch failed — check network and user ID.";
                    return false;
                }

                var ofp = simbrief.LastResponse;
                if (ofp == null)
                {
                    state.LastFetchError = "SimBrief returned no data.";
                    return false;
                }

                ApplyOfp(ofp, source);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                state.LastFetchError = $"Fetch error: {ex.Message}";
                return false;
            }
            finally
            {
                state.IsBusy = false;
            }
        }

        public virtual void SetOverride(string field, object value)
        {
            if (string.IsNullOrWhiteSpace(field)) return;
            var state = _app?.EfbFlightPlan;
            if (state == null) return;

            var flags = new Dictionary<string, bool>(state.OverrideFlags) { [field] = true };
            var values = new Dictionary<string, object>(state.OverrideValues) { [field] = value };
            state.OverrideFlags = flags;
            state.OverrideValues = values;
        }

        public virtual void ClearOverride(string field)
        {
            if (string.IsNullOrWhiteSpace(field)) return;
            var state = _app?.EfbFlightPlan;
            if (state == null) return;

            if (!state.OverrideFlags.ContainsKey(field) && !state.OverrideValues.ContainsKey(field))
                return;

            var flags = new Dictionary<string, bool>(state.OverrideFlags);
            var values = new Dictionary<string, object>(state.OverrideValues);
            flags.Remove(field);
            values.Remove(field);
            state.OverrideFlags = flags;
            state.OverrideValues = values;
        }

        public virtual void ClearAllOverrides()
        {
            var state = _app?.EfbFlightPlan;
            if (state == null) return;

            if (state.OverrideFlags.Count == 0 && state.OverrideValues.Count == 0) return;
            state.OverrideFlags = new Dictionary<string, bool>();
            state.OverrideValues = new Dictionary<string, object>();
        }

        public virtual void ResetFlight()
        {
            var state = _app?.EfbFlightPlan;
            if (state == null) return;

            state.CurrentOfp = null;
            state.OverrideFlags = new Dictionary<string, bool>();
            state.OverrideValues = new Dictionary<string, object>();
            state.Status = OfpStatus.Empty;
            state.Source = OfpSource.None;
            state.FetchedAt = null;
            state.LastFetchError = "";
            state.IsBusy = false;
        }

        protected virtual void ApplyOfp(SimbriefResponse ofp, OfpSource source)
        {
            var state = _app?.EfbFlightPlan;
            if (state == null) return;

            double conv = _app?.Config?.WeightConversion ?? 2.2046226218;
            var data = SimbriefOfpMapper.ToOFPData(ofp, conv);
            if (data == null) return;

            state.CurrentOfp = data;
            state.FetchedAt = data.FetchedAt;
            state.Source = source;
            state.Status = OfpStatus.Loaded;
            state.LastFetchError = "";
        }
    }
}
