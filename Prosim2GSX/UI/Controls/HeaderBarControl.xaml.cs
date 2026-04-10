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

        public HeaderBarControl()
        {
            InitializeComponent();

            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _updateTimer.Tick += OnUpdate;
            _updateTimer.Start();
        }

        private void OnUpdate(object sender, EventArgs e)
        {
            try
            {
                var appService = AppService.Instance;
                var simConnect = appService?.SimConnect;
                var gsxService = appService?.GsxService;
                var aircraft = gsxService?.AircraftInterface;

                // Flight number
                string flightNum = aircraft?.FlightNumber;
                FlightNumber = !string.IsNullOrWhiteSpace(flightNum) ? flightNum : "--------";

                // Time: sim zulu time when connected, system UTC otherwise
                bool simConnected = simConnect?.IsSimConnected == true && simConnect?.IsSessionRunning == true;
                if (simConnected && aircraft != null)
                {
                    int zuluSec = aircraft.ZuluTimeSeconds;
                    int hours = (zuluSec / 3600) % 24;
                    int minutes = (zuluSec % 3600) / 60;
                    TimeDisplay = $"{hours:D2}:{minutes:D2}Z";
                }
                else
                {
                    TimeDisplay = DateTime.UtcNow.ToString("HH:mm") + "Z";
                }

                // Date
                DateDisplay = DateTime.UtcNow.ToString("dd MMM").ToUpper();
            }
            catch
            {
                // AppService may not be ready yet during startup
            }
        }
    }
}
