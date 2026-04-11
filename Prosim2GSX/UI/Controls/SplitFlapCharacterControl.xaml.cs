using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Prosim2GSX.UI
{
    public partial class SplitFlapCharacterControl : UserControl
    {
        private static readonly SolidColorBrush BrushAmber = new(Color.FromArgb(0xFF, 0xFF, 0xD7, 0x00));
        private static readonly SolidColorBrush BrushDimAmber = new(Color.FromArgb(0xFF, 0x8B, 0x69, 0x14));

        // Single unified drum — space, letters, digits, and punctuation
        private static readonly string UnifiedDrum = " ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789:-/";

        // Animation tuning
        private const int MinimumFlips = 5;
        private const double FastTickMs = 30;
        private const double SlowTickMs = 90;
        private const int DecelerationZone = 3;

        private char _currentChar = '-';
        private char _targetChar = '-';
        private int _drumIndex;
        private int _targetIndex;
        private int _remainingFlips;
        private int _totalFlips;
        private DispatcherTimer _flipTimer;

        public static readonly DependencyProperty TargetCharacterProperty =
            DependencyProperty.Register(nameof(TargetCharacter), typeof(char), typeof(SplitFlapCharacterControl),
                new PropertyMetadata('-', OnTargetCharacterChanged));

        public char TargetCharacter
        {
            get => (char)GetValue(TargetCharacterProperty);
            set => SetValue(TargetCharacterProperty, value);
        }

        public bool IsFlipping => _flipTimer?.IsEnabled == true;

        public SplitFlapCharacterControl()
        {
            InitializeComponent();

            _flipTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(FastTickMs)
            };
            _flipTimer.Tick += OnFlipTick;
        }

        private static void OnTargetCharacterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SplitFlapCharacterControl control)
            {
                control._targetChar = (char)e.NewValue;
                control.StartFlip();
            }
        }

        private void StartFlip()
        {
            _flipTimer.Stop();

            char target = char.ToUpper(_targetChar);

            // Check animation toggle — snap directly if disabled
            bool animationEnabled = AppService.Instance?.Config?.SolariAnimationEnabled ?? true;
            if (!animationEnabled)
            {
                _currentChar = target;
                UpdateDisplay(_currentChar);
                ResetFlipTransform();
                return;
            }

            int targetIdx = UnifiedDrum.IndexOf(target);
            if (targetIdx < 0)
            {
                // Unknown character — snap directly
                _currentChar = target;
                UpdateDisplay(_currentChar);
                ResetFlipTransform();
                return;
            }

            int currentIdx = UnifiedDrum.IndexOf(char.ToUpper(_currentChar));
            if (currentIdx < 0)
                currentIdx = 0;

            // Already at target — nothing to do
            if (char.ToUpper(_currentChar) == target)
                return;

            _drumIndex = currentIdx;
            _targetIndex = targetIdx;

            // Calculate forward distance on drum (wrapping)
            int forwardDistance = (_targetIndex - _drumIndex + UnifiedDrum.Length) % UnifiedDrum.Length;

            // Enforce minimum flip cycles — add a full revolution if too short
            if (forwardDistance < MinimumFlips)
                _remainingFlips = forwardDistance + UnifiedDrum.Length;
            else
                _remainingFlips = forwardDistance;

            _totalFlips = _remainingFlips;
            _flipTimer.Interval = TimeSpan.FromMilliseconds(FastTickMs);
            _flipTimer.Start();
        }

        private void OnFlipTick(object sender, EventArgs e)
        {
            // Advance one position on the drum
            _drumIndex = (_drumIndex + 1) % UnifiedDrum.Length;
            _currentChar = UnifiedDrum[_drumIndex];
            _remainingFlips--;

            // Apply visual flip effect
            ApplyFlipEffect();
            UpdateDisplay(_currentChar);

            if (_remainingFlips <= 0)
            {
                _flipTimer.Stop();
                ResetFlipTransform();
                return;
            }

            // Deceleration: slow down for the last few flips
            if (_remainingFlips <= DecelerationZone)
            {
                double t = 1.0 - ((double)_remainingFlips / DecelerationZone);
                double ms = FastTickMs + t * (SlowTickMs - FastTickMs);
                _flipTimer.Interval = TimeSpan.FromMilliseconds(ms);
            }
        }

        private void ApplyFlipEffect()
        {
            // Squeeze vertically to simulate flap folding
            FlipTransform.ScaleY = 0.3;
            CharDisplay.Foreground = BrushDimAmber;

            // Restore on next render pass — new character "drops" into place
            Dispatcher.BeginInvoke(DispatcherPriority.Render, () =>
            {
                FlipTransform.ScaleY = 1.0;
            });
        }

        private void ResetFlipTransform()
        {
            FlipTransform.ScaleY = 1.0;
            CharDisplay.Foreground = BrushAmber;
        }

        private void UpdateDisplay(char c)
        {
            CharDisplay.Text = c.ToString();
        }
    }
}
