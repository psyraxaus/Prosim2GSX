using CommunityToolkit.Mvvm.ComponentModel;
using Prosim2GSX.UI.Views.Checklists;
using System.Collections.Generic;

namespace Prosim2GSX.State
{
    // Per-item runtime state — definition is the static schema, IsChecked is the
    // mutating progress flag. Manual items (DataRef==null) only flip via user
    // click or RESET; dataref-driven items also flip via StateUpdateWorker.UpdateChecklist.
    public partial class ChecklistItemRuntime : ObservableObject
    {
        public ChecklistItem Definition { get; }
        [ObservableProperty] private bool _IsChecked;

        public ChecklistItemRuntime(ChecklistItem definition)
        {
            Definition = definition;
        }
    }

    // Long-lived observable store for the Checklist tab. Owned by AppService;
    // read by both ModelChecklist (WPF) and the web layer. Lives across tab
    // switches but resets on app restart (no disk persistence of progress —
    // fresh app = fresh checklist).
    public partial class ChecklistState : ObservableObject
    {
        [ObservableProperty] private string _CurrentChecklistName = "";
        [ObservableProperty] private ChecklistDefinition _Definition;
        [ObservableProperty] private int _CurrentSectionIndex = 0;
        [ObservableProperty] private int _CurrentItemIndex = 0;

        // Section index -> ordered list of item runtimes (1:1 with
        // Definition.Sections[i].Items). Rebuilt on LoadDefinition.
        public Dictionary<int, List<ChecklistItemRuntime>> ItemsBySection { get; } = new();

        // Available checklist filenames (without extension). Populated by
        // ModelChecklist on Start() from ChecklistService.
        [ObservableProperty] private List<string> _AvailableChecklists = new();

        public virtual void LoadDefinition(ChecklistDefinition def, string name)
        {
            CurrentChecklistName = name ?? "";
            Definition = def;
            ItemsBySection.Clear();
            if (def?.Sections != null)
            {
                for (int i = 0; i < def.Sections.Count; i++)
                {
                    var list = new List<ChecklistItemRuntime>();
                    foreach (var item in def.Sections[i].Items)
                        list.Add(new ChecklistItemRuntime(item));
                    ItemsBySection[i] = list;
                }
            }
            CurrentSectionIndex = 0;
            CurrentItemIndex = FindFirstActionableItem(0);
        }

        public virtual void ResetSection(int sectionIndex)
        {
            if (!ItemsBySection.TryGetValue(sectionIndex, out var items)) return;
            foreach (var it in items)
                it.IsChecked = false;
            if (sectionIndex == CurrentSectionIndex)
                CurrentItemIndex = FindFirstActionableItem(sectionIndex);
        }

        public virtual void ResetAll()
        {
            foreach (var kvp in ItemsBySection)
                foreach (var it in kvp.Value)
                    it.IsChecked = false;
            CurrentSectionIndex = 0;
            CurrentItemIndex = FindFirstActionableItem(0);
        }

        public virtual void ResetSections(params string[] sectionTitles)
        {
            if (Definition?.Sections == null) return;
            for (int i = 0; i < Definition.Sections.Count; i++)
            {
                var title = Definition.Sections[i].Title ?? "";
                foreach (var match in sectionTitles)
                {
                    if (title.Equals(match, System.StringComparison.OrdinalIgnoreCase))
                    {
                        ResetSection(i);
                        break;
                    }
                }
            }
        }

        public virtual int FindFirstActionableItem(int sectionIndex)
        {
            if (!ItemsBySection.TryGetValue(sectionIndex, out var items)) return 0;
            for (int i = 0; i < items.Count; i++)
            {
                var def = items[i].Definition;
                if (def.IsNote || def.IsSeparator) continue;
                if (!items[i].IsChecked) return i;
            }
            return -1;
        }

        public virtual void RecomputeCurrentItem()
        {
            CurrentItemIndex = FindFirstActionableItem(CurrentSectionIndex);
        }
    }
}
