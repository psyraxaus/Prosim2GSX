using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

namespace Prosim2GSX.ViewModels.Base
{
    /// <summary>
    /// Base class for all ViewModels that implements INotifyPropertyChanged
    /// to support data binding with the UI
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        /// <summary>
        /// Event that is raised when a property on this object has a new value
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event for the specified property name
        /// </summary>
        /// <param name="propertyName">Name of the property that changed. 
        /// If not provided, the calling member name will be used.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                // Ensure property change notifications happen on the UI thread
                if (Application.Current.Dispatcher.CheckAccess())
                {
                    handler(this, new PropertyChangedEventArgs(propertyName));
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        handler(this, new PropertyChangedEventArgs(propertyName));
                    });
                }
            }
        }

        /// <summary>
        /// Sets the field to the value if they are not equal and raises the PropertyChanged event
        /// </summary>
        /// <typeparam name="T">Type of the property</typeparam>
        /// <param name="field">Reference to the backing field</param>
        /// <param name="value">New value for the property</param>
        /// <param name="propertyName">Name of the property. If not provided, the calling member name will be used.</param>
        /// <returns>True if the value was changed, false if the existing value matched the new value</returns>
        protected virtual bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            // Check if the value has changed
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            // Update the field with the new value
            field = value;

            // Notify that the property has changed
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Executes an action on the UI thread
        /// </summary>
        /// <param name="action">The action to execute</param>
        protected void ExecuteOnUIThread(Action action)
        {
            if (action == null) return;

            if (Application.Current?.Dispatcher?.CheckAccess() == true)
            {
                action();
            }
            else
            {
                Application.Current?.Dispatcher?.Invoke(action);
            }
        }
    }
}
