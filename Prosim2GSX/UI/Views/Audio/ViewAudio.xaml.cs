using CFIT.AppFramework.UI.ViewModels;
using CFIT.AppLogger;
using CFIT.AppTools;
using Prosim2GSX.AppConfig;
using Prosim2GSX.Audio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        // Process.GetProcesses() enumerates every process on the system and
        // can take 100s of ms on a busy box (MSFS / ProSim / plugins). Plus
        // the returned Process objects each hold an OS handle until GC, so
        // calling it on every keystroke leaks handles and locks up the UI.
        // Strategy: enumerate once on focus, cache the resulting NAMES for a
        // few seconds, and let every keystroke just filter that cached list
        // in memory. Keystrokes touch the dispatcher only to update the
        // ListView's ItemsSource with the (capped) filtered set.
        private CancellationTokenSource _processSearchCts;
        private List<string> _processNamesCache;
        private DateTime _processNamesCachedAt = DateTime.MinValue;
        private static readonly TimeSpan ProcessCacheTtl = TimeSpan.FromSeconds(3);
        private const int MaxProcessSuggestions = 50;

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

                if (_processNamesCache == null || DateTime.UtcNow - _processNamesCachedAt > ProcessCacheTtl)
                {
                    var names = await Task.Run(EnumerateProcessNames, token);
                    if (token.IsCancellationRequested) return;
                    _processNamesCache = names;
                    _processNamesCachedAt = DateTime.UtcNow;
                }

                IEnumerable<string> filtered = string.IsNullOrWhiteSpace(query)
                    ? _processNamesCache
                    : _processNamesCache.Where(n => n.Contains(query, StringComparison.InvariantCultureIgnoreCase));

                ListActiveProcesses.ItemsSource = filtered.Take(MaxProcessSuggestions).ToList();
            }
            catch (TaskCanceledException) { /* superseded by a newer keystroke */ }
            catch (Exception ex) { Logger.LogException(ex); }
        }

        private static List<string> EnumerateProcessNames()
        {
            var seen = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            var result = new List<string>();
            Process[] procs;
            try { procs = Process.GetProcesses(); }
            catch (Exception ex) { Logger.LogException(ex); return result; }

            try
            {
                foreach (var p in procs)
                {
                    try
                    {
                        if (seen.Add(p.ProcessName))
                            result.Add(p.ProcessName);
                    }
                    catch { }
                }
            }
            finally
            {
                // Dispose the Process handles eagerly — otherwise each one
                // sits in the finalizer queue holding an OS handle until GC.
                foreach (var p in procs)
                {
                    try { p.Dispose(); } catch { }
                }
            }

            result.Sort(StringComparer.InvariantCultureIgnoreCase);
            return result;
        }

        protected virtual void OnProcessSelected(object sender, SelectionChangedEventArgs e)
        {
            if (ListActiveProcesses?.SelectedIndex != -1 && ListActiveProcesses?.SelectedValue is string str)
            {
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
        }

        public virtual void Stop()
        {
            
        }
    }
}
