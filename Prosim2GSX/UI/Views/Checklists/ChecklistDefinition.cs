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

        // Single-condition form (kept for back-compat).
        public string DataRef { get; set; }
        public string DataRefCondition { get; set; }

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
    }
}
