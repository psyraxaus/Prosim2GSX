using Prosim2GSX.Models;
using Prosim2GSX.Services.Audio;
using Prosim2GSX.ViewModels.Base;
using Prosim2GSX.ViewModels.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using static Prosim2GSX.Services.Audio.AudioChannelConfig;

namespace Prosim2GSX.ViewModels.Components
{
    /// <summary>
    /// ViewModel representing a single audio channel configuration (INT, VHF1, etc.)
    /// </summary>
    public class AudioChannelViewModel : ViewModelBase
    {
        #region Fields

        private readonly ServiceModel _serviceModel;
        private readonly AudioChannel _channelType;
        private bool _volumeControlEnabled;
        private bool _latchMuteEnabled;
        private string _processName;
        private bool _isStripSelected = true;
        private List<KeyValuePair<string, string>> _voiceMeeterDevices;
        private string _selectedDeviceKey;
        private bool _isLoadingSettings;
        private readonly AudioSettingsViewModel _parentViewModel;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the name of this audio channel
        /// </summary>
        public string ChannelName => _channelType.ToString();

        /// <summary>
        /// Gets whether the Core Audio UI should be visible
        /// </summary>
        public bool ShowCoreAudioUI => _parentViewModel.IsCoreAudioSelected;

        /// <summary>
        /// Gets whether the VoiceMeeter UI should be visible
        /// </summary>
        public bool ShowVoiceMeeterUI => _parentViewModel.IsVoiceMeeterSelected;

        /// <summary>
        /// Notifies that visibility properties have changed
        /// </summary>
        public void RefreshVisibility()
        {
            // Update visibility-related properties
            OnPropertyChanged(nameof(ShowCoreAudioUI));
            OnPropertyChanged(nameof(ShowVoiceMeeterUI));
        }


        /// <summary>
        /// Gets or sets whether volume control is enabled for this channel
        /// </summary>
        public bool VolumeControlEnabled
        {
            get => _volumeControlEnabled;
            set
            {
                if (SetProperty(ref _volumeControlEnabled, value))
                {
                    // Save the setting
                    if (!_isLoadingSettings)
                    {
                        SaveVolumeControlSetting(value);
                    }

                    // Update dependent properties
                    OnPropertyChanged(nameof(ProcessNameEnabled));
                }
            }
        }

        /// <summary>
        /// Gets or sets whether latch mute is enabled for this channel
        /// </summary>
        public bool LatchMuteEnabled
        {
            get => _latchMuteEnabled;
            set
            {
                if (SetProperty(ref _latchMuteEnabled, value) && !_isLoadingSettings)
                {
                    SaveLatchMuteSetting(value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the process name for Core Audio control
        /// </summary>
        public string ProcessName
        {
            get => _processName;
            set
            {
                if (SetProperty(ref _processName, value))
                {
                    // Save the setting immediately
                    SaveProcessNameSetting(value);
                }
            }
        }


        /// <summary>
        /// Gets or sets whether process name field is enabled
        /// </summary>
        public bool ProcessNameEnabled => VolumeControlEnabled;

        /// <summary>
        /// Gets or sets whether the Strip (Input) radio button is selected
        /// </summary>
        public bool IsStripSelected
        {
            get => _isStripSelected;
            set
            {
                if (SetProperty(ref _isStripSelected, value) && !_isLoadingSettings)
                {
                    // Only save when selected (avoid double-save)
                    if (value)
                    {
                        SaveDeviceTypeSetting(VoiceMeeterDeviceType.Strip);
                        LoadVoiceMeeterDevices();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the Bus (Output) radio button is selected
        /// </summary>
        public bool IsBusSelected
        {
            get => !_isStripSelected;
            set
            {
                // Inverse of IsStripSelected
                if (value != !_isStripSelected)
                {
                    _isStripSelected = !value;
                    OnPropertyChanged(nameof(IsStripSelected));
                    OnPropertyChanged();

                    if (!_isLoadingSettings && value)
                    {
                        SaveDeviceTypeSetting(VoiceMeeterDeviceType.Bus);
                        LoadVoiceMeeterDevices();
                    }
                }
            }
        }

        /// <summary>
        /// Gets the collection of available VoiceMeeter devices
        /// </summary>
        public List<KeyValuePair<string, string>> VoiceMeeterDevices
        {
            get => _voiceMeeterDevices;
            private set => SetProperty(ref _voiceMeeterDevices, value);
        }

        /// <summary>
        /// Gets or sets the key of the currently selected VoiceMeeter device
        /// </summary>
        public string SelectedDeviceKey
        {
            get => _selectedDeviceKey;
            set
            {
                if (SetProperty(ref _selectedDeviceKey, value) && !_isLoadingSettings && !string.IsNullOrEmpty(value))
                {
                    SaveSelectedDeviceSetting(value);
                }
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command to refresh the list of VoiceMeeter devices
        /// </summary>
        public RelayCommand RefreshDevicesCommand { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new instance of AudioChannelViewModel
        /// </summary>
        /// <param name="serviceModel">The service model containing settings</param>
        /// <param name="channelType">The type of audio channel this represents</param>
        public AudioChannelViewModel(ServiceModel serviceModel, AudioChannel channelType, AudioSettingsViewModel parentViewModel)
        {
            _serviceModel = serviceModel ?? throw new ArgumentNullException(nameof(serviceModel));
            _channelType = channelType;
            _parentViewModel = parentViewModel ?? throw new ArgumentNullException(nameof(parentViewModel));

            // Initialize commands
            RefreshDevicesCommand = new RelayCommand(_ => LoadVoiceMeeterDevices());

            // Initialize the model from settings
            LoadSettings();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Loads the current settings for this audio channel
        /// </summary>
        public void LoadSettings()
        {
            _isLoadingSettings = true;

            try
            {
                switch (_channelType)
                {
                    case AudioChannel.INT:
                        VolumeControlEnabled = _serviceModel.GsxVolumeControl;
                        LatchMuteEnabled = _serviceModel.IntLatchMute;
                        ProcessName = _serviceModel.IntVolumeApp;
                        break;
                    case AudioChannel.VHF1:
                        VolumeControlEnabled = _serviceModel.Vhf1VolumeControl;
                        LatchMuteEnabled = _serviceModel.Vhf1LatchMute;
                        ProcessName = _serviceModel.Vhf1VolumeApp;
                        break;
                    case AudioChannel.VHF2:
                        VolumeControlEnabled = _serviceModel.Vhf2VolumeControl;
                        LatchMuteEnabled = _serviceModel.Vhf2LatchMute;
                        ProcessName = _serviceModel.Vhf2VolumeApp;
                        break;
                    case AudioChannel.VHF3:
                        VolumeControlEnabled = _serviceModel.Vhf3VolumeControl;
                        LatchMuteEnabled = _serviceModel.Vhf3LatchMute;
                        ProcessName = _serviceModel.Vhf3VolumeApp;
                        break;
                    case AudioChannel.HF1:
                        VolumeControlEnabled = _serviceModel.Hf1VolumeControl;
                        LatchMuteEnabled = _serviceModel.Hf1LatchMute;
                        ProcessName = _serviceModel.Hf1VolumeApp;
                        break;
                    case AudioChannel.HF2:
                        VolumeControlEnabled = _serviceModel.Hf2VolumeControl;
                        LatchMuteEnabled = _serviceModel.Hf2LatchMute;
                        ProcessName = _serviceModel.Hf2VolumeApp;
                        break;
                    case AudioChannel.CAB:
                        VolumeControlEnabled = _serviceModel.CabVolumeControl;
                        LatchMuteEnabled = _serviceModel.CabLatchMute;
                        ProcessName = _serviceModel.CabVolumeApp;
                        break;
                    case AudioChannel.PA:
                        VolumeControlEnabled = _serviceModel.PaVolumeControl;
                        LatchMuteEnabled = _serviceModel.PaLatchMute;
                        ProcessName = _serviceModel.PaVolumeApp;
                        break;
                }

                // Load VoiceMeeter device type
                if (_serviceModel.VoiceMeeterDeviceTypes.TryGetValue(_channelType, out var deviceType))
                {
                    IsStripSelected = deviceType == VoiceMeeterDeviceType.Strip;
                }

                // Load VoiceMeeter devices
                LoadVoiceMeeterDevices();

                // Load selected device
                if (_serviceModel.VoiceMeeterStrips.TryGetValue(_channelType, out var deviceKey))
                {
                    SelectedDeviceKey = deviceKey;
                }
            }
            finally
            {
                _isLoadingSettings = false;
            }
        }

        /// <summary>
        /// Loads the list of available VoiceMeeter devices
        /// </summary>
        public void LoadVoiceMeeterDevices()
        {
            try
            {
                var audioService = _serviceModel.GetAudioService();
                if (audioService == null || _serviceModel.AudioApiType != AudioApiType.VoiceMeeter)
                    return;

                if (!audioService.IsVoiceMeeterRunning())
                {
                    // Try to start VoiceMeeter
                    if (audioService.EnsureVoiceMeeterIsRunning())
                    {
                        // Wait for VoiceMeeter to initialize
                        System.Threading.Thread.Sleep(1000);
                    }
                    else
                    {
                        VoiceMeeterDevices = new List<KeyValuePair<string, string>>();
                        return;
                    }
                }

                // Get the appropriate list of devices
                if (IsStripSelected)
                {
                    VoiceMeeterDevices = audioService.GetAvailableVoiceMeeterStrips();
                }
                else
                {
                    VoiceMeeterDevices = audioService.GetAvailableVoiceMeeterBuses();
                }

                // Re-select the device if possible
                if (_serviceModel.VoiceMeeterStrips.TryGetValue(_channelType, out string deviceKey))
                {
                    if (VoiceMeeterDevices.Exists(d => d.Key == deviceKey))
                    {
                        SelectedDeviceKey = deviceKey;
                    }
                    else if (VoiceMeeterDevices.Count > 0)
                    {
                        SelectedDeviceKey = VoiceMeeterDevices[0].Key;
                        SaveSelectedDeviceSetting(SelectedDeviceKey);
                    }
                }
                else if (VoiceMeeterDevices.Count > 0)
                {
                    SelectedDeviceKey = VoiceMeeterDevices[0].Key;
                    SaveSelectedDeviceSetting(SelectedDeviceKey);
                }
            }
            catch (Exception ex)
            {
                // In a real app, we'd log this error
                System.Diagnostics.Debug.WriteLine($"Error loading VoiceMeeter devices: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves the volume control setting for this channel
        /// </summary>
        private void SaveVolumeControlSetting(bool enabled)
        {
            string settingKey = GetVolumeControlSettingKey();
            _serviceModel.SetSetting(settingKey, enabled.ToString().ToLower());
        }

        /// <summary>
        /// Saves the latch mute setting for this channel
        /// </summary>
        private void SaveLatchMuteSetting(bool enabled)
        {
            string settingKey = GetLatchMuteSettingKey();
            _serviceModel.SetSetting(settingKey, enabled.ToString().ToLower());
        }

        /// <summary>
        /// Saves the process name setting for this channel
        /// </summary>
        private void SaveProcessNameSetting(string processName)
        {
            string settingKey = GetProcessNameSettingKey();
            _serviceModel.SetSetting(settingKey, processName);
        }

        /// <summary>
        /// Saves the device type setting for this channel
        /// </summary>
        private void SaveDeviceTypeSetting(VoiceMeeterDeviceType deviceType)
        {
            _serviceModel.SetVoiceMeeterDeviceType(_channelType, deviceType);
        }

        /// <summary>
        /// Saves the selected device setting for this channel
        /// </summary>
        private void SaveSelectedDeviceSetting(string deviceKey)
        {
            // Get the label from the collection
            string deviceLabel = "";
            foreach (var device in VoiceMeeterDevices)
            {
                if (device.Key == deviceKey)
                {
                    deviceLabel = device.Value;
                    break;
                }
            }

            _serviceModel.SetVoiceMeeterStrip(_channelType, deviceKey, deviceLabel);
        }

        /// <summary>
        /// Gets the setting key for volume control based on channel type
        /// </summary>
        private string GetVolumeControlSettingKey()
        {
            return _channelType switch
            {
                AudioChannel.INT => "gsxVolumeControl",
                AudioChannel.VHF1 => "vhf1VolumeControl",
                AudioChannel.VHF2 => "vhf2VolumeControl",
                AudioChannel.VHF3 => "vhf3VolumeControl",
                AudioChannel.HF1 => "hf1VolumeControl",
                AudioChannel.HF2 => "hf2VolumeControl",
                AudioChannel.CAB => "cabVolumeControl",
                AudioChannel.PA => "paVolumeControl",
                _ => throw new NotImplementedException($"Unknown channel type: {_channelType}")
            };
        }

        /// <summary>
        /// Gets the setting key for latch mute based on channel type
        /// </summary>
        private string GetLatchMuteSettingKey()
        {
            return _channelType switch
            {
                AudioChannel.INT => "intLatchMute",
                AudioChannel.VHF1 => "vhf1LatchMute",
                AudioChannel.VHF2 => "vhf2LatchMute",
                AudioChannel.VHF3 => "vhf3LatchMute",
                AudioChannel.HF1 => "hf1LatchMute",
                AudioChannel.HF2 => "hf2LatchMute",
                AudioChannel.CAB => "cabLatchMute",
                AudioChannel.PA => "paLatchMute",
                _ => throw new NotImplementedException($"Unknown channel type: {_channelType}")
            };
        }

        /// <summary>
        /// Gets the setting key for process name based on channel type
        /// </summary>
        private string GetProcessNameSettingKey()
        {
            return _channelType switch
            {
                AudioChannel.INT => "intVolumeApp",
                AudioChannel.VHF1 => "vhf1VolumeApp",
                AudioChannel.VHF2 => "vhf2VolumeApp",
                AudioChannel.VHF3 => "vhf3VolumeApp",
                AudioChannel.HF1 => "hf1VolumeApp",
                AudioChannel.HF2 => "hf2VolumeApp",
                AudioChannel.CAB => "cabVolumeApp",
                AudioChannel.PA => "paVolumeApp",
                _ => throw new NotImplementedException($"Unknown channel type: {_channelType}")
            };
        }

        #endregion
    }
}
