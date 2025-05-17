using Microsoft.Extensions.Logging;
using Prosim2GSX.Models;
using Prosim2GSX.Services;
using Prosim2GSX.Services.Logging.Interfaces;
using Prosim2GSX.ViewModels;
using System;
using System.Windows;

namespace Prosim2GSX
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// The notify icon view model for managing tray icon behavior
        /// </summary>
        protected NotifyIconViewModel notifyModel;

        /// <summary>
        /// The service model that maintains application state
        /// </summary>
        protected ServiceModel serviceModel;

        /// <summary>
        /// The logger factory for creating loggers
        /// </summary>
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// The main view model that coordinates all component view models
        /// </summary>
        private MainViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the MainWindow class
        /// </summary>
        /// <param name="notifyModel">The notify icon view model</param>
        /// <param name="serviceModel">The service model for application state</param>
        /// <param name="loggerFactory">The factory for creating loggers</param>
        public MainWindow(NotifyIconViewModel notifyModel, ServiceModel serviceModel, ILoggerFactory loggerFactory)
        {
            InitializeComponent();

            this.notifyModel = notifyModel;
            this.serviceModel = serviceModel;
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

            // Get the UI log listener from ServiceLocator
            var logListener = ServiceLocator.GetService<IUiLogListener>();
            if (logListener == null)
            {
                throw new InvalidOperationException("IUiLogListener service is not available. Please ensure it is registered in the ServiceLocator.");
            }

            // Create the ViewModel with all required parameters
            _viewModel = new MainViewModel(
                serviceModel,
                notifyModel,
                _loggerFactory.CreateLogger<MainViewModel>(),
                _loggerFactory,
                logListener);

            ViewModelLocator.Instance.RegisterViewModel(_viewModel);

            // Set the DataContext for data binding
            DataContext = _viewModel;
        }

        /// <summary>
        /// Handles the window closing event by hiding instead of actually closing
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The event arguments</param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();

            // Clean up ViewModel resources
            _viewModel.Cleanup();
        }

        /// <summary>
        /// Handles the window visibility changed event
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The event arguments</param>
        protected void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible)
            {
                notifyModel.CanExecuteHideWindow = false;
                notifyModel.CanExecuteShowWindow = true;

                // Notify ViewModel that window is hidden
                _viewModel.OnWindowHidden();
            }
            else
            {
                // Notify ViewModel that window is visible
                _viewModel.OnWindowVisible();
            }
        }
    }
}
