using CFIT.AppFramework.UI.ViewModels;
using CFIT.AppLogger;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Prosim2GSX.AppConfig;
using Prosim2GSX.Checklists;
using Prosim2GSX.GSX;
using Prosim2GSX.State;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace Prosim2GSX.UI.Views.Checklists
{
    // Adapter view-model over the long-lived ChecklistState store. The store
    // owns the actual definition + per-item runtime; this Model surfaces the
    // pieces the WPF tab binds to, plus commands for RESET / C/L COMPLETE /
    // ITEM CLICK. The web layer subscribes directly to ChecklistState — this
    // class is WPF-only.
    public partial class ModelChecklist : ViewModelBase<AppService>, IView
    {
        protected virtual AppService AppService => Source;
        protected virtual ChecklistState State => AppService?.Checklist;
        protected virtual IChecklistService Service => AppService?.ChecklistService;
        protected virtual GsxController GsxController => AppService?.GsxService;
        protected virtual AircraftProfile AircraftProfile => GsxController?.AircraftProfile;

        public virtual List<string> AvailableChecklists => State?.AvailableChecklists ?? new List<string>();

        public virtual string CurrentChecklistName
        {
            get => State?.CurrentChecklistName ?? "";
            set
            {
                if (State == null || string.IsNullOrWhiteSpace(value)) return;
                if (value == State.CurrentChecklistName) return;
                LoadChecklistByName(value);
                if (AircraftProfile != null)
                {
                    AircraftProfile.ChecklistName = value;
                    try { AppService?.Config?.SaveConfiguration(); } catch (Exception ex) { Logger.LogException(ex); }
                }
                NotifyPropertyChanged(nameof(CurrentChecklistName));
            }
        }

        public virtual List<ChecklistSection> Sections => State?.Definition?.Sections ?? new List<ChecklistSection>();

        public virtual ChecklistSection CurrentSection
        {
            get
            {
                if (State?.Definition?.Sections == null) return null;
                int idx = State.CurrentSectionIndex;
                if (idx < 0 || idx >= State.Definition.Sections.Count) return null;
                return State.Definition.Sections[idx];
            }
            set
            {
                if (State?.Definition?.Sections == null || value == null) return;
                int idx = State.Definition.Sections.IndexOf(value);
                if (idx < 0 || idx == State.CurrentSectionIndex) return;
                State.CurrentSectionIndex = idx;
                State.RecomputeCurrentItem();
                NotifyPropertyChanged(nameof(CurrentSection));
                NotifyPropertyChanged(nameof(CurrentItems));
            }
        }

        public virtual List<ChecklistItemView> CurrentItems
        {
            get
            {
                var list = new List<ChecklistItemView>();
                if (State == null) return list;
                if (!State.ItemsBySection.TryGetValue(State.CurrentSectionIndex, out var items)) return list;
                int currentIdx = State.CurrentItemIndex;
                for (int i = 0; i < items.Count; i++)
                    list.Add(new ChecklistItemView(items[i], i == currentIdx));
                return list;
            }
        }

        public IRelayCommand ResetCommand { get; }
        public IRelayCommand CompleteCommand { get; }
        public IRelayCommand<ChecklistItemView> ItemClickCommand { get; }
        public IRelayCommand FocusSectionDropdownCommand { get; }

        public event Action FocusSectionDropdownRequested;

        public ModelChecklist(AppService appService) : base(appService)
        {
            ResetCommand = new RelayCommand(OnReset);
            CompleteCommand = new RelayCommand(OnComplete);
            ItemClickCommand = new RelayCommand<ChecklistItemView>(OnItemClick);
            FocusSectionDropdownCommand = new RelayCommand(() => FocusSectionDropdownRequested?.Invoke());
        }

        protected override void InitializeModel()
        {
            if (State != null)
                State.PropertyChanged += OnStateChanged;
        }

        private bool _started;

        public virtual void Start()
        {
            if (_started) return;
            _started = true;

            try
            {
                if (State != null)
                {
                    var available = Service?.GetAvailableChecklists() ?? new List<string>();
                    State.AvailableChecklists = new List<string>(available);

                    if (State.Definition == null)
                    {
                        var name = AircraftProfile?.ChecklistName;
                        if (string.IsNullOrWhiteSpace(name))
                            name = Service?.DefaultChecklistName;
                        LoadChecklistByName(name);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            NotifyPropertyChanged(nameof(AvailableChecklists));
            NotifyPropertyChanged(nameof(CurrentChecklistName));
            NotifyPropertyChanged(nameof(Sections));
            NotifyPropertyChanged(nameof(CurrentSection));
            NotifyPropertyChanged(nameof(CurrentItems));
        }

        public virtual void Stop()
        {
            // Keep subscriptions alive — the store is long-lived and the tab
            // can be re-entered. Mirroring ModelMonitor's pattern.
        }

        protected virtual void LoadChecklistByName(string name)
        {
            try
            {
                var def = Service?.LoadChecklist(name);
                if (def == null)
                {
                    Logger.Warning($"ModelChecklist: failed to load '{name}'");
                    return;
                }
                State?.LoadDefinition(def, name);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual void OnStateChanged(object sender, PropertyChangedEventArgs e)
        {
            var dispatcher = Application.Current?.Dispatcher;
            Action notify = e?.PropertyName switch
            {
                nameof(ChecklistState.Definition) => () =>
                {
                    NotifyPropertyChanged(nameof(Sections));
                    NotifyPropertyChanged(nameof(CurrentSection));
                    NotifyPropertyChanged(nameof(CurrentItems));
                },
                nameof(ChecklistState.CurrentSectionIndex) => () =>
                {
                    NotifyPropertyChanged(nameof(CurrentSection));
                    NotifyPropertyChanged(nameof(CurrentItems));
                },
                nameof(ChecklistState.CurrentItemIndex) => () => NotifyPropertyChanged(nameof(CurrentItems)),
                nameof(ChecklistState.AvailableChecklists) => () => NotifyPropertyChanged(nameof(AvailableChecklists)),
                nameof(ChecklistState.CurrentChecklistName) => () => NotifyPropertyChanged(nameof(CurrentChecklistName)),
                _ => null,
            };
            if (notify == null) return;
            if (dispatcher != null && !dispatcher.CheckAccess())
                dispatcher.BeginInvoke(notify);
            else
                notify();
        }

        protected virtual void OnReset()
        {
            State?.ResetSection(State.CurrentSectionIndex);
            NotifyPropertyChanged(nameof(CurrentItems));
        }

        protected virtual void OnComplete()
        {
            if (State == null) return;
            if (!State.ItemsBySection.TryGetValue(State.CurrentSectionIndex, out var items)) return;
            foreach (var it in items)
            {
                if (it.Definition.IsNote || it.Definition.IsSeparator) continue;
                if (!it.IsChecked) it.IsChecked = true;
            }
            // Advance to next section if available.
            if (State.Definition?.Sections != null && State.CurrentSectionIndex < State.Definition.Sections.Count - 1)
            {
                State.CurrentSectionIndex++;
                State.RecomputeCurrentItem();
            }
            NotifyPropertyChanged(nameof(CurrentSection));
            NotifyPropertyChanged(nameof(CurrentItems));
        }

        protected virtual void OnItemClick(ChecklistItemView view)
        {
            if (view?.Runtime == null) return;
            var def = view.Runtime.Definition;
            // Manual items only — dataref-driven items flip via the worker.
            if (def.IsNote || def.IsSeparator) return;
            if (!string.IsNullOrWhiteSpace(def.DataRef)) return;

            view.Runtime.IsChecked = !view.Runtime.IsChecked;
            State?.RecomputeCurrentItem();
            NotifyPropertyChanged(nameof(CurrentItems));
        }
    }

    // Lightweight projection of a ChecklistItemRuntime + "is current pointer"
    // flag, used as the DataTemplateSelector input. Recreated per CurrentItems
    // read so the IsCurrentItem flag stays in sync without per-item INPC.
    public class ChecklistItemView
    {
        public ChecklistItemRuntime Runtime { get; }
        public bool IsCurrentItem { get; }
        public ChecklistItem Definition => Runtime?.Definition;
        public bool IsChecked => Runtime?.IsChecked ?? false;
        public bool IsNote => Runtime?.Definition?.IsNote ?? false;
        public bool IsSeparator => Runtime?.Definition?.IsSeparator ?? false;
        public string Label => Runtime?.Definition?.Label ?? "";
        public string Value => Runtime?.Definition?.Value ?? "";
        public bool HasValue => !string.IsNullOrWhiteSpace(Value);

        public ChecklistItemView(ChecklistItemRuntime runtime, bool isCurrent)
        {
            Runtime = runtime;
            IsCurrentItem = isCurrent;
        }
    }
}
