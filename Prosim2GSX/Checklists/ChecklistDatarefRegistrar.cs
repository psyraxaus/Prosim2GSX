using CFIT.AppLogger;
using Prosim2GSX.UI.Views.Checklists;
using ProsimInterface;
using System;
using System.Collections.Generic;

namespace Prosim2GSX.Checklists
{
    // On-demand subscription + polling enrolment for checklist datarefs.
    // Walks a freshly-loaded ChecklistDefinition, dedupes every DataRef and
    // DataRefs[].DataRef it references, and registers each one with the SDK
    // so the worker can ReadDataRef them at evaluation time.
    //
    // Also resets the per-item IsManualFallback / NullEvaluationCount flags so
    // a checklist re-load (or a profile switch that swaps to a different
    // checklist file) re-validates from scratch.
    public static class ChecklistDatarefRegistrar
    {
        public static void EnsureSubscribed(ChecklistDefinition def, ProsimSdkInterface sdk)
        {
            if (def?.Sections == null || sdk == null) return;

            var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var section in def.Sections)
            {
                if (section?.Items == null) continue;
                foreach (var item in section.Items)
                {
                    if (item == null) continue;
                    item.IsManualFallback = false;
                    item.NullEvaluationCount = 0;

                    // Sanity-check momentary items without a steady alternative.
                    if (item.Momentary
                        && string.IsNullOrWhiteSpace(item.SteadyDataRef)
                        && !string.IsNullOrWhiteSpace(item.DataRef))
                    {
                        Logger.Warning(
                            $"Checklist '{def.Name}' / '{item.Label}': Momentary=true but no SteadyDataRef configured — polling the momentary switch directly will rarely catch the pulse. Add a SteadyDataRef pointing at the indicator LED or composite gate.");
                    }

                    // Subscribe both the documented DataRef and any steady
                    // alternative so the Debug tab shows the relationship and
                    // a typo in either is caught early.
                    if (!string.IsNullOrWhiteSpace(item.DataRef))
                        unique.Add(item.DataRef);
                    if (!string.IsNullOrWhiteSpace(item.SteadyDataRef))
                        unique.Add(item.SteadyDataRef);
                    if (item.DataRefs != null)
                    {
                        foreach (var c in item.DataRefs)
                        {
                            if (!string.IsNullOrWhiteSpace(c?.DataRef))
                                unique.Add(c.DataRef);
                            if (!string.IsNullOrWhiteSpace(c?.SteadyDataRef))
                                unique.Add(c.SteadyDataRef);
                        }
                    }
                }
            }

            int registered = 0;
            foreach (var name in unique)
            {
                try
                {
                    sdk.Subscribe(name, SdkLvarBridgeService.PollOnlyHandler);
                    sdk.RegisterPollDataref(name);
                    registered++;
                }
                catch (Exception ex) { Logger.LogException(ex, $"Checklist registrar: failed for {name}"); }
            }
            Logger.Information(
                $"Checklist '{def.Name}': registered {registered} unique dataref(s) for on-demand polling");
        }
    }
}
