using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Prosim2GSX.UI.EFB.ViewModels
{
    /// <summary>
    /// Base class for view models.
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        private readonly Dictionary<string, object> _propertyValues = new Dictionary<string, object>();
        private readonly Dictionary<string, CancellationTokenSource> _throttleCancellationTokens = new Dictionary<string, CancellationTokenSource>();
        private readonly Dispatcher _dispatcher;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseViewModel"/> class.
        /// </summary>
        protected BaseViewModel()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
        }
        
        /// <summary>
        /// Event raised when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        
        /// <summary>
        /// Gets or sets a property value.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>The property value.</returns>
        protected T GetProperty<T>([CallerMemberName] string propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }
            
            if (_propertyValues.TryGetValue(propertyName, out var value))
            {
                return (T)value;
            }
            
            return default;
        }
        
        /// <summary>
        /// Sets a property value.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="value">The property value.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>True if the property value changed, false otherwise.</returns>
        protected bool SetProperty<T>(T value, [CallerMemberName] string propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }
            
            if (_propertyValues.TryGetValue(propertyName, out var existingValue))
            {
                if (EqualityComparer<T>.Default.Equals((T)existingValue, value))
                {
                    return false;
                }
            }
            
            _propertyValues[propertyName] = value;
            OnPropertyChanged(propertyName);
            
            return true;
        }
        
        /// <summary>
        /// Sets a property value with throttling.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="value">The property value.</param>
        /// <param name="throttleMilliseconds">The throttle time in milliseconds.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>True if the property value changed, false otherwise.</returns>
        protected bool SetPropertyThrottled<T>(T value, int throttleMilliseconds, [CallerMemberName] string propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }
            
            if (_propertyValues.TryGetValue(propertyName, out var existingValue))
            {
                if (EqualityComparer<T>.Default.Equals((T)existingValue, value))
                {
                    return false;
                }
            }
            
            _propertyValues[propertyName] = value;
            
            if (_throttleCancellationTokens.TryGetValue(propertyName, out var existingCts))
            {
                existingCts.Cancel();
                _throttleCancellationTokens.Remove(propertyName);
            }
            
            var cts = new CancellationTokenSource();
            _throttleCancellationTokens[propertyName] = cts;
            
            Task.Delay(throttleMilliseconds, cts.Token)
                .ContinueWith(t =>
                {
                    if (t.IsCanceled)
                    {
                        return;
                    }
                    
                    _dispatcher.Invoke(() =>
                    {
                        _throttleCancellationTokens.Remove(propertyName);
                        OnPropertyChanged(propertyName);
                    });
                }, TaskScheduler.Default);
            
            return true;
        }
        
        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        /// <summary>
        /// Initializes the view model.
        /// </summary>
        public virtual void Initialize()
        {
        }
        
        /// <summary>
        /// Cleans up the view model.
        /// </summary>
        public virtual void Cleanup()
        {
            foreach (var cts in _throttleCancellationTokens.Values)
            {
                cts.Cancel();
            }
            
            _throttleCancellationTokens.Clear();
        }
    }
}
