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
        public string DataRef { get; set; }
        public string DataRefCondition { get; set; }
        public bool IsNote { get; set; }
        public bool IsSeparator { get; set; }
    }
}
