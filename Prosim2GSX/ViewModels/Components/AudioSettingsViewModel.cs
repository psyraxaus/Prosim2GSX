using Prosim2GSX.Models;
using Prosim2GSX.Services.Audio;
using Prosim2GSX.ViewModels.Base;
using Prosim2GSX.ViewModels.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace Prosim2GSX.ViewModels.Components
{
    /// <summary>
    /// ViewModel for the Audio Settings panel
    /// </summary>
    public class AudioSettingsViewModel : ViewModelBase
    {
        #region Fields

        private readonly ServiceModel _serviceModel;
        private bool _isCoreAudioSelected;
        private bool _isLoadingSettings;
        private bool _isRunningDiagnostics;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets whether Core Audio API is selected
        /// </summary>
        public bool IsCoreAudioSelected
        {
            get => _isCoreAudioSelected;
            set
            {
                if (SetProperty(ref _isCoreAudioSelected, value) && !_isLoadingSettings)
                {
                    AudioApiType apiType = value ? AudioApiType.CoreAudio : AudioApiType.VoiceMeeter;
                    _serviceModel.SetSetting("audioApiType", apiType.ToString());

                    // Update the UI
                    OnPropertyChanged(nameof(IsVoiceMeeterSelected));
                    OnPropertyChanged(nameof(ShowVoiceMeeterDiagnostics));

                    // Update channel views for each channel
                    foreach (var channel in AudioChannels)
                    {
                        channel.RefreshVisibility();
                        channel.LoadSettings();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets whether VoiceMeeter API is selected
        /// </summary>
        public bool IsVoiceMeeterSelected
        {
            get => !_isCoreAudioSelected;
            set
            {
                if (value != !_isCoreAudioSelected)
                {
                    _isCoreAudioSelected = !value;
                    OnPropertyChanged(nameof(IsCoreAudioSelected));
                    OnPropertyChanged();

                    if (!_isLoadingSettings)
                    {
                        AudioApiType apiType = !_isCoreAudioSelected ? AudioApiType.VoiceMeeter : AudioApiType.CoreAudio;
                        _serviceModel.SetSetting("audioApiType", apiType.ToString());

                        // Update the UI
                        OnPropertyChanged(nameof(ShowVoiceMeeterDiagnostics));

                        // Update channel views for each channel
                        foreach (var channel in AudioChannels)
                        {
                            channel.LoadSettings();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets whether to show VoiceMeeter diagnostics panel
        /// </summary>
        public bool ShowVoiceMeeterDiagnostics => !IsCoreAudioSelected;

        /// <summary>
        /// Gets whether diagnostics are currently running
        /// </summary>
        public bool IsRunningDiagnostics
        {
            get => _isRunningDiagnostics;
            private set => SetProperty(ref _isRunningDiagnostics, value);
        }

        /// <summary>
        /// Gets the collection of audio channel ViewModels
        /// </summary>
        public List<AudioChannelViewModel> AudioChannels { get; }

        /// <summary>
        /// Gets the VHF1 channel ViewModel
        /// </summary>
        public AudioChannelViewModel Vhf1Channel => AudioChannels[0];

        /// <summary>
        /// Gets the VHF2 channel ViewModel
        /// </summary>
        public AudioChannelViewModel Vhf2Channel => AudioChannels[1];

        /// <summary>
        /// Gets the VHF3 channel ViewModel
        /// </summary>
        public AudioChannelViewModel Vhf3Channel => AudioChannels[2];

        /// <summary>
        /// Gets the HF1 channel ViewModel
        /// </summary>
        public AudioChannelViewModel Hf1Channel => AudioChannels[3];

        /// <summary>
        /// Gets the HF2 channel ViewModel
        /// </summary>
        public AudioChannelViewModel Hf2Channel => AudioChannels[4];

        /// <summary>
        /// Gets the CAB channel ViewModel
        /// </summary>
        public AudioChannelViewModel CabChannel => AudioChannels[5];

        /// <summary>
        /// Gets the INT channel ViewModel
        /// </summary>
        public AudioChannelViewModel IntChannel => AudioChannels[6];

        /// <summary>
        /// Gets the PA channel ViewModel
        /// </summary>
        public AudioChannelViewModel PaChannel => AudioChannels[7];

        #endregion

        #region Commands

        /// <summary>
        /// Command to run VoiceMeeter diagnostics
        /// </summary>
        public RelayCommand RunDiagnosticsCommand { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new instance of AudioSettingsViewModel
        /// </summary>
        /// <param name="serviceModel">The service model</param>
        public AudioSettingsViewModel(ServiceModel serviceModel)
        {
            _serviceModel = serviceModel ?? throw new ArgumentNullException(nameof(serviceModel));

            // In AudioSettingsViewModel.cs constructor
            AudioChannels = new List<AudioChannelViewModel>
            {
                new AudioChannelViewModel(serviceModel, AudioChannel.VHF1, this),
                new AudioChannelViewModel(serviceModel, AudioChannel.VHF2, this),
                new AudioChannelViewModel(serviceModel, AudioChannel.VHF3, this),
                new AudioChannelViewModel(serviceModel, AudioChannel.HF1, this),
                new AudioChannelViewModel(serviceModel, AudioChannel.HF2, this),
                new AudioChannelViewModel(serviceModel, AudioChannel.CAB, this),
                new AudioChannelViewModel(serviceModel, AudioChannel.INT, this),
                new AudioChannelViewModel(serviceModel, AudioChannel.PA, this)
            };


            // Initialize commands
            RunDiagnosticsCommand = new RelayCommand(_ => RunVoiceMeeterDiagnostics(), _ => !IsRunningDiagnostics);

            // Load settings
            LoadSettings();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Loads settings from the service model
        /// </summary>
        public void LoadSettings()
        {
            _isLoadingSettings = true;
            try
            {
                // Set audio API type
                IsCoreAudioSelected = _serviceModel.AudioApiType == AudioApiType.CoreAudio;

                // Load channel settings
                foreach (var channel in AudioChannels)
                {
                    channel.LoadSettings();
                }
            }
            finally
            {
                _isLoadingSettings = false;
            }
        }

        /// <summary>
        /// Runs VoiceMeeter diagnostics and displays the results
        /// </summary>
        private async void RunVoiceMeeterDiagnostics()
        {
            // Check if VoiceMeeter API is selected
            if (_serviceModel.AudioApiType != AudioApiType.VoiceMeeter)
            {
                MessageBox.Show(
                    "VoiceMeeter API must be selected to run diagnostics. Please select VoiceMeeter API first.",
                    "VoiceMeeter API Not Selected",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            IsRunningDiagnostics = true;
            RunDiagnosticsCommand.RaiseCanExecuteChanged();

            try
            {
                bool success = false;

                // Run diagnostics on a background thread
                await Task.Run(() => {
                    var serviceController = IPCManager.ServiceController;
                    if (serviceController != null)
                    {
                        success = serviceController.PerformVoiceMeeterDiagnostics();
                    }
                });

                // Show a message box with the result
                if (success)
                {
                    MessageBox.Show(
                        "VoiceMeeter diagnostics completed successfully. Check the log for details.",
                        "Diagnostics Successful",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        "VoiceMeeter diagnostics completed with errors. Check the log for details.",
                        "Diagnostics Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error running VoiceMeeter diagnostics: {ex.Message}",
                    "Diagnostics Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsRunningDiagnostics = false;
                RunDiagnosticsCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion
    }
}
