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

        // Character sets for cycling — letters and digits on separate drums like a real Solari
        private static readonly string LetterDrum = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private static readonly string DigitDrum = "0123456789";

        private char _currentChar = '-';
        private char _targetChar = '-';
        private int _drumIndex = 0;
        private string _activeDrum = null;
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
                Interval = TimeSpan.FromMilliseconds(50)
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
            char target = char.ToUpper(_targetChar);

            // Determine which drum to use
            if (char.IsLetter(target) && char.IsLetter(_currentChar))
            {
                _activeDrum = LetterDrum;
                _drumIndex = LetterDrum.IndexOf(char.ToUpper(_currentChar));
                if (_drumIndex < 0) _drumIndex = 0;
            }
            else if (char.IsDigit(target) && char.IsDigit(_currentChar))
            {
                _activeDrum = DigitDrum;
                _drumIndex = DigitDrum.IndexOf(_currentChar);
                if (_drumIndex < 0) _drumIndex = 0;
            }
            else
            {
                // Different type or special char — snap directly
                _currentChar = target;
                UpdateDisplay(_currentChar);
                return;
            }

            // If already at target, nothing to do
            if (_currentChar == target)
                return;

            _flipTimer.Start();
        }

        private void OnFlipTick(object sender, EventArgs e)
        {
            if (_activeDrum == null)
            {
                _flipTimer.Stop();
                return;
            }

            // Advance one position on the drum
            _drumIndex = (_drumIndex + 1) % _activeDrum.Length;
            _currentChar = _activeDrum[_drumIndex];

            // Brief opacity dip to simulate flap snap
            CharDisplay.Opacity = 0.7;
            UpdateDisplay(_currentChar);

            // Restore opacity on next render
            Dispatcher.BeginInvoke(DispatcherPriority.Render, () =>
            {
                CharDisplay.Opacity = 1.0;
            });

            // Check if we've landed on the target
            if (_currentChar == char.ToUpper(_targetChar))
            {
                _flipTimer.Stop();
                _activeDrum = null;
                CharDisplay.Foreground = BrushAmber;
            }
        }

        private void UpdateDisplay(char c)
        {
            CharDisplay.Text = c.ToString();
            // While flipping, use slightly dimmer colour
            if (IsFlipping)
                CharDisplay.Foreground = BrushDimAmber;
            else
                CharDisplay.Foreground = BrushAmber;
        }
    }
}
