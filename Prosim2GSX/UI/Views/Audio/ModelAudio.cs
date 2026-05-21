using CommunityToolkit.Mvvm.Input;
using CoreAudio;
using Prosim2GSX.AppConfig;
using Prosim2GSX.Audio;
using Prosim2GSX.State;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Prosim2GSX.UI.Views.Audio
{
    public partial class ModelAudio : ModelBase<Config>
    {
        public ICommand CommandDebugInfo { get; } = new RelayCommand(() => AppService.Instance.AudioService.DeviceManager.WriteDebugInformation());

        // Audio backend selection lives on the long-lived AudioState store so the
        // future web layer can observe and toggle the same value. The setter just
        // writes through; INPC re-raise on this Model happens via the AudioState
        // PropertyChanged subscription wired in InitializeModel.
        protected virtual AudioState AudioState => AppService.Instance.Audio;

        public virtual bool IsCoreAudioSelected
        {
            get => !Source.UseVoiceMeeter;
            set
            {
                if (Source.UseVoiceMeeter == !value) return;
                Source.UseVoiceMeeter = !value;
                AudioState.IsCoreAudioSelected = value;
                Source.SaveConfiguration();
                NotifyPropertyChanged(nameof(IsCoreAudioSelected));
                NotifyPropertyChanged(nameof(IsVoiceMeeterSelected));
                NotifyPropertyChanged(nameof(UseVoiceMeeter));
                NotifyPropertyChanged(nameof(HasVoiceMeeterWarning));
                NotifyPropertyChanged(nameof(VoiceMeeterWarning));
                if (Source.UseVoiceMeeter) RefreshVoiceMeeterStrips();
            }
        }

        public virtual bool IsVoiceMeeterSelected
        {
            get => Source.UseVoiceMeeter;
            set => IsCoreAudioSelected = !value;
        }

        // Convenience for XAML bindings that want the positive sense.
        public virtual bool UseVoiceMeeter
        {
            get => Source.UseVoiceMeeter;
            set => IsCoreAudioSelected = !value;
        }

        public virtual string VoiceMeeterDllPath
        {
            get => Source.VoiceMeeterDllPath;
            set
            {
                if (Source.VoiceMeeterDllPath == value) return;
                Source.VoiceMeeterDllPath = value ?? "";
                Source.SaveConfiguration();
                NotifyPropertyChanged(nameof(VoiceMeeterDllPath));
                NotifyPropertyChanged(nameof(HasVoiceMeeterWarning));
                NotifyPropertyChanged(nameof(VoiceMeeterWarning));
                // If we're already in VoiceMeeter mode, kick a re-login on the
                // next audio service tick by toggling the service state.
                try { AudioController.VoiceMeeter?.Logout(); } catch { }
            }
        }

        public virtual ObservableCollection<VoiceMeeterStrip> AvailableStrips { get; } = new();

        public virtual string VoiceMeeterWarning
        {
            get
            {
                if (!Source.UseVoiceMeeter) return string.Empty;
                // Use IsLoaded (DLL loaded + Login OK) rather than IsAvailable
                // — the latter is false during a CoreAudio-mode suspend even
                // though the DLL is fine, which would falsely warn the user.
                if (AudioController?.VoiceMeeter?.IsLoaded == true) return string.Empty;
                if (string.IsNullOrWhiteSpace(Source.VoiceMeeterDllPath))
                    return "VoiceMeeter integration is enabled but no DLL path is configured. Browse to VoicemeeterRemote64.dll below.";
                return "VoiceMeeter is not running or the Remote API DLL was not found.";
            }
        }

        public virtual bool HasVoiceMeeterWarning => !string.IsNullOrEmpty(VoiceMeeterWarning);

        public virtual void RefreshVoiceMeeterStrips()
        {
            try
            {
                var vm = AudioController?.VoiceMeeter;
                if (vm == null) return;
                if (!vm.IsLoaded && !string.IsNullOrWhiteSpace(Source.VoiceMeeterDllPath))
                    vm.Login(Source.VoiceMeeterDllPath);

                var fresh = vm.IsLoaded ? vm.GetStrips() : Array.Empty<VoiceMeeterStrip>();
                AvailableStrips.Clear();
                foreach (var s in fresh) AvailableStrips.Add(s);

                NotifyPropertyChanged(nameof(HasVoiceMeeterWarning));
                NotifyPropertyChanged(nameof(VoiceMeeterWarning));
            }
            catch (Exception)
            {
                // Logger already covers internal failures; banner will reflect
                // the lack of strips on next read.
            }
        }

        public ModelAudio(AppService appService) : base(appService.Config, appService)
        {
            AppMappingCollection = new(this);
            AppMappingCollection.CollectionChanged += OnMappingCollectionChanged;
            AppMappingCollection.CollectionChanged += (_, _) => { SaveConfig(); AudioController.ResetMappings = true; };

            VoiceMeeterMappingCollection = new(this);
            VoiceMeeterMappingCollection.CollectionChanged += (_, _) =>
            {
                SaveConfig();
                AudioController.ResetVoiceMeeterBindings = true;
            };

            BlacklistCollection = new(this);
            BlacklistCollection.CollectionChanged += (_, _) => SaveConfig();

            // Subscribe to PropertyChanged on every existing mapping so the
            // banner re-evaluates when AudioSession.SetStatus writes a Status
            // value. Done in the ctor (after AppMappingCollection exists) —
            // InitializeModel runs from the base ctor before this point.
            HookMappings(Source.AudioMappings);
        }

        protected override void InitializeModel()
        {
            AudioState.PropertyChanged += OnAudioStateChanged;
            AudioController.DeviceManager.DevicesChanged += () => NotifyPropertyChanged(nameof(AudioDevices));
        }

        protected virtual void OnAudioStateChanged(object? sender, PropertyChangedEventArgs e)
        {
            // External change (e.g. web POST) — re-raise so the WPF radios
            // and dependent VoiceMeeter UI re-evaluate.
            if (e?.PropertyName == nameof(AudioState.IsCoreAudioSelected))
            {
                NotifyPropertyChanged(nameof(IsCoreAudioSelected));
                NotifyPropertyChanged(nameof(IsVoiceMeeterSelected));
                NotifyPropertyChanged(nameof(UseVoiceMeeter));
                NotifyPropertyChanged(nameof(HasVoiceMeeterWarning));
                NotifyPropertyChanged(nameof(VoiceMeeterWarning));
            }
        }

        public virtual Dictionary<AcpSide, string> AcpSideOptions { get; } = new()
        {
            { AcpSide.CPT, "Captain" },
            { AcpSide.FO, "First Officer" },
        };

        public virtual AcpSide AudioAcpSide
        {
            get => Source.AudioAcpSide;
            set
            {
                SetModelValue<AcpSide>(value);
                // Rebind subscriptions to the newly selected side and force a
                // resynch on the next tick so newly subscribed datarefs push
                // their current values into CoreAudio sessions / VoiceMeeter.
                AudioController.ResetMappings = true;
                AudioController.ResetVolumes = true;
                AudioController.ResetVoiceMeeterBindings = true;
            }
        }

        public virtual ModelAppMappings AppMappingCollection { get; }

        public virtual ModelVoiceMeeterMappings VoiceMeeterMappingCollection { get; }

        public virtual List<string> AudioDevices
        {
            get
            {
                var list = new List<string> { "All" };
                list.AddRange([.. AudioController.DeviceManager.GetDeviceNames()]);

                return list;
            }
        }

        public virtual ModelDeviceBlacklist BlacklistCollection { get; }

        public virtual Dictionary<DataFlow, string> DeviceDataFlows { get; } = new()
        {
            { DataFlow.Render, DataFlow.Render.ToString() },
            { DataFlow.Capture, DataFlow.Capture.ToString() },
            { DataFlow.All, DataFlow.All.ToString() },
        };
        public virtual DataFlow AudioDeviceFlow { get => Source.AudioDeviceFlow; set { SetModelValue<DataFlow>(value); AudioController.ResetMappings = true; } }

        public virtual Dictionary<DeviceState, string> DeviceStates { get; } = new()
        {
            { DeviceState.Active, DeviceState.Active.ToString() },
            { DeviceState.Disabled, DeviceState.Disabled.ToString() },
            { DeviceState.NotPresent, DeviceState.NotPresent.ToString() },
            { DeviceState.Unplugged, DeviceState.Unplugged.ToString() },
            { DeviceState.MaskAll, DeviceState.MaskAll.ToString() },
        };
        public virtual DeviceState AudioDeviceState { get => Source.AudioDeviceState; set { SetModelValue<DeviceState>(value); AudioController.ResetMappings = true; } }

        // Aggregates AudioMapping.Status across all mappings into a banner.
        // AudioSession.SetStatus marshals to the dispatcher before writing,
        // so the PropertyChanged we listen to here always arrives on the UI
        // thread; no extra marshaling needed in OnMappingPropertyChanged.

        public virtual bool HasMappingWarnings =>
            Source.AudioMappings.Any(m => !string.IsNullOrEmpty(m.Status));

        public virtual string MappingWarningText
        {
            get
            {
                var binaries = Source.AudioMappings
                    .Where(m => !string.IsNullOrEmpty(m.Status))
                    .Select(m => m.Binary)
                    .Distinct(StringComparer.InvariantCultureIgnoreCase)
                    .ToArray();
                if (binaries.Length == 0) return string.Empty;
                return $"Elevated process(es) detected: {string.Join(", ", binaries)}. " +
                       "Run Prosim2GSX as administrator to control these apps — " +
                       "otherwise these mappings are inactive.";
            }
        }

        private void OnMappingCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (AudioMapping m in e.OldItems) m.PropertyChanged -= OnMappingPropertyChanged;
            if (e.NewItems != null)
                foreach (AudioMapping m in e.NewItems) m.PropertyChanged += OnMappingPropertyChanged;
            RaiseBannerChanged();
        }

        private void HookMappings(IEnumerable<AudioMapping> mappings)
        {
            if (mappings == null) return;
            foreach (var m in mappings)
            {
                m.PropertyChanged -= OnMappingPropertyChanged;
                m.PropertyChanged += OnMappingPropertyChanged;
            }
        }

        private void OnMappingPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AudioMapping.Status) || e.PropertyName == nameof(AudioMapping.HasStatus))
                RaiseBannerChanged();
        }

        private void RaiseBannerChanged()
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                NotifyPropertyChanged(nameof(HasMappingWarnings));
                NotifyPropertyChanged(nameof(MappingWarningText));
            }
            else
            {
                dispatcher.BeginInvoke(new Action(() =>
                {
                    NotifyPropertyChanged(nameof(HasMappingWarnings));
                    NotifyPropertyChanged(nameof(MappingWarningText));
                }));
            }
        }
    }
}
