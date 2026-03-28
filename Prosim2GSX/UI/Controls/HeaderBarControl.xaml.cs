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
                new PropertyMetadata("----"));

        public string FlightNumber
        {
            get => (string)GetValue(FlightNumberProperty);
            set => SetValue(FlightNumberProperty, value);
        }

        public string CurrentDateDisplay => DateTime.Now.ToString("dd MMM yyyy");

        private readonly DispatcherTimer _dateTimer;

        public HeaderBarControl()
        {
            InitializeComponent();

            _dateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(1)
            };
            _dateTimer.Tick += (_, _) =>
            {
                var binding = GetBindingExpression(FlightNumberProperty);
                binding?.UpdateTarget();
                // Notify CurrentDateDisplay changed by triggering a layout update
                InvalidateVisual();
            };
            _dateTimer.Start();
        }
    }
}
