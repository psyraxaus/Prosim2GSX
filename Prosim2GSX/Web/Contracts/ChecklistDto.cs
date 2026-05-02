using Prosim2GSX.State;
using Prosim2GSX.UI.Views.Checklists;
using System.Collections.Generic;
using System.Linq;

namespace Prosim2GSX.Web.Contracts
{
    // Read-only Checklist tab snapshot. The wire shape mirrors the WPF view:
    //   - the loaded definition (sections + items, static schema)
    //   - per-item runtime state (IsChecked, projected per section)
    //   - current section / current item indices (for highlighting)
    //   - available checklist filenames + currently selected name
    public class ChecklistDto
    {
        public string CurrentChecklistName { get; set; } = "";
        public IList<string> AvailableChecklists { get; set; } = new List<string>();
        public string AircraftType { get; set; } = "";
        public string Name { get; set; } = "";
        public int CurrentSectionIndex { get; set; }
        public int CurrentItemIndex { get; set; }
        public bool IsSectionComplete { get; set; }
        public bool AllowManualOverride { get; set; }
        public IList<ChecklistSectionDto> Sections { get; set; } = new List<ChecklistSectionDto>();

        public static ChecklistDto From(AppService app)
        {
            var s = app?.Checklist;
            var dto = new ChecklistDto
            {
                CurrentChecklistName = s?.CurrentChecklistName ?? "",
                AvailableChecklists = s?.AvailableChecklists != null
                    ? new List<string>(s.AvailableChecklists)
                    : new List<string>(),
                AircraftType = s?.Definition?.AircraftType ?? "",
                Name = s?.Definition?.Name ?? "",
                CurrentSectionIndex = s?.CurrentSectionIndex ?? 0,
                CurrentItemIndex = s?.CurrentItemIndex ?? 0,
                AllowManualOverride = app?.Config?.AllowManualChecklistOverride ?? false,
                IsSectionComplete = ComputeSectionComplete(s),
            };
            if (s?.Definition?.Sections == null) return dto;

            for (int i = 0; i < s.Definition.Sections.Count; i++)
            {
                var def = s.Definition.Sections[i];
                var sectionDto = new ChecklistSectionDto { Title = def.Title ?? "" };
                if (s.ItemsBySection.TryGetValue(i, out var runtimes))
                {
                    sectionDto.Items = runtimes.Select(r => new ChecklistItemDto
                    {
                        Label = r.Definition?.Label ?? "",
                        Value = r.Definition?.Value ?? "",
                        DataRef = r.Definition?.DataRef ?? "",
                        IsNote = r.Definition?.IsNote ?? false,
                        IsSeparator = r.Definition?.IsSeparator ?? false,
                        IsManual = IsManualItem(r.Definition),
                        IsChecked = r.IsChecked,
                    }).ToList();
                }
                dto.Sections.Add(sectionDto);
            }
            return dto;
        }

        private static bool IsManualItem(global::Prosim2GSX.UI.Views.Checklists.ChecklistItem def)
        {
            if (def == null) return true;
            if (!string.IsNullOrWhiteSpace(def.DataRef)) return false;
            if (def.DataRefs != null && def.DataRefs.Count > 0) return false;
            return true;
        }

        private static bool ComputeSectionComplete(global::Prosim2GSX.State.ChecklistState s)
        {
            if (s == null) return false;
            if (!s.ItemsBySection.TryGetValue(s.CurrentSectionIndex, out var items)) return false;
            if (items.Count == 0) return false;
            for (int i = 0; i < items.Count; i++)
            {
                var d = items[i].Definition;
                if (d == null || d.IsNote || d.IsSeparator) continue;
                if (!items[i].IsChecked) return false;
            }
            return true;
        }
    }

    public class ChecklistSectionDto
    {
        public string Title { get; set; } = "";
        public IList<ChecklistItemDto> Items { get; set; } = new List<ChecklistItemDto>();
    }

    public class ChecklistItemDto
    {
        public string Label { get; set; } = "";
        public string Value { get; set; } = "";
        public string DataRef { get; set; } = "";
        public bool IsNote { get; set; }
        public bool IsSeparator { get; set; }
        public bool IsManual { get; set; }
        public bool IsChecked { get; set; }
    }
}
