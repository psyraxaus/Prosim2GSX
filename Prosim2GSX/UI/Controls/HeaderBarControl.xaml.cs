using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Prosim2GSX.UI
{
    public partial class HeaderBarControl : UserControl
    {
        public static readonly DependencyProperty FlightNumberProperty =
            DependencyProperty.Register(nameof(FlightNumber), typeof(string), typeof(HeaderBarControl),
                new PropertyMetadata("--------"));

        public static readonly DependencyProperty TimeDisplayProperty =
            DependencyProperty.Register(nameof(TimeDisplay), typeof(string), typeof(HeaderBarControl),
                new PropertyMetadata("--:--Z"));

        public static readonly DependencyProperty DateDisplayProperty =
            DependencyProperty.Register(nameof(DateDisplay), typeof(string), typeof(HeaderBarControl),
                new PropertyMetadata("------"));

        public string FlightNumber
        {
            get => (string)GetValue(FlightNumberProperty);
            set => SetValue(FlightNumberProperty, value);
        }

        public string TimeDisplay
        {
            get => (string)GetValue(TimeDisplayProperty);
            set => SetValue(TimeDisplayProperty, value);
        }

        public string DateDisplay
        {
            get => (string)GetValue(DateDisplayProperty);
            set => SetValue(DateDisplayProperty, value);
        }

        private readonly DispatcherTimer _updateTimer;
        private string _lastFlightNumber;
        private string _lastTimeDisplay;
        private string _lastDateDisplay;

        public HeaderBarControl()
        {
            InitializeComponent();

            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _updateTimer.Tick += OnUpdate;

            // The header lives permanently in AppWindow row 0, so without this
            // gating the 2 s timer would run — and re-animate the split-flap
            // cells every minute — for the entire process lifetime, including
            // the long stretches the window spends hidden to the system tray
            // (the dominant headless usage). While not rendering, the animation
            // work piled onto a UI message pump that never drained, exhausting
            // the per-thread PostMessage quota (the recurring crash). Run the
            // timer only while the control is actually visible.
            IsVisibleChanged += OnVisibilityChanged;
            Unloaded += OnUnloaded;
            if (IsVisible)
                _updateTimer.Start();
        }

        private void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible)
            {
                // Refresh immediately so the header is current the instant the
                // window is restored, instead of showing stale values until
                // the next 2 s tick.
                OnUpdate(this, EventArgs.Empty);
                _updateTimer.Start();
            }
            else
            {
                _updateTimer.Stop();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _updateTimer.Stop();
        }

        private void OnUpdate(object sender, EventArgs e)
        {
            try
            {
                var appService = AppService.Instance;
                var simConnect = appService?.SimConnect;
                var gsxService = appService?.GsxService;
                var aircraft = gsxService?.AircraftInterface;

                // Flight number — only update if changed
                string flightNum = !string.IsNullOrWhiteSpace(aircraft?.FlightNumber)
                    ? aircraft.FlightNumber
                    : "--------";
                if (flightNum != _lastFlightNumber)
                {
                    _lastFlightNumber = flightNum;
                    FlightNumber = flightNum;
                }

                // Time: sim zulu time when connected, system UTC otherwise — only update if changed
                string timeStr;
                bool simConnected = simConnect?.IsSimConnected == true && simConnect?.IsSessionRunning == true;
                if (simConnected && aircraft != null)
                {
                    int zuluSec = aircraft.ZuluTimeSeconds;
                    int hours = (zuluSec / 3600) % 24;
                    int minutes = (zuluSec % 3600) / 60;
                    timeStr = $"{hours:D2}:{minutes:D2}Z";
                }
                else
                {
                    timeStr = DateTime.UtcNow.ToString("HH:mm") + "Z";
                }
                if (timeStr != _lastTimeDisplay)
                {
                    _lastTimeDisplay = timeStr;
                    TimeDisplay = timeStr;
                }

                // Date — only update if changed
                string dateStr = DateTime.UtcNow.ToString("dd MMM").ToUpper();
                if (dateStr != _lastDateDisplay)
                {
                    _lastDateDisplay = dateStr;
                    DateDisplay = dateStr;
                }
            }
            catch
            {
                // AppService may not be ready yet during startup
            }
        }
    }
}
