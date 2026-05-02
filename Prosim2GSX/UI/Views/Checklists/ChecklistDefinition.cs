using System.Collections.Generic;
using Newtonsoft.Json;

namespace Prosim2GSX.UI.Views.Checklists
{
    public class ChecklistDefinition
    {
        public string Name { get; set; }
        public string AircraftType { get; set; }
        public List<ChecklistSection> Sections { get; set; } = new();
    }

    public class ChecklistSection
    {
        public string Title { get; set; }
        public List<ChecklistItem> Items { get; set; } = new();
    }

    public class ChecklistItem
    {
        public string Label { get; set; }
        public string Value { get; set; }

        // Primary dataref the item documents. For latched switches, this is
        // also the value read by the worker. For momentary switches (Prosim
        // pulses them 0 → 1 → 0 — see "My hardware type" → Momentary in the
        // Prosim Switch types config), set Momentary=true and provide a
        // SteadyDataRef pointing at the corresponding indicator LED, gate, or
        // composite state that actually holds the system's mode/value.
        public string DataRef { get; set; }
        public string DataRefCondition { get; set; }

        // Optional steady-state dataref. When non-empty, the worker reads
        // SteadyDataRef instead of DataRef but interprets the result against
        // DataRefCondition. Lets the JSON document "this item is about the
        // EXT PWR switch" while polling "the EXT PWR ON lamp".
        public string SteadyDataRef { get; set; }

        // Marks the primary DataRef as a momentary pulse. Used by the
        // registrar to warn when a momentary item has no SteadyDataRef
        // configured (otherwise the polling cache will be 0 99% of the time).
        public bool Momentary { get; set; }

        // Compound form: ALL listed conditions must hold for the item to flip.
        // Used for things like "all four fuel pumps on" or "all three IRs in
        // NAV". Null/empty means "use the single-condition fields above".
        public List<ChecklistDataRefCondition> DataRefs { get; set; }

        public bool IsNote { get; set; }
        public bool IsSeparator { get; set; }

        // Runtime-only state. Set when the registrar / worker determines the
        // dataref is unreachable (typo'd, not present in this aircraft variant,
        // SDK refused to poll it). When true, the item is treated as manual:
        // the user can tick / untick it. Never serialised so it doesn't bleed
        // back into the JSON.
        [JsonIgnore]
        public bool IsManualFallback { get; set; }

        // Counter used by the worker to escalate to IsManualFallback after
        // repeated null-eval results while the SDK is connected. Not part of
        // the JSON contract.
        [JsonIgnore]
        public int NullEvaluationCount { get; set; }
    }

    public class ChecklistDataRefCondition
    {
        public string DataRef { get; set; }
        public string Condition { get; set; }

        // Optional steady-state read for momentary switches (mirrors
        // ChecklistItem.SteadyDataRef). When non-empty the worker polls
        // SteadyDataRef but interprets it against Condition.
        public string SteadyDataRef { get; set; }
    }
}
