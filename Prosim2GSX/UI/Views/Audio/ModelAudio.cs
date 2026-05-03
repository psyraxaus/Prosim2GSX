using CommunityToolkit.Mvvm.Input;
using CoreAudio;
using Prosim2GSX.AppConfig;
using Prosim2GSX.Audio;
using Prosim2GSX.State;
using System.Collections.Generic;
using System.ComponentModel;
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
            get => AudioState.IsCoreAudioSelected;
            set => AudioState.IsCoreAudioSelected = value;
        }

        public virtual bool IsVoiceMeeterSelected
        {
            get => !AudioState.IsCoreAudioSelected;
            set => AudioState.IsCoreAudioSelected = !value;
        }

        public ModelAudio(AppService appService) : base(appService.Config, appService)
        {
            AppMappingCollection = new(this);
            AppMappingCollection.CollectionChanged += (_, _) => { SaveConfig(); AudioController.ResetMappings = true; };

            BlacklistCollection = new(this);
            BlacklistCollection.CollectionChanged += (_, _) => SaveConfig();
        }

        protected override void InitializeModel()
        {
            AudioState.PropertyChanged += OnAudioStateChanged;
            AudioController.DeviceManager.DevicesChanged += () => NotifyPropertyChanged(nameof(AudioDevices));
        }

        protected virtual void OnAudioStateChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e?.PropertyName == nameof(AudioState.IsCoreAudioSelected))
            {
                NotifyPropertyChanged(nameof(IsCoreAudioSelected));
                NotifyPropertyChanged(nameof(IsVoiceMeeterSelected));
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
                // their current values into CoreAudio sessions.
                AudioController.ResetMappings = true;
                AudioController.ResetVolumes = true;
            }
        }

        public virtual ModelAppMappings AppMappingCollection { get; }

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
    }
}
