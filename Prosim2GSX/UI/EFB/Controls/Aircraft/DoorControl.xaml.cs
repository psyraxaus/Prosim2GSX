using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Prosim2GSX.UI.EFB.Controls.Aircraft
{
    /// <summary>
    /// Interaction logic for DoorControl.xaml
    /// </summary>
    public partial class DoorControl : UserControl
    {
        private Storyboard _openDoorAnimation;
        private Storyboard _closeDoorAnimation;

        #region Dependency Properties

        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register("IsOpen", typeof(bool), typeof(DoorControl),
                new PropertyMetadata(false, OnIsOpenChanged));

        public bool IsOpen
        {
            get { return (bool)GetValue(IsOpenProperty); }
            set { SetValue(IsOpenProperty, value); }
        }

        public static readonly DependencyProperty DoorNameProperty =
            DependencyProperty.Register("DoorName", typeof(string), typeof(DoorControl),
                new PropertyMetadata("Door"));

        public string DoorName
        {
            get { return (string)GetValue(DoorNameProperty); }
            set { SetValue(DoorNameProperty, value); }
        }

        public static readonly DependencyProperty DoorTypeProperty =
            DependencyProperty.Register("DoorType", typeof(string), typeof(DoorControl),
                new PropertyMetadata(""));

        public string DoorType
        {
            get { return (string)GetValue(DoorTypeProperty); }
            set { SetValue(DoorTypeProperty, value); }
        }

        public static readonly DependencyProperty DoorCommandProperty =
            DependencyProperty.Register("DoorCommand", typeof(ICommand), typeof(DoorControl),
                new PropertyMetadata(null));

        public ICommand DoorCommand
        {
            get { return (ICommand)GetValue(DoorCommandProperty); }
            set { SetValue(DoorCommandProperty, value); }
        }

        public static readonly DependencyProperty DoorOrientationProperty =
            DependencyProperty.Register("DoorOrientation", typeof(DoorOrientation), typeof(DoorControl),
                new PropertyMetadata(DoorOrientation.Left, OnDoorOrientationChanged));

        public DoorOrientation DoorOrientation
        {
            get { return (DoorOrientation)GetValue(DoorOrientationProperty); }
            set { SetValue(DoorOrientationProperty, value); }
        }

        public static readonly DependencyProperty IsHighlightedProperty =
            DependencyProperty.Register("IsHighlighted", typeof(bool), typeof(DoorControl),
                new PropertyMetadata(false, OnIsHighlightedChanged));

        public bool IsHighlighted
        {
            get { return (bool)GetValue(IsHighlightedProperty); }
            set { SetValue(IsHighlightedProperty, value); }
        }

        #endregion

        public DoorControl()
        {
            InitializeComponent();
            
            // Get animations from resources
            _openDoorAnimation = (Storyboard)FindResource("OpenDoorAnimation");
            _closeDoorAnimation = (Storyboard)FindResource("CloseDoorAnimation");

            // Add event handlers
            this.MouseEnter += DoorControl_MouseEnter;
            this.MouseLeave += DoorControl_MouseLeave;
            this.MouseLeftButtonDown += DoorControl_MouseLeftButtonDown;
        }

        #region Event Handlers

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (DoorControl)d;
            bool isOpen = (bool)e.NewValue;

            if (isOpen)
            {
                control._openDoorAnimation.Begin();
            }
            else
            {
                control._closeDoorAnimation.Begin();
            }
        }

        private static void OnDoorOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (DoorControl)d;
            var orientation = (DoorOrientation)e.NewValue;

            // Update the door's render transform origin based on orientation
            switch (orientation)
            {
                case DoorOrientation.Left:
                    control.DoorPath.RenderTransformOrigin = new Point(0, 0.5);
                    break;
                case DoorOrientation.Right:
                    control.DoorPath.RenderTransformOrigin = new Point(1, 0.5);
                    break;
                case DoorOrientation.Top:
                    control.DoorPath.RenderTransformOrigin = new Point(0.5, 0);
                    break;
                case DoorOrientation.Bottom:
                    control.DoorPath.RenderTransformOrigin = new Point(0.5, 1);
                    break;
            }
        }

        private static void OnIsHighlightedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (DoorControl)d;
            bool isHighlighted = (bool)e.NewValue;

            // Update the highlight overlay opacity
            control.HighlightOverlay.Opacity = isHighlighted ? 0.3 : 0;
        }

        private void DoorControl_MouseEnter(object sender, MouseEventArgs e)
        {
            // Show highlight effect
            HighlightOverlay.Opacity = 0.3;
        }

        private void DoorControl_MouseLeave(object sender, MouseEventArgs e)
        {
            // Hide highlight effect if not highlighted
            if (!IsHighlighted)
            {
                HighlightOverlay.Opacity = 0;
            }
        }

        private void DoorControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Execute the door command if available
            if (DoorCommand != null && DoorCommand.CanExecute(DoorType))
            {
                DoorCommand.Execute(DoorType);
            }
        }

        #endregion
    }

    /// <summary>
    /// Defines the orientation of the door
    /// </summary>
    public enum DoorOrientation
    {
        Left,
        Right,
        Top,
        Bottom
    }
}
