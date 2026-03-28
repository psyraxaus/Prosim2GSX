using CFIT.AppLogger;
using Prosim2GSX.AppConfig;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace Prosim2GSX.Themes
{
    public class ThemeManager
    {
        private static ThemeManager _instance;
        public static ThemeManager Instance => _instance ??= new ThemeManager();

        private Config _config;
        private string _currentTheme = "Light";
        private readonly Dictionary<string, Theme> _themes = new();

        private ThemeManager() { }

        public void SetConfig(Config config) => _config = config;

        public string CurrentTheme => _currentTheme;
        public IEnumerable<string> AvailableThemes => _themes.Keys;

        public void Initialize()
        {
            try
            {
                // Load default resource dictionary before applying themes
                LoadThemeResources();

                var themesDir = GetThemesDirectory();
                if (!Directory.Exists(themesDir))
                    Directory.CreateDirectory(themesDir);

                LoadThemesFromDirectory(themesDir);

                var savedTheme = _config?.CurrentTheme ?? "Light";
                if (_themes.ContainsKey(savedTheme))
                    ApplyTheme(savedTheme);
                else if (_themes.Count > 0)
                    ApplyTheme(_themes.Keys.First());
                else
                    ApplyDefaultColors();
            }
            catch (Exception ex)
            {
                try { Logger.LogException(ex); } catch { }
                ApplyDefaultColors();
            }
        }

        private static void LoadThemeResources()
        {
            try
            {
                var uri = new Uri("pack://application:,,,/ThemeResources.xaml", UriKind.Absolute);
                var dict = new ResourceDictionary { Source = uri };
                Application.Current.Resources.MergedDictionaries.Add(dict);
            }
            catch (Exception ex)
            {
                try { Logger.LogException(ex); } catch { }
            }
        }

        private static string GetThemesDirectory()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Themes");
        }

        public void LoadThemesFromDirectory(string directory)
        {
            _themes.Clear();
            if (!Directory.Exists(directory)) return;

            foreach (var file in Directory.GetFiles(directory, "*.json"))
            {
                try
                {
                    var raw = File.ReadAllText(file);
                    var cleaned = RemoveJsonComments(raw);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var theme = JsonSerializer.Deserialize<Theme>(cleaned, options);
                    if (theme?.Name != null)
                        _themes[theme.Name] = theme;
                }
                catch (Exception ex)
                {
                    try { Logger.LogException(ex); } catch { }
                }
            }
        }

        private static string RemoveJsonComments(string json)
        {
            var lines = json.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var idx = lines[i].IndexOf("//", StringComparison.Ordinal);
                if (idx >= 0)
                    lines[i] = lines[i].Substring(0, idx);
            }
            return string.Join('\n', lines);
        }

        public void ApplyTheme(string themeName)
        {
            if (_config == null || !_themes.ContainsKey(themeName)) return;
            _currentTheme = themeName;
            if (_config.CurrentTheme != themeName)
            {
                _config.CurrentTheme = themeName;
                _config.SaveConfiguration();
            }
            ApplyThemeToResources();
        }

        private void ApplyDefaultColors()
        {
            ApplyThemeColorsToResources(new ThemeColors());
        }

        private void ApplyThemeToResources()
        {
            if (!_themes.TryGetValue(_currentTheme, out var theme)) return;
            ApplyThemeColorsToResources(theme.Colors);
        }

        private static void ApplyThemeColorsToResources(ThemeColors colors)
        {
            var app = Application.Current;
            if (app == null) return;

            void SetBrush(string key, Color color)
            {
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                app.Resources[key] = brush;
            }

            // Theme-controlled brushes
            SetBrush("PrimaryColor",        colors.GetPrimaryColor());
            SetBrush("SecondaryColor",      colors.GetSecondaryColor());
            SetBrush("AccentColor",         colors.GetAccentColor());
            SetBrush("HeaderBackground",    colors.GetHeaderBackground());
            SetBrush("ContentBackground",   colors.GetContentBackground());
            SetBrush("SectionBackground",   colors.GetSectionBackground());
            SetBrush("HeaderText",          colors.GetHeaderText());
            SetBrush("ContentText",         colors.GetContentText());
            SetBrush("CategoryText",        colors.GetCategoryText());
            SetBrush("TabBarBackground",    colors.GetTabBarBackground());

            // Flight phase brushes
            var p = colors.FlightPhaseColors;
            SetBrush("AtGatePhase",     p.GetAtGateColor());
            SetBrush("TaxiOutPhase",    p.GetTaxiOutColor());
            SetBrush("InFlightPhase",   p.GetInFlightColor());
            SetBrush("ApproachPhase",   p.GetApproachColor());
            SetBrush("ArrivedPhase",    p.GetArrivedColor());

            // Status brushes — hardcoded, same across all themes
            SetBrush("ActiveStatus",        Colors.Green);
            SetBrush("CompletedStatus",     Colors.Gold);
            SetBrush("WaitingStatus",       Color.FromRgb(0x1E, 0x90, 0xFF));
            SetBrush("DisconnectedStatus",  Colors.Red);
            SetBrush("InactiveStatus",      Colors.LightGray);

            // Legacy aliases for backward compatibility
            SetBrush("PrimaryColorBrush",       colors.GetPrimaryColor());
            SetBrush("HeaderBackgroundBrush",   colors.GetHeaderBackground());
            SetBrush("ContentBackgroundBrush",  colors.GetContentBackground());
            SetBrush("CategoryTextBrush",       colors.GetCategoryText());
            SetBrush("StatusActiveBrush",       Colors.Green);
            SetBrush("StatusCompletedBrush",    Colors.Gold);
            SetBrush("StatusWaitingBrush",      Color.FromRgb(0x1E, 0x90, 0xFF));
            SetBrush("StatusDisconnectedBrush", Colors.Red);
            SetBrush("StatusInactiveBrush",     Colors.LightGray);

            // Derive input/button background colors from SectionBackground.
            // No extra JSON keys needed: darker themes get lighter inputs, light themes get
            // slightly darker inputs so they are always distinguishable from the card behind them.
            var contentText = colors.GetContentText();
            var sectionBg   = colors.GetSectionBackground();
            var contentBg   = colors.GetContentBackground();

            int avg = (sectionBg.R + sectionBg.G + sectionBg.B) / 3;
            bool isDark = avg < 128;

            Color Shift(Color c, int d) => Color.FromRgb(
                (byte)Math.Max(0, Math.Min(255, c.R + d)),
                (byte)Math.Max(0, Math.Min(255, c.G + d)),
                (byte)Math.Max(0, Math.Min(255, c.B + d)));

            var inputBg  = Shift(sectionBg, isDark ? 22 : -15);   // slightly raised from card
            var buttonBg = Shift(sectionBg, isDark ? 12 : -28);   // slightly lowered from card
            var borderCol = isDark
                ? Color.FromRgb(85, 85, 85)
                : Color.FromRgb(171, 173, 179);

            SetBrush("InputBackground",  inputBg);
            SetBrush("ButtonBackground", buttonBg);
            SetBrush("InputBorderBrush", borderCol);

            // Override WPF SystemColors so controls whose templates reference them
            // (popup backgrounds, scrollbar tracks, etc.) also pick up theme colors.
            SolidColorBrush Frozen(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }

            app.Resources[SystemColors.WindowBrushKey]          = Frozen(inputBg);
            app.Resources[SystemColors.WindowTextBrushKey]      = Frozen(contentText);
            app.Resources[SystemColors.ControlBrushKey]         = Frozen(buttonBg);
            app.Resources[SystemColors.ControlTextBrushKey]     = Frozen(contentText);
            app.Resources[SystemColors.ControlLightBrushKey]    = Frozen(contentBg);
            // Popup / menu backgrounds (ComboBox dropdown, ContextMenu, tray icon menu)
            app.Resources[SystemColors.MenuBrushKey]              = Frozen(inputBg);
            app.Resources[SystemColors.MenuTextBrushKey]          = Frozen(contentText);
            app.Resources[SystemColors.MenuHighlightBrushKey]     = Frozen(colors.GetPrimaryColor());
            app.Resources[SystemColors.MenuBarBrushKey]           = Frozen(inputBg);
            // ComboBox toggle-button chrome (closed-state arrow button area)
            app.Resources[SystemColors.ControlLightLightBrushKey] = Frozen(inputBg);
            app.Resources[SystemColors.ControlDarkBrushKey]       = Frozen(borderCol);
            app.Resources[SystemColors.ControlDarkDarkBrushKey]   = Frozen(borderCol);
            // Highlight / selection colours
            app.Resources[SystemColors.HighlightBrushKey]         = Frozen(colors.GetPrimaryColor());
            app.Resources[SystemColors.HighlightTextBrushKey]     = Frozen(colors.GetHeaderText());
        }

        public void RefreshThemes()
        {
            LoadThemesFromDirectory(GetThemesDirectory());
            if (_themes.ContainsKey(_currentTheme))
                ApplyTheme(_currentTheme);
            else if (_themes.Count > 0)
                ApplyTheme(_themes.Keys.First());
        }
    }
}
