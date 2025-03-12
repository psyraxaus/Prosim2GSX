using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Prosim2GSX.UI.EFB.ViewModels
{
    /// <summary>
    /// Base view model class for all EFB UI view models.
    /// Provides common functionality for property change notification,
    /// initialization, cleanup, and throttled property updates.
    /// </summary>
    public class BaseViewModel : ObservableObject
    {
        private bool _isInitialized;
        private bool _isBusy;
        private string _statusMessage;
        private readonly Dictionary<string, CancellationTokenSource> _throttleCancellationTokens = new();

        /// <summary>
        /// Gets or sets a value indicating whether this view model is busy.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        /// <summary>
        /// Gets or sets the status message for this view model.
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Gets a value indicating whether this view model has been initialized.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Initializes this view model.
        /// </summary>
        public virtual void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
        }

        /// <summary>
        /// Cleans up this view model.
        /// </summary>
        public virtual void Cleanup()
        {
            // Cancel all throttled operations
            foreach (var cts in _throttleCancellationTokens.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }

            _throttleCancellationTokens.Clear();
            _isInitialized = false;
        }

        /// <summary>
        /// Updates a property with throttling to prevent rapid updates.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="field">The field to update.</param>
        /// <param name="value">The new value.</param>
        /// <param name="throttleInterval">The throttle interval.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>True if the property was updated, false otherwise.</returns>
        protected async Task<bool> UpdatePropertyThrottledAsync<T>(
            ref T field,
            T value,
            TimeSpan throttleInterval,
            [CallerMemberName] string propertyName = null)
        {
            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            // If the value hasn't changed, don't update
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            // Cancel any existing throttle operation for this property
            if (_throttleCancellationTokens.TryGetValue(propertyName, out var existingCts))
            {
                existingCts.Cancel();
                existingCts.Dispose();
            }

            // Create a new cancellation token source for this throttle operation
            var cts = new CancellationTokenSource();
            _throttleCancellationTokens[propertyName] = cts;

            try
            {
                // Wait for the throttle interval
                await Task.Delay(throttleInterval, cts.Token);

                // Update the property
                return SetProperty(ref field, value, propertyName);
            }
            catch (TaskCanceledException)
            {
                // The operation was cancelled, so don't update the property
                return false;
            }
            finally
            {
                // Remove the cancellation token source from the dictionary
                if (_throttleCancellationTokens.TryGetValue(propertyName, out var currentCts) && currentCts == cts)
                {
                    _throttleCancellationTokens.Remove(propertyName);
                }

                cts.Dispose();
            }
        }

        /// <summary>
        /// Updates a property with throttling to prevent rapid updates.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="field">The field to update.</param>
        /// <param name="value">The new value.</param>
        /// <param name="throttleInterval">The throttle interval.</param>
        /// <param name="propertyName">The name of the property.</param>
        protected void UpdatePropertyThrottled<T>(
            ref T field,
            T value,
            TimeSpan throttleInterval,
            [CallerMemberName] string propertyName = null)
        {
            _ = UpdatePropertyThrottledAsync(ref field, value, throttleInterval, propertyName);
        }
    }
}
