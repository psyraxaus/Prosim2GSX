using CommunityToolkit.Mvvm.Input;
using Prosim2GSX.AppConfig;
using Prosim2GSX.Themes;
using ProsimInterface;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Threading;

namespace Prosim2GSX.UI.Views.Settings
{
    public partial class ModelSettings : ModelBase<Config>
    {
        protected virtual DispatcherTimer UnitUpdateTimer { get; }
        public virtual ModelSavedFuelCollection ModelSavedFuel { get; }
        public virtual Dictionary<DisplayUnit, string> DisplayUnitDefaultItems { get; } = new()
        {
            { DisplayUnit.KG, "kg" },
            { DisplayUnit.LB, "lb" },
        };
        public virtual Dictionary<DisplayUnitSource, string> DisplayUnitSourceItems { get; } = new()
        {
            { DisplayUnitSource.App, "App" },
            { DisplayUnitSource.Aircraft, "Aircraft" },
        };

        public ModelSettings(AppService appService) : base(appService.Config, appService)
        {
            UnitUpdateTimer = new()
            {
                Interval = TimeSpan.FromMilliseconds(100),
            };
            UnitUpdateTimer.Tick += UnitUpdateTimer_Tick;

            ModelSavedFuel = new();
            ModelSavedFuel.CollectionChanged += OnSavedFuelCollectionChanged;
        }

        protected override void InitializeModel()
        {
            Config.PropertyChanged += OnConfigPropertyChanged;
            LoadThemes();
        }

        protected virtual void OnSavedFuelCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Config.SaveConfiguration();
        }

        protected virtual void OnConfigPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if ((e?.PropertyName == nameof(Config.DisplayUnitCurrent) || e?.PropertyName == nameof(Config.DisplayUnitCurrentString))
                && !UnitUpdateTimer.IsEnabled)
                UnitUpdateTimer.Start();
            if (e?.PropertyName == nameof(Config.WebServerAuthToken))
                NotifyPropertyChanged(nameof(WebServerAuthToken));
        }

        protected virtual void UnitUpdateTimer_Tick(object? sender, EventArgs e)
        {
            InhibitConfigSave = true;
            NotifyPropertyChanged(nameof(DisplayUnitCurrentString));
            NotifyPropertyChanged(nameof(ProsimWeightBag));
            NotifyPropertyChanged(nameof(FuelResetDefaultKg));
            NotifyPropertyChanged(nameof(FuelCompareVariance));
            ModelSavedFuel.NotifyCollectionChanged();
            InhibitConfigSave = false;
            UnitUpdateTimer.Stop();
        }

        public virtual string DisplayUnitCurrentString => Config.DisplayUnitCurrentString;
        public virtual DisplayUnit DisplayUnitDefault { get => Source.DisplayUnitDefault; set { SetModelValue<DisplayUnit>(value); Config.EvaluateDisplayUnit(); } }
        public virtual DisplayUnitSource DisplayUnitSource { get => Source.DisplayUnitSource; set { SetModelValue<DisplayUnitSource>(value); Config.EvaluateDisplayUnit(); } }
        public virtual double ProsimWeightBag { get => Config.ConvertKgToDisplayUnit(Source.ProsimWeightBag); set => SetModelValue<double>(Config.ConvertFromDisplayUnitKg(value)); }
        public virtual double FuelResetDefaultKg { get => Config.ConvertKgToDisplayUnit(Source.FuelResetDefaultKg); set => SetModelValue<double>(Config.ConvertFromDisplayUnitKg(value)); }
        public virtual double FuelCompareVariance { get => Config.ConvertKgToDisplayUnit(Source.FuelCompareVariance); set => SetModelValue<double>(Config.ConvertFromDisplayUnitKg(value)); }
        public virtual bool FuelRoundUp100 { get => Source.FuelRoundUp100; set => SetModelValue<bool>(value); }
        public virtual bool DingOnStartup { get => Source.DingOnStartup; set => SetModelValue<bool>(value); }
        public virtual bool DingOnFinal { get => Source.DingOnFinal; set => SetModelValue<bool>(value); }
        public virtual int CargoPercentChangePerSec { get => Source.CargoPercentChangePerSec; set => SetModelValue<int>(value); }
        public virtual int DoorCargoDelay { get => Source.DoorCargoDelay; set => SetModelValue<int>(value); }
        public virtual int DoorCargoOpenDelay { get => Source.DoorCargoOpenDelay; set => SetModelValue<int>(value); }
        // Not used in Prosim
        //public virtual int RefuelPanelOpenDelay { get => Source.RefuelPanelOpenDelay; set => SetModelValue<int>(value); }
        //public virtual int RefuelPanelCloseDelay { get => Source.RefuelPanelCloseDelay; set => SetModelValue<int>(value); }
        public virtual bool ResetGsxStateVarsFlight { get => Source.ResetGsxStateVarsFlight; set => SetModelValue<bool>(value); }
        public virtual bool RestartGsxOnTaxiIn { get => Source.RestartGsxOnTaxiIn; set => SetModelValue<bool>(value); }
        public virtual bool RestartGsxStartupFail { get => Source.RestartGsxStartupFail; set => SetModelValue<bool>(value); }
        public virtual int GsxMenuStartupMaxFail { get => Source.GsxMenuStartupMaxFail; set => SetModelValue<int>(value); }
        public virtual bool RunGsxService { get => Source.RunGsxService; set => SetModelValue<bool>(value); }
        public virtual bool RunAudioService { get => Source.RunAudioService; set => SetModelValue<bool>(value); }
        public virtual bool UseSayIntentions { get => Source.UseSayIntentions; set => SetModelValue<bool>(value); }
        public virtual bool OpenAppWindowOnStart { get => Source.OpenAppWindowOnStart; set => SetModelValue<bool>(value); }
        public virtual string ProSimSdkPath { get => Source.ProSimSdkPath; set => SetModelValue<string>(value); }
        public virtual bool SolariAnimationEnabled { get => Source.SolariAnimationEnabled; set => SetModelValue<bool>(value); }

        // ── Web interface (hot-toggled; WebHostService observes Config) ──────
        //
        // Config's INPC is not raised by the auto-property setters that
        // SetModelValue / SetSourceValue write through (Config only fires
        // PropertyChanged from its own NotifyPropertyChanged, which is
        // currently only used for DisplayUnitCurrent). For the WebHostService
        // subscription to react to runtime changes, each setter explicitly
        // raises Source.NotifyPropertyChanged after the write — otherwise the
        // hot-toggle silently does nothing until the next app launch.

        public virtual bool WebServerEnabled
        {
            get => Source.WebServerEnabled;
            set
            {
                SetModelValue<bool>(value);
                Source.NotifyPropertyChanged(nameof(Source.WebServerEnabled));
            }
        }

        public virtual int WebServerPort
        {
            get => Source.WebServerPort;
            set
            {
                SetModelValue<int>(value);
                Source.NotifyPropertyChanged(nameof(Source.WebServerPort));
            }
        }

        public virtual bool WebServerBindAll
        {
            get => Source.WebServerBindAll;
            set
            {
                SetModelValue<bool>(value);
                Source.NotifyPropertyChanged(nameof(Source.WebServerBindAll));
            }
        }

        // Token is read-only on the bound surface — regeneration goes through
        // the dedicated command so the host can also kick existing clients.
        public virtual string WebServerAuthToken => Source.WebServerAuthToken ?? "";

        [RelayCommand]
        private void RegenerateWebToken()
        {
            try
            {
                AppService.Instance?.WebHost?.RegenerateToken();
                NotifyPropertyChanged(nameof(WebServerAuthToken));
            }
            catch { }
        }

        // ── Theme selection ────────────────────────────────────────────────

        private int _selectedThemeIndex = 0;
        private string _selectedThemeName = "Light";
        private readonly ObservableCollection<string> _themes = [];
        private readonly string _themesPath =
            Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppDomain.CurrentDomain.BaseDirectory,
                "Themes");

        public ObservableCollection<string> Themes => _themes;
        public string SelectedThemeName => _selectedThemeName;
        public string ThemesPath => _themesPath;

        public int SelectedThemeIndex
        {
            get => _selectedThemeIndex;
            set
            {
                if (value < 0 || value >= _themes.Count) return;
                _selectedThemeIndex = value;
                _selectedThemeName = _themes[value];
                NotifyPropertyChanged(nameof(SelectedThemeIndex));
                NotifyPropertyChanged(nameof(SelectedThemeName));
                ThemeManager.Instance.ApplyTheme(_selectedThemeName);
            }
        }

        [RelayCommand]
        private void OpenThemeFolder()
        {
            try
            {
                if (Directory.Exists(_themesPath))
                    Process.Start(new ProcessStartInfo("explorer.exe", _themesPath) { UseShellExecute = true });
            }
            catch { }
        }

        [RelayCommand]
        private void RefreshThemes()
        {
            ThemeManager.Instance.RefreshThemes();
            LoadThemes();
        }

        public void LoadThemes()
        {
            _themes.Clear();
            foreach (var name in ThemeManager.Instance.AvailableThemes)
                _themes.Add(name);

            var saved = Source.CurrentTheme ?? "Light";
            var idx = _themes.IndexOf(saved);
            if (idx < 0 && _themes.Count > 0)
                idx = 0;
            if (idx >= 0)
            {
                _selectedThemeIndex = idx;
                _selectedThemeName = _themes[idx];
                NotifyPropertyChanged(nameof(SelectedThemeIndex));
                NotifyPropertyChanged(nameof(SelectedThemeName));
            }
        }
    }
}
