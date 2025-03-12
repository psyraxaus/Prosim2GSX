using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Prosim2GSX.UI.EFB.Controls.Aircraft
{
    /// <summary>
    /// Interaction logic for ServicePointControl.xaml
    /// </summary>
    public partial class ServicePointControl : UserControl
    {
        private Storyboard _connectAnimation;
        private Storyboard _disconnectAnimation;

        #region Dependency Properties

        public static readonly DependencyProperty IsConnectedProperty =
            DependencyProperty.Register("IsConnected", typeof(bool), typeof(ServicePointControl),
                new PropertyMetadata(false, OnIsConnectedChanged));

        public bool IsConnected
        {
            get { return (bool)GetValue(IsConnectedProperty); }
            set { SetValue(IsConnectedProperty, value); }
        }

        public static readonly DependencyProperty ServicePointNameProperty =
            DependencyProperty.Register("ServicePointName", typeof(string), typeof(ServicePointControl),
                new PropertyMetadata("Service Point"));

        public string ServicePointName
        {
            get { return (string)GetValue(ServicePointNameProperty); }
            set { SetValue(ServicePointNameProperty, value); }
        }

        public static readonly DependencyProperty ServiceTypeProperty =
            DependencyProperty.Register("ServiceType", typeof(string), typeof(ServicePointControl),
                new PropertyMetadata(""));

        public string ServiceType
        {
            get { return (string)GetValue(ServiceTypeProperty); }
            set { SetValue(ServiceTypeProperty, value); }
        }

        public static readonly DependencyProperty ServiceCommandProperty =
            DependencyProperty.Register("ServiceCommand", typeof(ICommand), typeof(ServicePointControl),
                new PropertyMetadata(null));

        public ICommand ServiceCommand
        {
            get { return (ICommand)GetValue(ServiceCommandProperty); }
            set { SetValue(ServiceCommandProperty, value); }
        }

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(object), typeof(ServicePointControl),
                new PropertyMetadata(null));

        public object Icon
        {
            get { return GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        public static readonly DependencyProperty ConnectedColorProperty =
            DependencyProperty.Register("ConnectedColor", typeof(Color), typeof(ServicePointControl),
                new PropertyMetadata(Colors.Green));

        public Color ConnectedColor
        {
            get { return (Color)GetValue(ConnectedColorProperty); }
            set { SetValue(ConnectedColorProperty, value); }
        }

        public static readonly DependencyProperty DisconnectedColorProperty =
            DependencyProperty.Register("DisconnectedColor", typeof(Color), typeof(ServicePointControl),
                new PropertyMetadata(Colors.Gray));

        public Color DisconnectedColor
        {
            get { return (Color)GetValue(DisconnectedColorProperty); }
            set { SetValue(DisconnectedColorProperty, value); }
        }

        public static readonly DependencyProperty IsHighlightedProperty =
            DependencyProperty.Register("IsHighlighted", typeof(bool), typeof(ServicePointControl),
                new PropertyMetadata(false, OnIsHighlightedChanged));

        public bool IsHighlighted
        {
            get { return (bool)GetValue(IsHighlightedProperty); }
            set { SetValue(IsHighlightedProperty, value); }
        }

        public static readonly DependencyProperty ProgressProperty =
            DependencyProperty.Register("Progress", typeof(double), typeof(ServicePointControl),
                new PropertyMetadata(0.0, OnProgressChanged));

        public double Progress
        {
            get { return (double)GetValue(ProgressProperty); }
            set { SetValue(ProgressProperty, value); }
        }

        #endregion

        public ServicePointControl()
        {
            InitializeComponent();
            
            // Get animations from resources
            _connectAnimation = (Storyboard)FindResource("ConnectAnimation");
            _disconnectAnimation = (Storyboard)FindResource("DisconnectAnimation");

            // Add event handlers
            this.MouseEnter += ServicePointControl_MouseEnter;
            this.MouseLeave += ServicePointControl_MouseLeave;
            this.MouseLeftButtonDown += ServicePointControl_MouseLeftButtonDown;

            // Set initial state
            UpdateVisualState();
        }

        #region Event Handlers

        private static void OnIsConnectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ServicePointControl)d;
            bool isConnected = (bool)e.NewValue;

            if (isConnected)
            {
                control._connectAnimation.Begin();
            }
            else
            {
                control._disconnectAnimation.Begin();
            }
        }

        private static void OnIsHighlightedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ServicePointControl)d;
            bool isHighlighted = (bool)e.NewValue;

            // Update the highlight overlay opacity
            control.HighlightOverlay.Opacity = isHighlighted ? 0.3 : 0;
        }

        private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ServicePointControl)d;
            double progress = (double)e.NewValue;

            // Update the pulse circle based on progress
            if (control.IsConnected && progress > 0)
            {
                // Adjust pulse rate based on progress
                var pulseAnimation = control._connectAnimation.Children[2] as DoubleAnimation;
                if (pulseAnimation != null)
                {
                    // Faster pulse as progress increases
                    TimeSpan duration = TimeSpan.FromSeconds(0.8 - (progress * 0.5));
                    pulseAnimation.Duration = duration;
                }
            }
        }

        private void ServicePointControl_MouseEnter(object sender, MouseEventArgs e)
        {
            // Show highlight effect
            HighlightOverlay.Opacity = 0.3;
        }

        private void ServicePointControl_MouseLeave(object sender, MouseEventArgs e)
        {
            // Hide highlight effect if not highlighted
            if (!IsHighlighted)
            {
                HighlightOverlay.Opacity = 0;
            }
        }

        private void ServicePointControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Execute the service command if available
            if (ServiceCommand != null && ServiceCommand.CanExecute(ServiceType))
            {
                ServiceCommand.Execute(ServiceType);
            }
        }

        #endregion

        private void UpdateVisualState()
        {
            // Set initial visual state based on IsConnected
            if (IsConnected)
            {
                ServicePointCircle.Fill = new SolidColorBrush(ConnectedColor);
                ConnectorPath.Opacity = 1;
                
                // Start pulse animation if connected
                var pulseAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 0.7,
                    Duration = TimeSpan.FromSeconds(0.5),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                };
                PulseCircle.BeginAnimation(OpacityProperty, pulseAnimation);
            }
            else
            {
                ServicePointCircle.Fill = new SolidColorBrush(DisconnectedColor);
                ConnectorPath.Opacity = 0;
                PulseCircle.Opacity = 0;
            }
        }
    }
}
