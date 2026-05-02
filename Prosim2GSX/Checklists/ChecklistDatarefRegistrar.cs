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
                    if (!string.IsNullOrWhiteSpace(item.DataRef))
                        unique.Add(item.DataRef);
                    if (item.DataRefs != null)
                    {
                        foreach (var c in item.DataRefs)
                        {
                            if (!string.IsNullOrWhiteSpace(c?.DataRef))
                                unique.Add(c.DataRef);
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
