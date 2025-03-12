using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Prosim2GSX.Services;

namespace Prosim2GSX.UI.EFB.ViewModels
{
    /// <summary>
    /// Service for binding data between the EFB UI and the ServiceModel.
    /// </summary>
    public class EFBDataBindingService
    {
        private readonly object _serviceModel;
        private readonly Dictionary<string, PropertyInfo> _propertyCache = new Dictionary<string, PropertyInfo>();
        private readonly Dictionary<string, List<Action<object>>> _propertyChangedCallbacks = new Dictionary<string, List<Action<object>>>();
        private readonly Dispatcher _dispatcher;
        private readonly Timer _pollingTimer;
        private readonly Dictionary<string, object> _lastValues = new Dictionary<string, object>();
        private readonly int _pollingIntervalMilliseconds;
        private readonly ILogger _logger;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="EFBDataBindingService"/> class.
        /// </summary>
        /// <param name="serviceModel">The service model to bind to.</param>
        /// <param name="pollingIntervalMilliseconds">The polling interval in milliseconds.</param>
        /// <param name="logger">Optional logger instance.</param>
        public EFBDataBindingService(object serviceModel, int pollingIntervalMilliseconds = 500, ILogger logger = null)
        {
            _serviceModel = serviceModel ?? throw new ArgumentNullException(nameof(serviceModel));
            _dispatcher = Dispatcher.CurrentDispatcher;
            _pollingIntervalMilliseconds = pollingIntervalMilliseconds;
            _logger = logger;
            
            _logger?.Log(LogLevel.Debug, "EFBDataBindingService:Constructor", 
                $"Initializing data binding service with polling interval: {pollingIntervalMilliseconds}ms");
            
            // Start polling timer
            _pollingTimer = new Timer(PollProperties, null, _pollingIntervalMilliseconds, _pollingIntervalMilliseconds);
            
            // Subscribe to property changed events if the service model implements INotifyPropertyChanged
            if (_serviceModel is INotifyPropertyChanged notifyPropertyChanged)
            {
                notifyPropertyChanged.PropertyChanged += OnServiceModelPropertyChanged;
                _logger?.Log(LogLevel.Debug, "EFBDataBindingService:Constructor", 
                    "Service model implements INotifyPropertyChanged, subscribing to events");
            }
            else
            {
                _logger?.Log(LogLevel.Debug, "EFBDataBindingService:Constructor", 
                    "Service model does not implement INotifyPropertyChanged, using polling");
            }
        }
        
        /// <summary>
        /// Gets a property value from the service model.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>The property value.</returns>
        public T GetValue<T>(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }
            
            var property = GetPropertyInfo(propertyName);
            if (property == null)
            {
                var errorMessage = $"Property '{propertyName}' not found on service model.";
                _logger?.Log(LogLevel.Error, "EFBDataBindingService:GetValue", errorMessage);
                throw new ArgumentException(errorMessage, nameof(propertyName));
            }
            
            return (T)property.GetValue(_serviceModel);
        }
        
        /// <summary>
        /// Sets a property value on the service model.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The property value.</param>
        public void SetValue<T>(string propertyName, T value)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }
            
            var property = GetPropertyInfo(propertyName);
            if (property == null)
            {
                var errorMessage = $"Property '{propertyName}' not found on service model.";
                _logger?.Log(LogLevel.Error, "EFBDataBindingService:SetValue", errorMessage);
                throw new ArgumentException(errorMessage, nameof(propertyName));
            }
            
            if (!property.CanWrite)
            {
                var errorMessage = $"Property '{propertyName}' is read-only.";
                _logger?.Log(LogLevel.Error, "EFBDataBindingService:SetValue", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
            
            property.SetValue(_serviceModel, value);
        }
        
        /// <summary>
        /// Subscribes to property changes.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="callback">The callback to invoke when the property changes.</param>
        public void Subscribe(string propertyName, Action<object> callback)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }
            
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }
            
            if (!_propertyChangedCallbacks.TryGetValue(propertyName, out var callbacks))
            {
                callbacks = new List<Action<object>>();
                _propertyChangedCallbacks[propertyName] = callbacks;
            }
            
            callbacks.Add(callback);
            _logger?.Log(LogLevel.Debug, "EFBDataBindingService:Subscribe", 
                $"Subscribed to property '{propertyName}'");
            
            // Initialize with current value
            var property = GetPropertyInfo(propertyName);
            if (property != null)
            {
                var value = property.GetValue(_serviceModel);
                _lastValues[propertyName] = value;
                callback(value);
                _logger?.Log(LogLevel.Debug, "EFBDataBindingService:Subscribe", 
                    $"Initialized property '{propertyName}' with current value");
            }
        }
        
        /// <summary>
        /// Unsubscribes from property changes.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="callback">The callback to remove.</param>
        public void Unsubscribe(string propertyName, Action<object> callback)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }
            
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }
            
            if (_propertyChangedCallbacks.TryGetValue(propertyName, out var callbacks))
            {
                callbacks.Remove(callback);
                _logger?.Log(LogLevel.Debug, "EFBDataBindingService:Unsubscribe", 
                    $"Unsubscribed from property '{propertyName}'");
                
                if (callbacks.Count == 0)
                {
                    _propertyChangedCallbacks.Remove(propertyName);
                    _logger?.Log(LogLevel.Debug, "EFBDataBindingService:Unsubscribe", 
                        $"Removed all callbacks for property '{propertyName}'");
                }
            }
        }
        
        /// <summary>
        /// Cleans up the service.
        /// </summary>
        public void Cleanup()
        {
            _logger?.Log(LogLevel.Debug, "EFBDataBindingService:Cleanup", "Cleaning up data binding service");
            
            _pollingTimer?.Dispose();
            
            if (_serviceModel is INotifyPropertyChanged notifyPropertyChanged)
            {
                notifyPropertyChanged.PropertyChanged -= OnServiceModelPropertyChanged;
                _logger?.Log(LogLevel.Debug, "EFBDataBindingService:Cleanup", 
                    "Unsubscribed from service model property changed events");
            }
            
            _propertyChangedCallbacks.Clear();
            _lastValues.Clear();
            _propertyCache.Clear();
            
            _logger?.Log(LogLevel.Debug, "EFBDataBindingService:Cleanup", "Data binding service cleanup completed");
        }
        
        private PropertyInfo GetPropertyInfo(string propertyName)
        {
            if (_propertyCache.TryGetValue(propertyName, out var property))
            {
                return property;
            }
            
            property = _serviceModel.GetType().GetProperty(propertyName);
            _propertyCache[propertyName] = property;
            
            return property;
        }
        
        private void OnServiceModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName))
            {
                return;
            }
            
            if (_propertyChangedCallbacks.TryGetValue(e.PropertyName, out var callbacks))
            {
                var property = GetPropertyInfo(e.PropertyName);
                if (property != null)
                {
                    var value = property.GetValue(_serviceModel);
                    
                    _dispatcher.Invoke(() =>
                    {
                        foreach (var callback in callbacks)
                        {
                            callback(value);
                        }
                    });
                }
            }
        }
        
        private void PollProperties(object state)
        {
            if (_serviceModel is INotifyPropertyChanged)
            {
                // No need to poll if the service model implements INotifyPropertyChanged
                return;
            }
            
            foreach (var propertyName in _propertyChangedCallbacks.Keys)
            {
                var property = GetPropertyInfo(propertyName);
                if (property != null)
                {
                    var value = property.GetValue(_serviceModel);
                    
                    if (_lastValues.TryGetValue(propertyName, out var lastValue))
                    {
                        if (!Equals(value, lastValue))
                        {
                            _lastValues[propertyName] = value;
                            
                            if (_propertyChangedCallbacks.TryGetValue(propertyName, out var callbacks))
                            {
                                _dispatcher.Invoke(() =>
                                {
                                    foreach (var callback in callbacks)
                                    {
                                        callback(value);
                                    }
                                });
                            }
                        }
                    }
                    else
                    {
                        _lastValues[propertyName] = value;
                    }
                }
            }
        }
    }
}
