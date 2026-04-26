using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Prosim2GSX.UI
{
    public enum KorryArrow
    {
        TailLeft,
        Straight,
        TailRight,
    }

    public partial class KorryPushbackButton : UserControl
    {
        public static readonly DependencyProperty ArrowProperty =
            DependencyProperty.Register(nameof(Arrow), typeof(KorryArrow), typeof(KorryPushbackButton),
                new FrameworkPropertyMetadata(KorryArrow.Straight, OnVisualPropertyChanged));

        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(KorryPushbackButton),
                new FrameworkPropertyMetadata(false, OnVisualPropertyChanged));

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(KorryPushbackButton),
                new FrameworkPropertyMetadata(string.Empty, OnLabelChanged));

        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(KorryPushbackButton),
                new FrameworkPropertyMetadata(string.Empty, OnSubtitleChanged));

        public static readonly RoutedEvent ActivatedEvent =
            EventManager.RegisterRoutedEvent(nameof(Activated), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(KorryPushbackButton));

        public KorryArrow Arrow
        {
            get => (KorryArrow)GetValue(ArrowProperty);
            set => SetValue(ArrowProperty, value);
        }

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public string Subtitle
        {
            get => (string)GetValue(SubtitleProperty);
            set => SetValue(SubtitleProperty, value);
        }

        public event RoutedEventHandler Activated
        {
            add => AddHandler(ActivatedEvent, value);
            remove => RemoveHandler(ActivatedEvent, value);
        }

        public KorryPushbackButton()
        {
            InitializeComponent();
            Loaded += (_, _) => ApplyVisualState();
        }

        private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is KorryPushbackButton btn)
                btn.ApplyVisualState();
        }

        private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is KorryPushbackButton btn)
                btn.LegendText.Text = (e.NewValue as string ?? string.Empty).ToUpperInvariant();
        }

        private static void OnSubtitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is KorryPushbackButton btn)
                btn.SubtitleText.Text = (e.NewValue as string ?? string.Empty).ToUpperInvariant();
        }

        private void ApplyVisualState()
        {
            ArrowTailLeft.Visibility = Arrow == KorryArrow.TailLeft ? Visibility.Visible : Visibility.Collapsed;
            ArrowStraight.Visibility = Arrow == KorryArrow.Straight ? Visibility.Visible : Visibility.Collapsed;
            ArrowTailRight.Visibility = Arrow == KorryArrow.TailRight ? Visibility.Visible : Visibility.Collapsed;

            ButtonBody.BorderBrush = (Brush)(IsActive
                ? FindResource("KorryBodyBorderActive")
                : FindResource("KorryBodyBorder"));

            bool isStraight = Arrow == KorryArrow.Straight;
            IconZone.Background = (Brush)(IsActive
                ? FindResource(isStraight ? "KorryIconZoneActiveAuto" : "KorryIconZoneActiveGreen")
                : FindResource("KorryBodyBackground"));

            ApplyArrowColors();
            ApplyLegendVisual();
        }

        private void ApplyArrowColors()
        {
            switch (Arrow)
            {
                case KorryArrow.TailLeft:
                    {
                        var brush = (Brush)FindResource(IsActive ? "KorryArrowGreenActive" : "KorryArrowInactive");
                        TailLeftCurve.Stroke = brush;
                        TailLeftHead.Fill = brush;
                        break;
                    }
                case KorryArrow.TailRight:
                    {
                        var brush = (Brush)FindResource(IsActive ? "KorryArrowGreenActive" : "KorryArrowInactive");
                        TailRightCurve.Stroke = brush;
                        TailRightHead.Fill = brush;
                        break;
                    }
                case KorryArrow.Straight:
                    {
                        var arrowBrush = (Brush)FindResource(IsActive ? "KorryAutoActive" : "KorryAutoInactive");
                        StraightStem.Stroke = arrowBrush;
                        StraightHead.Fill = arrowBrush;
                        break;
                    }
            }
        }

        private void ApplyLegendVisual()
        {
            if (!IsActive)
            {
                LegendBox.Background = Brushes.Transparent;
                LegendBox.BorderThickness = new Thickness(0);
                LegendText.Foreground = (Brush)FindResource("KorryLegendInactiveText");
                LegendBox.Effect = null;
                return;
            }

            bool isStraight = Arrow == KorryArrow.Straight;
            var bgKey = isStraight ? "KorryAmberLegendBackground" : "KorryGreenLegendBackground";
            var textKey = isStraight ? "KorryAmberLegendText" : "KorryGreenLegendText";
            var glowColor = isStraight
                ? Color.FromRgb(0xFF, 0xC2, 0x00)
                : Color.FromRgb(0x1A, 0x8F, 0xFF);

            LegendBox.Background = (Brush)FindResource(bgKey);
            LegendBox.BorderThickness = new Thickness(1.5);
            LegendBox.BorderBrush = (Brush)FindResource(textKey);
            LegendText.Foreground = (Brush)FindResource(textKey);
            LegendBox.Effect = new DropShadowEffect
            {
                Color = glowColor,
                BlurRadius = 8,
                ShadowDepth = 0,
                Opacity = 0.7,
            };
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            PressTransform.Y = 1;
            e.Handled = true;
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            PressTransform.Y = 0;
            if (IsMouseOver)
            {
                IsActive = true;
                RaiseEvent(new RoutedEventArgs(ActivatedEvent, this));
            }
            e.Handled = true;
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            PressTransform.Y = 0;
        }
    }
}
