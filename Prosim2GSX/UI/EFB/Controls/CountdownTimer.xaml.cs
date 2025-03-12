using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Prosim2GSX.UI.EFB.Controls
{
    /// <summary>
    /// Interaction logic for CountdownTimer.xaml
    /// </summary>
    public partial class CountdownTimer : UserControl, INotifyPropertyChanged
    {
        private readonly DispatcherTimer _timer;
        private DateTime _endTime;
        private TimeSpan _duration;
        private TimeSpan _remainingTime;
        private bool _isRunning;
        
        /// <summary>
        /// Event raised when the timer completes.
        /// </summary>
        public event EventHandler TimerCompleted;
        
        /// <summary>
        /// Event raised when a property changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        
        #region Dependency Properties
        
        /// <summary>
        /// Dependency property for Label.
        /// </summary>
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string), typeof(CountdownTimer),
                new PropertyMetadata("Countdown"));
        
        /// <summary>
        /// Dependency property for Duration.
        /// </summary>
        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register("Duration", typeof(TimeSpan), typeof(CountdownTimer),
                new PropertyMetadata(TimeSpan.FromMinutes(5), OnDurationChanged));
        
        /// <summary>
        /// Dependency property for IsRunning.
        /// </summary>
        public static readonly DependencyProperty IsRunningProperty =
            DependencyProperty.Register("IsRunning", typeof(bool), typeof(CountdownTimer),
                new PropertyMetadata(false, OnIsRunningChanged));
        
        /// <summary>
        /// Dependency property for ShowProgress.
        /// </summary>
        public static readonly DependencyProperty ShowProgressProperty =
            DependencyProperty.Register("ShowProgress", typeof(bool), typeof(CountdownTimer),
                new PropertyMetadata(true, OnShowProgressChanged));
        
        /// <summary>
        /// Dependency property for TimeFormat.
        /// </summary>
        public static readonly DependencyProperty TimeFormatProperty =
            DependencyProperty.Register("TimeFormat", typeof(string), typeof(CountdownTimer),
                new PropertyMetadata("mm\\:ss", OnTimeFormatChanged));
        
        /// <summary>
        /// Gets or sets the label for the countdown timer.
        /// </summary>
        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }
        
        /// <summary>
        /// Gets or sets the duration of the countdown timer.
        /// </summary>
        public TimeSpan Duration
        {
            get { return (TimeSpan)GetValue(DurationProperty); }
            set { SetValue(DurationProperty, value); }
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether the countdown timer is running.
        /// </summary>
        public bool IsRunning
        {
            get { return (bool)GetValue(IsRunningProperty); }
            set { SetValue(IsRunningProperty, value); }
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether to show the progress bar.
        /// </summary>
        public bool ShowProgress
        {
            get { return (bool)GetValue(ShowProgressProperty); }
            set { SetValue(ShowProgressProperty, value); }
        }
        
        /// <summary>
        /// Gets or sets the time format for the countdown timer.
        /// </summary>
        public string TimeFormat
        {
            get { return (string)GetValue(TimeFormatProperty); }
            set { SetValue(TimeFormatProperty, value); }
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Gets the remaining time as a formatted string.
        /// </summary>
        public string FormattedTime
        {
            get { return _remainingTime.ToString(TimeFormat); }
        }
        
        /// <summary>
        /// Gets the progress percentage (0-100).
        /// </summary>
        public double Progress
        {
            get
            {
                if (_duration.TotalSeconds == 0)
                    return 0;
                    
                return 100 - (_remainingTime.TotalSeconds / _duration.TotalSeconds * 100);
            }
        }
        
        #endregion
        
        /// <summary>
        /// Initializes a new instance of the <see cref="CountdownTimer"/> class.
        /// </summary>
        public CountdownTimer()
        {
            InitializeComponent();
            
            // Initialize timer
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += OnTimerTick;
            
            // Initialize properties
            _duration = Duration;
            _remainingTime = _duration;
            
            // Update UI
            UpdateProgressBarVisibility();
        }
        
        /// <summary>
        /// Starts the countdown timer.
        /// </summary>
        public void Start()
        {
            IsRunning = true;
        }
        
        /// <summary>
        /// Stops the countdown timer.
        /// </summary>
        public void Stop()
        {
            IsRunning = false;
        }
        
        /// <summary>
        /// Resets the countdown timer.
        /// </summary>
        public void Reset()
        {
            _remainingTime = _duration;
            OnPropertyChanged(nameof(FormattedTime));
            OnPropertyChanged(nameof(Progress));
        }
        
        /// <summary>
        /// Sets the end time for the countdown timer.
        /// </summary>
        /// <param name="endTime">The end time.</param>
        public void SetEndTime(DateTime endTime)
        {
            _endTime = endTime;
            _duration = _endTime - DateTime.Now;
            _remainingTime = _duration;
            OnPropertyChanged(nameof(FormattedTime));
            OnPropertyChanged(nameof(Progress));
        }
        
        private static void OnDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var timer = (CountdownTimer)d;
            timer._duration = (TimeSpan)e.NewValue;
            timer._remainingTime = timer._duration;
            timer.OnPropertyChanged(nameof(FormattedTime));
            timer.OnPropertyChanged(nameof(Progress));
        }
        
        private static void OnIsRunningChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var timer = (CountdownTimer)d;
            timer._isRunning = (bool)e.NewValue;
            
            if (timer._isRunning)
            {
                // Start the timer
                timer._endTime = DateTime.Now.Add(timer._remainingTime);
                timer._timer.Start();
            }
            else
            {
                // Stop the timer
                timer._timer.Stop();
            }
        }
        
        private static void OnShowProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var timer = (CountdownTimer)d;
            timer.UpdateProgressBarVisibility();
        }
        
        private static void OnTimeFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var timer = (CountdownTimer)d;
            timer.OnPropertyChanged(nameof(FormattedTime));
        }
        
        private void OnTimerTick(object sender, EventArgs e)
        {
            // Calculate remaining time
            _remainingTime = _endTime - DateTime.Now;
            
            // Check if timer has completed
            if (_remainingTime.TotalSeconds <= 0)
            {
                _remainingTime = TimeSpan.Zero;
                _timer.Stop();
                IsRunning = false;
                OnTimerCompleted();
            }
            
            // Update UI
            OnPropertyChanged(nameof(FormattedTime));
            OnPropertyChanged(nameof(Progress));
        }
        
        private void UpdateProgressBarVisibility()
        {
            ProgressBar.Visibility = ShowProgress ? Visibility.Visible : Visibility.Collapsed;
        }
        
        private void OnTimerCompleted()
        {
            TimerCompleted?.Invoke(this, EventArgs.Empty);
        }
        
        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
