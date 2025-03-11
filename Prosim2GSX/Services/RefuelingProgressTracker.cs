using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Tracks the progress of refueling operations
    /// </summary>
    public class RefuelingProgressTracker
    {
        private int _progressPercentage;
        private double _currentAmount;
        private double _targetAmount;
        private string _units;
        private readonly ILogger _logger;
        
        /// <summary>
        /// Event raised when the refueling progress changes
        /// </summary>
        public event EventHandler<RefuelingProgressChangedEventArgs> ProgressChanged;
        
        /// <summary>
        /// Gets the progress percentage (0-100)
        /// </summary>
        public int ProgressPercentage => _progressPercentage;
        
        /// <summary>
        /// Gets the current fuel amount
        /// </summary>
        public double CurrentAmount => _currentAmount;
        
        /// <summary>
        /// Gets the target fuel amount
        /// </summary>
        public double TargetAmount => _targetAmount;
        
        /// <summary>
        /// Gets the fuel units (KG or LBS)
        /// </summary>
        public string Units => _units;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="RefuelingProgressTracker"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        public RefuelingProgressTracker(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _progressPercentage = 0;
            _currentAmount = 0;
            _targetAmount = 0;
            _units = "KG";
        }
        
        /// <summary>
        /// Updates the refueling progress
        /// </summary>
        /// <param name="currentAmount">The current fuel amount</param>
        /// <param name="targetAmount">The target fuel amount</param>
        /// <param name="units">The fuel units (KG or LBS)</param>
        public void UpdateProgress(double currentAmount, double targetAmount, string units)
        {
            _currentAmount = currentAmount;
            _targetAmount = targetAmount;
            _units = units;
            
            // Calculate percentage
            int newPercentage = targetAmount > 0 
                ? (int)((currentAmount / targetAmount) * 100) 
                : 0;
            newPercentage = Math.Min(100, newPercentage);
            
            // Only raise event if percentage has changed
            if (newPercentage != _progressPercentage)
            {
                _progressPercentage = newPercentage;
                
                _logger.Log(LogLevel.Debug, "RefuelingProgressTracker:UpdateProgress", 
                    $"Progress updated to {_progressPercentage}% ({_currentAmount}/{_targetAmount} {_units})");
                
                OnProgressChanged(_progressPercentage, currentAmount, targetAmount);
            }
        }
        
        /// <summary>
        /// Resets the progress tracker
        /// </summary>
        public void Reset()
        {
            _progressPercentage = 0;
            _currentAmount = 0;
            _targetAmount = 0;
            
            _logger.Log(LogLevel.Debug, "RefuelingProgressTracker:Reset", "Progress tracker reset");
        }
        
        /// <summary>
        /// Raises the ProgressChanged event
        /// </summary>
        /// <param name="percentage">The progress percentage</param>
        /// <param name="current">The current fuel amount</param>
        /// <param name="target">The target fuel amount</param>
        protected virtual void OnProgressChanged(int percentage, double current, double target)
        {
            try
            {
                ProgressChanged?.Invoke(this, new RefuelingProgressChangedEventArgs(
                    percentage, current, target, _units));
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "RefuelingProgressTracker:OnProgressChanged", 
                    $"Error raising ProgressChanged event: {ex.Message}");
            }
        }
    }
}
