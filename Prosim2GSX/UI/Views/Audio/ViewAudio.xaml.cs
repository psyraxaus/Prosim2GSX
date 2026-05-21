using CFIT.AppFramework.UI.ViewModels;
using CFIT.AppLogger;
using CFIT.AppTools;
using Microsoft.Win32;
using Prosim2GSX.AppConfig;
using Prosim2GSX.Audio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Prosim2GSX.UI.Views.Audio
{
    public partial class ViewAudio : UserControl, IView
    {
        protected virtual ModelAudio ViewModel { get; }
        protected virtual ViewModelSelector<AudioMapping, AudioMapping> ViewModelMappings { get; }
        protected virtual ViewModelSelector<VoiceMeeterMapping, VoiceMeeterMapping> ViewModelVoiceMeeterMappings { get; }
        protected virtual ViewModelSelector<string, string> ViewModelBlacklist { get; }
        public virtual bool HasSelection => GridAudioMappings?.SelectedIndex != -1;

        public ViewAudio()
        {
            InitializeComponent();
            ViewModel = new(AppService.Instance);
            this.DataContext = ViewModel;

            ViewModelMappings = new(GridAudioMappings, ViewModel.AppMappingCollection, AppWindow.IconLoader);
            ButtonAddMapping.Command = ViewModelMappings.BindAddUpdateButton(ButtonAddMapping, ImageAddMapping, GetMappingItem);
            ButtonRemoveMapping.Command = ViewModelMappings.BindRemoveButton(ButtonRemoveMapping);

            SelectorMappingChannel.ItemsSource = Enum.GetValues<AudioChannel>();
            ViewModelMappings.BindMember(SelectorMappingChannel, nameof(AudioMapping.Channel));

            ViewModelMappings.BindTextElement(InputMappingApp, nameof(AudioMapping.Binary));
            ViewModelMappings.AddUpdateCommand.Subscribe(InputMappingApp);

            SelectorMappingDevice.ItemsSource = ViewModel.AudioDevices;
            ViewModelMappings.BindMember(SelectorMappingDevice, nameof(AudioMapping.DeviceName));
            ViewModelMappings.AddUpdateCommand.Subscribe(SelectorMappingDevice);

            ViewModelMappings.BindMember(CheckboxMappingMute, nameof(AudioMapping.UseLatch));
            ViewModelMappings.AddUpdateCommand.Subscribe(CheckboxMappingMute);

            ViewModelMappings.BindMember(CheckboxOnlyActive, nameof(AudioMapping.OnlyActive));
            ViewModelMappings.AddUpdateCommand.Subscribe(CheckboxOnlyActive);

            GridAudioMappings.SizeChanged += OnGridSizeChanged;

            // VoiceMeeter mappings grid — channel + target combos drive the
            // ViewModelSelector that wraps Source.VoiceMeeterMappings.
            ViewModelVoiceMeeterMappings = new(GridVoiceMeeterMappings, ViewModel.VoiceMeeterMappingCollection, AppWindow.IconLoader);
            ButtonAddVmMapping.Command = ViewModelVoiceMeeterMappings.BindAddUpdateButton(ButtonAddVmMapping, ImageAddVmMapping, GetVoiceMeeterMappingItem);
            ButtonRemoveVmMapping.Command = ViewModelVoiceMeeterMappings.BindRemoveButton(ButtonRemoveVmMapping);

            SelectorVmChannel.ItemsSource = Enum.GetValues<AudioChannel>();
            ViewModelVoiceMeeterMappings.BindMember(SelectorVmChannel, nameof(VoiceMeeterMapping.Channel));
            ViewModelVoiceMeeterMappings.BindMember(SelectorVmTarget, nameof(VoiceMeeterMapping.TargetKey));
            ViewModelVoiceMeeterMappings.AddUpdateCommand.Subscribe(SelectorVmChannel);
            ViewModelVoiceMeeterMappings.AddUpdateCommand.Subscribe(SelectorVmTarget);
            ViewModelVoiceMeeterMappings.BindMember(CheckboxVmMute, nameof(VoiceMeeterMapping.UseLatch));
            ViewModelVoiceMeeterMappings.AddUpdateCommand.Subscribe(CheckboxVmMute);

            ViewModelBlacklist = new(ListDeviceBlacklist, ViewModel.BlacklistCollection, AppWindow.IconLoader);
            ButtonAddDevice.Command = ViewModelBlacklist.BindAddUpdateButton(ButtonAddDevice, ImageAddDevice, GetDeviceItem);
            ButtonRemoveDevice.Command = ViewModelBlacklist.BindRemoveButton(ButtonRemoveDevice);

            ViewModelBlacklist.BindTextElement(InputDevice);
            ViewModelBlacklist.AddUpdateCommand.Subscribe(InputDevice);

            CommandBinding copyCommandBinding = new(ApplicationCommands.Copy,
                            (_, _) => { InputDevice.Text = SelectorMappingDevice.SelectedValue as string; ViewModelBlacklist.AddUpdateCommand.NotifyCanExecuteChanged(); });
            CommandBindings.Add(copyCommandBinding);
            SelectorMappingDevice.CommandBindings.Add(copyCommandBinding);

            ListDeviceBlacklist.SizeChanged += OnListSizeChanged;

            InputMappingApp.KeyUp += InputMappingApp_KeyUp;
            ListActiveProcesses.SelectionChanged += OnProcessSelected;
            InputMappingApp.GotFocus += OnBinaryInputFocused;
            InputMappingApp.LostFocus += OnBinaryInputUnfocused;
        }

        protected virtual void OnGridSizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                int offset = 4;
                SelectorMappingChannel.Width = GridAudioMappings.Columns[0].ActualWidth - offset;
                InputMappingApp.Width = GridAudioMappings.Columns[1].ActualWidth - offset;
                ListActiveProcesses.Width = GridAudioMappings.Columns[1].ActualWidth - offset;
                SelectorMappingDevice.Width = GridAudioMappings.Columns[2].ActualWidth - offset;
                PanelMute.Width = GridAudioMappings.Columns[3].ActualWidth;
                PanelActive.Width = GridAudioMappings.Columns[4].ActualWidth;
            }
            catch { }
        }

        protected virtual void OnListSizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                InputDevice.Width = ListDeviceBlacklist.ActualWidth;
            }
            catch { }
        }

        protected virtual AudioMapping GetMappingItem()
        {
            try
            {
                if (SelectorMappingChannel?.SelectedValue is AudioChannel channel
                    && SelectorMappingDevice?.SelectedValue is string device && !string.IsNullOrWhiteSpace(device)
                    && !string.IsNullOrWhiteSpace(InputMappingApp?.Text)
                    && CheckboxMappingMute?.IsChecked is bool unmute
                    && CheckboxOnlyActive?.IsChecked is bool onlyActive)
                    return new AudioMapping(channel, (device == "All" ? "" : device), InputMappingApp?.Text, unmute, onlyActive);
            }
            catch { }

            return null;
        }

        protected virtual VoiceMeeterMapping GetVoiceMeeterMappingItem()
        {
            try
            {
                if (SelectorVmChannel?.SelectedValue is AudioChannel channel
                    && SelectorVmTarget?.SelectedValue is string key && !string.IsNullOrEmpty(key)
                    && CheckboxVmMute?.IsChecked is bool useLatch)
                {
                    var mapping = new VoiceMeeterMapping(channel, 0, false, useLatch) { TargetKey = key };
                    return mapping;
                }
            }
            catch { }
            return null;
        }

        protected virtual void ButtonReloadStrips_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.RefreshVoiceMeeterStrips();
        }

        protected virtual string GetDeviceItem()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(InputDevice?.Text))
                    return InputDevice?.Text;
            }
            catch { }

            return null;
        }

        // Suggestions come from the audio service's AudioSessionRegistry —
        // only processes that currently own a CoreAudio session, deduped by
        // ProcessName. Refreshed every audio service tick, so the typeahead
        // path does no COM work and no Process.GetProcesses() enumeration.
        // Inaccessible (elevated) entries are annotated " — elevated" inline;
        // OnProcessSelected strips that suffix before assigning to the input.
        private const string ElevatedSuffix = " — elevated";
        private const int MaxProcessSuggestions = 50;
        private CancellationTokenSource _processSearchCts;

        protected virtual async void OnBinaryInputFocused(object sender, RoutedEventArgs e)
        {
            ListActiveProcesses.Visibility = Visibility.Visible;
            await RefreshProcessListAsync(InputMappingApp?.Text, debounceMs: 0);
        }

        protected virtual async void InputMappingApp_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e)) return;
            await RefreshProcessListAsync(InputMappingApp?.Text, debounceMs: 150);
        }

        private async Task RefreshProcessListAsync(string query, int debounceMs)
        {
            _processSearchCts?.Cancel();
            var cts = new CancellationTokenSource();
            _processSearchCts = cts;
            var token = cts.Token;

            try
            {
                if (debounceMs > 0)
                    await Task.Delay(debounceMs, token);

                IReadOnlyList<global::Prosim2GSX.Audio.AudioSessionProcess> snapshot =
                    AppService.Instance?.AudioService?.SessionRegistry?.Snapshot
                    ?? (IReadOnlyList<global::Prosim2GSX.Audio.AudioSessionProcess>)Array.Empty<global::Prosim2GSX.Audio.AudioSessionProcess>();

                IEnumerable<global::Prosim2GSX.Audio.AudioSessionProcess> filtered = string.IsNullOrWhiteSpace(query)
                    ? snapshot
                    : snapshot.Where(p => p.ProcessName.Contains(query, StringComparison.InvariantCultureIgnoreCase));

                ListActiveProcesses.ItemsSource = filtered
                    .Take(MaxProcessSuggestions)
                    .Select(p => p.IsAccessible ? p.ProcessName : p.ProcessName + ElevatedSuffix)
                    .ToList();
            }
            catch (TaskCanceledException) { /* superseded by a newer keystroke */ }
            catch (Exception ex) { Logger.LogException(ex); }
        }

        protected virtual void OnProcessSelected(object sender, SelectionChangedEventArgs e)
        {
            if (ListActiveProcesses?.SelectedIndex != -1 && ListActiveProcesses?.SelectedValue is string str)
            {
                if (str.EndsWith(ElevatedSuffix, StringComparison.Ordinal))
                    str = str.Substring(0, str.Length - ElevatedSuffix.Length);
                InputMappingApp.Text = str;
                ListActiveProcesses.ItemsSource = null;
                ListActiveProcesses.Visibility = Visibility.Collapsed;
            }
        }

        protected virtual void OnBinaryInputUnfocused(object sender, RoutedEventArgs e)
        {
            ListActiveProcesses.Visibility = Visibility.Collapsed;
        }

        public virtual async void Start()
        {
            // GetDeviceNames() enumerates Core Audio COM endpoints — run off the UI thread
            // to avoid blocking when switching to this tab.
            var devices = await System.Threading.Tasks.Task.Run(() => ViewModel.AudioDevices);
            SelectorMappingDevice.ItemsSource = null;
            SelectorMappingDevice.ItemsSource = devices;
            if (SelectorMappingDevice.Items.Count > 0)
                SelectorMappingDevice.SelectedIndex = 0;

            // Refresh VoiceMeeter strip list so per-row combos are current.
            // Cheap when VoiceMeeter is disabled — returns early in the model.
            ViewModel.RefreshVoiceMeeterStrips();
        }

        protected virtual void ButtonBrowseVoiceMeeter_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select VoicemeeterRemote64.dll",
                Filter = "VoiceMeeter Remote (VoicemeeterRemote64.dll)|VoicemeeterRemote64.dll|DLL files (*.dll)|*.dll|All files (*.*)|*.*",
                DefaultExt = ".dll",
                CheckFileExists = true,
                CheckPathExists = true,
            };
            try
            {
                string current = ViewModel.VoiceMeeterDllPath;
                if (!string.IsNullOrEmpty(current) && File.Exists(current))
                    dialog.InitialDirectory = Path.GetDirectoryName(current);
            }
            catch { }

            if (dialog.ShowDialog() == true)
            {
                ViewModel.VoiceMeeterDllPath = dialog.FileName;
                ViewModel.RefreshVoiceMeeterStrips();
            }
        }

        public virtual void Stop()
        {
            
        }
    }
}
