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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace Prosim2GSX.UI.Views.Checklists
{
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
                RebuildCurrentItems();
                NotifyPropertyChanged(nameof(CurrentSection));
                NotifyPropertyChanged(nameof(IsSectionComplete));
            }
        }

        // Stable collection bound by the WPF ItemsControl. We MUTATE this in
        // place (rather than recreating a fresh List on every read) so per-item
        // INPC propagates correctly and the visual tree is reused. Rebuilding
        // the source wholesale was the root cause of "click off / on to refresh".
        public ObservableCollection<ChecklistItemView> CurrentItems { get; } = new();

        public virtual bool IsSectionComplete
        {
            get
            {
                if (State == null) return false;
                if (!State.ItemsBySection.TryGetValue(State.CurrentSectionIndex, out var items)) return false;
                for (int i = 0; i < items.Count; i++)
                {
                    var def = items[i].Definition;
                    if (def == null || def.IsNote || def.IsSeparator) continue;
                    if (!items[i].IsChecked) return false;
                }
                return items.Count > 0;
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

            RebuildCurrentItems();
            NotifyPropertyChanged(nameof(AvailableChecklists));
            NotifyPropertyChanged(nameof(CurrentChecklistName));
            NotifyPropertyChanged(nameof(Sections));
            NotifyPropertyChanged(nameof(CurrentSection));
            NotifyPropertyChanged(nameof(IsSectionComplete));
        }

        public virtual void Stop()
        {
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
                    RebuildCurrentItems();
                    NotifyPropertyChanged(nameof(IsSectionComplete));
                },
                nameof(ChecklistState.CurrentSectionIndex) => () =>
                {
                    NotifyPropertyChanged(nameof(CurrentSection));
                    RebuildCurrentItems();
                    NotifyPropertyChanged(nameof(IsSectionComplete));
                },
                nameof(ChecklistState.CurrentItemIndex) => () =>
                {
                    UpdateCurrentItemFlags();
                    NotifyPropertyChanged(nameof(IsSectionComplete));
                },
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

        // Per-item runtime change (e.g., IsChecked flip from worker). Forwards
        // to IsSectionComplete recompute and lets the per-item view notify the
        // template selector via its own INPC.
        protected virtual void OnItemRuntimeChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e?.PropertyName == nameof(ChecklistItemRuntime.IsChecked))
            {
                var dispatcher = Application.Current?.Dispatcher;
                Action notify = () => NotifyPropertyChanged(nameof(IsSectionComplete));
                if (dispatcher != null && !dispatcher.CheckAccess())
                    dispatcher.BeginInvoke(notify);
                else
                    notify();
            }
        }

        private void DetachItemHandlers()
        {
            for (int i = 0; i < CurrentItems.Count; i++)
                CurrentItems[i].Detach(OnItemRuntimeChanged);
            CurrentItems.Clear();
        }

        private void RebuildCurrentItems()
        {
            DetachItemHandlers();
            if (State == null) return;
            if (!State.ItemsBySection.TryGetValue(State.CurrentSectionIndex, out var items)) return;
            int currentIdx = State.CurrentItemIndex;
            for (int i = 0; i < items.Count; i++)
            {
                var view = new ChecklistItemView(items[i], i == currentIdx);
                view.Attach(OnItemRuntimeChanged);
                CurrentItems.Add(view);
            }
        }

        private void UpdateCurrentItemFlags()
        {
            if (State == null) return;
            int currentIdx = State.CurrentItemIndex;
            for (int i = 0; i < CurrentItems.Count; i++)
                CurrentItems[i].IsCurrentItem = (i == currentIdx);
        }

        protected virtual void OnReset()
        {
            State?.ResetSection(State.CurrentSectionIndex);
            NotifyPropertyChanged(nameof(IsSectionComplete));
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
            if (State.Definition?.Sections != null && State.CurrentSectionIndex < State.Definition.Sections.Count - 1)
            {
                State.CurrentSectionIndex++;
                State.RecomputeCurrentItem();
            }
            NotifyPropertyChanged(nameof(CurrentSection));
            NotifyPropertyChanged(nameof(IsSectionComplete));
        }

        protected virtual void OnItemClick(ChecklistItemView view)
        {
            if (view?.Runtime == null) return;
            var def = view.Runtime.Definition;
            if (def.IsNote || def.IsSeparator) return;

            bool isManual = string.IsNullOrWhiteSpace(def.DataRef)
                            && (def.DataRefs == null || def.DataRefs.Count == 0);
            bool overrideAllowed = AppService?.Config?.AllowManualChecklistOverride ?? false;
            if (!isManual && !overrideAllowed) return;

            view.Runtime.IsChecked = !view.Runtime.IsChecked;
            State?.RecomputeCurrentItem();
            NotifyPropertyChanged(nameof(IsSectionComplete));
        }
    }

    // INPC projection of (runtime + isCurrent flag). Subscribes to runtime
    // PropertyChanged so IsChecked flips in the underlying store propagate to
    // the WPF binding without rebuilding the parent collection.
    public class ChecklistItemView : ObservableObject
    {
        public ChecklistItemRuntime Runtime { get; }

        private bool _isCurrentItem;
        public bool IsCurrentItem
        {
            get => _isCurrentItem;
            set => SetProperty(ref _isCurrentItem, value);
        }

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
            _isCurrentItem = isCurrent;
            if (Runtime != null)
                Runtime.PropertyChanged += OnRuntimeChanged;
        }

        public void Attach(PropertyChangedEventHandler externalHandler)
        {
            if (Runtime == null || externalHandler == null) return;
            Runtime.PropertyChanged += externalHandler;
        }

        public void Detach(PropertyChangedEventHandler externalHandler)
        {
            if (Runtime != null)
            {
                Runtime.PropertyChanged -= OnRuntimeChanged;
                if (externalHandler != null)
                    Runtime.PropertyChanged -= externalHandler;
            }
        }

        private void OnRuntimeChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e?.PropertyName == nameof(ChecklistItemRuntime.IsChecked))
            {
                OnPropertyChanged(nameof(IsChecked));
            }
        }
    }
}
