using Prosim2GSX.ViewModels.Components;
using System;
using System.Collections.Generic;

namespace Prosim2GSX.ViewModels
{
    /// <summary>
    /// Provides access to view model instances throughout the application
    /// </summary>
    public class ViewModelLocator
    {
        private static ViewModelLocator _instance;
        private readonly Dictionary<Type, object> _viewModels = new Dictionary<Type, object>();

        /// <summary>
        /// Gets the singleton instance of the ViewModelLocator
        /// </summary>
        public static ViewModelLocator Instance => _instance ?? (_instance = new ViewModelLocator());

        /// <summary>
        /// Gets or creates a view model of the specified type
        /// </summary>
        /// <typeparam name="T">The type of view model to get</typeparam>
        /// <returns>An instance of the requested view model</returns>
        public T GetViewModel<T>() where T : class
        {
            var type = typeof(T);
            if (!_viewModels.ContainsKey(type))
            {
                throw new InvalidOperationException($"ViewModel of type {type.Name} has not been registered.");
            }

            return (T)_viewModels[type];
        }

        /// <summary>
        /// Registers a view model instance
        /// </summary>
        /// <typeparam name="T">The type of view model being registered</typeparam>
        /// <param name="viewModel">The view model instance</param>
        public void RegisterViewModel<T>(T viewModel, bool registerChildViewModels = true) where T : class
        {
            _viewModels[typeof(T)] = viewModel;

            // Register child ViewModels if requested
            if (registerChildViewModels)
            {
                // If this is a MainViewModel, register its child ViewModels
                if (viewModel is MainViewModel mainViewModel)
                {
                    RegisterViewModel<ConnectionStatusViewModel>(mainViewModel.ConnectionStatus, false);
                    RegisterViewModel<LogMessagesViewModel>(mainViewModel.LogMessages, false);
                }
            }
        }


        /// <summary>
        /// Removes a view model instance
        /// </summary>
        /// <typeparam name="T">The type of view model to remove</typeparam>
        public void UnregisterViewModel<T>() where T : class
        {
            if (_viewModels.ContainsKey(typeof(T)))
            {
                _viewModels.Remove(typeof(T));
            }
        }
    }
}
