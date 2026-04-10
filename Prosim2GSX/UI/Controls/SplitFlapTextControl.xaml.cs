using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Prosim2GSX.UI
{
    public partial class SplitFlapTextControl : UserControl
    {
        private SplitFlapCharacterControl[] _cells;

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(SplitFlapTextControl),
                new PropertyMetadata("", OnTextChanged));

        public static readonly DependencyProperty CharacterCountProperty =
            DependencyProperty.Register(nameof(CharacterCount), typeof(int), typeof(SplitFlapTextControl),
                new PropertyMetadata(8, OnCharacterCountChanged));

        public static readonly DependencyProperty StaggerDelayMsProperty =
            DependencyProperty.Register(nameof(StaggerDelayMs), typeof(int), typeof(SplitFlapTextControl),
                new PropertyMetadata(80));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public int CharacterCount
        {
            get => (int)GetValue(CharacterCountProperty);
            set => SetValue(CharacterCountProperty, value);
        }

        public int StaggerDelayMs
        {
            get => (int)GetValue(StaggerDelayMsProperty);
            set => SetValue(StaggerDelayMsProperty, value);
        }

        public SplitFlapTextControl()
        {
            InitializeComponent();
            BuildCells();
        }

        private static void OnCharacterCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SplitFlapTextControl control)
                control.BuildCells();
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SplitFlapTextControl control)
                control.UpdateText((string)e.NewValue);
        }

        private void BuildCells()
        {
            CharacterPanel.Children.Clear();
            _cells = new SplitFlapCharacterControl[CharacterCount];

            for (int i = 0; i < CharacterCount; i++)
            {
                _cells[i] = new SplitFlapCharacterControl();
                CharacterPanel.Children.Add(_cells[i]);
            }

            // Apply current text if any
            if (!string.IsNullOrEmpty(Text))
                UpdateText(Text);
        }

        private void UpdateText(string newText)
        {
            if (_cells == null) return;

            string padded = (newText ?? "").PadRight(CharacterCount);
            int staggerMs = StaggerDelayMs;

            for (int i = 0; i < _cells.Length; i++)
            {
                char targetChar = i < padded.Length ? padded[i] : ' ';
                int delay = i * staggerMs;

                if (delay == 0)
                {
                    _cells[i].TargetCharacter = targetChar;
                }
                else
                {
                    int cellIndex = i;
                    var timer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(delay)
                    };
                    timer.Tick += (s, e) =>
                    {
                        timer.Stop();
                        _cells[cellIndex].TargetCharacter = targetChar;
                    };
                    timer.Start();
                }
            }
        }
    }
}
