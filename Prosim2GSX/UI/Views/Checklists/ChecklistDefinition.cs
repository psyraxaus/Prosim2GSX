using System.Collections.Generic;

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
    }

    public class ChecklistDataRefCondition
    {
        public string DataRef { get; set; }
        public string Condition { get; set; }
    }
}
